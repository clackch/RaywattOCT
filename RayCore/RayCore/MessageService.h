#pragma once
#include <mutex>
#include <vector>

class CMessageService
{
private:
	std::mutex m_mutexQueue;
	std::vector<std::tuple<int, WPARAM, LPARAM>> m_vMessageQueue;

public:
	CMessageService() {};
	~CMessageService() {};

	void postMessage(int, WPARAM, LPARAM);
	std::tuple<int, WPARAM, LPARAM> popMessage();

};

