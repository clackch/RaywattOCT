#include "Utility.h"
#include <sys/timeb.h>
#include <time.h>

BOOL CUtility::StartThread(THREADPROC threadFunc, CThread*& pThread, LPVOID param) {
	if (pThread != nullptr)
		return FALSE;

	pThread = new CThread(threadFunc, param);

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

	std::lock_guard<std::mutex> lock(pThread->mMutex);
	pThread->shouldResume = true;

	pThread->sEvent.notify_one();
}
void CUtility::SuspendThread(CThread* pThread) {
	if (pThread == nullptr) return;

	std::unique_lock<std::mutex> lock(pThread->mMutex);
	pThread->sEvent.wait(lock, [&]() { return pThread->shouldResume; });
	pThread->shouldResume = false;
}


std::vector<tstring> CUtility::findSerialPort() {
	std::vector<tstring> vComPort;
	HKEY hKey;
	RegOpenKey(HKEY_LOCAL_MACHINE, TEXT("HARDWARE\\DEVICEMAP\\SERIALCOMM"), &hKey);

	wchar_t szData[20] = {};
	wchar_t szName[100] = {};
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

	if (now == nullptr) {
		PLOGI.printf("Getting localTime Error");
	}
	else {
		sprintf(strTime, "%d:%d:%d:%d", now->tm_hour, now->tm_min, now->tm_sec, msec);
	}
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
bool CUtility::IsExist(std::string path, bool isFile)
{
	struct _stat info;
	if (_stat(path.c_str(), &info) != 0) {
		return false; // Not exist
	}

	if (isFile) {
		return (info.st_mode & _S_IFREG) != 0;  // if it is a regular file, true.
	}
	else {
		return (info.st_mode & _S_IFDIR) != 0;  // if it is a directory, true.
	}
}
int CUtility::GetPrivateProfileIntEx(LPCWSTR lpAppName, LPCWSTR lpKeyName, int nDefault, LPCWSTR lpFileName)
{
	int value = GetPrivateProfileIntW(lpAppName, lpKeyName, nDefault, lpFileName);

	// 키가 없으면 GetPrivateProfileIntW는 그냥 nDefault 반환
	// 따라서 파일에 실제 기록이 되어 있는지 확인해야 함
	wchar_t buffer[256];
	DWORD len = GetPrivateProfileStringW(lpAppName, lpKeyName, L"", buffer, 256, lpFileName);

	if (len == 0) {
		// 키가 존재하지 않음 → 기본값을 ini에 기록
		wchar_t defaultStr[32];
		_snwprintf_s(defaultStr, 32, L"%d", nDefault);
		WritePrivateProfileStringW(lpAppName, lpKeyName, defaultStr, lpFileName);
	}

	return value;
}