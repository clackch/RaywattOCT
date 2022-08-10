#include "pch.h"
#include "MoriaCalibration.h"
#include "MoriaConfiguration.h"

CMoriaCalibration::CMoriaCalibration() :
	indexMap(NULL),
	weightMap(NULL),
	window(NULL),
	dispersion(NULL)
{
}

CMoriaCalibration::~CMoriaCalibration() 
{
	releaseMemory();
}

BOOL CMoriaCalibration::Initialize()
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

BOOL CMoriaCalibration::loadCalibration(LPCTSTR calibrationFileName){
	const CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nScans = pConfig->nScans;

	// open calibration file
	HANDLE hCalibFile = CreateFile(calibrationFileName, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);

	if (hCalibFile == INVALID_HANDLE_VALUE) 
		return FALSE;

	int fileSize =0;
	DWORD dwIgnored;
	Ipp32f *dispersionReal = ippsMalloc_32f(nScans);

	ReadFile(hCalibFile, indexMap, sizeof(Ipp32s) * nScans/2, &dwIgnored, NULL); fileSize += dwIgnored;
	ReadFile(hCalibFile, weightMap, sizeof(Ipp32f) * nScans/2, &dwIgnored, NULL); fileSize += dwIgnored;
	ReadFile(hCalibFile, dispersionReal, sizeof(Ipp32f) * nScans, &dwIgnored, NULL); fileSize += dwIgnored;

	CloseHandle(hCalibFile);

	if (fileSize != 2 * nScans * sizeof(Ipp32s))
	{
		::MessageBox(NULL, _T("Calibration file has wrong size.") ,_T( "Error"), MB_OK|MB_ICONSTOP);
		ippsFree(dispersionReal);
		return false;
	}

	// 실수 허수부를 복합하여 리턴
	ippsRealToCplx_32f(dispersionReal, dispersionReal + nScans/2, dispersion, nScans / 2);
	ippsFree(dispersionReal);

	return true;
}

void CMoriaCalibration::setWindow(enum Windows eWindow)
{
	const CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nScans = pConfig->nScans;
	const int order = pConfig->constantValues.Order;
	const int nScans2n = 1 << order;

	ippsSet_32f(1.0f, window, nScans2n);

	switch (eWindow)
	{
	case None:
		break;

	case Hanning:
		ippsWinHann_32f_I(window, nScans);
		break;

	case Hamming:
		ippsWinHamming_32f_I(window, nScans);
		break;

	case Gauss1:
		for (int i = 0; i < nScans; i++)
			window[i] = exp(-(2.0f/((float) nScans))*(2.0f/((float) nScans))*(i-((float) nScans)/2.0f)*(i-((float) nScans)/2.0f));
		break;
	}

	ippsZero_32f(window + nScans, nScans2n - nScans);
}

void CMoriaCalibration::allocateMemory() {
	const CMoriaConfiguration* pConfig = CMoriaConfiguration::GetInstance();
	const int nScans = pConfig->nScans;
	const int order = pConfig->constantValues.Order;
	const int nScans2n = 1 << order;

	// memory allocate
	indexMap = ippsMalloc_32s(nScans / 2);
	weightMap = ippsMalloc_32f(nScans / 2);
	dispersion = ippsMalloc_32fc(nScans / 2);
	window = ippsMalloc_32f(nScans2n);
}
void CMoriaCalibration::releaseMemory(){
	if (indexMap) {ippsFree(indexMap); indexMap = NULL;}
	if (weightMap) {ippsFree(weightMap); weightMap = NULL;}
	if (dispersion) { ippsFree(dispersion); dispersion = NULL; }
	if (window) { ippsFree(window); window = NULL; }
}