#pragma once
#include "Config.h"
#include <mutex>
#include <vector>

#define WM_PROCESS_CROSSSECTION	(WM_USER + 0x0001)
#define WM_PROCESS_CUTVIEW		(WM_USER + 0x0002)
#define WM_PROCESS_DETECTION	(WM_USER + 0x0003)

#define WM_UPDATE_SCANNER_STATE		(WM_USER + 0x1001)
#define WM_UPDATE_SAVE_RAW			(WM_USER + 0x1002)
#define WM_NOTIFY_PROCESS_DONE		(WM_USER + 0x1003)
#define WM_NOTIFY_EVENT_OCCURED		(WM_USER + 0x1004)
#define WM_NOTIFY_DEVICE_WORK_DONE	(WM_USER + 0x1005)
#define WM_NOTIFY_ERROR_OCCURED		(WM_USER + 0x1006)
#define WM_UPDATE_CATHETER_STATE	(WM_USER + 0x1007)
#define WM_START_REVIEW_SESSION		(WM_USER + 0x1008)

class CMessageService
{
private:
	std::mutex m_mutexQueue;
	std::vector<std::tuple<int, WPARAM, LPARAM>> m_vMessageQueue;
	std::vector<std::tuple<int, WPARAM, LPARAM>> m_vPriorMessageQueue;

public:
	CMessageService() {};
	~CMessageService() {};

	void postPriorMessage(int, WPARAM wParam = 0, LPARAM lParam = 0);
	void postMessage(int, WPARAM wParam = 0, LPARAM lParam = 0);
	std::tuple<int, WPARAM, LPARAM> popMessage();

};

