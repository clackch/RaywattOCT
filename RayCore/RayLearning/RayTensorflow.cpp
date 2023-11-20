#include "RayTensorflow.h"

#define IMG_INPUT_WIDTH 512
#define IMG_INPUT_HEIGHT 512
#define MODEL_INPUT_NAME "serving_default_input_5"
#define MODEL_OUTPUT_NAME "StatefulPartitionedCall"

CRayUnetr::CRayUnetr(){
	m_hGetProcIDDLL = LoadLibrary(L"tensorflow.dll");
}

CRayUnetr::~CRayUnetr() {
	if (m_hGetProcIDDLL) {
		m_TFRayDelSession(m_pSession, m_pStatus);
		m_TFRayDelBuffer(m_pRunOptions);
		m_TFRayDelSessionOptions(m_pSessionOptions);
		m_TFRayDelGraph(m_pGraph);
		m_TFRayDelStatus(m_pStatus);
		FreeLibrary(m_hGetProcIDDLL);
	}
}

float* CRayUnetr::RunModel(float* image_data) {
	int64_t indims[] = { 1, IMG_INPUT_WIDTH, IMG_INPUT_HEIGHT, 3 };  // Batch size x Height x Width x Channels
	int64_t outdims[] = { 1, IMG_INPUT_WIDTH, IMG_INPUT_HEIGHT, 1 };  // Batch size x Height x Width x Channels
	float* out_data = new float[IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT * 1];
	fill(out_data, out_data + (IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT), 0.0f);

	auto* input_tensor = m_TFRayTensor(
		TF_FLOAT,
		indims,
		4,
		image_data,
		IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT * 3 * sizeof(float),
		[](void* data, size_t len, void* arg) {}, nullptr
	);

	auto* output_tensor = m_TFRayTensor(
		TF_FLOAT,
		outdims,
		4,
		out_data,
		IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT * 1 * sizeof(float),
		[](void* data, size_t len, void* arg) {}, nullptr
	);

	auto* input_operation = m_TFRayGraphOperationByName(m_pGraph, MODEL_INPUT_NAME);
	auto* output_operation = m_TFRayGraphOperationByName(m_pGraph, MODEL_OUTPUT_NAME);

	TF_Output inputs[] = { input_operation };
	TF_Tensor* input_values[] = { input_tensor };
	TF_Output outputs[] = { output_operation };
	TF_Tensor* output_values[] = { output_tensor };

	m_TFRaySessionRun(m_pSession, m_pRunOptions,
		inputs, input_values, 1,
		outputs, output_values, 1,
		nullptr, 0,
		nullptr, m_pStatus
	);

	void* buff = m_TFRayTensorData(output_values[0]);
	auto* result_data = (float*)buff;

	m_TFRayDelTensor(output_tensor);
	m_TFRayDelTensor(input_tensor);
	return result_data;
}

void CRayUnetr::Initialize(bool useGPU) {
	if (m_hGetProcIDDLL != INVALID_HANDLE_VALUE) {
		m_TFRayStatus = (TF_NewStatusFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewStatus");
		m_TFRayGraph = (TF_NewGraphFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewGraph");
		m_TFRaySessionOptions = (TF_NewSessionOptionsFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewSessionOptions");
		m_TFRayLoadSessionFromSavedModel = (TF_LoadSessionFromSavedModelFunction)GetProcAddress(m_hGetProcIDDLL, "TF_LoadSessionFromSavedModel");
		m_TFRayGetCode = (TF_GetCodeFunction)GetProcAddress(m_hGetProcIDDLL, "TF_GetCode");
		m_TFRayGraphOperationByName = (TF_GraphOperationByNameFunction)GetProcAddress(m_hGetProcIDDLL, "TF_GraphOperationByName");
		m_TFRayTensor = (TF_NewTensorFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewTensor");
		m_TFRaySessionRun = (TF_SessionRunFunction)GetProcAddress(m_hGetProcIDDLL, "TF_SessionRun");
		m_TFRayTensorData = (TF_TensorDataFunction)GetProcAddress(m_hGetProcIDDLL, "TF_TensorData");
		m_TFRayMessage = (TF_MessageFunction)GetProcAddress(m_hGetProcIDDLL, "TF_Message");
		m_TFRayTensorByteSize = (TF_TensorByteSizeFunction)GetProcAddress(m_hGetProcIDDLL, "TF_TensorByteSize");

		m_TFRayDelStatus = (TF_DeleteStatusFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteStatus");
		m_TFRayDelGraph = (TF_DeleteGraphFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteGraph");
		m_TFRayDelSessionOptions = (TF_DeleteSessionOptionsFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteSessionOptions");
		m_TFRayDelSession = (TF_DeleteSessionFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteSession");
		m_TFRayDelBuffer = (TF_DeleteBufferFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteBuffer");
		m_TFRayDelTensor = (TF_DeleteTensorFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteTensor");
	}

	_putenv("CUDA_VISIBLE_DEVICES=0,1");

	m_pStatus = m_TFRayStatus();
	m_pGraph = m_TFRayGraph();
	m_pSessionOptions = m_TFRaySessionOptions();
	const char* tags[] = { "serve" };
	m_pRunOptions = nullptr;
	m_pSession = m_TFRayLoadSessionFromSavedModel(m_pSessionOptions, m_pRunOptions,
		"C:\\Raywatt\\system\\3rdparty\\model", tags, 1, m_pGraph, nullptr, m_pStatus);
}

cv::Mat CRayUnetr::FindLumen(cv::Mat image) {
	cv::Mat input_image = image.clone();
	bool resize = false;

	cv::rotate(input_image, input_image, cv::ROTATE_180);
	
	if (image.channels() == 1) {
		cv::cvtColor(input_image, input_image, cv::COLOR_GRAY2RGB);
	}
	
	if (image.cols != IMG_INPUT_WIDTH|| image.rows != IMG_INPUT_HEIGHT) {
		cv::resize(input_image, input_image, cv::Size(IMG_INPUT_HEIGHT, IMG_INPUT_WIDTH));
		resize = true;
	}
	
	input_image.convertTo(input_image, CV_32F);
	input_image = input_image / 255.0;

	float* data = input_image.ptr<float>(0);
	PLOGI.printf("Tensorflow RunModel Run -> \"Tensorflow Model OK\" need to follow");
	data = RunModel(data); 
	if (data == nullptr) {
		PLOGI.printf("Tensorflow Model Error");
	}
	else {
		PLOGI.printf("Tensorflow Model OK");
	}

	cv::Mat output_image(IMG_INPUT_HEIGHT, IMG_INPUT_HEIGHT, CV_32FC1, data);

	cv::threshold(output_image, output_image, 0.5, 1, cv::THRESH_BINARY);
	output_image *= 255;
	output_image.convertTo(output_image, CV_8UC1);

	if (resize) {
		cv::resize(output_image, output_image, cv::Size(image.rows, image.cols));
	}

	cv::rotate(output_image, output_image, cv::ROTATE_180);

	return output_image;
}
