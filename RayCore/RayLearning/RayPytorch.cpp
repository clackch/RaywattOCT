#include "RayPytorch.h"

#define COMPNET_INPUT_WIDTH		512
#define COMPNET_INPUT_HEIGHT	512

CRayCompNet::CRayCompNet() {
	// to us GPU
	LoadLibrary(L"torch_cuda.dll");

	m_useGPU = false;
}
CRayCompNet::~CRayCompNet() {}

void CRayCompNet::Initialize(bool useGPU) {
	if (useGPU && torch::cuda::is_available()) {
		m_compNet->to(torch::kCUDA);
		m_useGPU = true;
	}

	torch::load(m_compNet, (m_useGPU ? "C:\\Raywatt\\system\\3rdparty\\model\\pytorch\\compnet_gpu.pt" : "C:\\Raywatt\\system\\3rdparty\\model\\pytorch\\compnet_cpu.pt"));
	m_compNet->eval();
}

cv::Mat CRayCompNet::FindLumen(cv::Mat image) {
	vector<vector<cv::Point>> contours;

	cv::Mat imgInput;
	bool resize = false;

	if (image.cols != COMPNET_INPUT_WIDTH || image.rows != COMPNET_INPUT_HEIGHT) {
		cv::resize(image, imgInput, cv::Size(COMPNET_INPUT_HEIGHT, COMPNET_INPUT_WIDTH));
		resize = true;
	}
	else {
		imgInput = image.clone();
	}

	// to tensor
	torch::Tensor input = torch::from_blob(imgInput.data, { imgInput.rows, imgInput.cols, 3 }, torch::kByte);

	// transpose
	input = input.permute({ 2, 0, 1 });
	input = input.toType(at::kFloat);
	input.div_(255.);
	input = input.expand({ 1, input.sizes()[0], input.sizes()[1], input.sizes()[2] });
	input = (m_useGPU) ? input.cuda() : input;

	// predict
	tuple<torch::Tensor, torch::Tensor> result = m_compNet(input);

	torch::Tensor pred_y = get<0>(result);
	pred_y = torch::sigmoid(pred_y);
	pred_y = torch::squeeze(pred_y);
	pred_y = (m_useGPU) ? pred_y.cpu() : pred_y;

	cv::Mat imgLumen(imgInput.rows, imgInput.cols, CV_8UC1);
	for (int y = 0; y < imgLumen.rows; y++) {
		for (int x = 0; x < imgLumen.cols; x++) {
			float data = ((float*)pred_y.data_ptr())[y * pred_y.size(1) + x];
			imgLumen.at<char>(y, x) = (data > 0.5f) ? 255 : 0;
		}
	}

	if (resize) {
		cv::resize(imgLumen, imgLumen, cv::Size(image.rows, image.cols));
	}

	return imgLumen;
}

