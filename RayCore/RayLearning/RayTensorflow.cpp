#include "RayTensorflow.h"

#define IMG_INPUT_WIDTH 512
#define IMG_INPUT_HEIGHT 512

CRayUnetr::CRayUnetr(){
	m_hGetProcIDDLL = LoadLibrary(L"tensorflow.dll");
}

CRayUnetr::~CRayUnetr() {
	if (m_hGetProcIDDLL) {
		FreeLibrary(m_hGetProcIDDLL);
	}
}

float* CRayUnetr::RunModel(float* image_data) {
	TF_NewStatusFunction TFRayStatus;
	TF_NewGraphFunction TFRayGraph;
	TF_NewSessionOptionsFunction TFRaySessionOptions;
	TF_LoadSessionFromSavedModelFunction TFRayLoadSessionFromSavedModel;
	TF_GetCodeFunction TFRayGetCode;
	TF_GraphOperationByNameFunction TFRayGraphOperationByName;
	TF_NewTensorFunction TFRayTensor;
	TF_SessionRunFunction TFRaySessionRun;
	TF_TensorDataFunction TFRayTensorData;
	TF_MessageFunction TFRayMessage;
	TF_TensorByteSizeFunction TFRayTensorByteSize;

	TF_DeleteStatusFunction TFRayDelStatus;
	TF_DeleteGraphFunction TFRayDelGraph;
	TF_DeleteSessionOptionsFunction TFRayDelSessionOptions;
	TF_DeleteSessionFunction TFRayDelSession;
	TF_DeleteBufferFunction TFRayDelBuffer;
	TF_DeleteTensorFunction TFRayDelTensor;

	if (m_hGetProcIDDLL != INVALID_HANDLE_VALUE) {
		TFRayStatus = (TF_NewStatusFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewStatus");
		TFRayGraph = (TF_NewGraphFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewGraph");
		TFRaySessionOptions = (TF_NewSessionOptionsFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewSessionOptions");
		TFRayLoadSessionFromSavedModel = (TF_LoadSessionFromSavedModelFunction)GetProcAddress(m_hGetProcIDDLL, "TF_LoadSessionFromSavedModel");
		TFRayGetCode = (TF_GetCodeFunction)GetProcAddress(m_hGetProcIDDLL, "TF_GetCode");
		TFRayGraphOperationByName = (TF_GraphOperationByNameFunction)GetProcAddress(m_hGetProcIDDLL, "TF_GraphOperationByName");
		TFRayTensor = (TF_NewTensorFunction)GetProcAddress(m_hGetProcIDDLL, "TF_NewTensor");
		TFRaySessionRun = (TF_SessionRunFunction)GetProcAddress(m_hGetProcIDDLL, "TF_SessionRun");
		TFRayTensorData = (TF_TensorDataFunction)GetProcAddress(m_hGetProcIDDLL, "TF_TensorData");
		TFRayMessage = (TF_MessageFunction)GetProcAddress(m_hGetProcIDDLL, "TF_Message");
		TFRayTensorByteSize = (TF_TensorByteSizeFunction)GetProcAddress(m_hGetProcIDDLL, "TF_TensorByteSize");

		TFRayDelStatus = (TF_DeleteStatusFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteStatus");
		TFRayDelGraph = (TF_DeleteGraphFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteGraph");
		TFRayDelSessionOptions = (TF_DeleteSessionOptionsFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteSessionOptions");
		TFRayDelSession = (TF_DeleteSessionFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteSession");
		TFRayDelBuffer = (TF_DeleteBufferFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteBuffer");
		TFRayDelTensor = (TF_DeleteTensorFunction)GetProcAddress(m_hGetProcIDDLL, "TF_DeleteTensor");
	}
	else {
		cout << "Dynamic library is not loaded" << endl;
		return nullptr;
	}

	//GPU 환경 변수 추가
	_putenv("CUDA_VISIBLE_DEVICES=0,1");
	
	auto* status = TFRayStatus();
	if (status == nullptr) {
		cout << "Status not initialized" << endl;
	}

	auto* graph = TFRayGraph();
	if (graph == nullptr) {
		cout << "Graph not initialized" << endl;
	}

	auto* sess_opts = TFRaySessionOptions();
	const char* tags[] = { "serve" };
	TF_Buffer* run_opts = nullptr;

	auto* session = TFRayLoadSessionFromSavedModel(sess_opts, run_opts,
		".\\model", tags, 1, graph, nullptr, status);
	if (session == nullptr) {
		cout << "Session not initialized: " << TFRayMessage(status) << endl;
	}
	if (TFRayGetCode(status) == TF_OK)
	{
		printf("TF_LoadSessionFromSavedModel OK\n");
	}

	int64_t indims[] = { 1, IMG_INPUT_WIDTH, IMG_INPUT_HEIGHT, 3 };  // Batch size x Height x Width x Channels
	int64_t outdims[] = { 1, IMG_INPUT_WIDTH, IMG_INPUT_HEIGHT, 1 };  // Batch size x Height x Width x Channels
	float* out_data = new float[IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT * 1];
	fill(out_data, out_data + (IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT), 0.0f);

	auto* input_tensor = TFRayTensor(
		TF_FLOAT,
		indims,
		4,
		image_data,
		IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT * 3 * sizeof(float),
		[](void* data, size_t len, void* arg) {}, nullptr
	);

	if (input_tensor == nullptr) {
		cout << "Input tensor not initialized: " << TFRayMessage(status) << endl;
	}

	auto* output_tensor = TFRayTensor(
		TF_FLOAT,
		outdims,
		4,
		out_data,
		IMG_INPUT_WIDTH * IMG_INPUT_HEIGHT * 1 * sizeof(float),
		[](void* data, size_t len, void* arg) {}, nullptr
	);

	if (output_tensor == nullptr) {
		cout << "Output tensor not initialized: " << TFRayMessage(status) << endl;
	}

	auto* input_operation = TFRayGraphOperationByName(graph, "serving_default_input_5");
	if (input_operation == nullptr) {
		cout << "Error: Input operation not found." << endl;
		return nullptr;
	}

	auto* output_operation = TFRayGraphOperationByName(graph, "StatefulPartitionedCall");
	if (output_operation == nullptr) {
		cout << "Error: Output operation not found." << endl;
		return nullptr;
	}


	TF_Output inputs[] = { input_operation };
	TF_Tensor* input_values[] = { input_tensor };
	TF_Output outputs[] = { output_operation };
	TF_Tensor* output_values[] = { output_tensor };

	TFRaySessionRun(session, run_opts,
		inputs, input_values, 1,
		outputs, output_values, 1,
		nullptr, 0,
		nullptr, status
	);

	if (TFRayGetCode(status) != TF_OK) {
		cout << "Error in session run: " << TFRayMessage(status) << endl;
		TFRayDelStatus(status);
		return nullptr;
	}

	// Print the address of input_values and output_values
	cout << "Address of input_values: " << &input_values << endl;
	cout << "Address of output_values: " << &output_values << endl;

	// Check the state of input tensor and output tensor
	if (input_values[0] != nullptr && output_values[0] != nullptr) {
		cout << "Input and Output tensors are not null" << endl;
	}
	else {
		cout << "One of the tensors is null" << endl;
	}

	void* buff = TFRayTensorData(output_values[0]);
	auto* result_data = (float*)buff;

	if (result_data == nullptr) {
		cout << "출력 데이터 할당 오류" << endl;
	}

	size_t byte_size = TFRayTensorByteSize(output_tensor);
	cout << "Allocated byte size for output_tensor: " << byte_size << endl;

	TFRayDelTensor(output_tensor);
	TFRayDelTensor(input_tensor);
	TFRayDelSession(session, status);
	TFRayDelSessionOptions(sess_opts);
	TFRayDelGraph(graph);
	TFRayDelStatus(status);
	TFRayDelBuffer(run_opts);

	return result_data;
}

void CRayUnetr::Initialize(bool useGPU) {}

vector<vector<cv::Point>> CRayUnetr::FindLumen(cv::Mat image) {
	cv::Mat input_image;

	if (image.cols != IMG_INPUT_WIDTH|| image.rows != IMG_INPUT_HEIGHT) {
		cv::resize(image, input_image, cv::Size(IMG_INPUT_HEIGHT, IMG_INPUT_WIDTH));
	}
	else {
		input_image = image.clone();
	}

	input_image.convertTo(input_image, CV_32F); // 이미지를 float 타입으로 변환
	input_image = input_image / 255.0;

	float* data = input_image.ptr<float>(0);
	data = RunModel(data); 

	cv::Mat output_image(IMG_INPUT_HEIGHT, IMG_INPUT_HEIGHT, CV_32FC1, data);

	cv::threshold(output_image, output_image, 0.5, 1, cv::THRESH_BINARY);
	output_image *= 255;
	output_image.convertTo(output_image, CV_8UC1);

	vector<vector<cv::Point>> contours;
	cv::findContours(output_image, contours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

	// filtering - erase wrong contours
	cv::Point ptCenter;
	ptCenter.x = image.cols / 2;
	ptCenter.y = image.rows / 2;
	for (int i = contours.size() - 1; i >= 0; i--)
	{
		cv::Rect boundingBox = cv::boundingRect(contours[i]);
		if (!boundingBox.contains(ptCenter))
		{
			contours.erase(contours.begin() + i);
		}
	}

	return contours;
}
