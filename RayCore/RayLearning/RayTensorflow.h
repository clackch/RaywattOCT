#pragma once
#include "IRayLearning.h"
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
	TF_NewStatusFunction m_TFRayStatus;
	TF_NewGraphFunction m_TFRayGraph;
	TF_NewSessionOptionsFunction m_TFRaySessionOptions;
	TF_LoadSessionFromSavedModelFunction m_TFRayLoadSessionFromSavedModel;
	TF_GetCodeFunction m_TFRayGetCode;
	TF_GraphOperationByNameFunction m_TFRayGraphOperationByName;
	TF_NewTensorFunction m_TFRayTensor;
	TF_SessionRunFunction m_TFRaySessionRun;
	TF_TensorDataFunction m_TFRayTensorData;
	TF_MessageFunction m_TFRayMessage;
	TF_TensorByteSizeFunction m_TFRayTensorByteSize;
	TF_DeleteStatusFunction m_TFRayDelStatus;
	TF_DeleteGraphFunction m_TFRayDelGraph;
	TF_DeleteSessionOptionsFunction m_TFRayDelSessionOptions;
	TF_DeleteSessionFunction m_TFRayDelSession;
	TF_DeleteBufferFunction m_TFRayDelBuffer;
	TF_DeleteTensorFunction m_TFRayDelTensor;
	TF_Status* m_pStatus;
	TF_Graph* m_pGraph;
	TF_SessionOptions* m_pSessionOptions;
	TF_Buffer* m_pRunOptions;
	TF_Session* m_pSession;

public:
	CRayUnetr();
	virtual ~CRayUnetr();
	float* RunModel(float* image_data);
	void Initialize(bool useGPU) override;
	cv::Mat FindLumen(cv::Mat image) override;
};