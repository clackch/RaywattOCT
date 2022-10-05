#pragma once
#include <ipp.h>
#include <opencv2/opencv.hpp>
#include <vector>
#include "Config.h"
#include "Imaging.h"

class CCalibration;
class CThread;
class CMessageService;
class COCTImaging : public IImaging
{
protected:
	CMessageService* m_msg;

	CThread* m_pThread;
	bool m_waitForFringes;
	Ipp16u* m_pFringesBuffer;

	CCalibration* calibration;
	cv::Mat matXMap;
	cv::Mat matYMap;

	cv::Mat imageResult;
	cv::Mat imageResultColor;
	cv::Mat imageCircle;
	cv::Mat imageMask;
	cv::Mat imageBackground;

	// using in GenerateBackground
	Ipp32f *fringes32f;
	Ipp32f *fringes32fAverage;

	// using in Gen_8bit_Image
	Ipp32f *fBuffer_Window;
	Ipp32fc *fcBuffer_FFT;
	Ipp32fc *fcBuffer_IFFT;
	Ipp32f *fFFTResult;
	Ipp32f *fOutput;
	IppsFFTSpec_R_32f *fftSpecFirst;	// first FFT
	IppsFFTSpec_C_32fc *ifftSpec, *fftSpecSecond;	// Inverse, second FFT

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

	virtual void Initialize(tstring calibFile);
	virtual void Process(USHORT* fringes);

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

	cv::Mat GetCircleImage() { return imageCircle; }
	USHORT* GetFringesBuffer() { return m_pFringesBuffer; }

protected:
	void allocateMemory();
	void releaseMemory();
	void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	void releaseCircularizeMap();

	void generateBackground(Ipp16u *fringes);
	void fftProcessing(const Ipp32f* fringes32f);
	void generateImage(bool bInvert);
	void postProcessing();
	void circularizeImage(cv::Mat& src, cv::Mat& dst);
	void applyHotColor(cv::Mat& image);
	void loadLUT(const char* strLUTPath);
	void applyLUT(cv::Mat& image);
	void generateMask(cv::Mat& image);

	static UINT threadRender(LPVOID param);
};