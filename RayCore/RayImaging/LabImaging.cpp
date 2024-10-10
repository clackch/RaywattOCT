#include "LabImaging.h"
#include "Calibration.h"

CLabImaging::CLabImaging(Setting setting, CMessageService* pMsg)
	: COCTImaging(setting, pMsg) {
	backgroundData = nullptr;
	backgroundFFT = nullptr;
	backgroundSubtracted = nullptr;
	logData = nullptr;

	scopeData = nullptr;
	scopeFFTData = nullptr;

	subtract = false;

	newCalibration = nullptr;
	hasNewCalibration = false;
}
CLabImaging::~CLabImaging() {
	if(backgroundData != nullptr) delete[] backgroundData;
	if(backgroundFFT != nullptr) delete[] backgroundFFT;
	if(backgroundSubtracted != nullptr) delete[] backgroundSubtracted;
	if(logData != nullptr) delete[] logData;

	if(scopeData != nullptr) delete[] scopeData;
	if (scopeFFTData != nullptr) delete[] scopeFFTData;

	imageRectangle.release();
}

void CLabImaging::Initialize(CCalibration* calibration, USHORT* backgroundData) {
	COCTImaging::Initialize(calibration);

	const int nAScan = m_setting.nAScan;
	const int nBScan = m_setting.nBScan;
	const int nBufferSize = m_setting.nBufferSize;
	const int nOutputLength = m_setting.nOutputLength;

	this->backgroundData = backgroundData;
	this->backgroundFFT = new float[nOutputLength * nBScan];
	this->backgroundSubtracted = new float[nOutputLength * nBScan];
	this->logData = new float[nOutputLength * nBScan];

	generateBackground((Ipp16u*)backgroundData);
	fftProcessing(fringes32f);

	ippsCopy_32f(fFFTResult, backgroundFFT, nOutputLength * nBScan);

	scopeData = new USHORT[nAScan * 2];
	scopeFFTData = new USHORT[nOutputLength * 2];

	imageRectangle.create(nOutputLength, nBScan, CV_8UC3);

	goodClockStart = 0;
	goodClockEnd = nAScan;
}
void CLabImaging::Process(char* fringes) {
	const bool bInvert = m_bInvert;
	const int nAScan = m_setting.nAScan;
	const int nBScan = m_setting.nBScan;
	const int nOutputLength = m_setting.nOutputLength;
	const int nFFTLength = m_setting.nFFTLength;

	if (fringes == nullptr) return;

	if (hasNewCalibration) {
		delete calibration;
		calibration = newCalibration;

		hasNewCalibration = false;
	}

	// copy first line to display scope
	ippsCopy_16s((Ipp16s*)fringes, (Ipp16s*)scopeData, nAScan);

	generateBackground((Ipp16u*)fringes);

	//cropSignalData(fringes, goodClockStart, goodClockEnd);

	fftProcessing(fringes32f);
	computeLogarithm(fFFTResult, logData);

	if (subtract) {
		subtractBackground<float>(fFFTResult, backgroundFFT, backgroundSubtracted, nOutputLength * nBScan);
		computeLogarithm(backgroundSubtracted, logData);
	}

	generateScopeData(logData, scopeFFTData);

	if (!subtract) {
		memset(scopeFFTData + nOutputLength, 0x00, sizeof(USHORT) * nOutputLength);
	}
	else {
		generateScopeData(backgroundFFT, scopeFFTData + nOutputLength);
	}
	
	generateImage(logData, false);

	adaptive_compensation();
}

void CLabImaging::PostProcess(cv::Mat image) {
	COCTImaging::PostProcess(image);
	cv::rotate(imageResultColor, imageRectangle, cv::ROTATE_90_COUNTERCLOCKWISE);
}

void CLabImaging::ChangeCalibration(CCalibration* pNewCalib) {
	newCalibration = pNewCalib;
	hasNewCalibration = true;
}

template <typename T>
void CLabImaging::subtractBackground(T* fringes, T* background, T* dst, int size) {
	if (fringes == nullptr || background == nullptr || dst == nullptr) return;

#pragma omp parallel for
	for (int i = 0; i < size; i++) {
		dst[i] = fringes[i] - background[i];
	}
}

void CLabImaging::generateScopeData(Ipp32f* output, Ipp16u* scope) {
	const int nFFTLength = m_setting.nFFTLength;
	const int nOutputLength = m_setting.nOutputLength;
	Ipp32f* temp = new Ipp32f[nOutputLength];
	
	ippsSubC_32f(output, m_setting.lowLevel, temp, nOutputLength);
	ippsMulC_32f_I(USHRT_MAX / m_setting.highLevel, temp, nOutputLength);
	ippsConvert_32f16u_Sfs(temp, scope, nOutputLength, ippRndNear, 0);

	delete[] temp;
}

void CLabImaging::cropSignalData(USHORT* fringes, int start, int end) {
	const int nAScan = m_setting.nAScan;
	const int nBScan = m_setting.nBScan;

	for (int i = 0; i < nBScan; i++) {
		USHORT* buffer = fringes + i * nAScan;
		memset(buffer, 0x00, sizeof(USHORT) * start);
		memset(buffer + end - 1, 0x00, sizeof(USHORT) * (nAScan - end));
	}
}