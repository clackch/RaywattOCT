#pragma once

#include "Config.h"
#include "Imaging.h"
#include "AcquisitionDevice.h"
#include "OCTMeasurement.h"

class CConfiguration
{
public:
	class LaserModuleSetting {
	public:
		TCHAR port[MAX_PATH];
		int voaValue;
		int vldValue;
		int delayPosition;
		int delayPositionOnePointSeven;
		int polarPosition;
	};

	class StepMotorSetting {
	public:
		TCHAR port[MAX_PATH];
		int pullbackDistance;
		int pullbackSpeed;
		int noPullbackTime;
		int SMPullbackProfile;
		int unLoadDistance;
		int homingSpeed;
	};

	class BLDCMotorSetting {
	public:
		TCHAR port[MAX_PATH];
		int velocityPullback;
		int velocityLiveView;
		int velocityLoad;
		int settleDown;
	};

	class CatheterSetting {
	public:
		int rotationTime;	// To-Do: remove
		bool manualLoad;
		int length;			// 2.6fr -> 1.6fr
		bool catheterValidationOnOff;
		bool catheterAutoCalibrationOnOff;
		int  catheterUsage;
	};

	class Volume {
	public:
		int size;
		int threshold;
	};

private:
	CConfiguration();
	CConfiguration(const CConfiguration& ref) {};
	CConfiguration& operator=(const CConfiguration& ref) {};
	~CConfiguration();
	
public:
	bool isInit;
	tstring configFilePath;
	tstring configPath;

	IAcquisitionDevice::Setting acquisition;
	IImaging::Setting imaging;
	COCTMeasurement::Setting measurement;
	LaserModuleSetting laserModule;
	StepMotorSetting stepMotor;
	BLDCMotorSetting bldcMotor;
	CatheterSetting catheter;
	Volume volume;
	TCHAR logRootPath[MAX_PATH];
	int shutterSerial;
public:
	static CConfiguration& GetInstance();
	
	void SetPath(tstring configPath);
	bool IsInit(){ return isInit; }
	void Initialize(tstring configFile);

	void SaveLaserModuleSettings();
	void SaveStepMotorSettings();
	void SaveBLDCMotorSettings();

	//Property
	double GetLoadCatheterTime();

private:
	double getPrivateProfileFloat(LPCWSTR lpAppName, LPCWSTR lpKeyName, double fDefault, LPCWSTR lpFileName);
};