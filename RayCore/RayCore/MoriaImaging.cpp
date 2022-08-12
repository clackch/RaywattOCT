#include "pch.h"
#include "MoriaImaging.h"
#include "MoriaCalibration.h"
#include "MoriaConfiguration.h"
#include "Utility.h"
#include "MessageService.h"
#include <omp.h>
#include "opencv2/opencv.hpp"

void ippsRelease(void *&ptr) {
	if (ptr) {
		ippsFree(ptr);
		ptr = NULL;
	}
}
void ippsRelease_double_ptr(void**& ptr, int dim) {
	if (ptr) {
		for (int i = 0; i < dim; i++) {
			ippsRelease(ptr[i]);
		}
		delete[] ptr;
		ptr = NULL;
	}
}

CMoriaImaging::CMoriaImaging(CMessageService* pMsg) {
	m_msg = pMsg;

	m_pThread = NULL;
	m_waitForFringes = true;
	m_pFringesBuffer = NULL;

	calibration = new CMoriaCalibration();

	scopeData = NULL;
	scopeFFTData = NULL;

	fringes32f = NULL;
	fringes32fSum = NULL;
	ref_fringe = NULL;
	backgroundImage = NULL;
	backgroundImage_Deinterlaced = NULL;
	backgroundImage32f_Deinterlaced = NULL;

	fBuffer_BackgroundFringes = NULL;
	uDataFringes_Deinterlaced = NULL;
	uDataFingees_DeinterlacedwithPadding = NULL;
	uBackgroundFringe_Deinterlaced = NULL;
	fOutput = NULL;

	specReal32FFT = NULL;
	specComp32FFT = NULL;
	specComp32ZoomFFT = NULL;

	m_bInvert = false;
	m_bColor = false;
	m_fBrightness = 0.0f;
	m_fContrast = 1.0f;

	m_nCurFrame = 0;
	m_nTotalFrame = 0;
}

CMoriaImaging::~CMoriaImaging() {
	releaseMemory();
	if (calibration != NULL) delete calibration;
}

void CMoriaImaging::Initialize() {
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nScans = pConfig->nScans;
	const int nAlines = pConfig->nAlines;
	const int nBufferSize = pConfig->nBufferSize;
	const int nDmaChannels = pConfig->nDmaChannels;

	releaseMemory();
	allocateMemory();
	calibration->Initialize();

	// load Background image from file
	FILE* fp = fopen("BACKGROUND.bin", "rb");
	if (fp) {
		size_t readSize = fread(backgroundImage, sizeof(Ipp16u), nBufferSize, fp);
		if (readSize != pConfig->nBufferSize) {
			memset(backgroundImage, 0x00, sizeof(Ipp16u) * nBufferSize);
		}
		fclose(fp);

		// interleave background
		ippsDeinterleave_16s((Ipp16s*)backgroundImage, nDmaChannels, nScans * nAlines, (Ipp16s**)backgroundImage_Deinterlaced);
		for (int ch = 0; ch < nDmaChannels; ch++) {
			ippsConvert_16u32f(backgroundImage_Deinterlaced[ch], backgroundImage32f_Deinterlaced[ch], nScans * nAlines);
		}
	}

	loadLUT("LUT.csv");
}
void CMoriaImaging::Process(const Ipp16u* fringes) {
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nAlines = pConfig->nAlines;
	const int nFFTLength = pConfig->nFftLength;
	const bool bInvert = m_bInvert;
	const bool bColor = m_bColor;

	if (fringes == NULL) return;

	generateBackground((Ipp16u*)fringes);
	generateImage(fringes, bInvert);

	cv::cvtColor(imageResult, imageResultColor, cv::COLOR_GRAY2RGB);
	cv::flip(imageResultColor, imageResultColor, 1);

	if(!bInvert && !bColor){
		for (int i = 0; i < imageResultColor.rows * imageResultColor.cols; i++) {
			int nChannels = imageResultColor.channels();
			for (int ch = 0; ch < nChannels; ch++) {
				imageResultColor.data[i * nChannels + ch] = 255 - imageResultColor.data[i * nChannels + ch];
			}
		}
	}

	if (bInvert) cv::bitwise_not(imageResultColor, imageResultColor);
	if (bColor) applyLUT(imageResultColor);

	cv::convertScaleAbs(imageResultColor, imageResultColor, m_fContrast, m_fBrightness);

	circularizeImage(imageResultColor, imageCircle);
}

