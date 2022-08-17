#pragma once
#include <vector>
#include <thread>
#include <mutex>
#include <locale>

typedef UINT(_cdecl* THREADPROC)(LPVOID);

class CThread {
public:
	std::thread thread;
	std::mutex mMutex;
	std::condition_variable sEvent;
	bool isRun;

public:
	CThread(THREADPROC threadFunc, LPVOID param) : thread(threadFunc, param) {
		isRun = true;
	}

	virtual ~CThread() {}
};

class CUtility
{
public:
	CUtility() {}
	virtual ~CUtility() {}

public:
	static BOOL StartThread(THREADPROC threadFunc, CThread *&pThread, LPVOID param);
	static void StopThread(CThread *&pThread);
	static void ResumeThread(CThread* pThread);
	static void SuspendThread(CThread* pThread);

	static std::vector<tstring> findSerialPort();
	static void GetCurTime(char* strTime);
	static std::wstring StringToWstring(const std::string& var);
};

