#include "OCTImaging.h"
#include "Calibration.h"
#include "LookUpTable.h"
#include "Utility.h"
#include "MessageService.h"
#include "opencv2/opencv.hpp"
#include <omp.h>
#include "FFTSpecFactory.h"

#define _USE_MATH_DEFINES
#include <math.h>
#include <cmath>
#include <numeric>

float EXPONENTIAL_FACTOR = -1.00f;
float BRIGHTNESS_CONTROL = -1.00f;
float ENERGY_THRESHOLD = -1.00f;
static int INTENSITY_THRESHOLD = -1;
static bool bCompensated = true;
double last_entropy = 0;
double alpha = 0.4;
double entropy_threshold = 0.05;
std::vector<double> cdf_i(256, 1.0);

void ippsRelease(void *&ptr) {
	if (ptr) {
		ippsFree(ptr);
		ptr = nullptr;
	}
}
void ippsRelease_double_ptr(void**& ptr, int dim) {
	if (ptr) {
		for (int i = 0; i < dim; i++) {
			ippsRelease(ptr[i]);
		}
		delete[] ptr;
		ptr = nullptr;
	}
}

template<typename T>
static inline T clamp_v(T v, T lo, T hi) { return (v < lo) ? lo : ((v > hi) ? hi : v); }

static inline int round_to_even(double x) {
	double fl = std::floor(x);
	double frac = x - fl;
	if (frac > 0.5) return (int)(fl + 1);
	if (frac < 0.5) return (int)fl;
	int base = (int)fl;
	return (base % 2 == 0) ? base : base + 1;
}

COCTImaging::COCTImaging(Setting setting, CMessageService* pMsg) {
	m_setting = setting;
	m_msg = pMsg;

	m_pThread = nullptr;
	m_waitForFringes = true;
	m_pFringesBuffer = nullptr;

	calibration = nullptr;

	fringes32f = nullptr;
	fringes32fAverage = nullptr;

	fFFTResult = nullptr;
	fOutput = nullptr;

	fftSpecFirst = nullptr;
	fftSpecSecond = nullptr;
	ifftSpec = nullptr;

	m_bInvert = false;
	m_bColor = false;
	m_bShowCalibGuide = false;

	m_nCurFrame = 0;
	m_nTotalFrame = 0;

	m_nSheathPosition = 0;
	m_nPixelNum = 0;

	clahe = cv::createCLAHE(0.02, cv::Size(8, 8));
}

COCTImaging::~COCTImaging() {
	releaseMemory();
	if (calibration != nullptr) delete calibration;
}

void COCTImaging::Initialize(CCalibration* calibration) {
	releaseMemory();
	allocateMemory();

	this->calibration = calibration;

	releaseCircularizeMap();
	initCircularizeMap(m_setting.nOutputLength, m_setting.nBScan, m_setting.nOutputLength, m_setting.nCircleSize, m_setting.nCircleSize, 2.0f);
	releaseInversedCircularizeMap();
	initInversedCircularizeMap(m_setting.nCircleSize, m_setting.nCircleSize, m_setting.nCircleSize, m_setting.nCircleSize, m_setting.nCircleSize, 2.0f);

	m_nWidth = m_setting.nCircleSize;
	m_nHeight = m_setting.nCircleSize;
	m_nChannels = 3;	// RGB
}
void COCTImaging::Process(char* fringes) {
	if (fringes == nullptr) return;

	generateBackground((Ipp16u*)fringes);
	fftProcessing(fringes32f);
	computeLogarithm(fFFTResult, fFFTResult);
	//findSheath(fFFTResult);
	generateImage(fFFTResult, false);
	adaptive_compensation();
}
void COCTImaging::PostProcess(cv::Mat image) {
	const bool bInvert = m_bInvert;
	const bool bColor = m_bColor;

	if (m_FindingSheathMathod == AutoCalibrationMathod::FindingMinMagnitude)
	{
		CalculateMagnitude(image);
	}
	else if (m_FindingSheathMathod == AutoCalibrationMathod::FindingSheath)
	{
		findSheath(image);
	}else if(m_FindingSheathMathod == AutoCalibrationMathod::CheckSheathPixelNum)
	{
		CheckSheathPixels(image);
	}

	cv::cvtColor(image, imageResultColor, cv::COLOR_GRAY2RGB);

	if (bInvert) cv::bitwise_not(imageResultColor, imageResultColor);
	if (bColor) {
		CLookUpTable& lut = CLookUpTable::GetInstance();
		if (lut.GetEnhancedLUT()) {
			lut.Apply(imageResultColor, 3 /*LUT_enhanced.csv*/);
			lut.Apply(imageResultColor, lut.GetCurrentColormap());
		}
		else {
			lut.Apply(imageResultColor, lut.GetCurrentColormap());
		}
	}

	cv::convertScaleAbs(imageResultColor, imageResultColor, m_setting.contrast, m_setting.brightness);

	if (m_bShowCalibGuide) {
		drawGuideLine(imageResultColor, m_measureSetting.nSheathPosition, cv::Scalar(0x60, 0xd7, 0x1e));
		//drawGuideLine(imageResultColor, m_nSheathPosition, cv::Scalar(0xff, 0xff, 0xff));
	}

	CircularizeImage(imageResultColor, imageCircle);
}
void COCTImaging::ApplyZOffset(const cv::Mat& src, cv::Mat& dst, int zOffset) {
	cv::Mat img = src.clone();

	cv::Mat translation_matrix = (cv::Mat_<double>(2, 3) << 1, 0, zOffset * -1, 0, 1, 0);
	cv::warpAffine(img, dst, translation_matrix, img.size());
}
int COCTImaging::Start() {
	BOOL result = FALSE;
	result = CUtility::StartThread(threadRender, m_pThread, (LPVOID)this);

	if (result) return NOERROR;
	else return -1;
}
int COCTImaging::Stop() {
	CUtility::StopThread(m_pThread);

	return NOERROR;
}
void* COCTImaging::GetCalibrationData() {
	if (calibration == nullptr) return nullptr;

	return calibration->data;
}
void COCTImaging::DoAsyncRender(char* fringes) {
	if (m_pThread == nullptr || m_pThread->isRun == false) return;

	if (m_waitForFringes) {
		m_pFringesBuffer = (USHORT *)fringes;
		CUtility::ResumeThread(m_pThread);
	}
}
void COCTImaging::CircularizeImage(cv::Mat& src, cv::Mat& dst)
{
	cv::Mat imgFoVOrigin;
	cv::remap(src, imgFoVOrigin, matXMap, matYMap, cv::INTER_LINEAR);

	cv::Mat imgFoV = getFoVImage(imgFoVOrigin, MAX_FIELD_OF_VIEW);
	imgFoV.copyTo(dst);
}

void COCTImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst) {
	dst = src.clone();
	cv::remap(dst, dst, imatXMap, imatYMap, cv::INTER_LINEAR);

	cv::rotate(dst, dst, cv::ROTATE_90_COUNTERCLOCKWISE);
}

cv::Mat COCTImaging::GetProcessedImage() {
	return imageResult;
}

void COCTImaging::SetImageCompensation(bool ImageCompensated) { 
	bCompensated = ImageCompensated;
}

void COCTImaging::SetImageCompensationControlWindow(bool ImageCompensationControlWindowOn, Setting setting) {

	const char* strWindowName = "Compensation";
	try {
		if (ImageCompensationControlWindowOn == 1) {
			cv::namedWindow(strWindowName, cv::WINDOW_AUTOSIZE);

			// 슬라이더 값 범위는 정수로만 가능하므로, 원하는 범위로 매핑
			int exponential_factor_slider = (int)(setting.exponentialFactor * 10);
			int brightness_control_slider = (int)(setting.brightnessControl * 10);
			int energy_threshold_slider = (int)(setting.energyThreshold * 10);
			int alpha_slider = (int)(setting.GCAlpha * 10);
			int intensity_threshold = setting.intensityThreshold;

			cv::createTrackbar("Cont", strWindowName, &exponential_factor_slider, 100, on_trackbar);
			cv::createTrackbar("Bright", strWindowName, &brightness_control_slider, 100, on_trackbar);
			cv::createTrackbar("Eng", strWindowName, &energy_threshold_slider, 100, on_trackbar);
			cv::createTrackbar("Alpha", strWindowName, &alpha_slider, 100, on_trackbar);
			cv::createTrackbar("GCTH", strWindowName, &intensity_threshold, 255, on_trackbar);

			// 초기 콜백 호출
			on_trackbar(0, 0);
		}
		else {
			cv::destroyWindow(strWindowName);
		}
	}
	catch (const cv::Exception& e) {
		PLOGI.printf("OpenCV Error: %s", e.what());  // OpenCV 관련 에러 처리
	}
	catch (const std::exception& e) {
		PLOGI.printf("Standard Error: %s", e.what());  // 다른 표준 라이브러리 예외 처리
	}
	catch (...) {
		PLOGI.printf("Unknown error occurred in SetImageCompensationControlWindow");  // 예기치 않은 에러 처리
	}
}

