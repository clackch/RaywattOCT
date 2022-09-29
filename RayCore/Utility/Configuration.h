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
		int nAcqBufferCount;
		bool bUserRayImaging;
		unsigned int msAtsTimeOut;
	};

	class Measurement {
	public:
		double fAxialResolutionScale;
		int nNoiseSkip;
		int nNoiseAverage;
	};

	class ConstantValues {
	public:
		const double Pi = 3.141592;
		const int Order = 11;
		const int Zoom = 2;
	};

	class Coloring {
	public:
		float R;
		float G;
		float B;
	};

	class Invert {
	public:
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
		int velocity;
		int settleDown;
	};

	class Catheter {
	public:
		int position;		// load catheter position
		int speed;			// move to position with speed
		int velocity;		// motor rotation velocity
		int rotationTime;
		int waitingTime;
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
	int nFftLength;
	int nCircleSize;
	int nAcqBufCount;
	int nTriggerDelaySample;
	tstring configFilePath;
	tstring patientFileRootPath;

	SettingsOpenMP settingsOpenMP;
	SettingsAlazar settingsAlazar;
	Measurement measurementValues;
	ConstantValues constantValues;
	Coloring coloring;
	Invert invert;
	Zaber zaber;
	Motor motor;
	Catheter catheter;
	int shutterSerial;
public:
	static CConfiguration& GetInstance();

	bool IsInit(){ return isInit; }
	void Initialize(tstring configFile);

	void SaveZaberSettings();
	void SaveMotorSettings();

	int getDmaXferSamples();
	int getDmaBufferSamples();
	int getScopeLength();

	//Property
	double GetLoadCatheterTime();
};