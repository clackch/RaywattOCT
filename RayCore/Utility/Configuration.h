#pragma once

#include "Config.h"

class CConfiguration
{
	friend class CConfiguration;
	class SettingsOpenMP {
	public:
		int numThread;
		int numDynamic;
	};

	class SettingsAlazar {
	public:
		unsigned int nAcqBufferCount;
		unsigned int msTimeOut;
		unsigned int nTriggerDelaySample;
		bool bUseKClock;
		double usGoodClockDuration;
		double usBadClockDuration;
		bool bUseDES;
	};

	class Measurement {
	public:
		double fAxialResolutionScale;	// um per pixel
		int nNoiseSkip;
		int nNoiseAverage;
		double fSheathRadius;	// mm
		int nSheathPosition;	// pixel
	};

	class Imaging {
	public:
		float brightness;
		float contrast;
		float lowLevel;
		float highLevel;
	};

	class Zaber {
	public:
		TCHAR pullback[MAX_PATH];
		TCHAR interferometer[MAX_PATH];
		int pullbackDistance;
		int pullbackSpeed;
	};

	class Motor {
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
	int nAScan;		// Axial Scan (z-Depth)
	int nAScanPadding;
	int nBScan;		// Number of A-Scan (Transverse)
	int nBufferSize;
	int nLaserSpeed;
	int nCircleSize;
	int nFFTOrder;
	int nFFTLength;
	int nOutputLength;
	tstring configFilePath;
	tstring patientFileRootPath;

	SettingsOpenMP settingsOpenMP;
	SettingsAlazar settingsAlazar;
	Measurement measurementValues;
	Imaging imaging;
	Zaber zaber;
	Motor motor;
	Catheter catheter;
	Volume volume;
	int shutterSerial;
public:
	static CConfiguration& GetInstance();

	bool IsInit(){ return isInit; }
	void Initialize(tstring configFile);

	void SaveZaberSettings();
	void SaveMotorSettings();

	int getScopeLength();

	double getPrivateProfileFloat(LPCWSTR lpAppName, LPCWSTR lpKeyName, double fDefault, LPCWSTR lpFileName);

	//Property
	double GetLoadCatheterTime();
};