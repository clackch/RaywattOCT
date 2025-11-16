#include "pch.h"
#include <opencv2/cudaimgproc.hpp>
#include "yolov8.h"

YoloV8::YoloV8(const std::string& onnxModelPath, const YoloV8Config& config)
    : PROBABILITY_THRESHOLD(config.probabilityThreshold)
    , NMS_THRESHOLD(config.nmsThreshold)
    , TOP_K(config.topK)
    , SEG_CHANNELS(config.segChannels)
    , SEG_H(config.segH)
    , SEG_W(config.segW)
    , SEGMENT_THRESHOLD(config.segmentThreshold)
    , CLASS_NAMES(config.classNames) {
    // Specify options for GPU inference
    Options options;
    options.optBatchSize = 1;
    options.maxBatchSize = 1;

    options.precision = config.precision;
    options.calibrationDataDirectoryPath = config.calibrationDataDirectory;
    CLASS_THRESHOLDS = config.classThresholds;

    if (options.precision == Precision::INT8) {
        if (options.calibrationDataDirectoryPath.empty()) {
            throw std::runtime_error("Error: Must supply calibration data path for INT8 calibration");
        }
    }

    // Create our TensorRT inference engine
    m_trtEngine = std::make_unique<Engine>(options);

    // Build the onnx model into a TensorRT engine file
    // If the engine file already exists, this function will return immediately
    // The engine file is rebuilt any time the above Options are changed.
    auto succ = m_trtEngine->build(onnxModelPath, SUB_VALS, DIV_VALS, NORMALIZE);
    if (!succ) {
        const std::string errMsg = "Error: Unable to build the TensorRT engine. "
            "Try increasing TensorRT log severity to kVERBOSE (in /libs/tensorrt-cpp-api/engine.cpp).";
        throw std::runtime_error(errMsg);
    }

    // Load the TensorRT engine file
    succ = m_trtEngine->loadNetwork();
    if (!succ) {
        throw std::runtime_error("Error: Unable to load TensorRT engine weights into memory.");
    }
}

std::vector<std::vector<cv::cuda::GpuMat>> YoloV8::preprocess(const cv::cuda::GpuMat& gpuImg) {
    // Populate the input vectors
    const auto& inputDims = m_trtEngine->getInputDims();

    // Convert the image from BGR to RGB
    cv::cuda::GpuMat rgbMat;
    cv::cuda::cvtColor(gpuImg, rgbMat, cv::COLOR_BGR2RGB);

    auto resized = rgbMat;

    // Resize to the model expected input size while maintaining the aspect ratio with the use of padding
    if (resized.rows != inputDims[0].d[1] || resized.cols != inputDims[0].d[2]) {
        // Only resize if not already the right size to avoid unecessary copy
        resized = Engine::resizeKeepAspectRatioPadRightBottom(rgbMat, inputDims[0].d[1], inputDims[0].d[2]);
    }

    // Convert to format expected by our inference engine
    // The reason for the strange format is because it supports models with multiple inputs as well as batching
    // In our case though, the model only has a single input and we are using a batch size of 1.
    std::vector<cv::cuda::GpuMat> input{ std::move(resized) };
    std::vector<std::vector<cv::cuda::GpuMat>> inputs{ std::move(input) };

    // These params will be used in the post-processing stage
    m_imgHeight = rgbMat.rows;
    m_imgWidth = rgbMat.cols;
    m_ratio = 1.f / std::min(inputDims[0].d[2] / static_cast<float>(rgbMat.cols), inputDims[0].d[1] / static_cast<float>(rgbMat.rows));

    return inputs;
}

