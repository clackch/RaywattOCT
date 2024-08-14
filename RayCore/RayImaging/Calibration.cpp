#include <Windows.h>
#include <math.h>
#include "Calibration.h"

CCalibration::CCalibration(int nAScan, int nFFTLength) :
	nAScan(nAScan),
	nFFTLength(nFFTLength),
	data(nullptr),
	window(nullptr),
	dispersion(nullptr)
{
	allocateMemory();
	setWindow(Hanning);
}

CCalibration::~CCalibration() 
{
	releaseMemory();
}

bool CCalibration::Initialize(tstring calibFile)
{
	bool result = readCalibration(calibFile.c_str());
	if (!result) return false;

	return loadCalibration();
}
bool CCalibration::Initialize(char* data)
{
	const int calibrationSize = nAScan * sizeof(float) * 2;	// dispersion map (real, imag)
	if (data == nullptr) return false;

	memcpy(this->data, data, calibrationSize);

	return loadCalibration();
}

bool CCalibration::loadCalibration() {
	float* dispersionReal = new float[nAScan * 2];

	int offset = 0;
	memcpy(dispersionReal, data + offset, nAScan * 2 * sizeof(float)); offset += (nAScan * 2 * sizeof(float));

	// 실수 허수부를 복합하여 리턴
	ippsRealToCplx_32f(dispersionReal, dispersionReal + nAScan, (Ipp32fc*)dispersion, nAScan);

	delete[] dispersionReal;

	return true;
}
bool CCalibration::readCalibration(LPCTSTR calibrationFileName){
	const int calibrationSize = nAScan * sizeof(int) * 2;	// dispersion map (real, imag)

	// open calibration file
	HANDLE hCalibFile = CreateFile(calibrationFileName, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);

	if (hCalibFile == INVALID_HANDLE_VALUE) 
		return FALSE;

	int fileSize = 0;
	DWORD dwIgnored;

	ReadFile(hCalibFile, data, calibrationSize, &dwIgnored, nullptr); fileSize += dwIgnored;

	CloseHandle(hCalibFile);

	if (fileSize != calibrationSize)
	{
		return false;
	}


	return true;
}

void CCalibration::setWindow(enum Windows eWindow)
{
	const float fAScan = (float) nAScan;

	ippsSet_32f(1.0f, window, nFFTLength);

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

	ippsZero_32f(window + nAScan, nFFTLength - nAScan);
}

void CCalibration::allocateMemory() {
	// memory allocate
	data = new char[nAScan * sizeof(float) * 2];
	dispersion = new complex_t[nAScan];
	window = new float[nFFTLength];
}

void CCalibration::releaseMemory() {
	if (data) { delete[] data; data = nullptr; }
	if (dispersion) { delete[] dispersion; dispersion = nullptr; }
	if (window) { delete[] window; window = nullptr; }
}