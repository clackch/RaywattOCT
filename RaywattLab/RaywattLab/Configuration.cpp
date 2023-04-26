#include "pch.h"
#include "Configuration.h"
#define _USE_MATH_DEFINES
#include <math.h>

CConfiguration::CConfiguration():
	isInit(false)
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
	int nAScan = ::GetPrivateProfileInt(_T("Imaging"), _T("AScan"), 1920, configFilePath.c_str());
	int nBScan = ::GetPrivateProfileInt(_T("Imaging"), _T("BScan"), 500, configFilePath.c_str());
	this->imaging.Set(nAScan, nBScan);
	this->imaging.lowLevel = getPrivateProfileFloat(_T("Imaging"), _T("LowLevel"), 40.0f, configFilePath.c_str());
	this->imaging.highLevel = getPrivateProfileFloat(_T("Imaging"), _T("HighLevel"), 65.0f, configFilePath.c_str());
	this->imaging.brightness = getPrivateProfileFloat(_T("Imaging"), _T("Brightness"), 0.f, configFilePath.c_str());
	this->imaging.contrast = getPrivateProfileFloat(_T("Imaging"), _T("Contrast"), 0.875f, configFilePath.c_str());

	// [Measurement]
	this->measurement.fAxialResolutionScale = getPrivateProfileFloat(_T("Measurement"), _T("AxialResolutionScale"), 8.3, configFilePath.c_str());
	this->measurement.nNoiseSkip = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseSkip"), 300, configFilePath.c_str());
	this->measurement.nNoiseAverage = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseAverage"), 100, configFilePath.c_str());
	this->measurement.fSheathRadius = getPrivateProfileFloat(_T("Measurement"), _T("SheathRadius"), 0.43, configFilePath.c_str());
	this->measurement.nSheathPosition = measurement.fSheathRadius * 1000.f / measurement.fAxialResolutionScale;

	// [Acquisition]
	this->acquisition.nAScan = imaging.nAScan;
	this->acquisition.nBScan = imaging.nBScan;
	this->acquisition.nLaserSpeed = ::GetPrivateProfileInt(_T("Acquisition"), _T("LaserSpeed"), 200000, configFilePath.c_str());
	this->acquisition.nBufferCount = ::GetPrivateProfileInt(_T("Acquisition"), _T("BufferCount"), 4, configFilePath.c_str());
	this->acquisition.msTimeOut = ::GetPrivateProfileInt(_T("Acquisition"), _T("TimeOutInMilliSecond"), 5000, configFilePath.c_str());
	this->acquisition.nTriggerDelaySample = ::GetPrivateProfileInt(_T("Acquisition"), _T("TriggerDelaySample"), 0, configFilePath.c_str());
	this->acquisition.bUseKClock = ::GetPrivateProfileInt(_T("Acquisition"), _T("UseKClock"), 1, configFilePath.c_str());
	this->acquisition.usGoodClockDuration = getPrivateProfileFloat(_T("Acquisition"), _T("GoodClockInMicroSecond"), 5.0, configFilePath.c_str());
	this->acquisition.usBadClockDuration = getPrivateProfileFloat(_T("Acquisition"), _T("BadClockInMicroSecond"), 4.0, configFilePath.c_str());
	this->acquisition.bUseDES = ::GetPrivateProfileInt(_T("Acquisition"), _T("UseDES"), 0, configFilePath.c_str());

	// [StepMotor]
	::GetPrivateProfileString(_T("StepMotor"), _T("Pullback"), _T(""), this->stepMotor.pullback, sizeof(this->stepMotor.pullback), configFilePath.c_str());
	::GetPrivateProfileString(_T("StepMotor"), _T("DelayLine"), _T(""), this->stepMotor.delayline, sizeof(this->stepMotor.delayline), configFilePath.c_str());
	this->stepMotor.pullbackDistance = ::GetPrivateProfileInt(_T("StepMotor"), _T("PullbackDistance"), 10, configFilePath.c_str());
	this->stepMotor.pullbackSpeed = ::GetPrivateProfileInt(_T("StepMotor"), _T("PullbackSpeed"), 10, configFilePath.c_str());
	this->stepMotor.pullbackStart = ::GetPrivateProfileInt(_T("StepMotor"), _T("PullbackStart"), 0, configFilePath.c_str());

	// [Motor]
	this->bldcMotor.velocityPullback = ::GetPrivateProfileInt(_T("BLDCMotor"), _T("VelocityPullback"), 3005, configFilePath.c_str());
	this->bldcMotor.velocityLiveView = ::GetPrivateProfileInt(_T("BLDCMotor"), _T("VelocityLiveView"), 3005, configFilePath.c_str());
	this->bldcMotor.velocityHoming = ::GetPrivateProfileInt(_T("BLDCMotor"), _T("VelocityHoming"), 3005, configFilePath.c_str());
	this->bldcMotor.settleDown = ::GetPrivateProfileInt(_T("BLDCMotor"), _T("SettleDown"), 1000, configFilePath.c_str());

	// [Shutter]
	this->shutterSerial = ::GetPrivateProfileInt(_T("Shutter"), _T("Serial"), 478, configFilePath.c_str());

	// [Catheter]
	this->catheter.rotationTime = ::GetPrivateProfileInt(_T("Catheter"), _T("RotationTime"), 10000, configFilePath.c_str());

	// [Volume]
	this->volume.size = ::GetPrivateProfileInt(_T("Volume"), _T("Size"), 600, configFilePath.c_str());
	this->volume.threshold = ::GetPrivateProfileInt(_T("Volume"), _T("Threshold"), 50, configFilePath.c_str());

	isInit = true;
}

void CConfiguration::SaveStepMotorSettings() {
	tstring strValue = _T("");

	strValue = std::to_wstring(this->stepMotor.pullbackDistance);
	::WritePrivateProfileString(_T("StepMotor"), _T("PullbackDistance"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->stepMotor.pullbackSpeed);
	::WritePrivateProfileString(_T("StepMotor"), _T("PullbackSpeed"), strValue.c_str(), configFilePath.c_str());
}
void CConfiguration::SaveBLDCMotorSettings() {
	tstring strValue = _T("");

	strValue = std::to_wstring(this->bldcMotor.velocityPullback);
	::WritePrivateProfileString(_T("BLDCMotor"), _T("VelocityPullback"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->bldcMotor.velocityLiveView);
	::WritePrivateProfileString(_T("BLDCMotor"), _T("VelocityLiveView"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->bldcMotor.settleDown);
	::WritePrivateProfileString(_T("BLDCMotor"), _T("SettleDown"), strValue.c_str(), configFilePath.c_str());
}

double CConfiguration::GetLoadCatheterTime() {
	return catheter.rotationTime;
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