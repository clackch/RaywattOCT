#pragma once
#include <ipp.h>
#include "Config.h"

enum Windows { None, Hanning, Hamming, Gauss1 };

class CCalibration
{	
private:
	int nAScan;
	int nFFTLength;

public:
	// from calibration file
	char* data;
	int *indexMap;
	float *weightMap;
	float *dispersion;
	bool isLoaded;
	
	float *window;
	float *kWindow;
public:
	CCalibration(int nAScan, int nFFTLength);
	~CCalibration(void);

	bool Initialize(tstring calibFile);
	bool Initialize(char* data);
private:
	void allocateMemory();
	void releaseMemory();

	bool loadCalibration();
	bool readCalibration(LPCTSTR calibrationFileName);
	void setWindow(enum Windows eWindow);
};