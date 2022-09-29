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
	configFilePath = configFile;

	this->nAScan = ::GetPrivateProfileInt(_T("Imaging"), _T("AScan"), 1920, configFilePath.c_str());
	this->nAScanPadding = ::GetPrivateProfileInt(_T("Imaging"), _T("AScanPadding"), 0, configFilePath.c_str());
	this->nBScan = ::GetPrivateProfileInt(_T("Imaging"), _T("BScan"), 500, configFilePath.c_str());

	TCHAR sIniValueString[2048] = _T("");
	int nIniValueInt = -1;
	char converted[2048];

	this->nBufferSize = (this->nBScan * (this->nAScan + this->nAScanPadding));
	this->nAcqBufCount = ::GetPrivateProfileInt(_T("Alazar"), _T("AcqBufferCount"), 4, configFilePath.c_str());
	this->nTriggerDelaySample = ::GetPrivateProfileInt(_T("Alazar"), _T("TriggerDelaySample"), 0, configFilePath.c_str());

	::GetPrivateProfileString(_T("Patient"), _T("RootPath"), _T("D:\\DataSave\\"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	this->patientFileRootPath = sIniValueString;
	_tmkdir(this->patientFileRootPath.c_str());

	this->nFftLength = ::GetPrivateProfileInt(_T("Imaging"), _T("FFTLength"), 1024, configFilePath.c_str());
	this->nLaserSpeed = ::GetPrivateProfileInt(_T("Imaging"), _T("LaserSpeed"), 200000, configFilePath.c_str());
	this->nCircleSize = ::GetPrivateProfileInt(_T("Imaging"), _T("CircleSize"), 1024, configFilePath.c_str());

	::GetPrivateProfileString(_T("Measurement"), _T("AxialResolutionScale"), _T("8.3"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->measurementValues.fAxialResolutionScale = ::atof(converted);
	this->measurementValues.nNoiseSkip = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseSkip"), 300, configFilePath.c_str());
	this->measurementValues.nNoiseAverage = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseAverage"), 100, configFilePath.c_str());

	this->settingsOpenMP.numThread = ::GetPrivateProfileInt(_T("OpenMP"), _T("NumThread"), 8, configFilePath.c_str());
	this->settingsOpenMP.numDynamic = ::GetPrivateProfileInt(_T("OpenMP"), _T("NumDynamic"), 1, configFilePath.c_str());

	this->settingsAlazar.nAcqBufferCount = ::GetPrivateProfileInt(_T("Alazar"), _T("AcqBufferCount"), 4, configFilePath.c_str());
	this->settingsAlazar.bUserRayImaging = ::GetPrivateProfileInt(_T("Alazar"), _T("UseRayImaging"), 1, configFilePath.c_str());
	this->settingsAlazar.msAtsTimeOut = ::GetPrivateProfileInt(_T("Alazar"), _T("TimeOutInMilliSecond"), 5000, configFilePath.c_str());

	::GetPrivateProfileString(_T("Coloring"), _T("R"), _T("255.f"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->coloring.R = ::atof(converted);
	::GetPrivateProfileString(_T("Coloring"), _T("G"), _T("255.f"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->coloring.G = ::atof(converted);
	::GetPrivateProfileString(_T("Coloring"), _T("B"), _T("255.f"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->coloring.B = ::atof(converted);

	::GetPrivateProfileString(_T("Invert"), _T("LowLevel"), _T("40.0f"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->invert.lowLevel = ::atof(converted);
	::GetPrivateProfileString(_T("Invert"), _T("HighLevel"), _T("65.0f"), sIniValueString, sizeof(sIniValueString), configFilePath.c_str());
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->invert.highLevel = ::atof(converted);

	::GetPrivateProfileString(_T("Zaber"), _T("Pullback"), _T(""), this->zaber.pullback, sizeof(this->zaber.pullback), configFilePath.c_str());
	::GetPrivateProfileString(_T("Zaber"), _T("Interferometer"), _T(""), this->zaber.interferometer, sizeof(this->zaber.interferometer), configFilePath.c_str());
	this->zaber.pullbackDistance = ::GetPrivateProfileInt(_T("Zaber"), _T("PullbackDistance"), 10, configFilePath.c_str());
	this->zaber.pullbackSpeed = ::GetPrivateProfileInt(_T("Zaber"), _T("PullbackSpeed"), 10, configFilePath.c_str());

	this->motor.velocity = ::GetPrivateProfileInt(_T("Motor"), _T("Velocity"), 800, configFilePath.c_str());
	this->motor.settleDown = ::GetPrivateProfileInt(_T("Motor"), _T("SettleDown"), 1000, configFilePath.c_str());

	this->shutterSerial = ::GetPrivateProfileInt(_T("Shutter"), _T("Serial"), 478, configFilePath.c_str());

	this->catheter.position = ::GetPrivateProfileInt(_T("Catheter"), _T("Position"), 75, configFilePath.c_str());
	this->catheter.speed = ::GetPrivateProfileInt(_T("Catheter"), _T("Speed"), 5, configFilePath.c_str());
	this->catheter.velocity = ::GetPrivateProfileInt(_T("Catheter"), _T("MotorVelocity"), 50, configFilePath.c_str());
	this->catheter.rotationTime = ::GetPrivateProfileInt(_T("Catheter"), _T("RotationTime"), 10000, configFilePath.c_str());
	this->catheter.waitingTime = ::GetPrivateProfileInt(_T("Catheter"), _T("WaitingTime"), 10000, configFilePath.c_str());

	isInit = true;
}

void CConfiguration::SaveZaberSettings() {
	tstring strValue = _T("");

	strValue = this->zaber.pullbackDistance;
	::WritePrivateProfileString(_T("Zaber"), _T("PullbackDistance"), strValue.c_str(), configFilePath.c_str());

	strValue = this->zaber.pullbackSpeed;
	::WritePrivateProfileString(_T("Zaber"), _T("PullbackSpeed"), strValue.c_str(), configFilePath.c_str());
}
void CConfiguration::SaveMotorSettings() {
	tstring strValue = _T("");

	strValue = this->motor.velocity;
	::WritePrivateProfileString(_T("Motor"), _T("Velocity"), strValue.c_str(), configFilePath.c_str());

	strValue = this->motor.settleDown;
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

double CConfiguration::GetLoadCatheterTime() {
	return (catheter.rotationTime + catheter.waitingTime) / 1000;
}