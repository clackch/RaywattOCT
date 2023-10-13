#pragma once

#include "Config.h"
#include "Imaging.h"
#include "AcquisitionDevice.h"
#include "OCTMeasurement.h"

class CConfiguration
{
public:
	class StepMotorSetting {
	public:
		TCHAR rotaryJunction[MAX_PATH];
		TCHAR delayline[MAX_PATH];
		int pullbackDistance;
		int pullbackSpeed;
		int pullbackStart;	// pullback start position
	};

	class BLDCMotorSetting {
	public:
		TCHAR port[MAX_PATH];
		int velocityPullback;
		int velocityLiveView;
		int velocityHoming;
		int settleDown;
	};

	class CatheterSetting {
	public:
		int rotationTime;	// To-Do: remove
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

	IAcquisitionDevice::Setting acquisition;
	IImaging::Setting imaging;
	COCTMeasurement::Setting measurement;
	StepMotorSetting stepMotor;
	BLDCMotorSetting bldcMotor;
	CatheterSetting catheter;
	Volume volume;
	TCHAR logRootPath[MAX_PATH];
	int shutterSerial;
public:
	static CConfiguration& GetInstance();

	bool IsInit(){ return isInit; }
	void Initialize(tstring configFile);

	void SaveStepMotorSettings();
	void SaveBLDCMotorSettings();

	//Property
	double GetLoadCatheterTime();

private:
	double getPrivateProfileFloat(LPCWSTR lpAppName, LPCWSTR lpKeyName, double fDefault, LPCWSTR lpFileName);
};