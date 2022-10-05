#include "OCTMeasurement.h"
#include "Configuration.h"

COCTMeasurement::COCTMeasurement() {}
COCTMeasurement::~COCTMeasurement() {}

void COCTMeasurement::CalculateAxialResolution(USHORT* fftData, USHORT& nPeakValue, int& nPeakIndex, int& nLineWidth) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nOutputLength = config.nOutputLength;
	const float fScaleFactor = config.measurementValues.fAxialResolutionScale;
	const int nFindRange = 40;

	// get peak and index
	nPeakIndex = 0;
	nPeakValue = 0;
	for (int i = 0; i < nOutputLength; i++) {
		if (fftData[i] > nPeakValue) {
			nPeakIndex = i;
			nPeakValue = fftData[i];
		}
	}
	unsigned short nFWHM = nPeakValue - 3000;	// 3000 : 3db

	// find left 3db
	int nLeftIndex = 0;
	int nStart = nPeakIndex - nFindRange;
	nStart = (nStart < 0) ? 0 : nStart;
	for (int i = nStart; i <= nPeakIndex; i++) {
		if (fftData[i] > nFWHM) {
			nLeftIndex = i;
			break;
		}
	}

	// find right 3db
	int nRightIndex = 0;
	int nEnd = nPeakIndex + nFindRange;
	nEnd = (nEnd >= nOutputLength) ? nOutputLength - 1 : nEnd;
	for (int i = nPeakIndex; i <= nEnd; i++) {
		if (fftData[i] < nFWHM) {
			nRightIndex = i;
			break;
		}
	}

	double fLeftWidth = (double)(fftData[nLeftIndex] - nFWHM) / (double)(fftData[nLeftIndex] - fftData[nLeftIndex - 1]);
	double fRightWidth = (double)(fftData[nRightIndex] - nFWHM) / (double)(fftData[nRightIndex - 1] - fftData[nRightIndex]);

	nLineWidth = ((fRightWidth + nRightIndex) - (fLeftWidth + nLeftIndex)) * fScaleFactor;
}
void COCTMeasurement::CalculateNoisePower(USHORT* fftData, int nPeakIndex, USHORT& nNoisePower) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nOutputLength = config.nOutputLength;
	const int nNoiseSkip = config.measurementValues.nNoiseSkip;
	const int nNoiseAverage = config.measurementValues.nNoiseAverage;

	int nStart, nEnd;
	unsigned int nSum = 0;
	int nCount = 0;

	nStart = nPeakIndex - nNoiseSkip - nNoiseAverage;
	nStart = (nStart < 0) ? 0 : nStart;
	nEnd = nPeakIndex - nNoiseSkip;
	nEnd = (nEnd < 0) ? 0 : nEnd;

	for (int i = nStart; i <= nEnd; i++) {
		nSum += fftData[i];
		nCount++;
	}

	nStart = nPeakIndex + nNoiseSkip;
	nStart = (nStart >= nOutputLength) ? nOutputLength - 1 : nStart;
	nEnd = nPeakIndex + nNoiseSkip + nNoiseAverage;
	nEnd = (nEnd >= nOutputLength) ? nOutputLength - 1 : nEnd;

	for (int i = nStart; i <= nEnd; i++) {
		nSum += fftData[i];
		nCount++;
	}

	nNoisePower = nSum / nCount;
}

