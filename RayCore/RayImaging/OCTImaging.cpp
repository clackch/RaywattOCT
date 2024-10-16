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

float EXPONENTIAL_FACTOR = 1.8f;
float EXPONENTIAL_CONTROL = 0.6f;
float ENERGY_THRESHOLD = 0.001f;
const int SHEATH_OFFSET = 15;
const int SEARCH_LENGTH = 100;
static bool bCompensated;
static bool bVignetted;
static int myint = 0;

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
	releaseMemory();
	allocateMemory();

	this->calibration = calibration;

	releaseCircularizeMap();
	initCircularizeMap(m_setting.nOutputLength, m_setting.nBScan, m_setting.nOutputLength, m_setting.nCircleSize, m_setting.nCircleSize, 2.0f);

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
	cv::remap(src, dst, matXMap, matYMap, cv::INTER_LINEAR);
}

void COCTImaging::InverseCircularizeImage(cv::Mat& src, cv::Mat& dst) {}

void COCTImaging::EraseStentOutLier(cv::Mat& stent) {}

void COCTImaging::SetLumenContourOffset(std::vector<cv::Point> lumenContour) {}

cv::Mat COCTImaging::GetProcessedImage() {
	return bCompensated ? imageCompensated : imageResult; 
}

void COCTImaging::SetImageCompensation(bool ImageCompensated) { 
	bCompensated = ImageCompensated;
}

void COCTImaging::SetImageLumenVignetting(bool ImageLumenVignetted) {
	bVignetted = ImageLumenVignetted;
}

