#include <Windows.h>
#include <math.h>
#include "Calibration.h"
#include "Configuration.h"

CCalibration::CCalibration() :
	indexMap(nullptr),
	weightMap(nullptr),
	window(nullptr),
	dispersion(nullptr)
{
}

CCalibration::~CCalibration() 
{
	releaseMemory();
}

bool CCalibration::Initialize()
{
	releaseMemory();
	allocateMemory();

	// level param
	lowLevel = 108.0f;
	highLevel = 109.0f;
	
	// Setup options
	setWindow(Hanning);
	return loadCalibration(_T("CALIBRATION.DAT"));
}

bool CCalibration::loadCalibration(LPCTSTR calibrationFileName){
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;

	// open calibration file
	HANDLE hCalibFile = CreateFile(calibrationFileName, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);

	if (hCalibFile == INVALID_HANDLE_VALUE) 
		return FALSE;

	int fileSize =0;
	DWORD dwIgnored;
	float* dispersionReal = new float[nAScan];

	ReadFile(hCalibFile, indexMap, sizeof(int) * nAScan /2, &dwIgnored, nullptr); fileSize += dwIgnored;
	ReadFile(hCalibFile, weightMap, sizeof(float) * nAScan /2, &dwIgnored, nullptr); fileSize += dwIgnored;
	ReadFile(hCalibFile, dispersionReal, sizeof(float) * nAScan, &dwIgnored, nullptr); fileSize += dwIgnored;

	CloseHandle(hCalibFile);

	if (fileSize != 2 * nAScan * sizeof(int))
	{
		delete[] dispersionReal;
		return false;
	}

	// 실수 허수부를 복합하여 리턴
	ippsRealToCplx_32f(dispersionReal, dispersionReal + nAScan /2, (Ipp32fc *)dispersion, nAScan / 2);
	
	delete[] dispersionReal;

	return true;
}

void CCalibration::setWindow(enum Windows eWindow)
{
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const float fAScan = (float) nAScan;
	const int order = config.constantValues.Order;
	const int nScans2n = 1 << order;

	ippsSet_32f(1.0f, window, nScans2n);

	switch (eWindow)
	{
	case None:
		break;

	case Hanning:
		ippsWinHann_32f_I(window, nAScan);
		break;

	case Hamming:
		ippsWinHamming_32f_I(window, nAScan);
		break;

	case Gauss1:
		for (int i = 0; i < nAScan; i++)
			window[i] = exp(-(2.0f / fAScan) * (2.0f / fAScan) * (i - fAScan /2.0f) * (i- fAScan /2.0f));
		break;
	}

	ippsZero_32f(window + nAScan, nScans2n - nAScan);
}

void CCalibration::allocateMemory() {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const int order = config.constantValues.Order;
	const int nScans2n = 1 << order;

	// memory allocate
	indexMap = new int[nAScan / 2];
	weightMap = new float[nAScan / 2];
	dispersion = new complex_t[nAScan / 2];
	window = new float[nScans2n];
}

void CCalibration::releaseMemory(){
	if (indexMap) { delete[] indexMap; indexMap = nullptr; }
	if (weightMap) { delete[] weightMap; weightMap = nullptr;}
	if (dispersion) { delete[] dispersion; dispersion = nullptr; }
	if (window) { delete[] window; window = nullptr; }
}