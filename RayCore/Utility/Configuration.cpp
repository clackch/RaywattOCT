#include "Configuration.h"

#define _USE_MATH_DEFINES
#include <math.h>

CConfiguration::CConfiguration():
	isInit(false),
	nAScan(0),
	nAScanPadding(0),
	nBScan(0),
	nBufferSize(0)
{
}

CConfiguration::~CConfiguration() 
{
}

CConfiguration& CConfiguration::GetInstance() {
	static CConfiguration pInstance;
	return pInstance;
}

void CConfiguration::Initialize(tstring configFile)
{
	TCHAR sIniValueString[MAX_PATH] = _T("");

	configFilePath = configFile;

	// [Imaging]
	this->nAScan = ::GetPrivateProfileInt(_T("Imaging"), _T("AScan"), 1920, configFilePath.c_str());
	this->nAScanPadding = ::GetPrivateProfileInt(_T("Imaging"), _T("AScanPadding"), 0, configFilePath.c_str());
	this->nBScan = ::GetPrivateProfileInt(_T("Imaging"), _T("BScan"), 500, configFilePath.c_str());
	this->nLaserSpeed = ::GetPrivateProfileInt(_T("Imaging"), _T("LaserSpeed"), 200000, configFilePath.c_str());
	this->nCircleSize = ::GetPrivateProfileInt(_T("Imaging"), _T("CircleSize"), 1024, configFilePath.c_str());
	this->nBufferSize = (this->nBScan * (this->nAScan + this->nAScanPadding));
	this->nFFTOrder = 1;
	this->nFFTLength = 1 << this->nFFTOrder;
	while (this->nFFTLength < this->nAScan) { // AScan 보다 큰 2^n 중에서 제일 작은 수
		this->nFFTOrder++;
		this->nFFTLength = 1 << this->nFFTOrder;		
	}
	this->nOutputLength = this->nFFTLength / 2;

	// [Measurement]
	this->measurementValues.fAxialResolutionScale = getPrivateProfileFloat(_T("Measurement"), _T("AxialResolutionScale"), 8.3, configFilePath.c_str());
	this->measurementValues.nNoiseSkip = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseSkip"), 300, configFilePath.c_str());
	this->measurementValues.nNoiseAverage = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseAverage"), 100, configFilePath.c_str());
	this->measurementValues.fSheathRadius = getPrivateProfileFloat(_T("Measurement"), _T("SheathRadius"), 0.43, configFilePath.c_str());
	this->measurementValues.nSheathPosition = measurementValues.fSheathRadius * 1000.f / measurementValues.fAxialResolutionScale;

	// [OpenMP]
	this->settingsOpenMP.numThread = ::GetPrivateProfileInt(_T("OpenMP"), _T("NumThread"), 8, configFilePath.c_str());
	this->settingsOpenMP.numDynamic = ::GetPrivateProfileInt(_T("OpenMP"), _T("NumDynamic"), 1, configFilePath.c_str());

	// [Alazar]
	this->settingsAlazar.nAcqBufferCount = ::GetPrivateProfileInt(_T("Alazar"), _T("AcqBufferCount"), 4, configFilePath.c_str());
	this->settingsAlazar.msTimeOut = ::GetPrivateProfileInt(_T("Alazar"), _T("TimeOutInMilliSecond"), 5000, configFilePath.c_str());
	this->settingsAlazar.nTriggerDelaySample = ::GetPrivateProfileInt(_T("Alazar"), _T("TriggerDelaySample"), 0, configFilePath.c_str());
	this->settingsAlazar.bUseKClock = ::GetPrivateProfileInt(_T("Alazar"), _T("UseKClock"), 1, configFilePath.c_str());
	this->settingsAlazar.usGoodClockDuration = getPrivateProfileFloat(_T("Alazar"), _T("GoodClockInMicroSecond"), 5.0, configFilePath.c_str());
	this->settingsAlazar.usBadClockDuration = getPrivateProfileFloat(_T("Alazar"), _T("BadClockInMicroSecond"), 4.0, configFilePath.c_str());

	// [Invert]	
	this->invert.lowLevel = getPrivateProfileFloat(_T("Invert"), _T("LowLevel"), 40.0f, configFilePath.c_str());
	this->invert.highLevel = getPrivateProfileFloat(_T("Invert"), _T("HighLevel"), 65.0f, configFilePath.c_str());

	// [Patient]
	::GetPrivateProfileString(_T("Patient"), _T("RootPath"), _T("D:\\DataSave\\"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	this->patientFileRootPath = sIniValueString;
	_tmkdir(this->patientFileRootPath.c_str());

	// [Zaber]
	::GetPrivateProfileString(_T("Zaber"), _T("Pullback"), _T(""), this->zaber.pullback, sizeof(this->zaber.pullback), configFilePath.c_str());
	::GetPrivateProfileString(_T("Zaber"), _T("Interferometer"), _T(""), this->zaber.interferometer, sizeof(this->zaber.interferometer), configFilePath.c_str());
	this->zaber.pullbackDistance = ::GetPrivateProfileInt(_T("Zaber"), _T("PullbackDistance"), 10, configFilePath.c_str());
	this->zaber.pullbackSpeed = ::GetPrivateProfileInt(_T("Zaber"), _T("PullbackSpeed"), 10, configFilePath.c_str());

	// [Motor]
	this->motor.velocity = ::GetPrivateProfileInt(_T("Motor"), _T("Velocity"), 800, configFilePath.c_str());
	this->motor.settleDown = ::GetPrivateProfileInt(_T("Motor"), _T("SettleDown"), 1000, configFilePath.c_str());

	// [Shutter]
	this->shutterSerial = ::GetPrivateProfileInt(_T("Shutter"), _T("Serial"), 478, configFilePath.c_str());

	// [Catheter]
	this->catheter.position = ::GetPrivateProfileInt(_T("Catheter"), _T("Position"), 75, configFilePath.c_str());
	this->catheter.speed = ::GetPrivateProfileInt(_T("Catheter"), _T("Speed"), 5, configFilePath.c_str());
	this->catheter.velocity = ::GetPrivateProfileInt(_T("Catheter"), _T("MotorVelocity"), 50, configFilePath.c_str());
	this->catheter.rotationTime = ::GetPrivateProfileInt(_T("Catheter"), _T("RotationTime"), 10000, configFilePath.c_str());
	this->catheter.waitingTime = ::GetPrivateProfileInt(_T("Catheter"), _T("WaitingTime"), 10000, configFilePath.c_str());

	// [Volume]
	this->volume.size = ::GetPrivateProfileInt(_T("Volume"), _T("Size"), 600, configFilePath.c_str());
	this->volume.threshold = ::GetPrivateProfileInt(_T("Volume"), _T("Threshold"), 50, configFilePath.c_str());

	isInit = true;
}

void CConfiguration::SaveZaberSettings() {
	tstring strValue = _T("");

	strValue = std::to_wstring(this->zaber.pullbackDistance);
	::WritePrivateProfileString(_T("Zaber"), _T("PullbackDistance"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->zaber.pullbackSpeed);
	::WritePrivateProfileString(_T("Zaber"), _T("PullbackSpeed"), strValue.c_str(), configFilePath.c_str());
}
void CConfiguration::SaveMotorSettings() {
	tstring strValue = _T("");

	strValue = std::to_wstring(this->motor.velocity);
	::WritePrivateProfileString(_T("Motor"), _T("Velocity"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->motor.settleDown);
	::WritePrivateProfileString(_T("Motor"), _T("SettleDown"), strValue.c_str(), configFilePath.c_str());
}

int CConfiguration::getDmaXferSamples() {
	int nSamples = nBScan * (nAScan + nAScanPadding);
	return nSamples;
}

int CConfiguration::getDmaBufferSamples()
{
	return 2 * getDmaXferSamples();
}

int CConfiguration::getScopeLength()
{
	return nAScan + nAScanPadding;
}

double CConfiguration::getPrivateProfileFloat(LPCWSTR lpAppName, LPCWSTR lpKeyName, double fDefault, LPCWSTR lpFileName) 
{
	TCHAR sIniValueString[MAX_PATH] = _T("");
	TCHAR sDefaultString[MAX_PATH] = _T("");
	char converted[MAX_PATH];

	wsprintf(sDefaultString, _T("%lf"), fDefault);
	::GetPrivateProfileString(lpAppName, lpKeyName, sDefaultString, sIniValueString, sizeof(sIniValueString), lpFileName);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	
	return ::atof(converted);
}

double CConfiguration::GetLoadCatheterTime() {
	return (catheter.rotationTime + catheter.waitingTime) / 1000;
}