int CMoriaImaging::Start() {
	BOOL result = FALSE;
	result = CUtility::StartThread(threadRender, m_pThread, (LPVOID)this);

	if (result) return NOERROR;
	else return -1;
}
int CMoriaImaging::Stop() {
	CUtility::StopThread(m_pThread);

	return NOERROR;
}
void CMoriaImaging::DoAsyncRender(Ipp16u* fringes) {
	if (m_pThread == NULL || m_pThread->isRun == false) return;

	if (m_waitForFringes) {
		m_pFringesBuffer = fringes;
		CUtility::ResumeThread(m_pThread);
	}
}

void CMoriaImaging::CalculateAxialResolution(Ipp16u* fftData, Ipp16u& nPeakValue, int& nPeakIndex, int& nLineWidth) {
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nFFTLength = pConfig->nFftLength;
	const float fScaleFactor = pConfig->measurementValues.fAxialResolutionScale;
	const int nFindRange = 40;

	// get peak and index
	nPeakIndex = 0;
	nPeakValue = 0;
	for (int i = 0; i < nFFTLength; i++) {
		if (fftData[i] > nPeakValue) {
			nPeakIndex = i;
			nPeakValue = fftData[i];
		}
	}
	unsigned short nFWHM = nPeakValue - 3000;	// 3000 : 3db

	// find left 3db
	int nLeftIndex = 0;
	int nStart = nPeakIndex - nFindRange;
	nStart = (nStart < 0) ? 0 : nStart;
	for (int i = nStart; i <= nPeakIndex; i++) {
		if (fftData[i] > nFWHM) {
			nLeftIndex = i;
			break;
		}
	}

	// find right 3db
	int nRightIndex = 0;
	int nEnd = nPeakIndex + nFindRange;
	nEnd = (nEnd >= nFFTLength) ? nFFTLength - 1 : nEnd;
	for (int i = nPeakIndex; i <= nEnd; i++) {
		if (fftData[i] < nFWHM) {
			nRightIndex = i;
			break;
		}
	}

	double fLeftWidth = (double)(fftData[nLeftIndex] - nFWHM) / (double)(fftData[nLeftIndex] - fftData[nLeftIndex - 1]);
	double fRightWidth = (double)(fftData[nRightIndex] - nFWHM) / (double)(fftData[nRightIndex - 1] - fftData[nRightIndex]);

	nLineWidth = ((fRightWidth + nRightIndex) - (fLeftWidth + nLeftIndex)) * fScaleFactor;
}
void CMoriaImaging::CalculateNoisePower(Ipp16u* fftData, int nPeakIndex, Ipp16u& nNoisePower) {
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nFFTLength = pConfig->nFftLength;
	const int nNoiseSkip = pConfig->measurementValues.nNoiseSkip;
	const int nNoiseAverage = pConfig->measurementValues.nNoiseAverage;

	int nStart, nEnd;
	unsigned int nSum = 0;
	int nCount = 0;

	nStart = nPeakIndex - nNoiseSkip - nNoiseAverage;
	nStart = (nStart < 0) ? 0 : nStart;
	nEnd = nPeakIndex - nNoiseSkip;
	nEnd = (nEnd < 0) ? 0 : nEnd;
	
	for (int i = nStart; i <= nEnd; i++) {
		nSum += fftData[i];
		nCount++;
	}

	nStart = nPeakIndex + nNoiseSkip;
	nStart = (nStart >= nFFTLength) ? nFFTLength - 1 : nStart;
	nEnd = nPeakIndex + nNoiseSkip + nNoiseAverage;
	nEnd = (nEnd >= nFFTLength) ? nFFTLength - 1 : nEnd;

	for (int i = nStart; i <= nEnd; i++) {
		nSum += fftData[i];
		nCount++;
	}

	nNoisePower = nSum / nCount;
}



