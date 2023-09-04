#include "RayLearning.h"
#include "RayTensorflow.h"
#include "RayPytorch.h"

IRayLearning::IRayLearning() {
	m_useGPU = false;
}
IRayLearning* IRayLearning::GetInstance() {
    static IRayLearning* pInstance = new CRayUnetr();
    return pInstance;
}

IRayLearning::~IRayLearning() {}