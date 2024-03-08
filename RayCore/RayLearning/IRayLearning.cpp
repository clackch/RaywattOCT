#include "IRayLearning.h"
#include "RayTensorflow.h"
#include "RayPytorch.h"
#include "RayPytorchUnet.h"
#include "RayYolo.h"

IRayLearning::IRayLearning() {
	m_useGPU = false;
}
IRayLearning* IRayLearning::GetInstance() {
    static IRayLearning* pInstance = new CRayYolo();
    return pInstance;
}

IRayLearning::~IRayLearning() {}