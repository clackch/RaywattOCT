#pragma once
#include <ipp.h>
#include <opencv2/opencv.hpp>
#include <vector>
#include "Config.h"
#include "Imaging.h"
#include "OCTMeasurement.h"

class CCalibration;
class CThread;
class CMessageService;

class COCTImaging : public IImaging
{
protected:
	Setting m_setting;
	COCTMeasurement::Setting m_measureSetting;
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

	// using in GenerateBackground
	Ipp32f* fringes32f;
	Ipp32f* fringes32fAverage;

	// using in Gen_8bit_Image
	Ipp32f* fBuffer_Window;
	Ipp32fc* fcBuffer_FFT;
	Ipp32fc* fcBuffer_IFFT;
	Ipp32f* fFFTResult;
	Ipp32f* fOutput;
	IppsFFTSpec_R_32f* fftSpecFirst;	// first FFT
	IppsFFTSpec_C_32fc* ifftSpec, * fftSpecSecond;	// Inverse, second FFT

	bool m_bInvert;
	bool m_bColor;
	bool m_bShowCalibGuide;

	int m_nCurFrame;
	int m_nTotalFrame;

	int m_nSheathPosition;
public:
	COCTImaging(Setting, CMessageService*);
	virtual ~COCTImaging(void);

	virtual void Initialize(CCalibration* calibration);
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);

	int Start();
	int Stop();
	virtual void DoAsyncRender(char* fringes);
	void SetInvert(bool bInvert) { m_bInvert = bInvert; }
	void SetColor(bool bColor) { m_bColor = bColor; }
	void ShowCalibGuide(bool bShow) { m_bShowCalibGuide = bShow; }
	void SetMeasurementSetting(COCTMeasurement::Setting setting) { m_measureSetting = setting; }
	void SetBrightnessContrast(double brightness, double contrast) {
		m_setting.brightness = brightness;
		m_setting.contrast = contrast;
	}
	void SetLevel(double low, double high) {
		m_setting.lowLevel = low;
		m_setting.highLevel = high;
	}
	virtual void SetFrameInfo(int nCurFrame, int nTotalFrame) {
		m_nCurFrame = nCurFrame;
		m_nTotalFrame = nTotalFrame;
	}

	virtual cv::Mat GetProcessedImage() { return imageResult; }
	cv::Mat GetCircleImage() { return imageCircle; }
	USHORT* GetFringesBuffer() { return m_pFringesBuffer; }
	Setting GetSetting() { return m_setting; }
	void GetFrameInfo(int& nCurFrame, int& nTotalFrame) { nCurFrame = m_nCurFrame; nTotalFrame = m_nTotalFrame; }
	void* GetCalibrationData();
	int GetFoundSheathPosition() { return m_nSheathPosition; }
	virtual void CircularizeImage(cv::Mat& src, cv::Mat& dst);

	int GetSheathPosition() { return m_nSheathPosition; }
protected:
	void allocateMemory();
	void releaseMemory();
	void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	void releaseCircularizeMap();

	void generateBackground(Ipp16u* fringes);
	void fftProcessing(const Ipp32f* fringes32f);
	void computeLogarithm(Ipp32f* src, Ipp32f* dst);
	void generateImage(Ipp32f* logaritihmData, bool bInvert);
	void findSheath(Ipp32f* logaritihmData);
	void drawGuideLine(cv::Mat& image, int nPosition, cv::Scalar color);

	static UINT threadRender(LPVOID param);
};