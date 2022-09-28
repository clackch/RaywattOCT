#pragma once
#include <ipp.h>

enum Windows { None, Hanning, Hamming, Gauss1 };

typedef struct {
	float  re;
	float  im;
}complex_t;

class CCalibration
{	
public:
	// from calibration file
	int *indexMap;
	float *weightMap;
	complex_t *dispersion;
	
	float *window;
	float lowLevel;
	float highLevel;
public:
	CCalibration();
	~CCalibration(void);

	bool Initialize();
private:
	void allocateMemory();
	void releaseMemory();

	bool loadCalibration(LPCTSTR calibrationFileName);
	void setWindow(enum Windows eWindow);
};