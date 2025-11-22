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

struct FFTThreadContext {
	Ipp32f* fBuffer_Window = nullptr;
	Ipp32fc* fcBuffer_FFT = nullptr;
	Ipp32fc* fcBuffer_IFFT = nullptr;
	Ipp8u* fftWorkBufFirst = nullptr;
	Ipp8u* fftWorkBufIFFT = nullptr;
	Ipp8u* fftWorkBufSecond = nullptr;
};

enum class AutoCalibrationMathod {
	Disable = 0,
	FindingMinMagnitude,
	FindingSheath,
	CheckSheathPixelNum
};

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
	cv::Mat imatXMap;  // circle image -> inverse circular -> Rotate CounterClock 90 -> Circluar -> Rotate_ClockWise 90
	cv::Mat imatYMap;
	cv::Mat autoCalibPatch;
	std::vector<cv::Point> inversedContourYPoints;

	cv::Mat imageResult;
	cv::Mat imageResultColor;
	cv::Mat imageCircle;
	cv::Mat imageResultWithoutCompensation;
	cv::Mat imageAutoCalib;

	// using in GenerateBackground
	Ipp32f* fringes32f;
	Ipp32f* fringes32fAverage;

	// using in Gen_8bit_Image

	Ipp32f* fFFTResult;
	Ipp32f* fOutput;
	IppsFFTSpec_R_32f* fftSpecFirst;	// first FFT
	IppsFFTSpec_C_32fc* ifftSpec, * fftSpecSecond;	// Inverse, second FFT

	int fftFirstWorkBufSize;
	int fftIFFTWorkBufSize;
	int fftSecondWorkBufSize;

	bool m_bInvert;
	bool m_bColor;
	bool m_bShowCalibGuide;

	int m_nCurFrame;
	int m_nTotalFrame;

	int m_nSheathPosition;
	int m_nSheathSearchRange;
	int m_nZOffset;
	int m_nPixelNum;

	cv::Ptr<cv::CLAHE> clahe;

	AutoCalibrationMathod m_FindingSheathMathod;
public:
	COCTImaging(Setting, CMessageService*);
	virtual ~COCTImaging(void);

	virtual void Initialize(CCalibration* calibration);
	virtual void Process(char* fringes);
	void ProcessAutoCalib();
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
	void SetDistPerPixel(double distPerPixel) { m_setting.distPerPixel = distPerPixel; }
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
	void InverseCircularizeImage(cv::Mat& src, cv::Mat& dst);
	void EraseStentOutLier(cv::Mat& stent);
	void SetLumenContourOffset(const std::vector<cv::Point>& lumenContour);
	void GetGuideWireCenterPoint(cv::Mat image, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& centerPoints, std::vector<float>& radius);

	int GetSheathPosition() { return m_nSheathPosition; }
	int GetPixelNum() { return m_nPixelNum; }
	void SetPatchImage(cv::Mat Patch) { if(autoCalibPatch.empty()) autoCalibPatch = Patch.clone(); }
	void SetZOffset(int nOffset) { m_nZOffset = nOffset; }

	static void SetImageCompensation(bool ImageCompensated);
	static void SetImageCompensationControlWindow(bool ImageCompensationControlWindowOn, Setting setting);

	void SetAutoCalibrationMathod(AutoCalibrationMathod mathod) { m_FindingSheathMathod = mathod; }
	AutoCalibrationMathod GetAutoCalibrationMathod() { return m_FindingSheathMathod; }

protected:
	void allocateMemory();
	void releaseMemory();
	void initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	void releaseCircularizeMap();
	void initInversedCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale);
	void releaseInversedCircularizeMap();

	void generateBackground(Ipp16u* fringes);
	void fftProcessing(const Ipp32f* fringes32f);
	void computeLogarithm(Ipp32f* src, Ipp32f* dst);
	void generateImage(Ipp32f* logaritihmData, bool bInvert);
	void findSheath(Ipp32f* logaritihmData);
	void CalculateMagnitude(cv::Mat img);
	void CheckSheathPixels(cv::Mat img);
	void findSheath(cv::Mat input);
	std::vector<double> normalize(const std::vector<double>& values, double scale = 1.0);
	void drawGuideLine(cv::Mat& image, int nPosition, cv::Scalar color);
	cv::Mat getFoVImage(cv::Mat image, double fov);
	cv::Mat ReCircularize(const cv::Mat& img);

	void adaptive_compensation();
	void min_max_normalization(const cv::Mat& img, cv::Mat& normalized_img, double& min_val, double& max_val);
	void logarithmic_contrast_stretching(cv::Mat& img, float lower_percentile = 1.0f, float upper_percentile = 99.0f);
	double euclidean_distance(cv::Point2f pt1, cv::Point2f pt2);
	std::vector<int> find_outliers(const std::vector<int>& y_values);
	static void on_trackbar(int, void*);
	void GetLumenOffsetPoints(std::vector<cv::Point>& lumenOffsetBoundary);
	void GetGuideWireCircleEdgePoints(cv::Mat image, std::vector<cv::Rect2f> GuideWires, std::vector<cv::Point>& edgePoints);
	void GetGuideWireShadowPointAngles(cv::Mat image, std::vector<cv::Point> edgePoints, std::vector<double>& theta);
	void InterpolateEdgePoints(std::vector<cv::Point>& edgePoints);
	void GetCircularizeTransformPoint(cv::Point src, cv::Point& dst);
	void GetAcuteAngleToXAxis(cv::Vec2d vector1, cv::Vec2d vector2, double& angle);

	void get_PDF_array(cv::Mat& img, std::vector<double>& pdf_i, bool& AGCWD_apply);
	void get_CDF_array(std::vector<double> pdf_i, std::vector<double>& cdf_i);

	void sharpening(cv::Mat& img);
	cv::Mat inverseFFT(cv::Mat& complexImg);
	cv::Mat computeFFT(cv::Mat& img);

	static UINT threadRender(LPVOID param);

};