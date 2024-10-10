#include "OCTImaging.h"
#include "Calibration.h"
#include "LookUpTable.h"
#include "Utility.h"
#include "MessageService.h"
#include "opencv2/opencv.hpp"
#include <omp.h>

#define _USE_MATH_DEFINES
#include <math.h>

const float EXPONENTIAL_FACTOR = 2.0f;
static bool bCompensated;

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
		m_nSheathPosition = std::get<0>(min_variance_row);
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
	imageResult += LUT_START_INDEX;

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

	//PLOGI.printf("[Start] adaptive_compensation");

	//rotate the image
	cv::Mat rotated_img;
	cv::rotate(imageResult, rotated_img, cv::ROTATE_90_COUNTERCLOCKWISE);

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

		// pow 적용
		cv::pow(I_n, EXPONENTIAL_FACTOR, I_n);

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
	double adaptive_threshold = 0.0001 * maxVal;

	// 각 열에 대해 계산 (OpenMP 사용)
#pragma omp parallel for
	for (int x = 0; x < cols; ++x) {
		cv::Mat I_n = normalized_img.col(x);

		// pow 적용
		cv::pow(I_n, EXPONENTIAL_FACTOR, I_n);

		// 누적 합 계산
		std::vector<float> cumulativeSum(rows, 0.0f);
		cumulativeSum[rows - 1] = I_n.at<float>(rows - 1);
		for (int i = rows - 2; i >= 0; --i) {
			cumulativeSum[i] = cumulativeSum[i + 1] + I_n.at<float>(i);
		}

		// 결과 계산
		for (int z = 0; z < rows; ++z) {
			float sum_val = cumulativeSum[z];

			if (mean_energy.at<float>(z) >= adaptive_threshold) {
				if (sum_val != 0) {
					result_img.at<float>(z, x) = I_n.at<float>(z) / (2 * sum_val);
				}
			}
			else {
				result_img.at<float>(z, x) = I_n.at<float>(z) * adaptive_threshold;
			}
		}
	}

	// Linear contrast stretching
	linear_contrast_stretching(result_img);
	result_img.convertTo(result_img, CV_8U, 255);

	// Apply CLAHE (객체 재사용)
	cv::Mat result_img_clahe;
	clahe->apply(result_img, result_img_clahe);

	// Rotate back to original angle
	cv::rotate(result_img_clahe, imageCompensated, cv::ROTATE_90_CLOCKWISE);

	//PLOGI.printf("[End] adaptive_compensation");
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

