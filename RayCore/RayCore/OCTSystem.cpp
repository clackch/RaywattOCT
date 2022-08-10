#include "pch.h"
#include "OCTSystem.h"
#include "Utility.h"

COCTSystem::COCTSystem() {
	m_cbInitialize = nullptr;
	
	m_pThreadService = nullptr;
	m_pThreadInitialize = nullptr;

	CUtility::StartThread(threadService, m_pThreadService, this);
}
COCTSystem::~COCTSystem() {
	CUtility::StopThread(m_pThreadService);
}

RayError COCTSystem::Initialize(FunctionPtr cb) {
	m_cbInitialize = cb;

	CUtility::StartThread(threadInitialize, m_pThreadInitialize, this);

	return RayError::OK;
}

void COCTSystem::pushThread(CThread** pThread) {
	m_mutexQueue.lock();
	
	m_vThreadQueue.push_back(pThread);

	m_mutexQueue.unlock();
}
CThread** COCTSystem::popThread() {
	CThread** pThread = nullptr;

	{
		m_mutexQueue.lock();
		if (!m_vThreadQueue.empty()) {
			pThread = m_vThreadQueue.at(0);
			m_vThreadQueue.erase(m_vThreadQueue.begin());
		}
		m_mutexQueue.unlock();	
	}

	return pThread;
}

UINT COCTSystem::threadService(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CThread* pThread = pSystem->m_pThreadService;
	
	while (pThread->isRun) {
		CThread** pop = pSystem->popThread();
		if (pop != nullptr) {
			CUtility::StopThread(*pop);
		}
	
		Sleep(10);
	}

	return (UINT)RayError::OK;
}

UINT COCTSystem::threadInitialize(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;

	// Initialize
	Sleep(5000);

	if(pSystem->m_cbInitialize != nullptr) pSystem->m_cbInitialize(11, 22);
	
	pSystem->pushThread(&pSystem->m_pThreadInitialize);
	while (pSystem->m_pThreadInitialize->isRun) {
		Sleep(10);
	}

	return (UINT) RayError::OK;
}