std::vector<Object> YoloV8::detectObjects(const cv::cuda::GpuMat& inputImageBGR) {
    // Preprocess the input image
    const auto input = preprocess(inputImageBGR);

    // Run inference using the TensorRT engine
    std::vector<std::vector<std::vector<float>>> featureVectors;
    auto succ = m_trtEngine->runInference(input, featureVectors);
    if (!succ) {
        throw std::runtime_error("Error: Unable to run inference.");
    }

    // Check if our model does only object detection or also supports segment
    std::vector<Object> ret;
    const auto& numOutputs = m_trtEngine->getOutputDims().size();

    if (numOutputs == 1) {
        // Object detection
        // Since we have a batch size of 1 and only 1 output, we must convert the output from a 3D array to a 1D array.
        std::vector<float> featureVector;
        Engine::transformOutput(featureVectors, featureVector);
        ret = postprocessDetect(featureVector);
    }
    else {
        // Segment
        // Since we have a batch size of 1 and 2 outputs, we must convert the output from a 3D array to a 2D array.
        std::vector<std::vector<float>> featureVector;
        Engine::transformOutput(featureVectors, featureVector);
        ret = postProcessSegment(featureVector);
    }

    return ret;
}

std::vector<Object> YoloV8::detectObjects(const cv::Mat& inputImageBGR) {
    // Upload the image to GPU memory
    cv::cuda::GpuMat gpuImg;
    gpuImg.upload(inputImageBGR);

    // Call detectObjects with the GPU image
    return detectObjects(gpuImg);
}

std::vector<Object> YoloV8::postProcessSegment(std::vector<std::vector<float>>& featureVectors) {
    const auto& outputDims = m_trtEngine->getOutputDims();

    int numChannels = outputDims[outputDims.size() - 1].d[1];
    int numAnchors = outputDims[outputDims.size() - 1].d[2];

    const auto numClasses = numChannels - SEG_CHANNELS - 4;

    // Ensure the output lengths are correct
    if (featureVectors[0].size() != static_cast<size_t>(SEG_CHANNELS) * SEG_H * SEG_W) {
        throw std::logic_error("Output at index 0 has incorrect length");
    }

    if (featureVectors[1].size() != static_cast<size_t>(numChannels) * numAnchors) {
        throw std::logic_error("Output at index 1 has incorrect length");
    }

    cv::Mat output = cv::Mat(numChannels, numAnchors, CV_32F, featureVectors[1].data());
    output = output.t();

    cv::Mat protos = cv::Mat(SEG_CHANNELS, SEG_H * SEG_W, CV_32F, featureVectors[0].data());

    std::vector<int> labels;
    std::vector<float> scores;
    std::vector<cv::Rect> bboxes;
    std::vector<cv::Mat> maskConfs;
    std::vector<int> indices;

    // Object the bounding boxes and class labels
    for (int i = 0; i < numAnchors; i++) {
        auto rowPtr = output.row(i).ptr<float>();
        auto bboxesPtr = rowPtr;
        auto scoresPtr = rowPtr + 4;
        auto maskConfsPtr = rowPtr + 4 + numClasses;
        auto maxSPtr = std::max_element(scoresPtr, scoresPtr + numClasses);
        float score = *maxSPtr;
        if (score > PROBABILITY_THRESHOLD) {
            float x = *bboxesPtr++;
            float y = *bboxesPtr++;
            float w = *bboxesPtr++;
            float h = *bboxesPtr;

            float x0 = std::clamp((x - 0.5f * w) * m_ratio, 0.f, m_imgWidth);
            float y0 = std::clamp((y - 0.5f * h) * m_ratio, 0.f, m_imgHeight);
            float x1 = std::clamp((x + 0.5f * w) * m_ratio, 0.f, m_imgWidth);
            float y1 = std::clamp((y + 0.5f * h) * m_ratio, 0.f, m_imgHeight);

            int label = maxSPtr - scoresPtr;
            cv::Rect_<float> bbox;
            bbox.x = x0;
            bbox.y = y0;
            bbox.width = x1 - x0;
            bbox.height = y1 - y0;

            cv::Mat maskConf = cv::Mat(1, SEG_CHANNELS, CV_32F, maskConfsPtr);

            bboxes.push_back(bbox);
            labels.push_back(label);
            scores.push_back(score);
            maskConfs.push_back(maskConf);
        }
    }

    // Require OpenCV 4.7 for this function
    cv::dnn::NMSBoxesBatched(
        bboxes,
        scores,
        labels,
        PROBABILITY_THRESHOLD,
        NMS_THRESHOLD,
        indices
    );

    // Obtain the segment masks
    cv::Mat masks;                 // [cnt x SEG_CHANNELS], CV_32F
    std::vector<Object> objs;
    int cnt = 0;
    for (auto& i : indices) {
        if (cnt >= TOP_K) break;

        Object obj;
        obj.label = labels[i];
        obj.rect = bboxes[i];
        obj.probability = scores[i];

        masks.push_back(maskConfs[i]); // 1 x SEG_CHANNELS, CV_32F
        objs.push_back(obj);
        cnt += 1;
    }

    // Convert segment mask to original frame
    if (!masks.empty()) {
        // masks:    [selected x SEG_CHANNELS]
        // protos:   [SEG_CHANNELS x (SEG_H*SEG_W)]
        // logits^T: [(SEG_H*SEG_W) x selected]
        cv::Mat logitsT = (masks * protos).t();     // CV_32F

        const int selected = static_cast<int>(objs.size()); // == cnt
        if (selected <= 0) return objs;

        // 가장 안전한 reshape 오버로드: reshape(newChannels, newRows)
        // total elems = SEG_H*SEG_W*selected 이므로
        // newChannels=selected, newRows=SEG_H → newCols 자동으로 SEG_W 계산됨
        cv::Mat maskMat = logitsT.reshape(/*newChannels=*/selected,
            /*newRows=*/SEG_H); // [SEG_H x SEG_W], ch=selected

        std::vector<cv::Mat> maskChannels;  // size == selected
        cv::split(maskMat, maskChannels);

        cv::Rect roi;
        if (m_imgHeight > m_imgWidth) {
            roi = cv::Rect(0, 0, SEG_W * m_imgWidth / m_imgHeight, SEG_H);
        }
        else {
            roi = cv::Rect(0, 0, SEG_W, SEG_H * m_imgHeight / m_imgWidth);
        }

        for (int i = 0; i < selected; ++i) {
            cv::Mat dest, mask;
            cv::exp(-maskChannels[i], dest);
            dest = 1.0 / (1.0 + dest);
            dest = dest(roi);
            cv::resize(dest, mask,
                cv::Size(static_cast<int>(m_imgWidth), static_cast<int>(m_imgHeight)),
                cv::INTER_LINEAR);
            objs[i].boxMask = mask(objs[i].rect) > SEGMENT_THRESHOLD;
        }
    }

    return objs;

}

