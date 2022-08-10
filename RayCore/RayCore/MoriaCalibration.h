#pragma once
#include <ipp.h>

enum Windows { None, Hanning, Hamming, Gauss1 };

class CMoriaCalibration
{	
public:
	// from calibration file
	Ipp32s *indexMap;
	Ipp32f *weightMap;
	Ipp32fc *dispersion;
	
	Ipp32f *window;
	Ipp32f lowLevel;
	Ipp32f highLevel;
public:
	CMoriaCalibration();
	~CMoriaCalibration(void);

	BOOL Initialize();
private:
	void allocateMemory();
	void releaseMemory();

	BOOL loadCalibration(LPCTSTR calibrationFileName);
	void setWindow(enum Windows eWindow);
};