#include "RayYolo.h"

int main()
{
    RayYolo rayYolo;
    rayYolo.LoadDLL();

    void* yoloSegment = rayYolo.InitializeSegment();
    void* yoloDetect = rayYolo.InitializeDetect();       
    void* yoloCalciumSegment = rayYolo.InitializeCalciumSegment();

    //std::vector<std::string> fileList = {"test_img1.png"};
    std::vector<std::string> fileList = { "test_img1.png", "test_img2.png", "test_img3.png", "test_img4.png", "ffrCrossSectionImage.png" };

    for (std::string inputImage : fileList) {

        auto img = cv::imread(inputImage);
        if (img.empty()) {
            std::cout << "Error: Unable to read image at path '" << inputImage << "'" << std::endl;
            return -1;
        }

        //Get Segment Objects
        std::map<int, cv::Mat>* mapSegment = static_cast<std::map<int, cv::Mat>*>(rayYolo.GetSegmentObjects(yoloSegment, img));

        //Show
        std::vector<std::string> classNameSegment = { "lumen", "side_branch" };
        std::map<int, cv::Mat>::iterator iterSegment;
        for (iterSegment = mapSegment->begin(); iterSegment != mapSegment->end(); iterSegment++) {
            cv::imshow(classNameSegment[iterSegment->first], iterSegment->second);
            cv::waitKey();
        }

        //Get Detect Objects
        std::map<int, std::vector<cv::Rect2f>>* mapDetect = static_cast<std::map<int, std::vector<cv::Rect2f>>*>(rayYolo.GetDetectObjects(yoloDetect, img));

        //Show
        std::vector<std::string> classNameDetect = { "stent", "guide_wire" };
        std::map<int, std::vector<cv::Rect2f>>::iterator iterDetect;
        for (iterDetect = mapDetect->begin(); iterDetect != mapDetect->end(); iterDetect++) {
            cv::Mat mask = img.clone();
            std::vector<cv::Rect2f> vRect = iterDetect->second;

            for (auto& rect : vRect) {
                cv::Point point(rect.x + rect.width / 2, rect.y + rect.height / 2);
                cv::circle(mask, point, 1, cv::Scalar(255, 255, 255), 3);
            }

            if (vRect.size() > 0) {
                cv::imshow(classNameDetect[iterDetect->first], mask);
                cv::waitKey();
            }
        }

        //Get Calcium Segment Objects
        std::map<int, cv::Mat>* mapCalciumSegment = static_cast<std::map<int, cv::Mat>*>(rayYolo.GetCalciumSegmentObjects(yoloCalciumSegment, img));

        //Show
        std::vector<std::string> classNameCalciumSegment = { "calcium" };
        std::map<int, cv::Mat>::iterator iterCalciumSegment;
        for (iterCalciumSegment = mapCalciumSegment->begin(); iterCalciumSegment != mapCalciumSegment->end(); iterCalciumSegment++) {
            cv::imshow(classNameCalciumSegment[iterCalciumSegment->first], iterCalciumSegment->second);
            cv::waitKey();
        }
    }

    rayYolo.FreeDLL();
}