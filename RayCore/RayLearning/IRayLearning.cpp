#include "IRayLearning.h"
#include "RayYolo.h"

IRayLearning::IRayLearning() {
	m_useGPU = false;
}
IRayLearning* IRayLearning::GetInstance() {
    static IRayLearning* pInstance = new CRayYolo();
    return pInstance;
}

IRayLearning::~IRayLearning() {}