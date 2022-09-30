#pragma once
#include "OCTImaging.h"

class CLabImaging : public COCTImaging
{
private:
	USHORT* fringesSubtracted;
	USHORT* backgroundData;
	float* backgroundFFT;

	cv::Mat imageRectangle;
	USHORT* scopeData;
	USHORT* scopeFFTData;
	
	bool subtract;
	bool subtractFFT;

	CCalibration* newCalibration;
	bool hasNewCalibration;

public:
	CLabImaging(CMessageService*);
	virtual ~CLabImaging();

	virtual void Initialize(tstring calibFile, const char* strBgFile);
	virtual void Process(USHORT* fringes);

	cv::Mat GetRectangleImage() { return imageRectangle; }
	USHORT* GetScopeData() { return scopeData; }
	USHORT* GetScopeFFTData() { return scopeFFTData; }

	void SetBackgroundSubtract(bool subtract) { this->subtract = subtract; }
	void SetBackgroundFFTSubtract(bool subtract) { this->subtractFFT = subtract; }
	void ChangeCalibration(CCalibration* pNewCalib);

private:
	template <typename T>
	void subtractBackground(T* fringes, T* background, int size);
	void generateScopeData(Ipp32f* output, Ipp16u* scope);
};

