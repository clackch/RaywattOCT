#include "OCTMeasurement.h"

COCTMeasurement::COCTMeasurement() {}
COCTMeasurement::~COCTMeasurement() {}

void COCTMeasurement::CalculateAxialResolution(USHORT* fftData, UINT nLength, Setting setting, USHORT& nPeakValue, int& nPeakIndex, int& nLineWidth) {
	const int nFindRange = 40;

	// get peak and index
	nPeakIndex = 0;
	nPeakValue = 0;
	for (int i = 0; i < nLength; i++) {
		if (fftData[i] > nPeakValue) {
			nPeakIndex = i;
			nPeakValue = fftData[i];
		}
	}
	unsigned short nFWHM = std::max(0, nPeakValue - 3000);;	// 3000 : 3db

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
	nEnd = (nEnd >= nLength) ? nLength - 1 : nEnd;
	for (int i = nPeakIndex; i <= nEnd; i++) {
		if (fftData[i] < nFWHM) {
			nRightIndex = i;
			break;
		}
	}

	double fLeftWidth = (double)(fftData[nLeftIndex] - nFWHM) / (double)(fftData[nLeftIndex] - fftData[nLeftIndex - 1]);
	double fRightWidth = (double)(fftData[nRightIndex] - nFWHM) / (double)(fftData[nRightIndex - 1] - fftData[nRightIndex]);

	nLineWidth = ((fRightWidth + nRightIndex) - (fLeftWidth + nLeftIndex)) * setting.fAxialResolutionScale;
}
void COCTMeasurement::CalculateNoisePower(USHORT* fftData, UINT nLength, Setting setting, int nPeakIndex, USHORT& nNoisePower) {
	int nStart, nEnd;
	unsigned int nSum = 0;
	int nCount = 0;

	nStart = nPeakIndex - setting.nNoiseSkip - setting.nNoiseAverage;
	nStart = (nStart < 0) ? 0 : nStart;
	nEnd = nPeakIndex - setting.nNoiseSkip;
	nEnd = (nEnd < 0) ? 0 : nEnd;

	for (int i = nStart; i <= nEnd; i++) {
		nSum += fftData[i];
		nCount++;
	}

	nStart = nPeakIndex + setting.nNoiseSkip;
	nStart = (nStart >= nLength) ? nLength - 1 : nStart;
	nEnd = nPeakIndex + setting.nNoiseSkip + setting.nNoiseAverage;
	nEnd = (nEnd >= nLength) ? nLength - 1 : nEnd;

	for (int i = nStart; i <= nEnd; i++) {
		nSum += fftData[i];
		nCount++;
	}

	if (nCount > 0) {
		nNoisePower = nSum / nCount;
	}
	else {
		nNoisePower = nSum;
	}
}

