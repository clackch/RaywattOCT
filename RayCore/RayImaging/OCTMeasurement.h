#pragma once
#include "Config.h"

class COCTMeasurement
{
public:
	COCTMeasurement();
	virtual ~COCTMeasurement();

	void CalculateAxialResolution(USHORT* fftData, USHORT& nPeakValue, int& nPeakIndex, int& nLineWidth);
	void CalculateNoisePower(USHORT* fftData, int nPeakIndex, USHORT& nNoisePower);
};

