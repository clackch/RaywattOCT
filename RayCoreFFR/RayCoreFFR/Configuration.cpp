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

void CConfiguration::SetPath(tstring configPath)
{
	this->configPath = configPath;
}

void CConfiguration::Initialize(tstring configFile)
{
	TCHAR sIniValueString[MAX_PATH] = _T("");

	configFilePath = configFile;

	// [Measurement]
	this->measurement.fAxialResolutionScale = getPrivateProfileFloat(_T("Measurement"), _T("AxialResolutionScale"), 8.3, configFilePath.c_str());
	this->measurement.nNoiseSkip = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseSkip"), 300, configFilePath.c_str());
	this->measurement.nNoiseAverage = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseAverage"), 100, configFilePath.c_str());
	
	this->measurement.fSheathRadiusOnePointSeven = getPrivateProfileFloat(_T("Measurement"), _T("SheathRadius1.7"), 0.28, configFilePath.c_str());
	this->measurement.fSheathRadiusTwoPointSix = getPrivateProfileFloat(_T("Measurement"), _T("SheathRadius2.6"), 0.43, configFilePath.c_str());	
	this->measurement.fSheathThicknessOnePointSeven = getPrivateProfileFloat(_T("Measurement"), _T("SheathThickness1.7"), 0.045, configFilePath.c_str());
	this->measurement.fSheathThicknessTwoPointSix = getPrivateProfileFloat(_T("Measurement"), _T("SheathThickness2.6"), 0.1, configFilePath.c_str());	
	this->measurement.fSheathRadius = getPrivateProfileFloat(_T("Measurement"), _T("SheathRadius"), 0.43, configFilePath.c_str());
	if (measurement.fAxialResolutionScale > 0) {
		this->measurement.nSheathPosition = measurement.fSheathRadius * 1000.f / measurement.fAxialResolutionScale;
	}
	
	// [Imaging]
	int nAScan = ::GetPrivateProfileInt(_T("Imaging"), _T("AScan"), 1920, configFilePath.c_str());
	int nBScan = ::GetPrivateProfileInt(_T("Imaging"), _T("BScan"), 500, configFilePath.c_str());
	this->imaging.Set(nAScan, nBScan);
	this->imaging.lowLevel = getPrivateProfileFloat(_T("Imaging"), _T("LowLevel"), 40.0f, configFilePath.c_str());
	this->imaging.highLevel = getPrivateProfileFloat(_T("Imaging"), _T("HighLevel"), 65.0f, configFilePath.c_str());
	this->imaging.brightness = getPrivateProfileFloat(_T("Imaging"), _T("Brightness"), 0.f, configFilePath.c_str());
	this->imaging.contrast = getPrivateProfileFloat(_T("Imaging"), _T("Contrast"), 0.875f, configFilePath.c_str());
	this->imaging.distPerPixel = measurement.fAxialResolutionScale;

	// [Log]
	::GetPrivateProfileString(_T("Log"), _T("LogRootPath"), _T(""), this->logRootPath, sizeof(this->logRootPath), configFilePath.c_str());

	// [Compensation]
	this->imaging.applyCompensation = ::GetPrivateProfileInt(_T("Compensation"), _T("ApplyCompensation"), 0, configFilePath.c_str());
	this->imaging.exponentialFactor = getPrivateProfileFloat(_T("Compensation"), _T("ExponentialFactor"), 1.8f, configFilePath.c_str());
	this->imaging.brightnessControl = getPrivateProfileFloat(_T("Compensation"), _T("BrightnessControl"), 0.7f, configFilePath.c_str());
	this->imaging.energyThreshold = getPrivateProfileFloat(_T("Compensation"), _T("EnergyThreshold"), 0.7f, configFilePath.c_str());
	this->imaging.applyGammaCorrection = ::GetPrivateProfileInt(_T("Compensation"), _T("ApplyGammaCorrection"), 0, configFilePath.c_str());
	this->imaging.GCAlpha = getPrivateProfileFloat(_T("Compensation"), _T("GCAlpha"), 0.4f, configFilePath.c_str());
	this->imaging.intensityThreshold = ::GetPrivateProfileInt(_T("Compensation"), _T("IntensityThreshold"), 255, configFilePath.c_str());

	// [Sharpness]
	this->imaging.applySharpness = ::GetPrivateProfileInt(_T("Sharpness"), _T("ApplySharpness"), 0, configFilePath.c_str());

	isInit = true;
}

void CConfiguration::SaveLaserModuleSettings() {
	tstring strValue = _T("");

	strValue = std::to_wstring(this->laserModule.voaValue);
	::WritePrivateProfileString(_T("LaserModule"), _T("VOA"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->laserModule.vldValue);
	::WritePrivateProfileString(_T("LaserModule"), _T("VLD"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->laserModule.delayPosition);
	::WritePrivateProfileString(_T("LaserModule"), _T("DelayLine"), strValue.c_str(), configFilePath.c_str());

	strValue = std::to_wstring(this->laserModule.polarPosition);
	::WritePrivateProfileString(_T("LaserModule"), _T("Polarization"), strValue.c_str(), configFilePath.c_str());
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

	strValue = std::to_wstring(this->bldcMotor.velocityLoad);
	::WritePrivateProfileString(_T("BLDCMotor"), _T("VelocityLoad"), strValue.c_str(), configFilePath.c_str());

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