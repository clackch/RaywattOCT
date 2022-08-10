#pragma once
#include "define.h"
#include <vector>
#include <mutex>

enum class AsyncWork {
	Initialize = 0
};

class CThread;
class COCTSystem
{
private:
	std::mutex m_mutexQueue;
	std::vector<CThread**> m_vThreadQueue;
	FunctionPtr m_cbInitialize;
	
	CThread* m_pThreadService;
	CThread* m_pThreadInitialize;
public:
	COCTSystem();
	virtual ~COCTSystem();
	
	RayError Initialize(FunctionPtr cb);

private:
	void pushThread(CThread** pThread);
	CThread** popThread();
	static UINT threadService(LPVOID param);
	static UINT threadInitialize(LPVOID param);
};

