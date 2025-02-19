#include "Utility.h"
#include <sys/timeb.h>
#include <time.h>

BOOL CUtility::StartThread(THREADPROC threadFunc, CThread*& pThread, LPVOID param) {

	PLOGI.printf("001");
	if (pThread != nullptr)
		return FALSE;

	PLOGI.printf("002");
	pThread = new CThread(threadFunc, param);
	PLOGI.printf("003");

	if (pThread) {
		return TRUE;
	}
	else {
		return FALSE;
	}
}

void CUtility::StopThread(CThread *&pThread) {
	if (pThread == nullptr) return;

	pThread->isRun = false;

	ResumeThread(pThread);
	pThread->thread.join();
	delete pThread;
	pThread = nullptr;
}
void CUtility::ResumeThread(CThread* pThread) {
	if (pThread == nullptr) return;

	pThread->sEvent.notify_one();
}
void CUtility::SuspendThread(CThread* pThread) {
	if (pThread == nullptr) return;

	{
		std::unique_lock<std::mutex> lock(pThread->mMutex);
		pThread->sEvent.wait(lock);
	}
}

std::vector<tstring> CUtility::findSerialPort() {
	std::vector<tstring> vComPort;
	HKEY hKey;
	RegOpenKey(HKEY_LOCAL_MACHINE, TEXT("HARDWARE\\DEVICEMAP\\SERIALCOMM"), &hKey);

	wchar_t szData[20];
	wchar_t szName[100];
	DWORD dwSize = 100;
	DWORD dwSize2 = 20;
	DWORD dwType = REG_SZ;

	int index = 0;
	while (RegEnumValue(hKey, index, szName, &dwSize, NULL, NULL, NULL, NULL) == ERROR_SUCCESS) {
		index++;
		RegQueryValueEx(hKey, szName, 0, &dwType, (LPBYTE)szData, &dwSize2);
		vComPort.push_back(szData);
		memset(szData, 0x00, sizeof(wchar_t) * 20);
		memset(szName, 0x00, sizeof(wchar_t) * 100);
		dwSize = 100;
		dwSize2 = 20;
	}

	return vComPort;
}
void CUtility::GetCurTime(char* strTime) {
	struct timeb timebuffer;
	struct tm* now;
	time_t ltime;
	int msec;

	ftime(&timebuffer);
	ltime = timebuffer.time;
	msec = timebuffer.millitm;
	now = localtime(&ltime);
	sprintf(strTime, "%d:%d:%d:%d", now->tm_hour, now->tm_min, now->tm_sec, msec);
}
std::wstring CUtility::StringToWstring(const std::string& var)
{
	static std::locale loc("");
	auto& facet = std::use_facet<std::codecvt<wchar_t, char, std::mbstate_t>>(loc);
	return std::wstring_convert<std::remove_reference<decltype(facet)>::type, wchar_t>(&facet).from_bytes(var);
}
std::string CUtility::GetFileExtension(const std::string path)
{
	size_t offset = path.find_last_of('.');
	return path.substr(offset + 1, path.length() - offset - 1);
}