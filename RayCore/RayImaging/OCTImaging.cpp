#include "OCTImaging.h"
#include "Calibration.h"
#include "Configuration.h"
#include "Utility.h"
#include "MessageService.h"
#include "opencv2/opencv.hpp"
#include <omp.h>

#define _USE_MATH_DEFINES
#include <math.h>

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

COCTImaging::COCTImaging(CMessageService* pMsg) {
	m_msg = pMsg;

	m_pThread = nullptr;
	m_waitForFringes = true;
	m_pFringesBuffer = nullptr;

	calibration = new CCalibration();

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
	m_fBrightness = 0.0f;
	m_fContrast = 1.0f;
	m_fLowLevel = 108.0f;
	m_fHighLevel = 109.0f;
	m_backgroundColor = cv::Scalar(0x00, 0x00, 0x00);

	m_nCurFrame = 0;
	m_nTotalFrame = 0;
}

COCTImaging::~COCTImaging() {
	releaseMemory();
	if (calibration != nullptr) delete calibration;
}

void COCTImaging::Initialize(tstring calibFile) {
	CConfiguration& config = CConfiguration::GetInstance();

	releaseMemory();
	allocateMemory();
	calibration->Initialize(calibFile);

	releaseCircularizeMap();
	initCircularizeMap(config.nOutputLength, config.nBScan, config.nOutputLength, config.nCircleSize, config.nCircleSize, 2.0f);
	generateMask(imageMask);

	loadLUT("LUT.csv");
}
void COCTImaging::Process(USHORT* fringes) {
	CConfiguration& config = CConfiguration::GetInstance();

	if (fringes == nullptr) return;

	generateBackground((Ipp16u*)fringes);
	fftProcessing(fringes32f);
	computeLogarithm(fFFTResult, fFFTResult);
	findSheath(fFFTResult);
	generateImage(fFFTResult, false);

	postProcessing();
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
void COCTImaging::DoAsyncRender(USHORT* fringes) {
	if (m_pThread == nullptr || m_pThread->isRun == false) return;

	if (m_waitForFringes) {
		m_pFringesBuffer = fringes;
		CUtility::ResumeThread(m_pThread);
	}
}

void COCTImaging::allocateMemory() {
	// ORDER = 11, nFFTLength = 2^11
	// nScans 보다 큰 2^n 중에서 제일 작은 수
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const int nAScanWithPadding = config.nAScan + config.nAScanPadding;
	const int nBScan = config.nBScan;
	const int nFFTOrder = config.nFFTOrder;
	const int nFFTLength = config.nFFTLength;
	const int nOutputLength = config.nOutputLength;
	const int nBufferSize = config.nBufferSize;
	const int nScopeLength = config.getScopeLength();
	const int nCircleSize = config.nCircleSize;

	fringes32f = ippsMalloc_32f(nAScan * nBScan);
	fringes32fAverage = ippsMalloc_32f(nAScan);

	imageResult.create(nBScan, nOutputLength, CV_8UC1);
	imageResultColor.create(nBScan, nOutputLength, CV_8UC3);
	imageCircle.create(nCircleSize, nCircleSize, CV_8UC3);
	imageMask.create(nCircleSize, nCircleSize, CV_8UC3);
	imageBackground.create(nCircleSize, nCircleSize, CV_8UC3);

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
	CConfiguration& config = CConfiguration::GetInstance();

	if (fringes32f) { ippsFree(fringes32f); fringes32f = nullptr; }
	if (fringes32fAverage) { ippsFree(fringes32fAverage); fringes32fAverage = nullptr; }

	imageResult.release();
	imageResultColor.release();
	imageCircle.release();
	imageMask.release();
	imageBackground.release();

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
	CConfiguration& config = CConfiguration::GetInstance();
	const int nWidth = config.nAScan;
	const int nHeight = config.nBScan;

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
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan, nBScan = config.nBScan;
	const int numDynamic = config.settingsOpenMP.numDynamic;
	const int numThreads = config.settingsOpenMP.numThread;
	const int nFFTLength = config.nFFTLength;
	const int nOutputLength = config.nOutputLength;

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
	CConfiguration& config = CConfiguration::GetInstance();
	const int nBScan = config.nBScan;
	const int nOutputLength = config.nOutputLength;

	ippsLn_32f(src, dst, nOutputLength * nBScan);
	ippsMulC_32f(dst, log10(exp(1)) * 10, dst, nOutputLength * nBScan);
}

void COCTImaging::findSheath(Ipp32f* logaritihmData) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nBScan = config.nBScan;
	const int nFFTLength = config.nFFTLength;
	const int nOutputLength = config.nOutputLength;
	const int minPeakHeight = 1500.f;
	const int distBetweenLayer = 24;

	Ipp32f* fScope = new Ipp32f[nOutputLength];
	std::vector<int> sheathPoints;
	int sheathPointSum = 0;
	for (int n = 0; n < nBScan; n++) {
		ippsSubC_32f(logaritihmData + n * nOutputLength, m_fLowLevel, fScope, nOutputLength);
		ippsMulC_32f_I(USHRT_MAX / m_fHighLevel, fScope, nOutputLength);
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

void COCTImaging::generateImage(Ipp32f* logaritihmData, bool bInvert){
	CConfiguration& config = CConfiguration::GetInstance();
	const int nBScan = config.nBScan;
	const float fHighLevel = (bInvert) ? config.imaging.highLevel : 0.0f;
	const float fLowLevel = (bInvert) ? config.imaging.lowLevel : 0.0f;
	const int nFFTLength = config.nFFTLength;
	const int nOutputLength = config.nOutputLength;

	for (int i = 0; i < nBScan; i++)
	{
		ippsSubC_32f(logaritihmData + i * nOutputLength, (m_fLowLevel + fLowLevel), fOutput + i * nOutputLength, nOutputLength);
		ippsMulC_32f_I(UCHAR_MAX / (m_fHighLevel - fHighLevel), fOutput + i * nOutputLength, nOutputLength);
		ippsConvert_32f8u_Sfs(fOutput + i * nOutputLength, imageResult.data + i * nOutputLength /*stepBytes*/, nOutputLength, ippRndNear, 0);
	}
}
void COCTImaging::postProcessing() {
	const bool bInvert = m_bInvert;
	const bool bColor = m_bColor;

	cv::cvtColor(imageResult, imageResultColor, cv::COLOR_GRAY2RGB);
	cv::flip(imageResultColor, imageResultColor, 1);

	if (bInvert) cv::bitwise_not(imageResultColor, imageResultColor);
	if (bColor) applyLUT(imageResultColor);

	cv::convertScaleAbs(imageResultColor, imageResultColor, m_fContrast, m_fBrightness);

	if (m_bShowCalibGuide) {
		CConfiguration& config = CConfiguration::GetInstance();
		drawGuideLine(imageResultColor, config.measurementValues.nSheathPosition, cv::Scalar(0xff, 0xcc, 0x33));
		drawGuideLine(imageResultColor, m_nSheathPosition, cv::Scalar(0xff, 0xff, 0xff));
	}

	//cv::rectangle(imageResultColor, cv::Rect(0, 0, 100, imageResultColor.rows), m_backgroundColor, cv::FILLED);

	circularizeImage(imageResultColor, imageCircle);

	imageBackground.setTo(m_backgroundColor);
	cv::copyTo(imageBackground, imageCircle, imageMask);
}

void COCTImaging::circularizeImage(cv::Mat& src, cv::Mat& dst)
{
	CConfiguration& config = CConfiguration::GetInstance();

	cv::remap(src, dst, matXMap, matYMap, cv::INTER_LINEAR);
}

void COCTImaging::applyHotColor(cv::Mat& image) {
	const int colorMapLength = 254;
	int nn = 90;

	for (int y = 0; y < image.rows; y++) {
		for (int x = 0; x < image.cols; x++) {
			cv::Vec3b color = image.at<cv::Vec3b>(y, x);
			cv::Vec3b hotColor;

			unsigned char red = color.val[2];
			unsigned char green = color.val[1];
			unsigned char blue = color.val[0];

			float r, g, b = 0.0f;

			if (red < nn) r = (float)(red + 1) / (float)nn;
			else r = 1;

			if (green < nn) g = 0;
			else if (green > 2 * nn) g = 1;
			else g = (float)(green + 1 - nn) / (float)nn;

			if (blue <= 2 * nn) b = 0;
			else b = (float)(blue + 1 - 2 * nn) / (float)(colorMapLength - 2 * nn);

			hotColor.val[0] = b * 255;
			hotColor.val[1] = g * 255;
			hotColor.val[2] = r * 255;
			
			image.at<cv::Vec3b>(y, x) = hotColor;
		}
	}
}

void COCTImaging::loadLUT(const char* strLUTPath) {
	m_vLUT.clear();
	FILE* fpLUT = fopen(strLUTPath, "r");
	if (fpLUT) {
		char strBuffer[MAX_PATH];
		bool bStartLUT = false;
		while (fscanf(fpLUT, "%s", strBuffer) != EOF) {
			if (strBuffer[0] == '0') {
				bStartLUT = true;
			}

			if (bStartLUT) {
				std::stringstream buffer(strBuffer);
				std::string token;
				cv::Vec3b color;

				std::getline(buffer, token, ',');	// index
				for (int i = 0; i < 3; i++) {
					std::getline(buffer, token, ',');
					color.val[i] = atoi(token.c_str());
				}
				m_vLUT.push_back(color);
			}
		}
	}
}
void COCTImaging::applyLUT(cv::Mat& image) {

	for (int y = 0; y < image.rows; y++) {
		for (int x = 0; x < image.cols; x++) {
			cv::Vec3b color = image.at<cv::Vec3b>(y, x);
			cv::Vec3b bgrColor = m_vLUT.at(color.val[0]);
			cv::Vec3b cvtColor;

			cvtColor.val[0] = bgrColor.val[2];
			cvtColor.val[1] = bgrColor.val[1];
			cvtColor.val[2] = bgrColor.val[0];

			image.at<cv::Vec3b>(y, x) = cvtColor;
		}
	}
}
void COCTImaging::generateMask(cv::Mat& image) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nBScan = config.nBScan;
	const int nOutputLength = config.nOutputLength;
	
	cv::Mat imgTemp(nBScan, nOutputLength, CV_8UC3);

	imgTemp.setTo(cv::Scalar(255, 255, 255));
	circularizeImage(imgTemp, image);
	cv::bitwise_not(image, image);
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
			pImaging->Process(pImaging->m_pFringesBuffer);
			// To-Do
			// double buffering 필요?
			// Invert, coloring 을 View (Dialog) 쪽으로 뺄 수 없을까?

			if(pMsg != nullptr) pMsg->postMessage(WM_PROCESS_OCT_DONE, pImaging->m_nCurFrame, pImaging->m_nTotalFrame);
		}
	}

	return NOERROR;
}