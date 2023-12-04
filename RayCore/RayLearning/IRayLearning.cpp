#include "IRayLearning.h"
#include "RayTensorflow.h"
#include "RayPytorch.h"
#include "RayPytorchUnet.h"

IRayLearning::IRayLearning() {
	m_useGPU = false;
}
IRayLearning* IRayLearning::GetInstance() {
    static IRayLearning* pInstance = new CRayTorchUnet();
    return pInstance;
}

IRayLearning::~IRayLearning() {}