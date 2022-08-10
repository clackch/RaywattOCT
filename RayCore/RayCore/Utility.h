#pragma once
#include <vector>
#include <thread>
#include <mutex>

typedef UINT(_cdecl* THREADPROC)(LPVOID);

class CThread {
public:
	std::thread thread;
	std::mutex mMutex;
	std::condition_variable sEvent;
	bool isPaused;

public:
	CThread(THREADPROC threadFunc, LPVOID param) : thread(threadFunc, param) {
		isPaused = false;
	}

	virtual ~CThread() {}
};

class CUtility
{
public:
	CUtility() {}
	virtual ~CUtility() {}

public:
	static BOOL StartThread(THREADPROC threadFunc, CThread *&pThread, bool &flagRun, LPVOID param);
	static void StopThread(CThread *&pThread, bool &flagRun);
	static void ResumeThread(CThread* pThread);
	static void SuspendThread(CThread* pThread);

	static std::vector<tstring> findSerialPort();
	static void GetCurTime(char* strTime);
};

