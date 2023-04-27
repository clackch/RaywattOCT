#pragma once
#include <ipp.h>
#include "Config.h"

enum Windows { None, Hanning, Hamming, Gauss1 };

typedef struct {
	float  re;
	float  im;
}complex_t;

class CCalibration
{	
private:
	int nAScan;
	int nFFTLength;

public:
	// from calibration file
	int *indexMap;
	float *weightMap;
	complex_t *dispersion;
	
	float *window;
public:
	CCalibration();
	~CCalibration(void);

	bool Initialize(tstring calibFile, int nAScan, int nFFTLength);
private:
	void allocateMemory();
	void releaseMemory();

	bool loadCalibration(LPCTSTR calibrationFileName);
	void setWindow(enum Windows eWindow);
};