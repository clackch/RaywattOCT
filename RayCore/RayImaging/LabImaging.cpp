#include "LabImaging.h"
#include "Configuration.h"
#include "Calibration.h"

CLabImaging::CLabImaging(CMessageService* pMsg) 
	: COCTImaging(pMsg) {
	fringesSubtracted = nullptr;
	backgroundData = nullptr;
	backgroundFFT = nullptr;

	scopeData = nullptr;
	scopeFFTData = nullptr;

	subtract = false;
	subtractFFT = false;

	newCalibration = nullptr;
	hasNewCalibration = false;
}
CLabImaging::~CLabImaging() {
	if (fringesSubtracted != nullptr) delete[] fringesSubtracted;
	if(backgroundData != nullptr) delete[] backgroundData;
	if(backgroundFFT != nullptr) delete[] backgroundFFT;

	if(scopeData != nullptr) delete[] scopeData;
	if (scopeFFTData != nullptr) delete[] scopeFFTData;

	imageRectangle.release();
}

void CLabImaging::Initialize(tstring calibFile, const char* strBgFile) {
	COCTImaging::Initialize(calibFile);

	CConfiguration& config = CConfiguration::GetInstance();
	const int nBScan = config.nBScan;
	const int nBufferSize = config.nBufferSize;
	const int nFFTLength = config.nFFTLength;
	const int nOutputLength = config.nOutputLength;
	const int nScopeLength = config.getScopeLength();

	fringesSubtracted = new USHORT[nBufferSize];
	backgroundData = new USHORT[nBufferSize];
	backgroundFFT = new float[nOutputLength * nBScan];

	memset(backgroundData, 0x00, sizeof(USHORT) * nBufferSize);
	FILE* fp = fopen(strBgFile, "rb");
	if (fp != nullptr) {
		if (fp) {
			size_t readSize = fread(backgroundData, sizeof(USHORT), nBufferSize, fp);
			if (readSize != nBufferSize) {
				memset(backgroundData, 0x00, sizeof(USHORT) * nBufferSize);
			}
			fclose(fp);
		}
	}

	generateBackground((Ipp16u*)backgroundData);
	fftProcessing(fringes32f);

	ippsCopy_32f(fFFTResult, backgroundFFT, nOutputLength * nBScan);

	scopeData = new USHORT[nScopeLength * 2];
	scopeFFTData = new USHORT[nOutputLength * 2];

	imageRectangle.create(nOutputLength, nBScan, CV_8UC3);
}
void CLabImaging::Process(USHORT* fringes) {
	CConfiguration& config = CConfiguration::GetInstance();
	const bool bInvert = m_bInvert;
	const int nBScan = config.nBScan;
	const int nScopeLength = config.getScopeLength();
	const int nOutputLength = config.nOutputLength;
	const int nFFTLength = config.nFFTLength;

	if (fringes == nullptr) return;

	if (hasNewCalibration) {
		delete calibration;
		calibration = newCalibration;

		hasNewCalibration = false;
	}

	// copy first line to display scope
	ippsCopy_16s((Ipp16s*)fringes, (Ipp16s*)scopeData, nScopeLength);

	ippsCopy_16s((Ipp16s*)fringes, (Ipp16s*)fringesSubtracted, config.nBufferSize);
	if (!subtract) {
		memset(scopeData + nScopeLength, 0x00, sizeof(USHORT) * nScopeLength);
	}
	else {
		subtractBackground<USHORT>(fringesSubtracted, backgroundData, config.nBufferSize);
		ippsCopy_16s((Ipp16s*)backgroundData, (Ipp16s*)scopeData + nScopeLength, nScopeLength);
	}

	generateBackground((Ipp16u*)fringesSubtracted);

	fftProcessing(fringes32f);

	generateScopeData(fFFTResult, scopeFFTData);

	if (!subtractFFT) {
		memset(scopeFFTData + nOutputLength, 0x00, sizeof(USHORT) * nOutputLength);
	}
	else {
		subtractBackground<float>(this->fFFTResult, backgroundFFT, nOutputLength * nBScan);
		generateScopeData(backgroundFFT, scopeFFTData + nOutputLength);
	}
	
	generateImage(false);

	postProcessing();

	cv::rotate(imageResultColor, imageRectangle, cv::ROTATE_90_COUNTERCLOCKWISE);
}

void CLabImaging::ChangeCalibration(CCalibration* pNewCalib) {
	newCalibration = pNewCalib;
	hasNewCalibration = true;
}

template <typename T>
void CLabImaging::subtractBackground(T* fringes, T* background, int size) {
	if (fringes == nullptr || background == nullptr) return;

#pragma omp parallel for
	for (int i = 0; i < size; i++) {
		fringes[i] = fringes[i] - background[i];
	}
}

void CLabImaging::generateScopeData(Ipp32f* output, Ipp16u* scope) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nFFTLength = config.nFFTLength;
	const int nOutputLength = nFFTLength / 2;
	Ipp32f temp[1024];

	ippsLn_32f(output, temp, nOutputLength);
	ippsMulC_32f_I(log10(exp(1)) * 10, temp, nOutputLength);
	ippsSubC_32f_I(calibration->lowLevel, temp, nOutputLength);
	ippsMulC_32f_I(65535 / (calibration->highLevel), temp, nOutputLength);
	ippsConvert_32f16u_Sfs(temp, scope, nOutputLength, ippRndNear, 0);
}