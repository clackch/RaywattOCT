#include "RayPytorchUnet.h"

#define UNETR_INPUT_WIDTH	512
#define UNETR_INPUT_HEIGHT	512

CRayTorchUnet::CRayTorchUnet() {
    LoadLibrary(L"torch_cuda.dll");
    m_useGPU = false;
}

CRayTorchUnet::~CRayTorchUnet() {}

void CRayTorchUnet::Initialize(bool useGPU) {
    torch::Device device(torch::kCPU);
    if (useGPU && torch::cuda::is_available()) {
        m_useGPU = useGPU;
        device = torch::Device(torch::kCUDA, 0);
    }

    std::string model_path = ".\\model_UnetR_torch.pt";
    try {
        m_model = torch::jit::load(model_path, device);
    }
    catch (const c10::Error& e) {
        PLOGI.printf(e.what());
    }
}

cv::Mat CRayTorchUnet::FindLumen(cv::Mat image) {
    cv::Mat imgInput;
    cv::cvtColor(image, imgInput, cv::COLOR_GRAY2RGB);
    
    bool resize = false;

    try {
        if (image.cols != UNETR_INPUT_WIDTH || image.rows != UNETR_INPUT_HEIGHT) {
            cv::resize(image, imgInput, cv::Size(UNETR_INPUT_HEIGHT, UNETR_INPUT_WIDTH));
            resize = true;
        }
        else {
            imgInput = image.clone();
        }

        torch::Tensor img_tensor = torch::from_blob(image.data, { image.rows, image.cols, 3 }, torch::kByte);
        img_tensor = img_tensor.permute({ 2, 0, 1 }); // H x W x C -> C x H x W
        img_tensor = img_tensor.to(torch::kFloat32); // Byte -> Float
        img_tensor = img_tensor.unsqueeze(0);  // Add batch dim
        img_tensor = img_tensor.to(m_useGPU ? torch::Device(torch::kCUDA, 0) : torch::Device(torch::kCPU));

        auto outputs = m_model.forward({ img_tensor }).toTensor();
        auto outputs_softmax = torch::softmax(outputs, /*dim=*/1); // 비율로 결과 추리기
        auto prediction_result = torch::argmax(outputs_softmax, /*dim=*/1); // 클래스중 최대값 선택하여 1
        auto squeezed_result = prediction_result.squeeze(); // 차원 줄이고
        auto cpu_result = squeezed_result.to(torch::kCPU); // CPU로 가져와서 CV작업
        auto accessor = cpu_result.accessor<int64_t, 2>();  // 2차원 텐서 = 2차원 배열에 각 class 결과 값 저장.

        int height = cpu_result.size(0);
        int width = cpu_result.size(1);

        cv::Mat lumen_mask(height, width, CV_8UC1, cv::Scalar(0));
        cv::Mat stent_mask(height, width, CV_8UC1, cv::Scalar(0));

        for (int i = 0; i < height; ++i) {
            for (int j = 0; j < width; ++j) {
                int class_label = accessor[i][j];
                if (class_label == 1) {
                    lumen_mask.at<uchar>(i, j) = 255;
                }
                else if (class_label == 2) {
                    stent_mask.at<uchar>(i, j) = 255; // todo Stent 데이터 추가 필요
                }
            }
        }

        if (resize) {
            cv::resize(lumen_mask, lumen_mask, cv::Size(image.rows, image.cols));
            cv::resize(stent_mask, stent_mask, cv::Size(image.rows, image.cols));
        }

        std::string path;
        static int cnt = 0;
        path = ".\\mlimage\\Lumen_mask_torchUnet" + std::to_string(cnt) + ".png";
        cv::imwrite(path, lumen_mask);
        path = ".\\mlimage\\stent_mask_torchUnet" + std::to_string(cnt++) + ".png";
        cv::imwrite(path, stent_mask);
        return lumen_mask;
    }
    catch (const std::exception& e) {
        PLOGI.printf(e.what());
    }
}