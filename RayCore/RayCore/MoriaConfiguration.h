#pragma once

#include "AlazarApi.h"
#include "ipp.h"
#include <opencv2/opencv.hpp>

class CMoriaConfiguration
{
	friend class CMoriaConfiguration;
	class SettingsOpenMP {
	public:
		int numThread;
		int numDynamic;
	};

	class SettingsAlazar {
	public:
		int nAcqBufferCount;
		bool bUserMoriaImaging;
		U32 msAtsTimeOut;
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
	static CMoriaConfiguration *pInstance;
	CMoriaConfiguration();
	
public:
	bool isInit;
	int nScans;
	int nScansPadding;
	int nAlines;
	int nBufferSize;
	int nLaserSpeed;
	int nDmaChannels;
	int nFftLength;
	int nCircleSize;
	int nAcqBufCount;
	double fTriggerDelay;
	// circularize map
	cv::Mat pXMap;
	cv::Mat pYMap;
	std::wstring patientFileRootPath;

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
	static CMoriaConfiguration *GetInstance();
	~CMoriaConfiguration(void);

	bool IsInit(){ return isInit; }
	void Initialize(int nScans=-1, int nScansPaddin=-1, int nAlines=-1);

	void SaveZaberSettings();
	void SaveMotorSettings();

	int getDmaXferSamples();
	int getDmaBufferSamples();
	int getScopeLength();

	//Property
	double GetLoadCatheterTimeProperty();

private:
	void initCircularizeMap();
	void releaseCircularizeMap();
};