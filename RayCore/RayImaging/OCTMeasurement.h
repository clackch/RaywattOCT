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
		double fSheathRadiusOnePointSix;	// mm
		double fSheathRadiusTwoPointSix;	// mm
		double fSheathThicknessOnePointSix;	// mm
		double fSheathThicknessTwoPointSix;	// mm
		double fSheathRadius;	// mm
		double fSheathThickness;	// mm

		int nSheathPosition;	// pixel
		int nSheathThickness;	// pixel
	};
public:
	COCTMeasurement();
	virtual ~COCTMeasurement();

	void CalculateAxialResolution(USHORT* fftData, UINT nLength, Setting setting, USHORT& nPeakValue, int& nPeakIndex, int& nLineWidth);
	void CalculateNoisePower(USHORT* fftData, UINT nLength, Setting setting, int nPeakIndex, USHORT& nNoisePower);
};