void COCTImaging::allocateMemory() {
	// ORDER = 11, nFFTLength = 2^11
	// nScans 보다 큰 2^n 중에서 제일 작은 수
	const int nAScan = m_setting.nAScan;
	const int nBScan = m_setting.nBScan;
	const int nFFTOrder = m_setting.nFFTOrder;
	const int nFFTLength = m_setting.nFFTLength;
	const int nOutputLength = m_setting.nOutputLength;
	const int nCircleSize = m_setting.nCircleSize;

	fringes32f = ippsMalloc_32f(nAScan * nBScan);
	fringes32fAverage = ippsMalloc_32f(nAScan);

	imageResult.create(nBScan, nOutputLength, CV_8UC1);
	imageResultColor.create(nBScan, nOutputLength, CV_8UC3);
	imageCircle.create(nCircleSize, nCircleSize, CV_8UC3);
	imageResultWithoutCompensation.create(nBScan, nOutputLength, CV_8UC1);

	fFFTResult = ippsMalloc_32f(nOutputLength * nBScan);
	fOutput = ippsMalloc_32f(nOutputLength * nBScan);

	// Prepare FFT
	CFFTSpecFactory& factory = CFFTSpecFactory::Instance();
	fftSpecFirst = factory.GetSpecR(nFFTOrder, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	fftFirstWorkBufSize = factory.GetBufferR(fftSpecFirst);

	ifftSpec = factory.GetSpecC(nFFTOrder, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	fftIFFTWorkBufSize = factory.GetBufferC(ifftSpec);

	fftSpecSecond = factory.GetSpecC(nFFTOrder - 1, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	fftSecondWorkBufSize = factory.GetBufferC(fftSpecSecond);
}
void COCTImaging::releaseMemory() {
	if (fringes32f) { ippsFree(fringes32f); fringes32f = nullptr; }
	if (fringes32fAverage) { ippsFree(fringes32fAverage); fringes32fAverage = nullptr; }

	imageResult.release();
	imageResultColor.release();
	imageCircle.release();
	imageResultWithoutCompensation.release();

	ippsRelease((void*&)fFFTResult);
	ippsRelease((void*&)fOutput);
}
void COCTImaging::initCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale) {
	int circOffset = 0;
	double radius = (diameter / 2) - 0.5f;

	matXMap.create(dstHeight, dstWidth, CV_32FC1);
	matYMap.create(dstHeight, dstWidth, CV_32FC1);

	matXMap.setTo(cv::Scalar::all(0));
	matYMap.setTo(cv::Scalar::all(0));

	for (int y = 0; y < dstHeight; y++)
	{
		for (int x = 0; x < dstWidth; x++)
		{
			double fy = (double)y - radius;
			double fx = (double)x - radius;

			float rvalue = (float)(srcWidth - scale * sqrt(pow(fy, 2) + pow(fx, 2))) + (float)circOffset;

			matXMap.at<float>(y, x) = rvalue;
			matYMap.at<float>(y, x) = (float)(((atan2(fy, fx) / M_PI) + 1.0) * 0.5 * (srcHeight - 1));
		}
	}
}

void COCTImaging::releaseCircularizeMap() {
	matXMap.release();
	matYMap.release();
}

void COCTImaging::initInversedCircularizeMap(int diameter, int srcHeight, int srcWidth, int dstHeight, int dstWidth, double scale) {

	double radius = (diameter / 2) - 0.5f;
	
	imatXMap.create(dstHeight, dstWidth, CV_32FC1);
	imatYMap.create(dstHeight, dstWidth, CV_32FC1);

	imatXMap.setTo(cv::Scalar::all(0));
	imatYMap.setTo(cv::Scalar::all(0));

	for (int y = 0; y < dstHeight; y++)
	{
		for (int x = 0; x < dstWidth; x++)
		{
			double r = (srcHeight - y) / scale;
			double theta = (x / float(dstWidth)) * 2.0 * M_PI;

			imatXMap.at<float>(y, x) = r * cos(theta) + radius;
			imatYMap.at<float>(y, x) = r * sin(theta) + radius;
		}
	}
}

void COCTImaging::releaseInversedCircularizeMap() {
	imatXMap.release();
	imatYMap.release();
}

void COCTImaging::generateBackground(Ipp16u* fringes) {
	const int nWidth = m_setting.nAScan;
	const int nHeight = m_setting.nBScan;

	// 모든 fringe의 평균으로 background를 계산한다. 
	ippsZero_32f(fringes32fAverage, nWidth);

	for (int y = 0; y < nHeight; y++)
	{
		ippsConvert_16u32f(fringes + y * nWidth, fringes32f + y * nWidth, nWidth);
		ippsAdd_32f_I(fringes32f + y * nWidth, fringes32fAverage, nWidth);
	}

	ippsMulC_32f_I(1.0f / ((float)nHeight), fringes32fAverage, nWidth);
}
void COCTImaging::fftProcessing(const Ipp32f* fringes32f) {
	const int numDynamic = 1;
	const int numThreads = 8;
	const int nAScan = m_setting.nAScan, nBScan = m_setting.nBScan;
	const int nFFTLength = m_setting.nFFTLength;
	const int nOutputLength = m_setting.nOutputLength;

	std::vector<FFTThreadContext> threadContexts(numThreads);
	
	// 스레드별 FFTSpec 및 버퍼 할당 및 초기화
	for (int t = 0; t < numThreads; ++t) {
		auto& ctx = threadContexts[t];

		// Window buffer
		ctx.fBuffer_Window = ippsMalloc_32f(nFFTLength);
		ippsZero_32f(ctx.fBuffer_Window, nFFTLength);

		// FFT buffers
		ctx.fcBuffer_FFT = ippsMalloc_32fc(nFFTLength);
		ippsZero_32fc(ctx.fcBuffer_FFT, nFFTLength);
		ctx.fcBuffer_IFFT = ippsMalloc_32fc(nFFTLength);

		// Work buffers
		ctx.fftWorkBufFirst = ippsMalloc_8u(fftFirstWorkBufSize);
		ctx.fftWorkBufIFFT = ippsMalloc_8u(fftIFFTWorkBufSize);
		ctx.fftWorkBufSecond = ippsMalloc_8u(fftSecondWorkBufSize);
	}

	// Process Frame
	// To-Do : enable openmp, check shared variables
	omp_set_dynamic(numDynamic);
	omp_set_num_threads(numThreads);
	//#pragma omp parallel
	{
		//#pragma omp for firstprivate(fBuffer_Window,fBuffer_BackgroundFringes,fcBuffer_FFT,fcBuffer_IFFT,j)
		#pragma omp parallel for
		for (int i = 0; i < nBScan; i++)
		{
			int tid = omp_get_thread_num();
			auto& ctx = threadContexts[tid];
			{
				// 1. Background Subtract
				ippsCopy_32f(fringes32f + i * nAScan, ctx.fBuffer_Window, nAScan);
				ippsSub_32f_I(fringes32fAverage, ctx.fBuffer_Window, nAScan);  // I의 의미:자기 자신에 이처리를 해서, 결과를 얻는다.

				// 2. Apply Window
				ippsMul_32f_I(calibration->window, ctx.fBuffer_Window, nFFTLength);

				// 3. First FFT
				ippsFFTFwd_RToPerm_32f_I(ctx.fBuffer_Window, fftSpecFirst, ctx.fftWorkBufFirst); // http://software.intel.com/sites/products/documentation/hpc/ipp/ipps/ipps_ch7/ch7_packed_formats.html#Perm
				ippsConjPerm_32fc(ctx.fBuffer_Window, ctx.fcBuffer_FFT, nFFTLength);

				// 4. Zero Pad & Reorder (1 | 2 | 0 | 0)
				ippsZero_32fc(ctx.fcBuffer_IFFT, nFFTLength);
				ippsCopy_32fc(ctx.fcBuffer_FFT, ctx.fcBuffer_IFFT, nOutputLength);

				// 5. Inverse FFT
				ippsFFTInv_CToC_32fc_I(ctx.fcBuffer_IFFT, ifftSpec, ctx.fftWorkBufIFFT);

				// 6. Interpolation
				ippsZero_32fc(ctx.fcBuffer_FFT, nOutputLength);
				for (int j = 0; j < nAScan / 2; j++) {
					ctx.fcBuffer_FFT[j].re = (calibration->weightMap[j] * ctx.fcBuffer_IFFT[calibration->indexMap[j]].re + (1.0f - calibration->weightMap[j]) * ctx.fcBuffer_IFFT[calibration->indexMap[j] + 1].re);
					ctx.fcBuffer_FFT[j].im = (calibration->weightMap[j] * ctx.fcBuffer_IFFT[calibration->indexMap[j]].im + (1.0f - calibration->weightMap[j]) * ctx.fcBuffer_IFFT[calibration->indexMap[j] + 1].im);
				}

				// 7. Numerical Dispersion Compensation
				ippsMul_32fc_I((Ipp32fc*)calibration->dispersion, ctx.fcBuffer_FFT, nAScan / 2);

				// 8. FFT Again
				ippsFFTFwd_CToC_32fc_I(ctx.fcBuffer_FFT, fftSpecSecond, ctx.fftWorkBufSecond);

				// 9. Extract Magnitude
				ippsPowerSpectr_32fc(ctx.fcBuffer_FFT, fFFTResult + i * nOutputLength, nOutputLength);
			}
		}
	} // end parallel region

	for (int t = 0; t < numThreads; ++t) {
		auto& ctx = threadContexts[t];
		ippsFree(ctx.fBuffer_Window);
		ippsFree(ctx.fcBuffer_FFT);
		ippsFree(ctx.fcBuffer_IFFT);
		ippsFree(ctx.fftWorkBufFirst);
		ippsFree(ctx.fftWorkBufIFFT);
		ippsFree(ctx.fftWorkBufSecond);
	}
}

void COCTImaging::computeLogarithm(Ipp32f* src, Ipp32f* dst) {
	const int nBScan = m_setting.nBScan;
	const int nOutputLength = m_setting.nOutputLength;

	ippsLn_32f(src, dst, nOutputLength * nBScan);
	ippsMulC_32f(dst, log10(exp(1)) * 10, dst, nOutputLength * nBScan);
}

void COCTImaging::findSheath(Ipp32f* logaritihmData) {
	const int nBScan = m_setting.nBScan;
	const int nFFTLength = m_setting.nFFTLength;
	const int nOutputLength = m_setting.nOutputLength;
	const int minPeakHeight = 1500.f;
	const int distBetweenLayer = 24;

	if (nOutputLength > 1024 * 10) {
		PLOGI.printf("nOutputLength is too big");
		return;
	}

	Ipp32f* fScope = new Ipp32f[nOutputLength];
	std::vector<int> sheathPoints;
	int sheathPointSum = 0;
	for (int n = 0; n < nBScan; n++) {
		ippsSubC_32f(logaritihmData + n * nOutputLength, m_setting.lowLevel, fScope, nOutputLength);
		ippsMulC_32f_I(USHRT_MAX / m_setting.highLevel, fScope, nOutputLength);
		ippsDivC_32f_I(1000.f, fScope, nOutputLength);	// db scale

		// linearize
		for (int i = 0; i < nOutputLength; i++) {
			fScope[i] = pow((fScope[i] / 10.f), 10.f);
		}

		// find peak
		std::vector<int> peakPoints;
		for (int i = 1; i < nOutputLength - 1; i++) {
			if (fScope[i] > fScope[i - 1] && fScope[i] > fScope[i + 1] && fScope[i] > minPeakHeight) {
				peakPoints.push_back(i);
			}
		}

		if (peakPoints.size() > 2) {
			int firstPeak = peakPoints.at(0);
			for (int peak = firstPeak + (distBetweenLayer - 10); peak < firstPeak + (distBetweenLayer + 10); peak++) {
				for (int idx = 1; idx < peakPoints.size(); idx++) {
					if (peakPoints.at(idx) == peak) {
						sheathPoints.push_back(peak);
						sheathPointSum += peak;
					}
				}
			}
		}
	}
	delete[] fScope;

	m_nSheathPosition = 0;
	if (sheathPoints.size() > 0) {
		m_nSheathPosition = sheathPointSum / sheathPoints.size();
	}
}

void COCTImaging::CalculateMagnitude(cv::Mat img) {
	cv::Mat edgeX, edgeY;
	cv::Sobel(img, edgeX, CV_32F, 1, 0, 3);
	cv::Sobel(img, edgeY, CV_32F, 0, 1, 3);

	cv::Mat absEdgeX, absEdgeY;
	cv::convertScaleAbs(edgeX, absEdgeX);
	cv::convertScaleAbs(edgeY, absEdgeY);

	cv::Mat edgeMagnitude, absEdgeMagnitude;
	cv::magnitude(edgeX, edgeY, edgeMagnitude);
	cv::convertScaleAbs(edgeMagnitude, absEdgeMagnitude);

	int totalX = 0, totalY = 0, totalMagnitude = 0;
	for (int y = 0; y < img.rows; y++) {
		for (int x = 0; x < img.cols; x++) {
			totalX += absEdgeX.at<uchar>(y, x);
			totalY += absEdgeY.at<uchar>(y, x);
			totalMagnitude += absEdgeMagnitude.at<uchar>(y, x);
		}
	}

	//cv::imwrite("origin_first" + std::to_string(i) + ".png", img);
	//PLOGI.printf("check the time - Magnitude: %d", totalMagnitude);
	m_nSheathPosition = totalMagnitude;
}

void COCTImaging::CheckSheathPixels(cv::Mat img)
{
	// LUT Table
	const int    TOP_BAND_WIDTH = 30;
	const double TARGET = 0.50;
	const double POWER_MIN = 0.60;
	const double POWER_MAX = 12.0;

	// 1. 클론 이미지 생성
	cv::Mat cloneImg = img.clone();
	cv::rotate(cloneImg, cloneImg, cv::ROTATE_180);

	// 2. Otsu 이진화
	cv::Mat binaryImg;
	cv::threshold(cloneImg, binaryImg, 0, 255, cv::THRESH_BINARY | cv::THRESH_OTSU);

	// 3. 흰 픽셀 수 카운팅
	int nowRow = 0;
	int startCol = 30, endCol = 100;
	int InnerSheathThickness = 15, outerSheathThickness = 3;
	int pixelCount = 0;
	for (int y = 0; y < binaryImg.rows; y++) {
		int thickCount = 0;
		int innerSheath = 0, outerSheath = 0;
		for (int x = startCol; x < endCol; x++) {
			if (binaryImg.at<uchar>(y, x) == 255) {
				thickCount++;
				if (thickCount > InnerSheathThickness) {
					innerSheath = x - InnerSheathThickness;
					break;
				}
			}
		}
		for (int x = innerSheath + 40; x < innerSheath + 60; x++) {
			if (binaryImg.at<uchar>(y, x) == 255) {
				thickCount++;
				if (thickCount > outerSheathThickness) {
					outerSheath = x - outerSheathThickness;
					break;
				}
			}
			else {
				thickCount = 0;
			}
		}
		if (outerSheath - innerSheath > 30 && outerSheath - innerSheath < 50) {
			pixelCount++;
		}
	}
	//PLOGI.printf("check the time - pixelCount: %d", pixelCount);
	if (pixelCount > binaryImg.rows - 100) {
		m_nPixelNum = 1;
	}
	else {
		m_nPixelNum = -1;
	}
}

cv::Mat COCTImaging::ReCircularize(const cv::Mat& img) {
	float scale = 2.0f;

	cv::Mat circularized;
	remap(img, circularized, matXMap, matYMap, cv::INTER_LINEAR);

	if (imatXMap.empty() && imatYMap.empty()) {
		int diameter = circularized.cols;
		int srcWidth = circularized.cols;
		int srcHeight = circularized.rows;
		int dstWidth = diameter;
		int dstHeight = diameter;

		initInversedCircularizeMap(diameter, srcHeight, srcWidth, dstHeight, dstWidth, scale);
	}
	cv::Mat result;
	remap(circularized, result, imatXMap, imatYMap, cv::INTER_NEAREST);

	return result;
}

void COCTImaging::findSheath(cv::Mat input) {
	cv::Mat gray;
	if (input.channels() == 3) {
		cvtColor(input, gray, cv::COLOR_BGR2GRAY);
	}
	else if (input.channels() == 4) {
		cv::Mat bgr; cvtColor(input, bgr, cv::COLOR_BGRA2BGR); cvtColor(bgr, gray, cv::COLOR_BGR2GRAY);
	}
	else {
		if (input.type() == CV_8UC1) gray = input.clone();
		else {
			double mn = 0.0, mx = 0.0; minMaxLoc(input, &mn, &mx);
			if (mx > mn) input.convertTo(gray, CV_8U, 255.0 / (mx - mn), -mn * 255.0 / (mx - mn));
			else         input.convertTo(gray, CV_8U);
		}
	}

	cv::rotate(gray, gray, cv::ROTATE_90_COUNTERCLOCKWISE);
	cv::Mat tmp = gray.clone();
	cv::threshold(gray, gray, 0, 255, cv::THRESH_OTSU);

	int nowRow = 0;
	int startRow = 100;
	int sheathThickness = 15;
	int thickCount = 0;

	for (int i = startRow; i < startRow + 300; i++) {
		int pixelCount = 0;
		bool isThereHighPixel = false;
		for (int x = 0; x < gray.cols; x++) {
			if (gray.at<uchar>(i, x) == 255)
				pixelCount++;
			if (tmp.at<uchar>(i, x) > 200) {
				isThereHighPixel = true;
			}
		}
		nowRow = i;
		if (pixelCount > gray.cols * 0.7 && pixelCount != gray.cols && isThereHighPixel) {
			thickCount++;
			if (thickCount > sheathThickness) {
				nowRow -= sheathThickness;
				break;
			}
		}
		else {
			thickCount = 0;
		}
	}

	//cv::line(tmp, cv::Point(0, nowRow), cv::Point(tmp.cols - 1, nowRow), cv::Scalar(255, 0, 0), 2);
	//cv::imwrite("origin" + std::to_string(i) + ".tif", tmp);
	m_nSheathPosition = nowRow;
	//cv::imwrite("binary" + std::to_string(i) + ".tif", gray);
}

// 정규화를 위한 함수
std::vector<double> COCTImaging::normalize(const std::vector<double>& values, double scale) {
	double min_val = *std::min_element(values.begin(), values.end());
	double max_val = *std::max_element(values.begin(), values.end());
	std::vector<double> normalized;

	for (double val : values) {
		normalized.push_back((val - min_val) / (max_val - min_val) * scale);
	}
	return normalized;
}

void COCTImaging::generateImage(Ipp32f* logaritihmData, bool bInvert){
	const int nBScan = m_setting.nBScan;
	const int nOutputLength = m_setting.nOutputLength;

	cv::Mat imgLog(cv::Size(nOutputLength, nBScan), CV_32FC1, logaritihmData);
	imgLog -= m_setting.lowLevel;
	imgLog *= (LUT_SCALE / m_setting.highLevel);
	cv::threshold(imgLog, imgLog, LUT_SCALE, LUT_SCALE, cv::THRESH_TRUNC);
	imgLog.convertTo(imageResult, CV_8UC1);
	cv::convertScaleAbs(imageResult, imageResult, 1.f / 80.f * LUT_SCALE, 0);
	cv::flip(imageResult, imageResult, 1);

	imageResult.copyTo(imageResultWithoutCompensation);
}

void COCTImaging::drawGuideLine(cv::Mat& image, int nPosition, cv::Scalar color) {
	int posDraw = image.cols - nPosition - 1;

	int lineSize = image.rows / 8;
	int lineStart = 0;

	cv::line(image, cv::Point(posDraw, lineStart), cv::Point(posDraw, (lineStart + lineSize / 2) - 1), color, 2);
	lineStart += (lineSize / 2);
	lineStart += (lineSize);
	for (int i = 0; i < 3; i++) {
		cv::line(image, cv::Point(posDraw, lineStart), cv::Point(posDraw, (lineStart + lineSize) - 1), color, 2);
		lineStart += (lineSize * 2);
	}
	cv::line(image, cv::Point(posDraw, lineStart), cv::Point(posDraw, (lineStart + lineSize / 2) - 1), color, 2);
}

cv::Mat COCTImaging::getFoVImage(cv::Mat image, double fov) {
	cv::Rect roi;
	roi.width = (int)(floor(round(fov * 1000.f / m_setting.distPerPixel))) / 2;
	roi.height = roi.width;
	roi.x = (image.cols - roi.width) / 2;
	roi.y = (image.rows - roi.height) / 2;

	cv::Mat imgROI;
	if (roi.width > image.cols ||
		roi.height > image.rows) {
		cv::Mat imgFov;
		cv::Rect fovRoi;

		fovRoi.x = abs(roi.x);
		fovRoi.y = abs(roi.y);
		fovRoi.width = image.cols;
		fovRoi.height = image.rows;

		imgFov.create(cv::Size(roi.width, roi.height), image.type());
		image.copyTo(imgFov(fovRoi));
		cv::resize(imgFov, imgROI, cv::Size(image.cols, image.rows));
	}
	else {
		cv::resize(image(roi), imgROI, cv::Size(image.cols, image.rows));
	}

	return imgROI;
}

UINT COCTImaging::threadRender(LPVOID param) {
	COCTImaging* pImaging = (COCTImaging*)param;
	CMessageService* pMsg = pImaging->m_msg;

	while(pImaging->m_pThread->isRun) {
		pImaging->m_waitForFringes = true;
		CUtility::SuspendThread(pImaging->m_pThread);
		pImaging->m_waitForFringes = false;

		if (pImaging->m_pThread->isRun) {
			pImaging->Process((char *)pImaging->m_pFringesBuffer);
			pImaging->PostProcess(pImaging->GetProcessedImage());
			// To-Do
			// double buffering 필요?
			// Invert, coloring 을 View (Dialog) 쪽으로 뺄 수 없을까?

			if (pMsg != nullptr) {
				int nFrameInfo = (pImaging->m_nCurFrame << 16) | (pImaging->m_nTotalFrame);
				pMsg->postMessage(WM_PROCESS_CROSSSECTION, pImaging->GetSession(), nFrameInfo);
			}
		}
	}

	return NOERROR;
}

void COCTImaging::adaptive_compensation()
{
	if (!bCompensated || m_setting.applyCompensation == 0)
		return;

	// 0) 설정값 확정 (원 로직 유지)
	EXPONENTIAL_FACTOR = (EXPONENTIAL_FACTOR <= -1.0f) ? m_setting.exponentialFactor : EXPONENTIAL_FACTOR;
	BRIGHTNESS_CONTROL = (BRIGHTNESS_CONTROL <= -1.0f) ? m_setting.brightnessControl : BRIGHTNESS_CONTROL;
	ENERGY_THRESHOLD = (ENERGY_THRESHOLD <= -1.0f) ? m_setting.energyThreshold : ENERGY_THRESHOLD;
	INTENSITY_THRESHOLD = (INTENSITY_THRESHOLD <= -1) ? m_setting.intensityThreshold : INTENSITY_THRESHOLD;

	// 1) 회전 + 32F 변환
	cv::Mat rotated;
	cv::rotate(imageResult, rotated, cv::ROTATE_90_COUNTERCLOCKWISE);
	rotated.convertTo(rotated, CV_32F);

	// 2) 하단 60행 0으로 (원 코드와 동일)
	const int rows = rotated.rows;
	const int cols = rotated.cols;
	if (rows > 0) {
		const int pad = std::min(60, rows);
		rotated.rowRange(rows - pad, rows).setTo(0);
	}

	// 3) 결과 버퍼
	cv::Mat result_img = cv::Mat::zeros(rotated.size(), CV_32F);

	// 4) 상수 사전 계산
	const float denom10 = static_cast<float>(std::pow(10.0f, static_cast<float>(ENERGY_THRESHOLD)));
	const int   tail = std::min(60, rows);                    // 0 패딩 영역 크기
	const float EPS = 1e-6f;
	const float tailConst = static_cast<float>(std::pow(EPS, EXPONENTIAL_FACTOR)); // pow(0+eps, k)

	// 5) A: OpenCV 내부 스레딩 비활성화 (중첩 스레딩 방지)
	const int prev_cv_threads = cv::getNumThreads();
	cv::setNumThreads(1);

	// 행 단위 stride(요소 단위)
	const size_t stepRot = rotated.step1(); // float 요소 단위
	const size_t stepRes = result_img.step1();

	// 6) B: 전치 없이 열을 스레드 로컬 1D 버퍼로 모아 add+pow(2패스) 수행
#pragma omp parallel
	{
		std::vector<float> col(rows), csum(rows);

#pragma omp for schedule(static)
		for (int x = 0; x < cols; ++x)
		{
			// 6-1) 열 → 연속 버퍼로 모으기 (gather)
			const float* baseIn = rotated.ptr<float>(0);
			for (int r = 0; r < rows; ++r)
				col[r] = baseIn[r * stepRot + x];

			// === 6-2) add + pow 로 2패스 축소 + 하단 60행 스킵 ===
			if (tail > 0) {
				// 하단 60행은 입력이 0 → pow(0+eps, k)로 동일 상수
				for (int r = rows - tail; r < rows; ++r)
					col[r] = tailConst;
			}
			if (rows > tail) {
				// 나머지 구간만 연산 (연속 메모리)
				cv::Mat headMat(rows - tail, 1, CV_32F, col.data());            // 상단 구간 뷰
				cv::add(headMat, cv::Scalar(EPS), headMat);                      // add
				cv::pow(headMat, static_cast<double>(EXPONENTIAL_FACTOR), headMat); // pow
			}

			// 6-3) 누적합(아래→위)
			if (rows > 0) {
				csum[rows - 1] = col[rows - 1];
				for (int i = rows - 2; i >= 0; --i)
					csum[i] = csum[i + 1] + col[i];
			}

			// 6-4) 임계 탐색 (energy_all(0,x) == csum[0]^2 와 동치)
			const float totalE = (rows > 0) ? (csum[0] * csum[0]) : 0.0f;
			const float thresh = (denom10 != 0.0f) ? (totalE / denom10) : std::numeric_limits<float>::infinity();

			int stop_row = rows - 1;
			for (int z = 0; z < rows; ++z) {
				if (csum[z] < thresh) { stop_row = z; break; }
			}

			// 6-5) 출력 계산 (원 로직/식 동일)
			double stop_cumsum = 0.0;
			float* baseOut = result_img.ptr<float>(0);

			for (int z = 0; z <= stop_row; ++z)
			{
				const float sv = csum[z];
				if (sv != 0.0f)
				{
					const float denom = std::exp(BRIGHTNESS_CONTROL * std::log(sv)) * 2.0f;
					baseOut[z * stepRes + x] = col[z] / denom;
					stop_cumsum = denom;
				}
				else
				{
					baseOut[z * stepRes + x] = 0.0f;
				}
			}
			for (int z = stop_row + 1; z < rows; ++z)
			{
				baseOut[z * stepRes + x] =
					(stop_cumsum != 0.0) ? (col[z] / static_cast<float>(stop_cumsum)) : 0.0f;
			}
		}
	}

	// 7) OpenCV 스레딩 복원
	cv::setNumThreads(prev_cv_threads);

	// Linear contrast stretching
	logarithmic_contrast_stretching(result_img);
	result_img.convertTo(result_img, CV_8U, INTENSITY_THRESHOLD);

	if (m_setting.applySharpness) {
		sharpening(result_img);
	}

	// Rotate back to original angle
	cv::rotate(result_img, imageResult, cv::ROTATE_90_CLOCKWISE);
}

void COCTImaging::min_max_normalization(const cv::Mat& img, cv::Mat& normalized_img, double& min_val, double& max_val)
{
	cv::minMaxLoc(img, &min_val, &max_val);
	normalized_img = (img - min_val) / (max_val - min_val);
}

void COCTImaging::logarithmic_contrast_stretching(cv::Mat& img, float lower_percentile, float upper_percentile)
{
	CV_Assert(img.type() == CV_32F);
	const int rows = img.rows;
	const int cols = img.cols;
	const size_t N = static_cast<size_t>(rows) * static_cast<size_t>(cols);
	if (N == 0) return;

	// 0) min/max 1패스 (스레드별 로컬 → 병합) : 스트리밍
	int nt = 1;
#ifdef _OPENMP
	nt = std::max(1, omp_get_max_threads());
#endif
	std::vector<float> tmin(nt, FLT_MAX), tmax(nt, -FLT_MAX);

#pragma omp parallel for schedule(static)
	for (int i = 0; i < rows; ++i) {
#ifdef _OPENMP
		const int tid = omp_get_thread_num();
#else
		const int tid = 0;
#endif
		const float* p = img.ptr<float>(i);
		float lmin = tmin[tid], lmax = tmax[tid];
		for (int j = 0; j < cols; ++j) {
			float v = p[j];
			if (v < lmin) lmin = v;
			if (v > lmax) lmax = v;
		}
		tmin[tid] = lmin;
		tmax[tid] = lmax;
	}
	float vmin = FLT_MAX, vmax = -FLT_MAX;
	for (int t = 0; t < nt; ++t) {
		if (tmin[t] < vmin) vmin = tmin[t];
		if (tmax[t] > vmax) vmax = tmax[t];
	}
	if (!(vmax > vmin)) { img.setTo(0); return; }

	// 1) 히스토그램 기반 퍼센타일
	constexpr int BINS = 4096;
	const float eps = 1e-8f;
	const float invWidth = (BINS - 1) / (vmax - vmin + eps);

	std::vector<std::vector<uint32_t>> localH(nt, std::vector<uint32_t>(BINS));		// 0으로 채워져 초기화.

#pragma omp parallel for schedule(static)
	for (int i = 0; i < rows; ++i) {
#ifdef _OPENMP
		const int tid = omp_get_thread_num();
#else
		const int tid = 0;
#endif
		auto& hist = localH[tid];
		const float* p = img.ptr<float>(i);
		for (int j = 0; j < cols; ++j) {
			int bin = (int)((p[j] - vmin) * invWidth + 0.5f);
			if (bin < 0) bin = 0;
			else if (bin >= BINS) bin = BINS - 1;
			hist[bin]++;
		}
	}

	std::vector<uint32_t> hist(BINS);
	for (int t = 0; t < nt; ++t) {
		const auto& h = localH[t];
		for (int b = 0; b < BINS; ++b) hist[b] += h[b];
	}

	// 누적합으로 퍼센타일 bin 찾기 + bin 내부 보간
	const size_t kL = (size_t)std::round(lower_percentile * 0.01f * (N - 1));
	const size_t kU = (size_t)std::round(upper_percentile * 0.01f * (N - 1));

	auto bin_to_value = [&](int bin, float frac)->float {
		const float binWidth = (vmax - vmin) / (float)BINS;
		const float start = vmin + bin * binWidth;
		return start + frac * binWidth;
		};

	auto quantile_from_hist = [&](size_t k)->float {
		size_t cum = 0;
		for (int b = 0; b < BINS; ++b) {
			uint32_t cnt = hist[b];
			if (cum + cnt > k) {
				float inside = (float)(k - cum) / (float)cnt;
				return bin_to_value(b, inside);
			}
			cum += cnt;
		}
		return vmax;
		};

	const float lower_bound = quantile_from_hist(kL);
	float upper_bound = quantile_from_hist(kU) * 1.5f;

	// 2) 정규화 상수 사전계산
	float range = upper_bound - lower_bound;
	if (range < eps) range = eps;
	const float denom = 1.0f / (log1pf(range) + eps); // log1p(range)

	// 3) 한 패스 변환
#pragma omp parallel for schedule(static)
	for (int i = 0; i < rows; ++i) {
		float* p = img.ptr<float>(i);
		for (int j = 0; j < cols; ++j) {
			float v = p[j] - lower_bound;              // shift
			if (v < 0.0f) v = 0.0f;                    // clamp low
			else if (v > range) v = range;             // clamp high
			v = log1pf(v + eps) * denom;               // log1p + normalize
			p[j] = v;
		}
	}
}

double COCTImaging::euclidean_distance(cv::Point2f pt1, cv::Point2f pt2) {
	return std::sqrt(std::pow(pt1.x - pt2.x, 2) + std::pow(pt1.y - pt2.y, 2));
}

std::vector<int> COCTImaging::find_outliers(const std::vector<int>& y_values) {
	std::vector<int> sorted_values = y_values;
	std::sort(sorted_values.begin(), sorted_values.end());

	// 1사분위수(Q1)와 3사분위수(Q3)를 계산
	int q1 = sorted_values[sorted_values.size() / 4];
	int q3 = sorted_values[3 * sorted_values.size() / 4];
	int iqr = q3 - q1;

	// 아웃라이어의 범위는 Q1 - 1.5 * IQR 이하이거나, Q3 + 1.5 * IQR 이상인 값
	int lower_bound = q1 - 1.5 * iqr;
	int upper_bound = q3 + 1.5 * iqr;

	// 아웃라이어 인덱스를 찾음
	std::vector<int> outlier_indices;
	for (int i = 0; i < y_values.size(); ++i) {
		if (y_values[i] < lower_bound || y_values[i] > upper_bound) {
			outlier_indices.push_back(i);
		}
	}

	return outlier_indices;
}

void COCTImaging::sharpening(cv::Mat& img) {
	cv::Mat origin = img.clone();
	origin.convertTo(origin, CV_32F);

	cv::Mat blur;
	GaussianBlur(origin, blur, cv::Size(9, 9), 0);

	cv::Mat originFFT = computeFFT(origin);
	cv::Mat blurFFT = computeFFT(blur);

	// 고주파 추출: originFFT - blurFFT
	cv::Mat highFreq;
	subtract(originFFT, blurFFT, highFreq);

	// Sharpened :originFFT + highFreq
	cv::Mat sharpenedFFT;
	add(originFFT, highFreq, sharpenedFFT);

	// DeFFT
	origin = inverseFFT(sharpenedFFT);

	cv::normalize(origin, origin, 0, 255, cv::NORM_MINMAX);
	origin.convertTo(img, CV_8U);
}

void COCTImaging::get_PDF_array(cv::Mat& img, std::vector<double>& pdf_i, bool& AGCWD_apply) {
	int number_of_pixels = img.rows * img.cols;
	// codesonar suppr C read-past-null-terminator
	pdf_i.assign(256, 0.0);

	// Histogram 계산
	for (int y = 0; y < img.rows; y++) {
		for (int x = 0; x < img.cols; x++) {
			int intensity = img.at<uchar>(y, x);
			pdf_i[intensity]++;
		}
	}

	// PDF를 위한 Histogram 정규화
	for (int i = 0; i < 256; i++) {
		pdf_i[i] /= number_of_pixels;
	}

	// Entropy를 활용하여 CDF 계산 필요 여부 확인
	double current_entropy = 0.0;
	for (double p : pdf_i) {
		if (p > 0) {
			current_entropy -= p * std::log(p);
		}
	}

	AGCWD_apply = false;
	double entropy_difference = std::abs(current_entropy - last_entropy);
	if (last_entropy == 0 || entropy_difference > entropy_threshold) {
		last_entropy = current_entropy;
		AGCWD_apply = true;
	}
}

void COCTImaging::get_CDF_array(std::vector<double> pdf_i, std::vector<double>& cdf_i) {
	cdf_i.resize(256);

	double pdf_min_val = *std::min_element(pdf_i.begin(), pdf_i.end(), [](double a, double b) { return (a > 0 && (b == 0 || a < b)); });
	double pdf_max_val = *std::max_element(pdf_i.begin(), pdf_i.end());

	// AGCWD에 알맞게 PDF를 PDFw로 계산
	std::vector<double> pdfw_i(256, pdf_min_val);
	for (int i = 0; i < 256; i++) {
		if (pdf_i[i] > 0) {
			pdfw_i[i] = pdf_min_val * std::pow((pdf_i[i] - pdf_min_val) / (pdf_max_val - pdf_min_val), alpha);
		}
	}

	// cumulative distribution function (CDF) 계산
	double pdf_sum = std::accumulate(pdfw_i.begin(), pdfw_i.end(), 0.0);
	double cumulative = 0.0;

	if (pdf_sum > 0) {
		for (int i = 0; i < 256; i++) {
			cumulative += pdfw_i[i] / pdf_sum;
			cdf_i[i] = cumulative;
		}
	}
	else {
		PLOGI.printf("pdf_sum value is zero. zero should not be used to divide any value");
		for (int i = 0; i < 256; i++) {
			cumulative += pdfw_i[i];
			cdf_i[i] = cumulative;
		}
	}
}

void COCTImaging::on_trackbar(int, void*) {
	const char* strWindowName = "Compensation";
	try {
		// 트랙바 값은 int로만 입력 가능하므로, 이를 원하는 범위로 변환
		EXPONENTIAL_FACTOR = cv::getTrackbarPos("Cont", strWindowName) / 10.0f;
		BRIGHTNESS_CONTROL = cv::getTrackbarPos("Bright", strWindowName) / 10.0f;
		ENERGY_THRESHOLD = cv::getTrackbarPos("Eng", strWindowName) / 10.0f;
		alpha = cv::getTrackbarPos("Alpha", strWindowName) / 10.f;
		INTENSITY_THRESHOLD = cv::getTrackbarPos("GCTH", strWindowName);

	}
	catch (const cv::Exception& e) {
		PLOGI.printf("OpenCV Error in on_trackbar: %s", e.what());  // OpenCV 관련 에러 처리
	}
	catch (const std::exception& e) {
		PLOGI.printf("Standard Error in on_trackbar: %s", e.what());  // 다른 표준 라이브러리 예외 처리
	}
	catch (...) {
		PLOGI.printf("Unknown error occurred in on_trackbar");  // 예기치 않은 에러 처리
	}
}

cv::Mat COCTImaging::computeFFT(cv::Mat& img) {
	cv::Mat planes[] = { img.clone(), cv::Mat::zeros(img.size(), CV_32F) };
	cv::Mat complexImg;
	merge(planes, 2, complexImg);
	dft(complexImg, complexImg);
	return complexImg;
}

cv::Mat COCTImaging::inverseFFT(cv::Mat& complexImg) {
	cv::Mat invDFT, planes[2];
	idft(complexImg, invDFT);
	split(invDFT, planes);
	magnitude(planes[0], planes[1], invDFT);
	return invDFT;
}

void COCTImaging::GetGuideWireCenterPoint(cv::Mat image,
	std::vector<cv::Rect2f> GuideWires,
	std::vector<cv::Point>& centerPoints,
	std::vector<float>& radius)
{
	if (GuideWires.empty()) {
		return;
	}
	centerPoints.clear();
	radius.clear();

	std::vector<cv::Point> edgePoints; // GuideWire에서 sheath 중심에 가장 가까운 점
	std::vector<double> theta;

	cv::Mat grayImage;
	switch (image.channels()) {
	case 3:
		cv::cvtColor(image, grayImage, cv::COLOR_BGR2GRAY);
		break;
	case 4:
		cv::cvtColor(image, grayImage, cv::COLOR_BGRA2GRAY);
		break;
	default:
		grayImage = image.clone();
	}

	GetGuideWireCircleEdgePoints(grayImage, GuideWires, edgePoints);
	InterpolateEdgePoints(edgePoints);
	GetGuideWireShadowPointAngles(grayImage, edgePoints, theta);

	if (theta.empty()) {
		PLOGI.printf("GuideWire Detection Fail");
		centerPoints.push_back(cv::Point(-1, -1));
		radius.push_back(-1);
		return;
	}

	// edgePoints와 theta는 같은 인덱스끼리 매칭
	int centerX = image.cols / 2;
	int centerY = image.rows / 2;

	for (size_t i = 0; i < edgePoints.size(); i++) {
		int XDirection = 0, YDirection = 0;
		double guideWireRadius = -1.0, angle = 0.0;

		if (edgePoints[i].x != -1 && edgePoints[i].y != -1 && i < theta.size()) {
			cv::Vec2d edgeVector(edgePoints[i].x - centerX, edgePoints[i].y - centerY);
			double centerToEdgePointDistance = std::sqrt(
				(double)(centerX - edgePoints[i].x) * (centerX - edgePoints[i].x) +
				(double)(centerY - edgePoints[i].y) * (centerY - edgePoints[i].y));

			if (std::abs(1 - std::sin(theta[i])) > 1e-9) {
				guideWireRadius = std::abs(centerToEdgePointDistance * std::sin(theta[i]) /
					(1 - std::sin(theta[i])));
			}
			else {
				guideWireRadius = -1.0;
			}

			cv::Vec2d unitVector = edgeVector[0] > 0 ? cv::Vec2d(1, 0) : cv::Vec2d(-1, 0);
			GetAcuteAngleToXAxis(edgeVector, unitVector, angle);

			if (edgeVector[0] != 0) XDirection = edgeVector[0] / std::abs(edgeVector[0]);
			if (edgeVector[1] != 0) YDirection = edgeVector[1] / std::abs(edgeVector[1]);
		}

		centerPoints.push_back(cv::Point(
			(int)(edgePoints[i].x + XDirection * guideWireRadius * std::cos(angle)),
			(int)(edgePoints[i].y + YDirection * guideWireRadius * std::sin(angle))));
		radius.push_back((float)guideWireRadius);
	}
}

void COCTImaging::GetGuideWireCircleEdgePoints(cv::Mat grayImage,
	std::vector<cv::Rect2f> GuideWires,
	std::vector<cv::Point>& edgePoints)
{
	int paddingSize = 1;
	for (const auto& rect : GuideWires) {
		if (rect.width == 0 && rect.height == 0) {
			edgePoints.push_back(cv::Point(-1, -1));
			continue;
		}

		int width = grayImage.cols;
		int height = grayImage.rows;
		int centerX = width / 2;
		int centerY = height / 2;
		double sigma = 100.0;

		cv::Mat mask(height, width, CV_8UC1);
		for (int y = 0; y < height; ++y) {
			for (int x = 0; x < width; ++x) {
				double dx = x - centerX;
				double dy = y - centerY;
				double distanceSquared = dx * dx + dy * dy;

				// 2D Gaussian Formula
				double value = std::exp(-distanceSquared / (2 * sigma * sigma));
				mask.at<uchar>(y, x) = static_cast<uchar>(value * 255.0);
			}
		}

		cv::Mat filtered;
		cv::Mat floatImage, floatMask;
		grayImage.convertTo(floatImage, CV_32F, 1.0 / 255.0);
		mask.convertTo(floatMask, CV_32F, 1.0 / 255.0);
		cv::multiply(floatImage, floatMask, filtered);
		filtered.convertTo(filtered, CV_8U, 255.0);

		// top 3 pixels에 대한 Mask 작업을 위한 Roi Padding 설정
		if (rect.x - paddingSize < 0 || rect.y - paddingSize < 0 ||
			rect.x + rect.width + paddingSize > filtered.cols ||
			rect.y + rect.height + paddingSize > filtered.rows) {
			continue;
		}

		int rx = std::max(0, cvFloor(rect.x) - paddingSize);
		int ry = std::max(0, cvFloor(rect.y) - paddingSize);
		int rw = std::min(filtered.cols - rx, cvCeil(rect.width) + 2 * paddingSize);
		int rh = std::min(filtered.rows - ry, cvCeil(rect.height) + 2 * paddingSize);
		if (rw <= 0 || rh <= 0) continue;

		cv::Rect roiRect(rx, ry, rw, rh);
		cv::Mat roi = filtered(roiRect);

		cv::Mat roiInt;
		if (roi.type() != CV_8U) roi.convertTo(roiInt, CV_8U);
		else roiInt = roi;

		int orw = std::max(0, std::min(cvCeil(rect.width), roiInt.cols - paddingSize));
		int orh = std::max(0, std::min(cvCeil(rect.height), roiInt.rows - paddingSize));
		if (orw <= 0 || orh <= 0) continue;

		cv::Rect originalRoiRect(paddingSize, paddingSize, orw, orh);
		if ((originalRoiRect & cv::Rect(0, 0, roiInt.cols, roiInt.rows)) != originalRoiRect) continue;

		// Roi Padding 없는 기존 GuideWire Rectangle Roi
		cv::Mat originalRoi = roiInt(originalRoiRect);

		std::vector<std::pair<int, cv::Point>> pixelValues;
		for (int y = 0; y < originalRoi.rows; y++) {
			for (int x = 0; x < originalRoi.cols; x++) {
				pixelValues.emplace_back(originalRoi.at<uchar>(y, x), cv::Point(x, y));
			}
		}
		if (pixelValues.empty()) continue;

		std::sort(pixelValues.begin(), pixelValues.end(),
			[](auto& a, auto& b) { return a.first > b.first; });

		std::vector<cv::Point> top3Points;
		for (int i = 0; i < 30 && i < (int)pixelValues.size(); i++) {
			top3Points.push_back(pixelValues[i].second);
		}

		// top 3 pixels에 대한 3x3 분류 작업
		std::vector<std::pair<int, cv::Point>> avgValues;
		for (const auto& pt : top3Points) {
			int startX = pt.x;
			int startY = pt.y;
			int width = paddingSize * 2 + 1;
			int height = paddingSize * 2 + 1;
			cv::Rect region(startX, startY, width, height);
			region &= cv::Rect(0, 0, roiInt.cols, roiInt.rows); // 경계 체크
			if (region.width <= 0 || region.height <= 0) continue;

			cv::Mat regionMat = roiInt(region);
			int sum = cv::sum(regionMat)[0];
			int regionAvg = (int)((double)sum / (region.width * region.height));
			avgValues.emplace_back(regionAvg, pt);
		}
		if (avgValues.empty()) continue;

		auto maxAvgIt = std::max_element(avgValues.begin(), avgValues.end(),
			[](auto& a, auto& b) { return a.first < b.first; });

		if (maxAvgIt != avgValues.end()) {
			cv::Point maxAvgPoint = maxAvgIt->second + cv::Point((int)rect.x, (int)rect.y);
			edgePoints.push_back(maxAvgPoint);
		}
	}
}

void COCTImaging::GetGuideWireShadowPointAngles(cv::Mat grayImage, std::vector<cv::Point> edgePoints, std::vector<double>& theta) {
	/*int height = m_nHeight;
	int width = m_nWidth;*/
	theta.clear();
	static int myint = 0;

	for (const auto& edgePoint : edgePoints) {
		if (edgePoint.x == -1 || edgePoint.y == -1) {
			theta.push_back(0);
			continue;
		}
		//cv::Mat cloneImage = grayImage.clone();

		//cv::Mat mask = (cloneImage == 255);
		//cloneImage.setTo(0, mask);
		//cloneImage.at<uchar>(edgePoint.y, edgePoint.x) = 255;

		//cv::Mat inversedImage;
		//cv::remap(cloneImage, inversedImage, inverseMatXMap, inverseMatYMap, cv::INTER_NEAREST);
		//cv::rotate(inversedImage, inversedImage, cv::ROTATE_90_COUNTERCLOCKWISE);

		//cv::Point inversedEdgePoint;
		//for (int y = 0; y < height; y++) {
		//	for (int x = 0; x < width; x++) {
		//		if (inversedImage.at<uchar>(y, x) == 255) {
		//			inversedEdgePoint = cv::Point(x, y);
		//			break;
		//		}
		//	}
		//}
		//int startY, startX, endX;

		//startY = inversedEdgePoint.y;
		//startX = 0;
		//endX = inversedEdgePoint.x - 100 < 0 ? inversedEdgePoint.x / 2 : inversedEdgePoint.x - 100;

		////GuideWire 중심점 row에 대한 pixel Value 합
		//double sumOfStandardValue = 0;
		//for (int x = startX; x <= endX; x++) {
		//	sumOfStandardValue += inversedImage.at<uchar>(startY, x);
		//}

		//double gap = 0;
		//int series = 0;
		//int rotationTimes = 0;

		//// + y 방향 탐색
		//for (int y = startY; y < height; y += 1, gap += 1.0) {
		//	int sumOfPixelValues = 0;
		//	for (int x = startX; x <= endX; x++) {
		//		sumOfPixelValues += inversedImage.at<uchar>(y, x);
		//	}

		//	PLOGI.printf("row %d : sumOfPixelValues = %d", y, sumOfPixelValues);

		//	if (sumOfPixelValues >= sumOfStandardValue * 1.5) {
		//		series++;
		//		if (series == 3) {
		//			PLOGI.printf("end_row1 %d : sumOfPixelValues = %d", y, sumOfPixelValues);
		//			series = 0;
		//			break;
		//		}
		//	}

		//	if (y == height - 1) {
		//		rotationTimes++;
		//		if (rotationTimes >= 2) {
		//			rotationTimes = 0;
		//			break;
		//		}
		//		y = 0;
		//	}
		//}

		//// - y 방향 탐색
		//for (int y = startY; y >= 0; y -= 1, gap += 1.0) {
		//	int sumOfPixelValues = 0;
		//	for (int x = startX; x <= endX; x++) {
		//		sumOfPixelValues += inversedImage.at<uchar>(y, x);
		//	}

		//	PLOGI.printf("row %d : sumOfPixelValues = %d", y, sumOfPixelValues);

		//	if (sumOfPixelValues >= sumOfStandardValue * 1.5) {
		//		series++;
		//		if (series == 3) {
		//			PLOGI.printf("end_row2 %d : sumOfPixelValues = %d", y, sumOfPixelValues);
		//			break;
		//		}
		//	}

		//	if (y == 0) {
		//		rotationTimes++;
		//		if (rotationTimes >= 2) {
		//			rotationTimes = 0;
		//			break;
		//		}
		//		y = height - 1;
		//	}
		//}

		double angle = 360.0 / m_nHeight * 45;

		if (!(angle >= 10.0 && angle <= 20.0)) { // Error 값 처리
			angle = 15.0; // Normal 값으로 Set
		}

		double tmp_theta = angle * CV_PI / 180.0;
		theta.push_back(tmp_theta);
	}
}

void COCTImaging::InterpolateEdgePoints(std::vector<cv::Point>& edgePoints) {
	int n = (int)edgePoints.size();
	for (int i = 0; i < n; ++i) {
		if (edgePoints[i].x == -1 || edgePoints[i].y == -1) {
			// 앞뒤에서 유효한 값을 찾음
			int prev = i - 1;
			int next = i + 1;

			// 이전 유효한 포인트 찾기
			while (prev >= 0 && (edgePoints[prev].x == -1 || edgePoints[prev].y == -1)) prev--;

			// 다음 유효한 포인트 찾기
			while (next < n && (edgePoints[next].x == -1 || edgePoints[next].y == -1)) next++;

			if (prev >= 0 && next < n && next != prev) {
				// 선형 보간
				float alpha = float(i - prev) / float(next - prev);
				int interpX = (int)((1 - alpha) * edgePoints[prev].x + alpha * edgePoints[next].x);
				int interpY = (int)((1 - alpha) * edgePoints[prev].y + alpha * edgePoints[next].y);
				edgePoints[i] = cv::Point(interpX, interpY);
			}
			// 양쪽 중 한 쪽만 유효할 경우: 가장 가까운 값으로 대체
			else if (prev >= 0) edgePoints[i] = edgePoints[prev];
			else if (next < n) edgePoints[i] = edgePoints[next];
			// 둘 다 없으면 (끝에서 전부 -1): 무시하거나 (0,0) 처리
		}
	}
}

void COCTImaging::GetCircularizeTransformPoint(cv::Point src, cv::Point& dst) {
	int diameter = m_setting.nAScan;

	// 평행이동 값
	int dx = diameter / 2;
	int dy = diameter / 2;

	// 좌표 변환 값
	double r = (diameter - src.x) / 2.0;
	double theta = (2.0 * CV_PI / diameter) * src.y;

	double cosTheta = std::cos(theta);
	double scale = (std::abs(cosTheta) > 1e-9) ? 1.0 / std::abs(cosTheta) : 1.0;

	// 좌표변환 식
	int fx = (int)std::round(scale * r * std::cos(theta) + dx);
	int fy = (int)std::round(scale * r * std::sin(-theta) + dy);

	dst = cv::Point(fx, fy);
}

void COCTImaging::GetAcuteAngleToXAxis(cv::Vec2d vector1, cv::Vec2d vector2, double& angle) {
	// 벡터 크기 계산
	double vector1Magnitude = std::sqrt(vector1[0] * vector1[0] + vector1[1] * vector1[1]);
	double vector2Magnitude = std::sqrt(vector2[0] * vector2[0] + vector2[1] * vector2[1]);

	if (vector1Magnitude < 1e-12 || vector2Magnitude < 1e-12) {
		angle = 0.0;
		return;
	}

	// 벡터와 X축 간의 내적 계산
	double dotProduct = vector1[0] * vector2[0] + vector1[1] * vector2[1];

	// 코사인 각도 계산
	double cosTheta = dotProduct / (vector1Magnitude * vector2Magnitude);
	if (cosTheta > 1.0) cosTheta = 1.0;
	else if (cosTheta < -1.0) cosTheta = -1.0;

	// 각도 계산 (라디안)
	angle = std::acos(cosTheta);
}

#include <chrono>

void COCTImaging::SetCalciumAngle(std::vector<std::vector<cv::Point>> calciumContours,
	int& angleNum,
	std::vector<int>& startAngle,
	std::vector<int>& endAngle,
	int frameNum)
{
	std::chrono::system_clock::time_point m_start, m_end;
	m_end = std::chrono::system_clock::now();
	std::chrono::milliseconds total_time = std::chrono::duration_cast<std::chrono::milliseconds>(m_end - m_start);
	double m_fps = 1000.f / total_time.count();

	startAngle.clear();
	endAngle.clear();
	angleNum = 0;

	if (calciumContours.empty())
		return;

	cv::Point2d center(m_nWidth / 2.0, m_nHeight / 2.0);

	const double MIN_AREA = 500.0;       // 너무 작은 칼슘 무시
	const double MIN_ANGLE_SPAN = 1.0;   // 1도 이하는 잡음으로 무시

	for (const auto& contour : calciumContours)
	{
		double area = cv::contourArea(contour);
		if (area <= MIN_AREA)
			continue;

		// 1) 컨투어의 모든 점을 중심 기준 각도(0~360)로 변환
		std::vector<double> angles;
		angles.reserve(contour.size());

		for (const auto& pt : contour)
		{
			// GetTheta는 0도 = 12시, 시계방향 증가" 를 반환한다고 가정
			double theta = GetTheta(pt, center);   // [0, 360) 범위
			if (theta < 0.0)   theta += 360.0;
			if (theta >= 360.0) theta -= 360.0;
			angles.push_back(theta);
		}

		if (angles.empty())
			continue;

		// 2) 각도들을 정렬 후, 가장 큰 gap을 찾아
		//    "가장 큰 gap을 제외한 나머지"를 하나의 연속된 arc로 간주
		std::sort(angles.begin(), angles.end());
		int n = static_cast<int>(angles.size());
		double maxGap = -1.0;
		int maxGapIdx = -1;

		for (int i = 0; i < n; ++i)
		{
			double a1 = angles[i];
			double a2 = (i + 1 < n) ? angles[i + 1] : angles[0] + 360.0;
			double gap = a2 - a1;
			if (gap > maxGap)
			{
				maxGap = gap;
				maxGapIdx = i;
			}
		}

		if (maxGapIdx < 0)
			continue;

		// 가장 큰 gap 다음부터, gap 직전까지가 실제 calcium arc
		double arcStart = angles[(maxGapIdx + 1) % n];
		double arcEnd = angles[maxGapIdx];

		if (arcEnd < arcStart)
			arcEnd += 360.0;   //  예: 350~10도, 350~370도로 취급

		double span = arcEnd - arcStart;
		if (span < MIN_ANGLE_SPAN)
			continue;

		// 3) 0도(=12시)를 기준으로 잘라야 하는 경우 처리
		//    - arcEnd <= 360 : 0 안 넘음, 그대로 사용
		//    - arcEnd >  360 : 0을 넘어서 wrap, [start,360], [0, arcEnd-360] 두 개로 분할
		if (arcEnd <= 360.0)
		{
			int s = static_cast<int>(std::round(arcStart));
			int e = static_cast<int>(std::round(arcEnd));
			startAngle.push_back(s);
			endAngle.push_back(e);
		}
		else
		{
			// 첫번째 조각: [arcStart, 360]
			double arcEnd1 = 360.0;
			double span1 = arcEnd1 - arcStart;

			if (span1 >= MIN_ANGLE_SPAN)
			{
				int s1 = static_cast<int>(std::round(arcStart));
				int e1 = 360;
				startAngle.push_back(s1);
				endAngle.push_back(e1);
			}

			// 두번째 조각: [0, arcEnd-360]
			double arcStart2 = 0.0;
			double arcEnd2 = arcEnd - 360.0;
			double span2 = arcEnd2 - arcStart2;

			if (span2 >= MIN_ANGLE_SPAN)
			{
				int s2 = 0;
				int e2 = static_cast<int>(std::round(arcEnd2));
				startAngle.push_back(s2);
				endAngle.push_back(e2);
			}
		}
	}

	PLOGI.printf("m_fps = %.4lf", m_fps);

	angleNum = static_cast<int>(startAngle.size());
}



double COCTImaging::GetTheta(cv::Point point, cv::Point center) {

	int dx = point.x - center.x;
	int dy = point.y - center.y;
	double angle = std::atan2(dx, -dy) * 180.0 / CV_PI;
	if (angle < 0) {
		angle += 360.0;
	}
	return angle;
}

cv::Point2f COCTImaging::matXY(const cv::Point2f& srcPt, int m_nWidth, int m_nHeight)
{
	float bestDist = std::numeric_limits<float>::max();
	cv::Point2f bestXY(0.f, 0.f);
	for (int y = 0; y < m_nHeight; y++)
	{
		const float* xMapRow = matXMap.ptr<float>(y);
		const float* yMapRow = matYMap.ptr<float>(y);

		for (int x = 0; x < m_nWidth; x++)
		{
			float dx = xMapRow[x] - srcPt.x;
			float dy = yMapRow[x] - srcPt.y;
			float distSq = dx * dx + dy * dy;

			if (distSq < bestDist)
			{
				bestDist = distSq;
				bestXY = cv::Point2f((float)x, (float)y);
			}
		}
	}
	float cx = 0.5f * m_nWidth;
	float cy = 0.5f * m_nHeight;
	float xShift = bestXY.x - cx;
	float yShift = bestXY.y - cy;
	float rx = yShift;
	float ry = -xShift;
	rx += cx;
	ry += cy;
	cv::Point test;
	test.x = rx; test.y = ry;
	cv::Point center(m_nWidth / 2, m_nHeight / 2);
	test = rotatePoint_CCW90(test, center);
	test = rotatePoint_CCW90(test, center);
	return cv::Point2f(test.x, test.y);
}

// 시계방향 회전
cv::Point2f COCTImaging::rotatePoint_CCW90(const cv::Point2f& point, const cv::Point2f& center)
{
	float translatedX = static_cast<float>(point.x) - center.x;
	float translatedY = static_cast<float>(point.y) - center.y;
	float rotatedX = -translatedY;
	float rotatedY = translatedX;
	int finalX = static_cast<int>(std::round(rotatedX + center.x));
	int finalY = static_cast<int>(std::round(rotatedY + center.y));
	return cv::Point(finalX, finalY);
}




