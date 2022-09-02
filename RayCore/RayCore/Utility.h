#pragma once
#include <vector>
#include <thread>
#include <mutex>
#include <locale>
#include <opencv2/opencv.hpp>

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
	double start = (double)cv::getTickCount();

public:
	StopWatch(std::string moduleName) {
		this->moduleName = moduleName;
		this->start = (double)cv::getTickCount();
	}
	virtual ~StopWatch() {
		double total_time = ((double)cv::getTickCount() - start) / cv::getTickFrequency();
		std::cout << moduleName << " : " << total_time * 1000 << " ms" << std::endl;
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
};