void COCTImaging::SetImageCompensationControlWindow(bool ImageCompensationControlWindowOn) {
	try {
		if (ImageCompensationControlWindowOn == 1) {
			cv::namedWindow("Window", cv::WINDOW_AUTOSIZE);

			// 슬라이더 값 범위는 정수로만 가능하므로, 원하는 범위로 매핑
			int exponential_factor_slider = 18;
			int exponential_control_slider = 6;
			int energy_threshold_slider = 10;

			cv::createTrackbar("Cont", "Window", &exponential_factor_slider, 100, on_trackbar);
			cv::createTrackbar("Bright", "Window", &exponential_control_slider, 100, on_trackbar);
			cv::createTrackbar("Eng", "Window", &energy_threshold_slider, 100, on_trackbar);

			// 초기 콜백 호출
			on_trackbar(0, 0);

			// 슬라이더와 함께 창 유지 (ESC로 종료)
			while (true) {
				int key = cv::waitKey(50);
				if (key == 27) {  // ESC key
					cv::destroyAllWindows();
					break;
				}
			}

			cv::destroyAllWindows();
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
	if (!bCompensated)
		return;

	// PLOGI.printf("[Start] adaptive_compensation");

	// Rotate the image
	cv::Mat rotated_img;
	cv::rotate(imageResult, rotated_img, cv::ROTATE_90_COUNTERCLOCKWISE);
	
	if(!bVignetted)
		lumen_detection_processing(rotated_img);

	// Normalize the image
	cv::Mat normalized_img;
	rotated_img.convertTo(normalized_img, CV_32F);
	double min_val, max_val;
	min_max_normalization(normalized_img, normalized_img, min_val, max_val);

	int rows = normalized_img.rows;
	int cols = normalized_img.cols;

	cv::Mat energy_all = cv::Mat::zeros(normalized_img.size(), CV_32F);
	cv::Mat result_img = cv::Mat::zeros(normalized_img.size(), CV_32F);

	// Compute energy using cumulative sum (with OpenMP)
#pragma omp parallel for
	for (int x = 0; x < cols; ++x) {
		cv::Mat I_n = normalized_img.col(x).clone(); // clone() 사용으로 독립적인 메모리

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
		}
	}

	// 전체 Row의 평균 에너지 계산
	cv::Mat mean_energy;
	cv::reduce(energy_all, mean_energy, 1, cv::REDUCE_AVG);

	// Adaptive threshold 설정
	double minVal, maxVal;
	cv::minMaxLoc(mean_energy, &minVal, &maxVal);
	double adaptive_threshold = ENERGY_THRESHOLD * maxVal;

	// threshold_row 계산: adaptive_threshold 이하인 첫 번째 행 찾기
	int threshold_row = 0;
	for (int z = 0; z < mean_energy.rows; ++z) {
		if (mean_energy.at<float>(z) <= adaptive_threshold) {
			threshold_row = z;
			break;
		}
	}

	// 각 열에 대해 계산 (OpenMP 사용)
#pragma omp parallel for
	for (int x = 0; x < cols; ++x) {
		cv::Mat I_n = normalized_img.col(x);

		// 자연 로그 계산 후 지수 연산 적용
		cv::Mat log_img, exp_img;
		cv::log(I_n + 1e-6, log_img);  // 로그 계산에서 0을 피하기 위해 1e-6을 추가
		cv::exp(EXPONENTIAL_FACTOR * log_img, exp_img);  // EXPONENTIAL_FACTOR 적용 후 exp 사용
		I_n = exp_img.clone();  // 결과 저장

		// 누적 합 계산
		std::vector<float> cumulativeSum(rows, 0.0f);
		cumulativeSum[rows - 1] = I_n.at<float>(rows - 1);
		for (int i = rows - 2; i >= 0; --i) {
			cumulativeSum[i] = cumulativeSum[i + 1] + I_n.at<float>(i);
		}

		double stop_threshold = 0;

		// 결과 계산
		for (int z = 0; z < rows; ++z) {
			float sum_val = cumulativeSum[z];

			// threshold_row를 기준으로 보정 적용
			if (z + 100 <= threshold_row) {
				float sum_val_pow = std::exp(EXPONENTIAL_CONTROL * std::log(sum_val));
				if (sum_val != 0) {
					result_img.at<float>(z, x) = I_n.at<float>(z) / (2 * sum_val_pow);
					stop_threshold = (2 * sum_val_pow);
				}
			}
			else {
				result_img.at<float>(z, x) = I_n.at<float>(z) / stop_threshold;
			}
		}
	}

	// Linear contrast stretching
	logarithmic_contrast_stretching(result_img);
	result_img.convertTo(result_img, CV_8U, 255);

	// Apply CLAHE (객체 재사용)
	cv::Mat result_img_clahe;
	clahe->apply(result_img, result_img_clahe);

	// Rotate back to original angle
	cv::rotate(result_img_clahe, imageCompensated, cv::ROTATE_90_CLOCKWISE);

	// PLOGI.printf("[End] adaptive_compensation");
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

	// 2. 벡터 정렬
	std::sort(img_values.begin(), img_values.end());

	// 3. 퍼센타일 값 계산
	int total_elements = img_values.size();
	int lower_idx = static_cast<int>(lower_percentile / 100.0 * total_elements);
	int upper_idx = static_cast<int>(upper_percentile / 100.0 * total_elements);

	float lower_bound = img_values[lower_idx];
	float upper_bound = img_values[upper_idx] * 1.5;

	// 4. OpenMP 병렬 처리로 로그 변환 및 정규화
#pragma omp parallel for
	for (int i = 0; i < img.rows; ++i) {
		float* img_ptr = img.ptr<float>(i);  // 한 번에 한 row의 데이터에 접근
		for (int j = 0; j < img.cols; ++j) {
			// 5. 클리핑: 퍼센타일에 맞게 값 클리핑
			img_ptr[j] = std::min(std::max(img_ptr[j], lower_bound), upper_bound);

			// 6. 로그 변환: 클리핑된 값을 기반으로 로그 변환
			img_ptr[j] = std::log1p(img_ptr[j] - lower_bound + 1e-8);  // log(1 + x) 계산 (offset 추가)

			// 7. 0-1로 정규화: 로그 변환 후 결과를 0-1 범위로 맞춤
			img_ptr[j] = (img_ptr[j] - std::log1p(0)) / (std::log1p(upper_bound - lower_bound) + 1e-8);
		}
	}
}

void COCTImaging::lumen_detection_processing(cv::Mat& img)
{
	try {
		// 첫 30개 행을 0으로 설정
		img(cv::Range(0, 30), cv::Range::all()).setTo(cv::Scalar(0));

		// 행의 평균 및 분산 계산	
		std::vector<double> mean_values(img.rows), variance_values(img.rows);

		// 행 단위로 평균과 분산 계산 (OpenMP 병렬 처리)
#pragma omp parallel for
		for (int i = 0; i < img.rows; ++i) {
			cv::Mat row = img.row(i);
			cv::Scalar mean_scalar, stddev_scalar;
			cv::meanStdDev(row, mean_scalar, stddev_scalar);
			mean_values[i] = mean_scalar[0];
			variance_values[i] = stddev_scalar[0] * stddev_scalar[0];
		}

		// 최소값과 최대값 찾기 (병렬화된 반복 작업이 아니므로 유지)
		auto minmax_mean = std::minmax_element(mean_values.begin(), mean_values.end());
		double min_mean = *minmax_mean.first;
		double max_mean = *minmax_mean.second;

		std::vector<double> mean_values_norm;
		mean_values_norm.reserve(mean_values.size());

		// 0~1 사이로 정규화 (병렬 처리)
#pragma omp parallel for
		for (int i = 0; i < mean_values.size(); ++i) {
			if (max_mean - min_mean == 0) {
				mean_values_norm[i] = 0.0;
			}
			else {
				mean_values_norm[i] = (mean_values[i] - min_mean) / (max_mean - min_mean);
			}
		}

		std::vector<int> valid_row_indices;
		std::vector<double> valid_row_variances;

		for (int i = 0; i < mean_values.size(); ++i) {
			if (mean_values_norm[i] >= 0.8) {
				valid_row_indices.push_back(i);
				valid_row_variances.push_back(variance_values[i]);
			}
		}

		if (valid_row_indices.empty()) {
			PLOGI.printf("No valid rows found with normalized mean >= 0.8.");
			return;
		}

		// valid_row_variances에서 분산이 가장 작은 행 찾기
		auto min_variance_it = std::min_element(valid_row_variances.begin(), valid_row_variances.end());
		int min_variance_index = std::distance(valid_row_variances.begin(), min_variance_it);
		int min_variance_row1 = valid_row_indices[min_variance_index];

		// 이미지 영역 선택
		cv::Mat selected_region = img.clone();

		int sheath_boundary = min_variance_row1 + SHEATH_OFFSET;

		if (sheath_boundary >= img.rows) {
			PLOGI.printf("The image have some noises at the end of each aline");
			return;
		}

		selected_region(cv::Range(0, sheath_boundary), cv::Range::all()).setTo(cv::Scalar(0));

		// Piecewise Linear Contrast 적용
		apply_piecewise_linear_contrast(selected_region, 48, 184, 0, 255);

		std::vector<cv::Point> red_points;

		// 각 열의 값 계산 및 정규화 (병렬 처리 가능)
#pragma omp parallel for
		for (int col = 0; col < selected_region.cols; ++col) {
			std::vector<double> col_values;
			for (int row = 10; row < selected_region.rows - 10; ++row) {
				double sum_value = cv::sum(selected_region(cv::Range(row - 10, row + 11), cv::Range(col, col + 1)))[0];
				col_values.push_back(sum_value);
			}

			// 각 col 값 정규화
			auto minmax_col = std::minmax_element(col_values.begin(), col_values.end());
			double min_col = *minmax_col.first;
			double max_col = *minmax_col.second;

			std::vector<double> normalized_col_values(col_values.size());
			for (int i = 0; i < col_values.size(); ++i) {
				if (max_col - min_col == 0) {
					normalized_col_values[i] = 0.0;
				}
				else {
					normalized_col_values[i] = (col_values[i] - min_col) / (max_col - min_col);
				}
			}

			// 첫 번째 정규화된 값이 0.6 이상인 행 선택
			for (int i = 0; i < normalized_col_values.size(); ++i) {
				if (normalized_col_values[i] >= 0.6) {
#pragma omp critical
					{
						red_points.push_back(cv::Point(col, i));
					}
					break;
				}
			}
		}

		// red_points를 col 값을 기준으로 정렬
		std::sort(red_points.begin(), red_points.end(), [](const cv::Point& a, const cv::Point& b) {
			return a.x < b.x;
			});

		std::vector<cv::Point> refined_curve_points;

		// 시작점 선택: red_points의 0~20 인덱스의 y 좌표 평균값에 가장 가까운 점 선택
		std::vector<cv::Point> first_20_points(red_points.begin(), red_points.begin() + 20);
		double avg_y = std::accumulate(first_20_points.begin(), first_20_points.end(), 0.0, [](double sum, const cv::Point& p) { return sum + p.y; }) / first_20_points.size();
		cv::Point start_point = *std::min_element(first_20_points.begin(), first_20_points.end(), [&](cv::Point a, cv::Point b) {
			return std::abs(a.y - avg_y) < std::abs(b.y - avg_y);
			});
		start_point.x = 0;  // x 좌표는 0으로 설정
		start_point.y = (int)start_point.y;

		// 시작점을 refined_curve_points에 추가
		refined_curve_points.push_back(start_point);

		int i = 0;

		// 중간 점들을 찾기 위한 루프
		while (i < red_points.size() - 1) {
			cv::Point last_point = refined_curve_points.back();  // 마지막으로 선택된 점
			std::vector<cv::Point> possible_candidates;
			int closest_index = -1;
			int min_distance = SEARCH_LENGTH * SEARCH_LENGTH * 4;  // 최소 거리를 초기화

			// i+2부터 SEARCH_LENGTH만큼의 범위 탐색
			for (int j = i + 2; j <= i + SEARCH_LENGTH && j < red_points.size(); j += 2) {
				int distance_squared = (last_point.x - red_points[j].x) * (last_point.x - red_points[j].x)
					+ (last_point.y - red_points[j].y) * (last_point.y - red_points[j].y);

				if (distance_squared < min_distance) {  // 제곱된 거리 조건
					min_distance = distance_squared;
					closest_index = j;
				}
			}

			if (closest_index == -1) {
				break;
			}

			// 가장 가까운 점을 추가하고 i를 갱신
			refined_curve_points.push_back(red_points[closest_index]);
			i = closest_index;  // i를 가장 가까운 점의 인덱스로 갱신
		}

		// 마지막 5개의 y 좌표의 평균 계산
		int num_points_to_average = std::min(5, (int)refined_curve_points.size());
		double sum_y = 0.0;
		for (int i = refined_curve_points.size() - num_points_to_average; i < refined_curve_points.size(); ++i) {
			sum_y += refined_curve_points[i].y;
		}
		double avg_y_last_points = sum_y / num_points_to_average;

		// 마지막 점 확인 후 추가: 마지막 점이 (img.cols - 1)이 아닐 경우에만 추가
		if (refined_curve_points.back().x != img.cols - 1) {
			// 마지막 점 추가: (img.cols - 1, 평균 y 값)
			refined_curve_points.push_back(cv::Point(img.cols - 1, static_cast<int>(avg_y_last_points)));
		}

		// 모든 col에 대해 y 값 저장, -1로 초기화하여 y 값이 없는 상태를 표시
		std::vector<cv::Point> full_curve_points(img.cols, cv::Point(-1, -1));

		// 먼저 refined_curve_points의 기존 값을 full_curve_points에 복사
		for (const auto& point : refined_curve_points) {
			full_curve_points[point.x] = point;  // 이미 존재하는 점을 그대로 유지
		}

		// 없는 col 인덱스에 대해 보간 또는 복사 수행
		for (int col = 0; col < img.cols; ++col) {
			// 이미 해당 col에 점이 있으면 건너뜀
			if (full_curve_points[col].y != -1) {
				continue;
			}

			bool point_added = false;  // y값이 추가되었는지 여부

			// 현재 col에 대해 가까운 두 점을 찾아 보간
			for (int i = 0; i < refined_curve_points.size() - 1; ++i) {
				int x0 = refined_curve_points[i].x;
				int x1 = refined_curve_points[i + 1].x;

				// col이 두 점 사이에 위치하면, 선형 보간을 수행
				if (col >= x0 && col <= x1) {
					double y0 = refined_curve_points[i].y;
					double y1 = refined_curve_points[i + 1].y;

					// 선형 보간으로 y 값 계산
					double interpolated_y = y0 + (y1 - y0) * (col - x0) / (x1 - x0);

					// 보간된 점을 추가
					full_curve_points[col] = cv::Point(col, static_cast<int>(interpolated_y));
					point_added = true;  // y 값이 추가되었음을 기록
					break;
				}
			}

			// 양쪽 보간할 점이 없는 경우, 가까운 값을 복사
			if (!point_added) {
				// 왼쪽 점 찾기
				int left_index = -1;
				for (int i = col - 1; i >= 0; --i) {
					if (full_curve_points[i].y != -1) {
						left_index = i;
						break;
					}
				}

				// 오른쪽 점 찾기
				int right_index = -1;
				for (int i = col + 1; i < img.cols; ++i) {
					if (full_curve_points[i].y != -1) {
						right_index = i;
						break;
					}
				}

				// 왼쪽과 오른쪽 중 하나만 있으면 그 값을 사용
				if (left_index != -1) {
					full_curve_points[col] = full_curve_points[left_index];  // 왼쪽 값 복사
				}
				else if (right_index != -1) {
					full_curve_points[col] = full_curve_points[right_index];  // 오른쪽 값 복사
				}
				else {
					// 만약 양쪽에 값이 전혀 없다면 refined_curve_points[0]을 사용 (첫 점 사용)
					full_curve_points[col] = refined_curve_points[0];
				}
			}
		}

		// 이미지 복사본 생성
		cv::Mat output_image = img.clone();

		// full_curve_points를 원으로 표시 (반지름 1의 circle)
		for (int col = 0; col < img.cols; ++col) {
			if (full_curve_points[col].y != -1) {
				// 점 그리기 (반지름 1, 색상은 빨간색 (BGR: 0, 0, 255))
				cv::circle(output_image, full_curve_points[col], 1, cv::Scalar(0, 0, 255), -1);
			}
		}

		// y좌표마다 경계선 높이를 가진 array 생성
		std::vector<int> curve_y(img.cols, 0);

		// 각 col에 대한 경계선을 full_curve_points의 y값을 사용하여 설정
		for (int col = 0; col < img.cols; ++col) {
			if (full_curve_points[col].y != -1) {
				curve_y[col] = full_curve_points[col].y;  // 각 col의 y 값을 경계선으로 설정
			}
		}

		// 스레드마다 고유한 벡터를 사용하여 중간 결과 저장 (타입을 uchar로 변경)
		std::vector<std::vector<uchar>> thread_pixel_values(omp_get_max_threads());

#pragma omp parallel for
		for (int col = 0; col < img.cols; ++col) {
			int thread_id = omp_get_thread_num();  // 각 스레드 ID를 가져옴
			for (int row = 0; row < curve_y[col]; ++row) {
				thread_pixel_values[thread_id].push_back(img.at<uchar>(row, col));  // 각 스레드별로 픽셀 값 저장
			}
		}

		// 벡터 병합
		std::vector<uchar> pixel_values_above_curve;
		for (const auto& thread_values : thread_pixel_values) {
			pixel_values_above_curve.insert(pixel_values_above_curve.end(), thread_values.begin(), thread_values.end());  // 스레드별 값을 병합
		}

		double selected_mean = 0;

		// 빈 벡터인지 확인 후 평균 계산
		if (!pixel_values_above_curve.empty()) {
			// 평균 계산 (정확도를 위해 accumulate의 초기값을 double로 설정)
			selected_mean = std::accumulate(pixel_values_above_curve.begin(), pixel_values_above_curve.end(), 0.0) / pixel_values_above_curve.size();
		}

		// 수집된 픽셀 값들의 분포를 계산
		double threshold_70 = 0, threshold_90 = 0;
		if (!pixel_values_above_curve.empty()) {
			std::sort(pixel_values_above_curve.begin(), pixel_values_above_curve.end());
			size_t idx_70 = pixel_values_above_curve.size() * 70 / 100;
			size_t idx_90 = pixel_values_above_curve.size() * 90 / 100;
			threshold_70 = pixel_values_above_curve[idx_70];
			threshold_90 = pixel_values_above_curve[idx_90];
		}

		// 각 픽셀의 밝기 조정 (임계값 이상인 경우에만 비선형 조정 적용)
		for (int col = 0; col < img.cols; ++col) {
			for (int row = 0; row < curve_y[col]; ++row) {
				uchar pixel_value = img.at<uchar>(row, col);

				// 비선형 조정 적용
				if (pixel_value >= threshold_90) {
					img.at<uchar>(row, col) = static_cast<uchar>(selected_mean);
				}
				else if (pixel_value >= threshold_70) {
					img.at<uchar>(row, col) = static_cast<uchar>(selected_mean);
				}
			}
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

void COCTImaging::apply_piecewise_linear_contrast(cv::Mat& img, int low_in, int high_in, int low_out, int high_out) {

	try {
		// 결과 행렬 초기화
		cv::Mat result = cv::Mat::zeros(img.size(), CV_8U);

		// img를 CV_32F로 변환하여 연산 처리
		cv::Mat img_float;
		img.convertTo(img_float, CV_32F);

		// 첫 구간 (low_in 이하의 값은 low_out으로 설정)
		result.setTo(low_out, img_float <= low_in);

		// 마지막 구간 (high_in 이상의 값은 high_out으로 설정)
		result.setTo(high_out, img_float >= high_in);

		// 중간 구간 선형 변환
		cv::Mat mask = (img_float > low_in) & (img_float < high_in);

		// 마스크 값 확인 (0 또는 255로 변환)
		mask.convertTo(mask, CV_8U, 255.0);

		// 중간 구간 계산 및 값 확인
		cv::Mat intermediate_result = (img_float - low_in) * (high_out - low_out) / (high_in - low_in) + low_out;

		// intermediate_result 값을 0~255 범위로 클램핑
		cv::Mat clamped_result;
		cv::threshold(intermediate_result, clamped_result, 255, 255, cv::THRESH_TRUNC); // 255로 클램핑
		cv::threshold(clamped_result, clamped_result, 0, 0, cv::THRESH_TOZERO);         // 0으로 클램핑

		// intermediate_result를 CV_8U로 변환
		clamped_result.convertTo(clamped_result, CV_8U);

		// mask와 clamped_result의 크기 및 자료형이 일치하는지 확인
		if (mask.size() == clamped_result.size() && mask.type() == CV_8U && clamped_result.type() == CV_8U) {
			// setTo 대신 copyTo 사용
			clamped_result.copyTo(result, mask);
		}

		// 최종 결과를 img에 복사
		result.copyTo(img);
	}
	catch (const cv::Exception& e) {
		PLOGI.printf("OpenCV Error: %s", e.what());
		return;
	}
	catch (...) {
		PLOGI.printf("Unknown error occurred in SetImageCompensationControlWindow");
		return;
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

void COCTImaging::on_trackbar(int, void*) {
	try {
		// 트랙바 값은 int로만 입력 가능하므로, 이를 원하는 범위로 변환
		EXPONENTIAL_FACTOR = cv::getTrackbarPos("Cont", "Window") / 10.0f;
		EXPONENTIAL_CONTROL = cv::getTrackbarPos("Bright", "Window") / 10.0f;
		ENERGY_THRESHOLD = cv::getTrackbarPos("Eng", "Window") / 10000.0f;

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