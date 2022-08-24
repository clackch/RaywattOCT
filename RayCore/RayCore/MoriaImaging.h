#pragma once
#include <ipp.h>
#include <opencv2/opencv.hpp>
#include <vector>

#define WM_PROCESS_OCT_DONE		(WM_USER + 0x0001)

class CMoriaCalibration;
class CThread;
class CMessageService;
class CMoriaImaging
{
private:
	CMessageService* m_msg;

	CThread* m_pThread;
	bool m_waitForFringes;
	Ipp16u* m_pFringesBuffer;

	CMoriaCalibration* calibration;

	cv::Mat imageResult;
	cv::Mat imageResultColor;
	cv::Mat imageRectangle;
	cv::Mat imageCircle;

	Ipp16u* scopeData;
	Ipp16u* scopeFFTData;

	// using in GenerateBackground
	Ipp32f *fringes32f;
	Ipp32f *fringes32fAverage;

	// using in Gen_8bit_Image
	Ipp32f fBuffer_Fringes[2048];
	Ipp32fc fBuffer_Complex[2048];
	Ipp32fc fBuffer_DFT[2048];
	Ipp32f **fBuffer_BackgroundFringes;
	Ipp16u **uDataFringes_Deinterlaced;
	Ipp16u **uDataFingees_DeinterlacedwithPadding;
	Ipp32f **fOutput;
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
	CMoriaImaging(CMessageService*);
	virtual ~CMoriaImaging(void);

	void Initialize();
	void Process(const Ipp16u* fringes);

	int Start();
	int Stop();
	void DoAsyncRender(Ipp16u* fringes);
	void SetInvert(bool bInvert) { m_bInvert = bInvert; }
	void SetColor(bool bColor) { m_bColor = bColor; }
	void SetBrightnessContrast(double brightness, double contrast) {
		m_fBrightness = brightness;
		m_fContrast = contrast;
	}
	void SetFrameInfo(int nCurFrame, int nTotalFrame) {
		m_nCurFrame = nCurFrame;
		m_nTotalFrame = nTotalFrame;
	}

	cv::Mat GetRectangleImage() { return imageRectangle; }
	cv::Mat GetCircleImage() { return imageCircle; }
	Ipp16u* GetScopeData() { return scopeData; }
	Ipp16u* GetScopeFFTData() { return scopeFFTData; }
	Ipp16u* GetFringesBuffer() { return m_pFringesBuffer; }

	void CalculateAxialResolution(Ipp16u* fftData, Ipp16u& nPeakValue, int& nPeakIndex, int& nLineWidth);
	void CalculateNoisePower(Ipp16u* fftData, int nPeakIndex, Ipp16u& nNoisePower);

private:
	void allocateMemory();
	void releaseMemory();

	void generateBackground(Ipp16u *fringes);
	void generateImage(const Ipp16u* fringes, bool bInvert);
	void generateScopeData(Ipp32f* output, Ipp16u* scope);
	void circularizeImage(cv::Mat& src, cv::Mat& dst);
	void applyHotColor(cv::Mat& image);
	void loadLUT(const char* strLUTPath);
	void applyLUT(cv::Mat& image);

	static UINT threadRender(LPVOID param);
};