std::vector<Object> YoloV8::postprocessDetect(std::vector<float>& featureVector) {
    const auto& outputDims = m_trtEngine->getOutputDims();
    auto numChannels = outputDims[0].d[1];
    auto numAnchors = outputDims[0].d[2];

    auto numClasses = CLASS_NAMES.size();

    std::vector<cv::Rect> bboxes;
    std::vector<float> scores;
    std::vector<int> labels;
    std::vector<int> indices;

    cv::Mat output = cv::Mat(numChannels, numAnchors, CV_32F, featureVector.data());
    output = output.t();

    // Get all the YOLO proposals
    for (int i = 0; i < numAnchors; i++) {
        auto rowPtr = output.row(i).ptr<float>();
        auto bboxesPtr = rowPtr;
        auto scoresPtr = rowPtr + 4;
        auto maxSPtr = std::max_element(scoresPtr, scoresPtr + numClasses);
        float score = *maxSPtr;
        if (score > PROBABILITY_THRESHOLD) {
            float x = *bboxesPtr++;
            float y = *bboxesPtr++;
            float w = *bboxesPtr++;
            float h = *bboxesPtr;

            float x0 = std::clamp((x - 0.5f * w) * m_ratio, 0.f, m_imgWidth);
            float y0 = std::clamp((y - 0.5f * h) * m_ratio, 0.f, m_imgHeight);
            float x1 = std::clamp((x + 0.5f * w) * m_ratio, 0.f, m_imgWidth);
            float y1 = std::clamp((y + 0.5f * h) * m_ratio, 0.f, m_imgHeight);

            int label = maxSPtr - scoresPtr;
            cv::Rect_<float> bbox;
            bbox.x = x0;
            bbox.y = y0;
            bbox.width = x1 - x0;
            bbox.height = y1 - y0;

            bboxes.push_back(bbox);
            labels.push_back(label);
            scores.push_back(score);
        }
    }

    // Run NMS
    cv::dnn::NMSBoxesBatched(bboxes, scores, labels, PROBABILITY_THRESHOLD, NMS_THRESHOLD, indices);

    std::vector<Object> objects;
    int cnt = 0;
    for (auto& idx : indices) {
        if (cnt >= TOP_K) break;

        int lbl = labels[idx];
        float sc = scores[idx];

        float thr = PROBABILITY_THRESHOLD;
        if (!CLASS_THRESHOLDS.empty() && lbl >= 0 && lbl < (int)CLASS_THRESHOLDS.size())
            thr = CLASS_THRESHOLDS[lbl];

        if (sc < thr) continue; // 클래스별로 컷

        printf("Class = %d, Score = %.2f\n", lbl, sc);

        Object obj{};
        obj.probability = sc;
        obj.label = lbl;
        obj.rect = bboxes[idx];
        objects.push_back(obj);
        cnt += 1;
    }

    return objects;
}

