#pragma once
#include "engine.h"
#include <fstream>
#include <map>

// Utility method for checking if a file exists on disk
inline bool doesFileExist(const std::string& name) {
    std::ifstream f(name.c_str());
    return f.good();
}

struct Object {
    // The object class.
    int label{};
    // The detection's confidence probability.
    float probability{};
    // The object bounding box rectangle.
    cv::Rect_<float> rect;
    // Semantic segment mask
    cv::Mat boxMask;
};

// Config the behavior of the YoloV8 detector.
// Can pass these arguments as command line parameters.
struct YoloV8Config {
    // The precision to be used for inference
    Precision precision = Precision::FP16;
    // Calibration data directory. Must be specified when using INT8 precision.
    std::string calibrationDataDirectory;
    // Probability threshold used to filter detected objects
    float probabilityThreshold = 0.1f;
    // Non-maximum suppression threshold
    float nmsThreshold = 0.65f;
    // Max number of detected objects to return
    int topK = 100;
    // Segment config options
    int segChannels = 32;
    int segH = 256;
    int segW = 256;
    float segmentThreshold = 0.5f;
    // Class thresholds
    std::vector<std::string> classNames = {
    };
};

class YoloV8 {
public:
    // Builds the onnx model into a TensorRT engine, and loads the engine into memory
    YoloV8(const std::string& onnxModelPath, const YoloV8Config& config);

    // Detect the objects in the image
    std::vector<Object> detectObjects(const cv::Mat& inputImageBGR);
    std::vector<Object> detectObjects(const cv::cuda::GpuMat& inputImageBGR);

    // Get Objects
    std::map<int, cv::Mat> getSegmentObjects(const std::vector<Object>& objects, int rows, int cols);
    std::map<int, std::vector<cv::Rect2f>> getDetectObjects(const std::vector<Object>& objects);


    std::map<int, cv::Mat> resSegment;
    std::map<int, std::vector<cv::Rect2f>> resDetect;
private:

    YoloV8* yoloV8;

    // Preprocess the input
    std::vector<std::vector<cv::cuda::GpuMat>> preprocess(const cv::cuda::GpuMat& gpuImg);

    // Postprocess the output
    std::vector<Object> postprocessDetect(std::vector<float>& featureVector);

    // Postprocess the output for segment model
    std::vector<Object> postProcessSegment(std::vector<std::vector<float>>& featureVectors);


    std::unique_ptr<Engine> m_trtEngine = nullptr;

    // Used for image preprocessing
    // YoloV8 model expects values between [0.f, 1.f] so we use the following params
    const std::array<float, 3> SUB_VALS{ 0.f, 0.f, 0.f };
    const std::array<float, 3> DIV_VALS{ 1.f, 1.f, 1.f };
    const bool NORMALIZE = true;

    float m_ratio = 1;
    float m_imgWidth = 0;
    float m_imgHeight = 0;

    // Filter thresholds
    const float PROBABILITY_THRESHOLD;
    const float NMS_THRESHOLD;
    const int TOP_K;

    // Segment constants
    const int SEG_CHANNELS;
    const int SEG_H;
    const int SEG_W;
    const float SEGMENT_THRESHOLD;

    // Object classes as strings
    const std::vector<std::string> CLASS_NAMES;
};

extern "C" __declspec(dllexport) YoloV8 * InitializeSegment();
extern "C" __declspec(dllexport) YoloV8 * InitializeDetect();
extern "C" __declspec(dllexport) YoloV8 * InitializeCalciumSegment();
extern "C" __declspec(dllexport) void * GetSegmentObjects(YoloV8 * obj, cv::Mat img);
extern "C" __declspec(dllexport) void * GetDetectObjects(YoloV8* obj, cv::Mat img);
extern "C" __declspec(dllexport) void* GetCalciumSegmentObjects(YoloV8 * obj, cv::Mat img);