void CMoriaImaging::allocateMemory() {
	// ORDER = 11, nScans2n = 2^11
	// nScans 보다 큰 2^n 중에서 제일 작은 수
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int order = pConfig->constantValues.Order;
	const int zoom = pConfig->constantValues.Zoom;
	const int nScans = pConfig->nScans;
	const int nScansWithPadding = pConfig->nScans + pConfig->nScansPadding;
	const int nAlines = pConfig->nAlines;
	const int nScans2n = (1 << order);
	const int nScansOver2 = nScans2n / 2;
	const int nBufferSize = pConfig->nBufferSize;
	const int nDmaChannels = pConfig->nDmaChannels;
	const int nScopeLength = pConfig->getScopeLength();
	const int nFftLength = pConfig->nFftLength;

	fringes32f = ippsMalloc_32f(nDmaChannels * nScans);
	fringes32fSum = ippsMalloc_32f(nDmaChannels * nScans);
	ref_fringe = (Ipp16u*)ippsMalloc_16s(nScans * nDmaChannels);
	ippsSet_16s(32768, (Ipp16s*)ref_fringe, nScans * nDmaChannels);
	backgroundImage = (Ipp16u*)ippsMalloc_16s(nBufferSize);
	memset(backgroundImage, 0x00, sizeof(Ipp16u) * pConfig->nBufferSize);

	imageResult.create(nAlines, nFftLength, CV_8UC1);
	imageResultColor.create(nAlines, nFftLength, CV_8UC3);
	imageRectangle.create(nFftLength, nAlines, CV_8UC3);
	imageCircle.create(1024, 1024, CV_8UC3);

	scopeData = ippsMalloc_16u(nScopeLength * nDmaChannels);
	scopeFFTData = ippsMalloc_16u(nFftLength * nDmaChannels);

	backgroundImage_Deinterlaced = new Ipp16u * [nDmaChannels];
	backgroundImage32f_Deinterlaced = new Ipp32f * [nDmaChannels];
	fBuffer_BackgroundFringes = new Ipp32f * [nDmaChannels];
	uDataFringes_Deinterlaced = new Ipp16u * [nDmaChannels];
	uDataFingees_DeinterlacedwithPadding = new Ipp16u * [nDmaChannels];
	uBackgroundFringe_Deinterlaced = new Ipp16u * [nDmaChannels];
	fOutput = new Ipp32f * [nDmaChannels];
	for (int ch = 0; ch < nDmaChannels; ch++) {
		fBuffer_BackgroundFringes[ch] = ippsMalloc_32f(2048);
		backgroundImage_Deinterlaced[ch] = ippsMalloc_16u(nScans * nAlines);
		backgroundImage32f_Deinterlaced[ch] = ippsMalloc_32f(nScans * nAlines);
		uDataFringes_Deinterlaced[ch] = ippsMalloc_16u(nScans * nAlines);
		uDataFingees_DeinterlacedwithPadding[ch] = ippsMalloc_16u(nScansWithPadding * nAlines);
		uBackgroundFringe_Deinterlaced[ch] = ippsMalloc_16u(nScans);
		fOutput[ch] = ippsMalloc_32f(nScansOver2 * nAlines);
	}

	// Prepare FFT
	ippsFFTInitAlloc_R_32f(&specReal32FFT, order, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	ippsFFTGetBufSize_R_32f(specReal32FFT, &nSizeSpecReal32FFT);
	ippsFFTInitAlloc_C_32fc(&specComp32ZoomFFT, order + zoom - 2, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	ippsFFTGetBufSize_C_32fc(specComp32ZoomFFT, &nSizeSpecComp32ZoomFFT);
	ippsFFTInitAlloc_C_32fc(&specComp32FFT, order + zoom - 3, IPP_FFT_NODIV_BY_ANY, ippAlgHintFast);
	ippsFFTGetBufSize_C_32fc(specComp32FFT, &nSizeSpecComp32FFT);
}
void CMoriaImaging::releaseMemory() {
	const CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nDmaChannels = pConfig->nDmaChannels;

	if (fringes32f) { ippsFree(fringes32f); fringes32f = NULL; }
	if (fringes32fSum) { ippsFree(fringes32fSum); fringes32fSum = NULL; }
	if (ref_fringe) { ippsFree(ref_fringe); ref_fringe = NULL; }
	if (backgroundImage) { ippsFree(backgroundImage); backgroundImage = NULL; }

	imageResult.release();
	imageResultColor.release();
	imageRectangle.release();
	imageCircle.release();

	ippsRelease((void*&)scopeData);
	ippsRelease((void*&)scopeFFTData);

	ippsRelease_double_ptr((void**&)fBuffer_BackgroundFringes, nDmaChannels);
	ippsRelease_double_ptr((void**&)backgroundImage_Deinterlaced, nDmaChannels);
	ippsRelease_double_ptr((void**&)backgroundImage32f_Deinterlaced, nDmaChannels);
	ippsRelease_double_ptr((void**&)uDataFringes_Deinterlaced, nDmaChannels);
	ippsRelease_double_ptr((void**&)uDataFingees_DeinterlacedwithPadding, nDmaChannels);
	ippsRelease_double_ptr((void**&)uBackgroundFringe_Deinterlaced, nDmaChannels);
	ippsRelease_double_ptr((void**&)fOutput, nDmaChannels);

	if (specReal32FFT) { ippsFFTFree_R_32f(specReal32FFT); specReal32FFT = NULL; }
	if (specComp32ZoomFFT) { ippsFFTFree_C_32fc(specComp32ZoomFFT); specComp32ZoomFFT = NULL; }
	if (specComp32FFT) { ippsFFTFree_C_32fc(specComp32FFT); specComp32FFT = NULL; }
}

//fringes는 2*nScans*nAlines signal data, 결과:ref_fringe
void CMoriaImaging::generateBackground(Ipp16u* fringes) {
	CMoriaConfiguration *pConfig = CMoriaConfiguration::GetInstance();
	const int nScans = pConfig->nScans;
	const int nAlines = pConfig->nAlines;
	const int nDmaChannels = pConfig->nDmaChannels;
	const int nWidth = nScans * nDmaChannels;

	// 모든 fringe의 평균으로 background를 계산한다. 
	ippsZero_32f(fringes32fSum, nWidth);

	for (int i = 0; i < nAlines; i++)
	{
		ippsConvert_16u32f(fringes + i * nWidth, fringes32f, nWidth);
		ippsAdd_32f_I(fringes32f, fringes32fSum, nWidth);
	}

	ippsMulC_32f_I(1.0f / ((float)nAlines), fringes32fSum, nWidth);
	ippsConvert_32f16u_Sfs(fringes32fSum, ref_fringe, nWidth, ippRndNear, 0);
}

void CMoriaImaging::generateImage(const Ipp16u *fringes, bool bInvert){
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nScans = pConfig->nScans, nAlines = pConfig->nAlines;
	const int nScansPadding = pConfig->nScansPadding;
	const int nScansWithPadding = (nScans + nScansPadding);
	const int nDmaChannels = pConfig->nDmaChannels;
	const float fHighLevel = (bInvert) ? pConfig->invert.highLevel : 0.0f;
	const float fLowLevel = (bInvert) ? pConfig->invert.lowLevel : 0.0f;
	const int order = pConfig->constantValues.Order;
	const int zoom = pConfig->constantValues.Zoom;
	const int numDynamic = pConfig->settingsOpenMP.numDynamic;
	const int numThreads = pConfig->settingsOpenMP.numThread;
	const int nScopeLength = pConfig->getScopeLength();
	const int X = 0;
	const int Y = 1;
	int i, j;

	// ORDER = 11, nScans2n = 2^11
	// nScans 보다 큰 2^n 중에서 제일 작은 수
	const int nScans2n = (1 << order);
	const int nScansOver2 = nScans2n / 2;
	const int nScansOver4 = nScans2n / 4;
	const int nScansZoom = (1 << (order + zoom - 2));

	// Deinterlace fringe
	ippsDeinterleave_16s((Ipp16s*)fringes, nDmaChannels, nAlines * nScans, (Ipp16s**)uDataFringes_Deinterlaced);
	
	// copy first line to display scope
	ippsCopy_16s((Ipp16s*)uDataFringes_Deinterlaced[0], (Ipp16s*)scopeData, nScopeLength);
	ippsCopy_16s((Ipp16s*)uDataFringes_Deinterlaced[1], (Ipp16s*)scopeData + nScopeLength, nScopeLength);

	// Deinterlace uBackgroundFringes
	ippsDeinterleave_16s((Ipp16s*)ref_fringe, nDmaChannels, nScans, (Ipp16s**)uBackgroundFringe_Deinterlaced);
	for (int ch = 0; ch < nDmaChannels; ch++) {
		ippsConvert_16u32f(uBackgroundFringe_Deinterlaced[ch], fBuffer_BackgroundFringes[ch], nScans);
	}

	// Process Frame
	// To-Do : enable openmp, check shared variables
	omp_set_dynamic(numDynamic);
	omp_set_num_threads(numThreads);
//#pragma omp parallel
	{
//#pragma omp for firstprivate(fBuffer_Fringes,fBuffer_BackgroundFringes,fBuffer_Complex,fBuffer_DFT,j)
		for (i = 0; i < nAlines; i++)
		{
			// Process X, Y Polarization
			for (int ch = 0; ch < nDmaChannels; ch++)
			{
				// 1. Background Subtract
				ippsConvert_16u32f(uDataFringes_Deinterlaced[ch] + i * nScans, fBuffer_Fringes, nScans);

				ippsSub_32f_I(fBuffer_BackgroundFringes[ch], fBuffer_Fringes, nScans);  // I의 의미:자기 자신에 이처리를 해서, 결과를 얻는다.

				// 2. Apply Window
				ippsMul_32f_I(calibration->window, fBuffer_Fringes, nScans2n);

				// 3. First FFT
				ippsFFTFwd_RToPerm_32f_I(fBuffer_Fringes, specReal32FFT, NULL); // http://software.intel.com/sites/products/documentation/hpc/ipp/ipps/ipps_ch7/ch7_packed_formats.html#Perm
				ippsConjPerm_32fc(fBuffer_Fringes, fBuffer_Complex, nScans2n);

				// 4. Zero Pad & Reorder (2 | 0 | 0 | 1)
				ippsZero_32fc(fBuffer_DFT, nScansZoom);
				ippsCopy_32fc(fBuffer_Complex + nScansOver4, fBuffer_DFT, nScansOver4);
				ippsCopy_32fc(fBuffer_Complex, fBuffer_DFT + nScansZoom - nScansOver4, nScansOver4);

				// 5. Inverse FFT
				ippsFFTInv_CToC_32fc_I(fBuffer_DFT, specComp32ZoomFFT, NULL);

				// 6. Interpolation
				ippsZero_32fc(fBuffer_Complex, nScansOver2);
				for (j = 0; j < nScans / 2; j++){
					fBuffer_Complex[j].re = (calibration->weightMap[j] * fBuffer_DFT[calibration->indexMap[j]].re + (1.0f - calibration->weightMap[j]) * fBuffer_DFT[calibration->indexMap[j] + 1].re);
					fBuffer_Complex[j].im = (calibration->weightMap[j] * fBuffer_DFT[calibration->indexMap[j]].im + (1.0f - calibration->weightMap[j]) * fBuffer_DFT[calibration->indexMap[j] + 1].im);
				}

				// 7. Numerical Dispersion Compensation
				ippsMul_32fc_I(calibration->dispersion, fBuffer_Complex, nScans / 2);

				// 8. FFT Again
				ippsFFTFwd_CToC_32fc_I(fBuffer_Complex, specComp32FFT, NULL);

				// 9. Extract Magnitude
				ippsPowerSpectr_32fc(fBuffer_Complex, fOutput[ch] + i * nScansOver2 + nScansOver4, nScansOver4);
				ippsPowerSpectr_32fc(fBuffer_Complex + nScansOver4, fOutput[ch] + i * nScansOver2, nScansOver4);
			}

			// Calculate data for scope control
			if (scopeFFTData && i == 0)
			{
				generateScopeData(fOutput[X], scopeFFTData);
				generateScopeData(fOutput[Y], scopeFFTData + 1024);
			}

			// Last. X축, Y축 데이터를 더해 이미지를 만든다. Polarization 데이터를 더해서, 최종이미지 생성
			ippsAdd_32f_I(fOutput[X] + i * nScansOver2, fOutput[Y] + i * nScansOver2, nScansOver2);
			ippsLn_32f_I(fOutput[Y] + i * nScansOver2, nScansOver2);
			ippsMulC_32f(fOutput[Y] + i * nScansOver2, 4.3429f, fOutput[X] + i * nScansOver2, nScansOver2);
			ippsSubC_32f_I((calibration->lowLevel + fLowLevel), fOutput[X] + i * nScansOver2, nScansOver2);
			ippsMulC_32f_I(255.0f / (calibration->highLevel - fHighLevel), fOutput[X] + i * nScansOver2, nScansOver2);
			ippsConvert_32f8u_Sfs(fOutput[X] + i * nScansOver2, imageResult.data + i * nScansOver2 /*stepBytes*/, nScansOver2, ippRndNear, 0);
		} // Process X, Y Polarization

	} // end parallel region
}
void CMoriaImaging::generateScopeData(Ipp32f* output, Ipp16u* scope) {
	const CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int order = pConfig->constantValues.Order;
	const int nScans2n = (1 << order);
	const int nScansOver2 = nScans2n / 2;
	Ipp32f temp[1024];

	ippsLn_32f(output, temp, nScansOver2);
	ippsMulC_32f_I(4.3429f, temp, nScansOver2);
	ippsSubC_32f_I(calibration->lowLevel, temp, nScansOver2);
	ippsMulC_32f_I(65535 / (calibration->highLevel), temp, nScansOver2);
	ippsConvert_32f16u_Sfs(temp, scope, nScansOver2, ippRndNear, 0);
}

void CMoriaImaging::circularizeImage(cv::Mat& src, cv::Mat& dst)
{
	CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	CMoriaConfiguration* conf = CMoriaConfiguration::GetInstance();

	cv::remap(src, dst, conf->pXMap, conf->pYMap, cv::INTER_LINEAR);
}

void CMoriaImaging::applyHotColor(cv::Mat& image) {
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

void CMoriaImaging::loadLUT(const char* strLUTPath) {
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
void CMoriaImaging::applyLUT(cv::Mat& image) {

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

UINT CMoriaImaging::threadRender(LPVOID param) {
	CMoriaImaging* pImaging = (CMoriaImaging*)param;
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