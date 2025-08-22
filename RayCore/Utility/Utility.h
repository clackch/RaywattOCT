#pragma once
#include "Config.h"
#include <vector>
#include <thread>
#include <mutex>
#include <locale>
#include <chrono>
#include <iostream>

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

class StopWatch {
private:
	std::string moduleName;
	std::chrono::system_clock::time_point start;

public:
	StopWatch(std::string moduleName) {
		this->moduleName = moduleName;
		this->start = std::chrono::system_clock::now();
	}
	virtual ~StopWatch() {
		std::chrono::system_clock::time_point end = std::chrono::system_clock::now();
		std::chrono::milliseconds total_time = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);
		std::cout << moduleName << " : " << total_time.count() << " ms" << std::endl;
	}
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
	static std::string GetFileExtension(const std::string path);
	static bool IsExist(std::string path, bool isFile);
};

