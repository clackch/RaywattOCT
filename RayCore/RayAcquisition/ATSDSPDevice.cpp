#include "Config.h"
#include "ATSDSPDevice.h"
#include "Configuration.h"
#include <stdio.h>
#include <vector>

CATSDSPDevice::CATSDSPDevice() {
	m_fftHandle = nullptr;
	m_bytesPerBuffer = 0;
	m_pBackgroundFringes = nullptr;
}
CATSDSPDevice::~CATSDSPDevice() {}

int CATSDSPDevice::stop() {
	// Abort the acquisition
	RETURN_CODE retCode = AlazarDSPAbortCapture(m_hATSBoard);

	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarDSPAbortCapture failed -- %s\n", AlazarErrorToText(retCode));

		return retCode;
	}

	return NOERROR;
}
unsigned short* CATSDSPDevice::acquire(int& nCurFrame, int& nTotalFrame) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAcqBufCount = config.settingsAlazar.nAcqBufferCount;
	const U32 timeout_ms = 5000;
	RETURN_CODE retCode;

	nCurFrame = 0;
	nTotalFrame = 0;

	m_pCurBuffer = m_pAcqBuffers[m_nBufferIndex];
	{
		// Add the buffer to the end of the list of available buffers.
		if (m_pPrevBuffer != nullptr) {
			retCode = AlazarPostAsyncBuffer(m_hATSBoard, m_pPrevBuffer, m_bytesPerBuffer);
		}

		// Wait for the buffer at the head of the list of available buffers
		// to be filled by the board.
		retCode = AlazarDSPGetBuffer(m_hATSBoard, m_pCurBuffer, timeout_ms);
		if (retCode != ApiSuccess)
		{
			printf("Error: AlazarDSPGetBuffer failed -- %s\n",
				AlazarErrorToText(retCode));
			return nullptr;
		}

		m_pPrevBuffer = m_pCurBuffer;
	}

	m_nBufferIndex++;
	m_nBufferIndex = (m_nBufferIndex >= nAcqBufCount) ? 0 : m_nBufferIndex;

	return m_pCurBuffer;
}

BOOL CATSDSPDevice::configureFPGA(HANDLE boardHandle) {
	RETURN_CODE retCode = ApiSuccess;
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const int nFFTLength = config.nFFTLength;

	// Get a handle to the FFT module
	U32 numModules;
	retCode = AlazarDSPGetModules(boardHandle, 0, nullptr, &numModules);
	if (numModules < 1) {
		printf("This board does any DSP modules.\n");
		return FALSE;
	}

	std::vector<dsp_module_handle> dspHandles(numModules);
	retCode = AlazarDSPGetModules(boardHandle, numModules, &dspHandles[0], nullptr);
	m_fftHandle = dspHandles[0];

	// Configure FFT module
	U32 dspModuleId;
	retCode = AlazarDSPGetInfo(m_fftHandle, &dspModuleId, nullptr, nullptr, nullptr, nullptr, nullptr);
	if (dspModuleId != DSP_MODULE_FFT) {
		printf("Error: DSP module is not FFT\n");
		return FALSE;
	}

	// Create and fill the window function

	std::vector<float> window(nFFTLength);
	U32 windowType = DSP_WINDOW_HANNING;

	retCode = AlazarDSPGenerateWindowFunction(windowType,
		&window[0],
		nAScan,
		nFFTLength - nAScan);

	// Set the window function

	retCode = AlazarFFTSetWindowFunction(m_fftHandle,
		nFFTLength,
		&window[0],
		nullptr);

	// Background subtraction
	if (m_pBackgroundFringes != nullptr) {
		retCode = AlazarFFTBackgroundSubtractionSetRecordS16(m_fftHandle, (S16*)m_pBackgroundFringes, nAScan);

		retCode = AlazarFFTBackgroundSubtractionSetEnabled(m_fftHandle, TRUE);
	}
	
	return (retCode == ApiSuccess);
}

BOOL CATSDSPDevice::calculateMemorySize(HANDLE boardHandle, U16 channelMask, U32 recordsPerBuffer, U32& samplesPerRecord, U32& bytesPerBuffer) {
	RETURN_CODE retCode = ApiSuccess;
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const int nFFTLength = config.nFFTLength;

	// Calculate the number of enabled channels from the channel mask
	int channelCount = 0;
	int channelsPerBoard = 2;
	for (int channel = 0; channel < channelsPerBoard; channel++)
	{
		U32 channelId = 1U << channel;
		if (channelMask & channelId)
			channelCount++;
	}

	// Configure the FFT
	samplesPerRecord = 0;

	// Select the FFT output format
	U32 outputFormat = FFT_OUTPUT_FORMAT_U16_LOG;

	// Select the presence of NPT footers
	U32 footer = FFT_FOOTER_NONE;

	retCode = AlazarFFTSetup(m_fftHandle,
		channelMask,
		nAScan,
		nFFTLength,
		outputFormat,
		footer,
		0,
		&samplesPerRecord);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarSetRecordSize failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	bytesPerBuffer = samplesPerRecord * recordsPerBuffer * channelCount;
	m_bytesPerBuffer = bytesPerBuffer;

	return TRUE;
}