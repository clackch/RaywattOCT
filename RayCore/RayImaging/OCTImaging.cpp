#include "OCTImaging.h"
#include "Calibration.h"
#include "LookUpTable.h"
#include "Utility.h"
#include "MessageService.h"
#include "opencv2/opencv.hpp"
#include <omp.h>

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

COCTImaging::COCTImaging(Setting setting, CMessageService* pMsg) {
	m_setting = setting;
	m_msg = pMsg;

	m_pThread = nullptr;
	m_waitForFringes = true;
	m_pFringesBuffer = nullptr;

	calibration = nullptr;

	fringes32f = nullptr;
	fringes32fAverage = nullptr;

	fBuffer_Window = nullptr;
	fcBuffer_FFT = nullptr;
	fcBuffer_IFFT = nullptr;
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

	clahe = cv::createCLAHE(0.02, cv::Size(8, 8));
}

COCTImaging::~COCTImaging() {
	releaseMemory();
	if (calibration != nullptr) delete calibration;
}

void COCTImaging::Initialize(CCalibration* calibration) {
	PLOGI.printf("initialize1");
	releaseMemory();
	LOGI.printf("initialize2");
	allocateMemory();
	LOGI.printf("initialize3");
	this->calibration = calibration;
	LOGI.printf("initialize4");
	releaseCircularizeMap();
	LOGI.printf("initialize5");
	initCircularizeMap(m_setting.nOutputLength, m_setting.nBScan, m_setting.nOutputLength, m_setting.nCircleSize, m_setting.nCircleSize, 2.0f);
	LOGI.printf("initialize6");
	m_nWidth = m_setting.nCircleSize;

	m_nHeight = m_setting.nCircleSize;
	m_nChannels = 3;	// RGB
}
void COCTImaging::Process(char* fringes) {
	if (fringes == nullptr) return;

	generateBackground((Ipp16u*)fringes);
	fftProcessing(fringes32f);
	computeLogarithm(fFFTResult, fFFTResult);
	findSheath(fFFTResult);
	generateImage(fFFTResult, false);
	adaptive_compensation();
}
void COCTImaging::PostProcess(cv::Mat image) {
	const bool bInvert = m_bInvert;
	const bool bColor = m_bColor;

	findSheath(image);

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
		drawGuideLine(imageResultColor, m_measureSetting.nSheathPosition, cv::Scalar(0xff, 0xcc, 0x33));
		drawGuideLine(imageResultColor, m_nSheathPosition, cv::Scalar(0xff, 0xff, 0xff));
	}

	CircularizeImage(imageResultColor, imageCircle);
}
void COCTImaging::ApplyZOffset(const cv::Mat& src, cv::Mat& dst, int zOffset) {
	cv::Mat img = src.clone();

	cv::Mat translation_matrix = (cv::Mat_<double>(2, 3) << 1, 0, zOffset * -1, 0, 1, 0);
	cv::warpAffine(img, dst, translation_matrix, img.size());
}
int COCTImaging::Start() {
	PLOGI.printf("007 or 010 -1");
	BOOL result = FALSE;
	PLOGI.printf("007 or 010 -2");
	result = CUtility::StartThread(threadRender, m_pThread, (LPVOID)this);
	PLOGI.printf("007 or 010 -3");

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
	cv::remap(src, dst, matXMap, matYMap, cv::INTER_LINEAR);

	cv::Mat imgFoV = getFoVImage(dst, MAX_FIELD_OF_VIEW);
	memcpy(dst.data, imgFoV.data, sizeof(char) * dst.cols * dst.rows * imgFoV.channels());
}

void COCTImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst) {}

void COCTImaging::EraseStentOutLier(cv::Mat& stent) {}

