#pragma once
#include "IRayLearning.h"
#include <iostream>
#include <vector>
#include <opencv2/opencv.hpp>
#include <tensorflow/c/c_api.h>
#include <tensorflow/c/tf_status.h>
#include <tensorflow/c/tf_tensor.h>
#include <tensorflow/c/tf_datatype.h>
#include <stdlib.h>

using namespace std;

#pragma region Def_TF_Functions
typedef TF_Status* (*TF_NewStatusFunction)();
typedef TF_Graph* (*TF_NewGraphFunction)();
typedef TF_SessionOptions* (*TF_NewSessionOptionsFunction)();
typedef TF_Session* (*TF_LoadSessionFromSavedModelFunction)(const TF_SessionOptions*, const TF_Buffer*, const char*, const char* const*, int, TF_Graph*, TF_Buffer*, TF_Status*);
typedef TF_Code(*TF_GetCodeFunction)(const TF_Status*);
typedef TF_Operation* (*TF_GraphOperationByNameFunction)(TF_Graph*, const char*);
typedef TF_Tensor* (*TF_NewTensorFunction)(TF_DataType, const int64_t* dims, int num_dims, void* data, size_t len,
	void (*deallocator)(void* data, size_t len, void* arg), void* deallocator_arg);
typedef void (*TF_SessionRunFunction)(TF_Session* session, const TF_Buffer* run_options,
	const TF_Output* inputs, TF_Tensor* const* input_values, int ninputs,
	const TF_Output* outputs, TF_Tensor* const* output_values, int noutputs,
	const TF_Operation* const* target_opers, int ntargets,
	TF_Buffer*, TF_Status*);
typedef void* (*TF_TensorDataFunction)(const TF_Tensor*);
typedef char* (*TF_MessageFunction)(TF_Status*);
typedef size_t(*TF_TensorByteSizeFunction)(TF_Tensor*);

typedef void (*TF_DeleteStatusFunction)(TF_Status*);
typedef void (*TF_DeleteGraphFunction)(TF_Graph*);
typedef void (*TF_DeleteSessionOptionsFunction)(TF_SessionOptions*);
typedef void (*TF_DeleteSessionFunction)(TF_Session*, TF_Status*);
typedef void (*TF_DeleteBufferFunction)(TF_Buffer*);
typedef void (*TF_DeleteTensorFunction)(TF_Tensor*);
#pragma endregion

class CRayUnetr : public IRayLearning {
private:
	HINSTANCE m_hGetProcIDDLL;

public:
	CRayUnetr();
	virtual ~CRayUnetr();
	float* RunModel(float* image_data);
	void Initialize(bool useGPU) override;
	vector<vector<cv::Point>> FindLumen(cv::Mat image) override;
};