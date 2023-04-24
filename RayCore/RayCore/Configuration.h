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
		TCHAR pullback[MAX_PATH];
		TCHAR delayline[MAX_PATH];
		int pullbackDistance;
		int pullbackSpeed;
	};

	class BLDCMotorSetting {
	public:
		int velocityPullback;
		int velocityLiveView;
		int settleDown;
	};

	class Catheter {
	public:
		int position;		// load catheter position
		int speed;			// move to position with speed
		int velocity;		// motor rotation velocity
		int rotationTime;
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
	Catheter catheter;
	Volume volume;
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