#pragma once
#include "OCTImaging.h"

class CLabImaging : public COCTImaging
{
private:
	USHORT* backgroundData;
	float* backgroundFFT;
	float* backgroundSubtracted;
	float* logData;

	cv::Mat imageRectangle;
	USHORT* scopeData;
	USHORT* scopeFFTData;
	
	bool subtract;

	CCalibration* newCalibration;
	bool hasNewCalibration;

	int goodClockStart;
	int goodClockEnd;

public:
	CLabImaging(Setting, CMessageService*);
	virtual ~CLabImaging();

	virtual void Initialize(CCalibration* calibration, USHORT* backgroundData);
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);

	cv::Mat GetRectangleImage() { return imageRectangle; }
	USHORT* GetScopeData() { return scopeData; }
	USHORT* GetScopeFFTData() { return scopeFFTData; }

	void SetBackgroundSubtract(bool subtract) { this->subtract = subtract; }
	void ChangeCalibration(CCalibration* pNewCalib);
	void SetGoodClockRange(int start, int end) { goodClockStart = start; goodClockEnd = end; }

	USHORT* GetBackground() { return backgroundData; }

private:
	template <typename T>
	void subtractBackground(T* fringes, T* background, T* dst, int size);
	void generateScopeData(Ipp32f* output, Ipp16u* scope);
	void cropSignalData(USHORT* fringes, int start, int end);
};

