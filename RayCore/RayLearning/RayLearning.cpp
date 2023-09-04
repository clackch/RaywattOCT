#include "RayLearning.h"
#include "RayTensorflow.h"

IRayLearning::IRayLearning() {
	m_useGPU = false;
}
IRayLearning* IRayLearning::GetInstance() {
    static IRayLearning* pInstance = new CRayUnetr();
    return pInstance;
}

IRayLearning::~IRayLearning() {}