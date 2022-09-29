#pragma once
#include <Windows.h>
#include <ipp.h>
#include <opencv2/opencv.hpp>
#include <vector>
#include "Imaging.h"

class CCalibration;
class CThread;
class CMessageService;
class COCTImaging : public IImaging
{
private:
	CMessageService* m_msg;

	CThread* m_pThread;
	bool m_waitForFringes;
	Ipp16u* m_pFringesBuffer;

	CCalibration* calibration;
	cv::Mat matXMap;
	cv::Mat matYMap;

	cv::Mat imageResult;
	cv::Mat imageResultColor;
	cv::Mat imageRectangle;
	cv::Mat imageCircle;
	cv::Mat imageMask;
	cv::Mat imageBackground;

	Ipp16u* scopeData;
	Ipp16u* scopeFFTData;

	// using in GenerateBackground
	Ipp32f *fringes32f;
	Ipp32f *fringes32fAverage;

	// using in Gen_8bit_Image
	// dispersion compensation 적용하면 1920 개 데이터가 960 개가 되므로, 2nd FFT 는 1st FFT (2048) 보다 1/2 인 1024 적용한다. fBuffer_Fringes 크기도 1024 면 충분.
	// dispersion compensation : Map size 는 960 개 인데, 적용할 때 보면 data[index] 와 data[index+1] 에 weight, 1-weight 를 적용해서 하나로 만들기 때문에 dc 를 적용하면 1920 -> 960 이 됨.
	Ipp32f fBuffer_Fringes[2048];
	Ipp32fc fBuffer_Complex[2048];
	Ipp32fc fBuffer_DFT[2048];
	Ipp32f *fOutput;
	IppsFFTSpec_R_32f *specReal32FFT;	// first FFT
	IppsFFTSpec_C_32fc *specComp32FFT, *specComp32ZoomFFT;	// Inverse, second FFT

	bool m_bInvert;
	bool m_bColor;
	double m_fBrightness;
	double m_fContrast;

	std::vector<cv::Vec3b> m_vLUT;

	int m_nCurFrame;
	int m_nTotalFrame;
public:
	COCTImaging(CMessageService*);
	virtual ~COCTImaging(void);

	void Initialize();
	void Process(const USHORT* fringes);

	int Start();
	int Stop();
	virtual void DoAsyncRender(USHORT* fringes);
	void SetInvert(bool bInvert) { m_bInvert = bInvert; }
	void SetColor(bool bColor) { m_bColor = bColor; }
	void SetBrightnessContrast(double brightness, double contrast) {
		m_fBrightness = brightness;
		m_fContrast = contrast;
	}
	virtual void SetFrameInfo(int nCurFrame, int nTotalFrame) {
		m_nCurFrame = nCurFrame;
		m_nTotalFrame = nTotalFrame;
	}

	cv::Mat GetRectangleImage() { return imageRectangle; }
	cv::Mat GetCircleImage() { return imageCircle; }
	USHORT* GetScopeData() { return scopeData; }
	USHORT* GetScopeFFTData() { return scopeFFTData; }
	USHORT* GetFringesBuffer() { return m_pFringesBuffer; }

	void CalculateAxialResolution(USHORT* fftData, USHORT& nPeakValue, int& nPeakIndex, int& nLineWidth);
	void CalculateNoisePower(USHORT* fftData, int nPeakIndex, USHORT& nNoisePower);

private:
	void allocateMemory();
	void releaseMemory();
	void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	void releaseCircularizeMap();

	void generateBackground(Ipp16u *fringes);
	void generateImage(const Ipp16u* fringes, bool bInvert);
	void generateScopeData(Ipp32f* output, Ipp16u* scope);
	void circularizeImage(cv::Mat& src, cv::Mat& dst);
	void applyHotColor(cv::Mat& image);
	void loadLUT(const char* strLUTPath);
	void applyLUT(cv::Mat& image);
	void generateMask(cv::Mat& image);

	static UINT threadRender(LPVOID param);
};