void COCTImaging::SetLumenContourOffset(std::vector<cv::Point> lumenContour) {}

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
	const int nBufferSize = m_setting.nBufferSize;
	const int nCircleSize = m_setting.nCircleSize;

	fringes32f = ippsMalloc_32f(nAScan * nBScan);
	fringes32fAverage = ippsMalloc_32f(nAScan);

	imageResult.create(nBScan, nOutputLength, CV_8UC1);
	imageResultColor.create(nBScan, nOutputLength, CV_8UC3);
	imageCircle.create(nCircleSize, nCircleSize, CV_8UC3);
	imageResultWithoutCompensation.create(nBScan, nOutputLength, CV_8UC1);

	fBuffer_Window = ippsMalloc_32f(nFFTLength);
	fcBuffer_FFT = ippsMalloc_32fc(nFFTLength);
	fcBuffer_IFFT = ippsMalloc_32fc(nFFTLength);
	fFFTResult = ippsMalloc_32f(nOutputLength * nBScan);
	fOutput = ippsMalloc_32f(nOutputLength * nBScan);

	// Prepare FFT
	ippsFFTInitAlloc_R_32f(&fftSpecFirst, nFFTOrder, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	ippsFFTInitAlloc_C_32fc(&ifftSpec, nFFTOrder, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	ippsFFTInitAlloc_C_32fc(&fftSpecSecond, nFFTOrder - 1, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
}
void COCTImaging::releaseMemory() {
	if (fringes32f) { ippsFree(fringes32f); fringes32f = nullptr; }
	if (fringes32fAverage) { ippsFree(fringes32fAverage); fringes32fAverage = nullptr; }

	imageResult.release();
	imageResultColor.release();
	imageCircle.release();
	imageResultWithoutCompensation.release();

	ippsRelease((void*&)fBuffer_Window);
	ippsRelease((void*&)fcBuffer_FFT);
	ippsRelease((void*&)fcBuffer_IFFT);
	ippsRelease((void*&)fFFTResult);
	ippsRelease((void*&)fOutput);

	if (fftSpecFirst) { ippsFFTFree_R_32f(fftSpecFirst); fftSpecFirst = nullptr; }
	if (ifftSpec) { ippsFFTFree_C_32fc(ifftSpec); ifftSpec = nullptr; }
	if (fftSpecSecond) { ippsFFTFree_C_32fc(fftSpecSecond); fftSpecSecond = nullptr; }
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

			matXMap.at<float>(x * dstHeight + y) = rvalue;
			matYMap.at<float>(x * dstHeight + y) = (float)(((atan2(fy, fx) / M_PI) + 1.0) * 0.5 * (srcHeight - 1));
		}
	}
}
void COCTImaging::releaseCircularizeMap() {
	matXMap.release();
	matYMap.release();
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

	// Process Frame
	// To-Do : enable openmp, check shared variables
	omp_set_dynamic(numDynamic);
	omp_set_num_threads(numThreads);
	//#pragma omp parallel
	{
		//#pragma omp for firstprivate(fBuffer_Window,fBuffer_BackgroundFringes,fcBuffer_FFT,fcBuffer_IFFT,j)
		for (int i = 0; i < nBScan; i++)
		{
			{
				// 1. Background Subtract
				ippsCopy_32f(fringes32f + i * nAScan, fBuffer_Window, nAScan);
				ippsSub_32f_I(fringes32fAverage, fBuffer_Window, nAScan);  // I의 의미:자기 자신에 이처리를 해서, 결과를 얻는다.

				// 2. Apply Window
				ippsMul_32f_I(calibration->window, fBuffer_Window, nFFTLength);

				// 3. First FFT
				ippsFFTFwd_RToPerm_32f_I(fBuffer_Window, fftSpecFirst, nullptr); // http://software.intel.com/sites/products/documentation/hpc/ipp/ipps/ipps_ch7/ch7_packed_formats.html#Perm
				ippsConjPerm_32fc(fBuffer_Window, fcBuffer_FFT, nFFTLength);

				// 4. Zero Pad & Reorder (1 | 2 | 0 | 0)
				ippsZero_32fc(fcBuffer_IFFT, nFFTLength);
				ippsCopy_32fc(fcBuffer_FFT, fcBuffer_IFFT, nOutputLength);

				// 5. Inverse FFT
				ippsFFTInv_CToC_32fc_I(fcBuffer_IFFT, ifftSpec, nullptr);

				// 6. Interpolation
				ippsZero_32fc(fcBuffer_FFT, nOutputLength);
				for (int j = 0; j < nAScan / 2; j++) {
					fcBuffer_FFT[j].re = (calibration->weightMap[j] * fcBuffer_IFFT[calibration->indexMap[j]].re + (1.0f - calibration->weightMap[j]) * fcBuffer_IFFT[calibration->indexMap[j] + 1].re);
					fcBuffer_FFT[j].im = (calibration->weightMap[j] * fcBuffer_IFFT[calibration->indexMap[j]].im + (1.0f - calibration->weightMap[j]) * fcBuffer_IFFT[calibration->indexMap[j] + 1].im);
				}

				// 7. Numerical Dispersion Compensation
				ippsMul_32fc_I((Ipp32fc*)calibration->dispersion, fcBuffer_FFT, nAScan / 2);

				// 8. FFT Again
				ippsFFTFwd_CToC_32fc_I(fcBuffer_FFT, fftSpecSecond, nullptr);

				// 9. Extract Magnitude
				ippsPowerSpectr_32fc(fcBuffer_FFT, fFFTResult + i * nOutputLength, nOutputLength);
			}
		}
	} // end parallel region

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

void COCTImaging::findSheath(cv::Mat img) {
	cv::Mat image;
	cv::rotate(img, image, cv::ROTATE_90_COUNTERCLOCKWISE);

	// 행의 평균과 분산 계산
	std::vector<double> mean_values;
	std::vector<double> variance_values;

	for (int i = 0; i < image.rows; ++i) {
		cv::Mat row = image.row(i);
		cv::Scalar mean, stddev;
		cv::meanStdDev(row, mean, stddev);
		mean_values.push_back(mean[0]);
		variance_values.push_back(stddev[0] * stddev[0]);  // 분산은 표준편차의 제곱
	}

	// 평균 값을 0~1로 정규화
	std::vector<double> mean_values_norm = normalize(mean_values);

	// 정규화된 평균이 0.9 이상인 행 필터링
	std::vector<std::tuple<int, double, double>> valid_rows;
	for (int i = 0; i < mean_values_norm.size(); ++i) {
		if (mean_values_norm[i] >= 0.9) {
			valid_rows.emplace_back(i, mean_values[i], variance_values[i]);
		}
	}

	if (!valid_rows.empty()) {
		// 분산이 가장 작은 행 찾기
		auto min_variance_row = *std::min_element(valid_rows.begin(), valid_rows.end(),
			[](const auto& a, const auto& b) { return std::get<2>(a) < std::get<2>(b); });
		m_nSheathPosition = std::get<0>(min_variance_row) + m_measureSetting.nSheathThickness;
	}
	else {
		m_nSheathPosition = 0;
	}
}

// 정규화를 위한 함수
std::vector<double> COCTImaging::normalize(const std::vector<double>& values) {
	double min_val = *std::min_element(values.begin(), values.end());
	double max_val = *std::max_element(values.begin(), values.end());
	std::vector<double> normalized;

	for (double val : values) {
		normalized.push_back((val - min_val) / (max_val - min_val));
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

	imageResultWithoutCompensation = imageResult.clone();
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
	roi.width = (int)(floor(round(fov * 1000.f / m_setting.distPerPixel))) * 2;
	roi.height = roi.width;
	roi.x = (image.cols - roi.width) / 2;
	roi.y = (image.rows - roi.height) / 2;

	if (roi.x < 0 || roi.y < 0 ||
		roi.width <= 0 || roi.height <= 0 ||
		roi.x + roi.width > image.cols ||
		roi.y + roi.height > image.rows) {
		return image.clone();
	}

	cv::Mat imgROI;
	cv::resize(image(roi), imgROI, cv::Size(image.cols, image.rows));

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

	// Rotate the image
	cv::Mat rotated_img;
	cv::rotate(imageResult, rotated_img, cv::ROTATE_90_COUNTERCLOCKWISE);
	rotated_img.convertTo(rotated_img, CV_32F);

	rotated_img(cv::Range(rotated_img.rows - 60, rotated_img.rows), cv::Range::all()).setTo(cv::Scalar(0));

	int rows = rotated_img.rows;
	int cols = rotated_img.cols;

	cv::Mat energy_all = cv::Mat::zeros(rotated_img.size(), CV_32F);
	cv::Mat result_img = cv::Mat::zeros(rotated_img.size(), CV_32F);

	std::vector<int> stop_rows(cols, 0);
	
	EXPONENTIAL_FACTOR = EXPONENTIAL_FACTOR <= -1.00f ? m_setting.exponentialFactor : EXPONENTIAL_FACTOR;
	BRIGHTNESS_CONTROL = BRIGHTNESS_CONTROL <= -1.00f ? m_setting.brightnessControl : BRIGHTNESS_CONTROL;
	ENERGY_THRESHOLD = ENERGY_THRESHOLD <= -1.00f ? m_setting.energyThreshold : ENERGY_THRESHOLD;
	INTENSITY_THRESHOLD = INTENSITY_THRESHOLD <= -1 ? m_setting.intensityThreshold : INTENSITY_THRESHOLD;
	// Compute energy using cumulative sum (with OpenMP)
#pragma omp parallel for
	for (int x = 0; x < cols; ++x) {
		cv::Mat I_n = rotated_img.col(x).clone(); // clone() 사용으로 독립적인 메모리

		// 자연 로그 계산 후 지수 연산 적용
		cv::Mat log_img, exp_img;
		cv::log(I_n + 1e-6, log_img);  // 1e-6을 추가해 로그 계산에서 0을 피함
		cv::exp(EXPONENTIAL_FACTOR * log_img, exp_img);  // EXPONENTIAL_FACTOR 적용 후 exp 사용
		I_n = exp_img.clone();  // 결과 저장

		// 누적 합 계산
		std::vector<float> cumulativeSum(rows, 0.0f);
		cumulativeSum[rows - 1] = I_n.at<float>(rows - 1);
		for (int i = rows - 2; i >= 0; --i) {
			cumulativeSum[i] = cumulativeSum[i + 1] + I_n.at<float>(i);
		}

		// 에너지 계산
		for (int z = 0; z < rows; ++z) {
			float sum_val = cumulativeSum[z];
			energy_all.at<float>(z, x) = sum_val * sum_val;

			if (sum_val < energy_all.at<float>(0, x) / std::pow(10.0, ENERGY_THRESHOLD)) {
				stop_rows[x] = z;

				break;
			}
		}

		double stop_cumsum = 0;

		// 결과 계산
		for (int z = 0; z < rows; ++z) {
			float sum_val = cumulativeSum[z];
			// threshold_row를 기준으로 보정 적용
			if (z <= stop_rows[x]) {
				float sum_val_pow = std::exp(BRIGHTNESS_CONTROL * std::log(sum_val)) * 2;
				if (sum_val != 0) {
					result_img.at<float>(z, x) = I_n.at<float>(z) / sum_val_pow;
					stop_cumsum = sum_val_pow;
				}
			}
			else {
				result_img.at<float>(z, x) = I_n.at<float>(z) / stop_cumsum;
			}
		}
	}
	// Linear contrast stretching
	logarithmic_contrast_stretching(result_img);
	result_img.convertTo(result_img, CV_8U, INTENSITY_THRESHOLD);

	if(m_setting.applyGammaCorrection)
		adaptive_gamma_correction(result_img, INTENSITY_THRESHOLD);

	// Rotate back to original angle
	cv::rotate(result_img, imageResult, cv::ROTATE_90_CLOCKWISE);
}

void COCTImaging::min_max_normalization(const cv::Mat& img, cv::Mat& normalized_img, double& min_val, double& max_val)
{
	cv::minMaxLoc(img, &min_val, &max_val);
	normalized_img = (img - min_val) / (max_val - min_val);
}

void COCTImaging::linear_contrast_stretching(cv::Mat& img, float lower_percentile, float upper_percentile)
{
	// 1. 1D 벡터로 변환 없이 퍼센타일 계산
	cv::Mat img_reshaped = img.reshape(1, img.rows * img.cols);  // 1D로 변환
	std::vector<float> img_values;
	img_values.assign((float*)img_reshaped.datastart, (float*)img_reshaped.dataend);

	// 2. 벡터 정렬
	std::sort(img_values.begin(), img_values.end());

	// 3. 퍼센타일 값 계산
	int total_elements = img_values.size();
	int lower_idx = static_cast<int>(lower_percentile / 100.0 * total_elements);
	int upper_idx = static_cast<int>(upper_percentile / 100.0 * total_elements);

	float lower_bound = img_values[lower_idx];
	float upper_bound = img_values[upper_idx];

	// 4. OpenMP 병렬 처리로 클리핑 및 정규화
#pragma omp parallel for
	for (int i = 0; i < img.rows; ++i) {
		float* img_ptr = img.ptr<float>(i);  // 한 번에 한 row의 데이터에 접근
		for (int j = 0; j < img.cols; ++j) {
			// 클리핑
			img_ptr[j] = std::min(std::max(img_ptr[j], lower_bound), upper_bound);
			// 0-1로 정규화
			img_ptr[j] = (img_ptr[j] - lower_bound) / (upper_bound - lower_bound + 1e-8);
		}
	}
}

void COCTImaging::logarithmic_contrast_stretching(cv::Mat& img, float lower_percentile, float upper_percentile)
{
	// 1. 1D 벡터로 변환하여 퍼센타일 계산
	cv::Mat img_reshaped = img.reshape(1, img.rows * img.cols);  // 1D로 변환
	std::vector<float> img_values;
	img_values.assign((float*)img_reshaped.datastart, (float*)img_reshaped.dataend);

	// 2. 퍼센타일 값 계산
	int total_elements = img_values.size();
	int lower_idx = static_cast<int>(lower_percentile / 100.0 * total_elements);
	int upper_idx = static_cast<int>(upper_percentile / 100.0 * total_elements);

	// 전체를 정렬하지 않고 표준 정규분포 상 표준 편차가 +-3(99%)인 값의 index만 추출
	std::nth_element(img_values.begin(), img_values.begin() + lower_idx, img_values.end());
	float lower_bound = img_values[lower_idx];

	std::nth_element(img_values.begin(), img_values.begin() + upper_idx, img_values.end());
	float upper_bound = img_values[upper_idx] * 1.5;


	// 3. OpenMP 병렬 처리로 로그 변환 및 정규화
#pragma omp parallel for
	for (int i = 0; i < img.rows; ++i) {
		float* img_ptr = img.ptr<float>(i);  // 한 번에 한 row의 데이터에 접근
		for (int j = 0; j < img.cols; ++j) {
			// 4. 클리핑: 퍼센타일에 맞게 값 클리핑
			img_ptr[j] = std::min(std::max(img_ptr[j], lower_bound), upper_bound);

			// 5. 로그 변환: 클리핑된 값을 기반으로 로그 변환
			img_ptr[j] = std::log1p(img_ptr[j] - lower_bound + 1e-8);  // log(1 + x) 계산 (offset 추가)

			// 6. 0-1로 정규화: 로그 변환 후 결과를 0-1 범위로 맞춤
			img_ptr[j] = (img_ptr[j] - std::log1p(0)) / (std::log1p(upper_bound - lower_bound) + 1e-8);
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

void COCTImaging::adaptive_gamma_correction(cv::Mat& img, int maxIntensity) {
	bool AGCWD_apply = false;
	std::vector<double> pdf_i;

	get_PDF_array(img, pdf_i, AGCWD_apply);

	if (AGCWD_apply) {
		get_CDF_array(pdf_i, cdf_i);
	}

	double max_intensity = maxIntensity;
	double calculated_max_intensity = *std::max_element(img.begin<uchar>(), img.end<uchar>());
	if (calculated_max_intensity > 0) {
		max_intensity = calculated_max_intensity;
	}

	cv::Mat output_image = img.clone();
	for (int y = 0; y < img.rows; y++) {
		for (int x = 0; x < img.cols; x++) {
			int intensity = img.at<uchar>(y, x);
			double intensity_ratio = intensity / max_intensity;
			double new_intensity = max_intensity * std::pow(intensity_ratio, 1 - cdf_i[intensity]);

			new_intensity = new_intensity > maxIntensity ? maxIntensity : (new_intensity < 0 ? 0 : new_intensity);
			output_image.at<uchar>(y, x) = static_cast<uchar>(new_intensity);
		}
	}
	img = output_image;
}

void COCTImaging::get_PDF_array(cv::Mat& img, std::vector<double>& pdf_i, bool& AGCWD_apply) {
	int number_of_pixels = img.rows * img.cols;
	pdf_i.assign(256, 0);

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
	for (int i = 0; i < 256; i++) {
		cumulative += pdfw_i[i] / pdf_sum;
		cdf_i[i] = cumulative;
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