std::map<int, cv::Mat> YoloV8::getSegmentObjects(const std::vector<Object>& objects, int rows, int cols)
{
    std::map<int, cv::Mat> map;

    if (!objects.empty() && !objects[0].boxMask.empty()) {
        for (const auto& object : objects) {
            cv::Mat mask(rows, cols, CV_8UC1, cv::Scalar(0, 0, 0));
            mask(object.rect).setTo(cv::Scalar(255, 255, 255), object.boxMask);
            map.insert(std::make_pair(object.label, mask));
        }
    }

    return map;
}

std::map<int, std::vector<cv::Rect2f>> YoloV8::getDetectObjects(const std::vector<Object>& objects)
{
    std::map<int, std::vector<cv::Rect2f>> map;
    std::vector<cv::Rect2f> stentLists, guidewireLists;

    for (auto& object : objects) {
        const auto& rect = object.rect;

        if (object.label == 0) {//stent
            stentLists.push_back(rect);
        }
        else if (object.label == 1) {//guide_wire
            guidewireLists.push_back(rect);
        }
    }

    map.insert(std::make_pair(0, stentLists));
    map.insert(std::make_pair(1, guidewireLists));

    return map;
}


YoloV8* InitializeSegment()
{
    YoloV8Config segmentConfig;
    segmentConfig.segH = 256;
    segmentConfig.segW = 256;
    segmentConfig.classNames = { "lumen", "side_branch" };
    std::string segmentModelPath = "C:\\Raywatt\\system\\3rdparty\\model\\yolo\\segment.onnx";
    return new YoloV8(segmentModelPath, segmentConfig);
}

YoloV8* InitializeDetect()
{
    YoloV8Config detectConfig;
    detectConfig.segH = 256;
    detectConfig.segW = 256;
    detectConfig.classThresholds = { 0.13f, 0.22f };
    detectConfig.classNames = { "stent", "guide_wire" };
    std::string segmentModelPath = "C:\\Raywatt\\system\\3rdparty\\model\\yolo\\detect.onnx";
    return new YoloV8(segmentModelPath, detectConfig);
}

YoloV8* InitializeCalciumSegment() 
{
    YoloV8Config segmentConfig;
    segmentConfig.segH = 256;
    segmentConfig.segW = 256;
    segmentConfig.classNames = { "calcium" };
    std::string segmentModelPath = "C:\\Raywatt\\system\\3rdparty\\model\\yolo\\calcium.onnx";
    return new YoloV8(segmentModelPath, segmentConfig);
}

void* GetSegmentObjects(YoloV8* obj, cv::Mat img)
{
    const auto objectsSegment = obj->detectObjects(img);

    obj->resSegment = obj->getSegmentObjects(objectsSegment, img.rows, img.cols);

    return static_cast<void*>(&obj->resSegment);
}

void* GetDetectObjects(YoloV8* obj, cv::Mat img)
{
    const auto objectsDetect = obj->detectObjects(img);

    obj->resDetect = obj->getDetectObjects(objectsDetect);

	return static_cast<void*>(&obj->resDetect);
}

void* GetCalciumSegmentObjects(YoloV8* obj, cv::Mat img)
{
    const auto objectsSegment = obj->detectObjects(img);

    obj->resSegment = obj->getSegmentObjects(objectsSegment, img.rows, img.cols);

    return static_cast<void*>(&obj->resSegment);
}

