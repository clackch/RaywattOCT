#pragma once
#include "Config.h"

class COCTMeasurement
{
public:
	class Setting {
	public:
		double fAxialResolutionScale;	// um per pixel
		int nNoiseSkip;
		int nNoiseAverage;
		double fSheathRadius;	// mm
		int nSheathPosition;	// pixel
	};
public:
	COCTMeasurement();
	virtual ~COCTMeasurement();

	void CalculateAxialResolution(USHORT* fftData, UINT nLength, Setting setting, USHORT& nPeakValue, int& nPeakIndex, int& nLineWidth);
	void CalculateNoisePower(USHORT* fftData, UINT nLength, Setting setting, int nPeakIndex, USHORT& nNoisePower);
};

