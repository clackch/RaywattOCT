#pragma once
#include <ipp.h>
#include <opencv2/opencv.hpp>
#include <vector>
#include <omp.h>
#include "Config.h"
#include "Imaging.h"
#include "OCTMeasurement.h"

constexpr auto LUT_START_INDEX = 10;
constexpr auto LUT_END_INDEX = 244;
constexpr auto LUT_SCALE = (LUT_END_INDEX - LUT_START_INDEX + 1);

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
	cv::Mat imageResultWithoutCompensation;

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
	int m_nZOffset;

	cv::Ptr<cv::CLAHE> clahe;
public:
	COCTImaging(Setting, CMessageService*);
	virtual ~COCTImaging(void);

	virtual void Initialize(CCalibration* calibration);
	virtual void Process(char* fringes);
	virtual void PostProcess(cv::Mat image);
	void ApplyZOffset(const cv::Mat& src, cv::Mat& dst, int zOffset);

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

	virtual cv::Mat GetProcessedImage();
	cv::Mat GetWithoutCompensationImage() { return imageResultWithoutCompensation; }
	cv::Mat GetCircleImage() { return imageCircle; }
	USHORT* GetFringesBuffer() { return m_pFringesBuffer; }
	Setting GetSetting() { return m_setting; }
	void GetFrameInfo(int& nCurFrame, int& nTotalFrame) { nCurFrame = m_nCurFrame; nTotalFrame = m_nTotalFrame; }
	void* GetCalibrationData();
	void CircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual void InverseCircularizeImage(cv::Mat& src, cv::Mat& dst);
	virtual void EraseStentOutLier(cv::Mat& stent);
	virtual void SetLumenContourOffset(std::vector<cv::Point> lumenContour);
	virtual void GetGuideWireCenterPoint(cv::Mat image, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& centerPoints, std::vector<double>& radius);

	int GetSheathPosition() { return m_nSheathPosition; }
	void SetZOffset(int nOffset) { m_nZOffset = nOffset; }

	static void SetImageCompensation(bool ImageCompensated);
	static void SetImageLumenVignetting(bool ImageLumenVignetted);
	static void SetImageCompensationControlWindow(bool ImageCompensationControlWindowOn);
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
	void findSheath(cv::Mat img);
	std::vector<double> normalize(const std::vector<double>& values);
	void drawGuideLine(cv::Mat& image, int nPosition, cv::Scalar color);
	cv::Mat getFoVImage(cv::Mat image, double fov);

	void adaptive_compensation();
	void min_max_normalization(const cv::Mat& img, cv::Mat& normalized_img, double& min_val, double& max_val);
	void linear_contrast_stretching(cv::Mat& img, float lower_percentile = 1.0f, float upper_percentile = 99.0f);
	void logarithmic_contrast_stretching(cv::Mat& img, float lower_percentile = 1.0f, float upper_percentile = 99.0f);
	void lumen_detection_processing(cv::Mat& img);
	void apply_piecewise_linear_contrast(cv::Mat& img, int low_in, int high_in, int low_out, int high_out);
	double euclidean_distance(cv::Point2f pt1, cv::Point2f pt2);
	std::vector<int> find_outliers(const std::vector<int>& y_values);
	static void on_trackbar(int, void*);

	void adaptive_gamma_correction(cv::Mat& img);
	void get_PDF_array(cv::Mat& img, std::vector<double>& pdf_i, bool& AGCWD_apply);
	void get_CDF_array(std::vector<double> pdf_i, std::vector<double>& cdf_i);

	static UINT threadRender(LPVOID param);

};