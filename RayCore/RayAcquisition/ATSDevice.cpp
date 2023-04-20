#include "Config.h"
#include "ATSDevice.h"
#include "Configuration.h"

CATSDevice::CATSDevice() {
	m_hATSBoard = NULL;
	m_nBufferIndex = 0;
	m_pAcqBuffers = NULL;
	m_pCurBuffer = NULL;
	m_pPrevBuffer = NULL;
}
CATSDevice::~CATSDevice() {}

int CATSDevice::InitDevice() {
	CleanUp();

	U32 systemId = 1;
	U32 boardId = 1;

	m_hATSBoard = AlazarGetBoardBySystemID(systemId, boardId);
	if (m_hATSBoard == NULL)
	{
		printf("Error: Unable to open board system Id %u board Id %u\n", systemId, boardId);
		return E_FAIL;
	}

	configureBoard(m_hATSBoard);

	m_isInit = true;

	return NOERROR;
}
int CATSDevice::CleanUp() {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAcqBufCount = config.settingsAlazar.nAcqBufferCount;

	// Free all memory allocated
	if (m_pAcqBuffers != NULL) {
		for (int bufferIndex = 0; bufferIndex < nAcqBufCount; bufferIndex++)
		{
			if (m_pAcqBuffers[bufferIndex] != NULL)
			{
#ifdef _WIN32
				VirtualFree(m_pAcqBuffers[bufferIndex], 0, MEM_RELEASE);
#else
				free(BufferArray[bufferIndex]);
#endif
			}
		}
		delete[] m_pAcqBuffers;
		m_pAcqBuffers = nullptr;
	}

	m_isInit = false;

	return NOERROR;
}

int CATSDevice::start() {
	configureAcquisition(m_hATSBoard);

	// Arm the board system to wait for a trigger event to begin the acquisition
	RETURN_CODE retCode = AlazarStartCapture(m_hATSBoard);

	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarStartCapture failed -- %s\n", AlazarErrorToText(retCode));
		MessageBox((HWND)"AlazarStartCapture failed", NULL, L"Error", MB_OK);

		return retCode;
	}

	return NOERROR;
}

int CATSDevice::stop() {
	// Abort the acquisition
	RETURN_CODE retCode = AlazarAbortAsyncRead(m_hATSBoard);

	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarAbortAsyncRead failed -- %s\n", AlazarErrorToText(retCode));
		
		return retCode;
	}

	return NOERROR;
}

unsigned short *CATSDevice::acquire(int& nCurFrame, int& nTotalFrame) {
	CConfiguration& config = CConfiguration::GetInstance();
	const int nBufferSize = config.nBufferSize;
	const int nAcqBufCount = config.settingsAlazar.nAcqBufferCount;
	const U32 timeout_ms = 5000;
	RETURN_CODE retCode;
	
	nCurFrame = 0;
	nTotalFrame = 0;

	m_end = std::chrono::system_clock::now();
	std::chrono::milliseconds total_time = std::chrono::duration_cast<std::chrono::milliseconds>(m_end - m_start);
	m_fps = 1000.f / total_time.count();
	m_start = m_end;

	m_pCurBuffer = m_pAcqBuffers[m_nBufferIndex];
	{
		// Add the buffer to the end of the list of available buffers.
		if (m_pPrevBuffer != NULL) {
			retCode = AlazarPostAsyncBuffer(m_hATSBoard, m_pPrevBuffer, nBufferSize * sizeof(U16));
		}

		// Wait for the buffer at the head of the list of available buffers
		// to be filled by the board.
		retCode = AlazarWaitAsyncBufferComplete(m_hATSBoard, m_pCurBuffer, timeout_ms);
		if (retCode != ApiSuccess)
		{
			printf("Error: AlazarWaitAsyncBufferComplete failed -- %s\n", AlazarErrorToText(retCode));
			return NULL;
		}

		m_pPrevBuffer = m_pCurBuffer;
	}
	
	m_nBufferIndex++;
	m_nBufferIndex = (m_nBufferIndex >= nAcqBufCount) ? 0 : m_nBufferIndex;

	return m_pCurBuffer;
}

BOOL CATSDevice::configureBoard(HANDLE boardHandle)
{
	RETURN_CODE retCode;
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const int nAScanPadding = config.nAScanPadding;
	const int nBScan = config.nBScan;
	const int nLaserSpeed = config.nLaserSpeed;
	const int nBufferSize = config.nBufferSize;
	const int nAcqBufCount = config.settingsAlazar.nAcqBufferCount;
	const int nTriggerDelaySample = config.settingsAlazar.nTriggerDelaySample;
	const bool useKClock = config.settingsAlazar.bUseKClock;
	const double secGoodClkDuration = config.settingsAlazar.usGoodClockDuration * 1e-6;
	const double secBadClkDuration = config.settingsAlazar.usBadClockDuration * 1e-6;
	const bool useDES = config.settingsAlazar.bUseDES;

	// TODO: Specify the sample rate (see sample rate id below)
	double dSamplePerSec = nAScan * nLaserSpeed;
	dSamplePerSec = ceil((dSamplePerSec / 1000000.f)) * 1000000.f;

	if (useDES) {
		retCode = AlazarSetParameterUL(boardHandle, CHANNEL_A, SET_ADC_MODE, ADC_MODE_DES);
		printf("Use DES Mode - %s\n", AlazarErrorToText(retCode));
	}

	printf("sample per sec : %.2f\n", dSamplePerSec);
	printf("sample per frame : %d\n", nBufferSize);
	printf("frame per sec : %.2f\n", dSamplePerSec / nBufferSize);
	// TODO: Select clock parameters as required to generate this sample rate.
	//
	// For example: if samplesPerSec is 100.e6 (100 MS/s), then:
	// - select clock source INTERNAL_CLOCK and sample rate SAMPLE_RATE_100MSPS
	// - select clock source FAST_EXTERNAL_CLOCK, sample rate SAMPLE_RATE_USER_DEF, and connect a
	//   100 MHz signal to the EXT CLK BNC connector.

	double dutyCycle = 0.5f;	// maximum 50%
	U32 srcClock = (useKClock) ? FAST_EXTERNAL_CLOCK : INTERNAL_CLOCK_10MHz_REF;
	U32 rate = (useKClock) ? SAMPLE_RATE_USER_DEF : dSamplePerSec / dutyCycle;
	retCode = AlazarSetCaptureClock(boardHandle,
		srcClock,
		rate,
		CLOCK_EDGE_RISING,
		0);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarSetCaptureClock failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}


	// TODO: Select channel A input parameters as required

	retCode = AlazarInputControlEx(boardHandle,
		CHANNEL_A,
		DC_COUPLING,
		INPUT_RANGE_PM_400_MV,
		IMPEDANCE_50_OHM);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarInputControlEx failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}


	// TODO: Select trigger inputs and levels as required

	retCode = AlazarSetTriggerOperation(boardHandle,
		TRIG_ENGINE_OP_J,
		TRIG_ENGINE_J,
		TRIG_EXTERNAL,
		TRIGGER_SLOPE_POSITIVE,
		150,
		TRIG_ENGINE_K,
		TRIG_DISABLE,
		TRIGGER_SLOPE_POSITIVE,
		128);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarSetTriggerOperation failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// TODO: Select external trigger parameters as required

	retCode = AlazarSetExternalTrigger(boardHandle,
		DC_COUPLING,
		ETR_TTL);

	// TODO: Set trigger delay as required.

	retCode = AlazarSetTriggerDelay(boardHandle, nTriggerDelaySample);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarSetTriggerDelay failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// TODO: Set trigger timeout as required.

	// NOTE:
	// The board will wait for a for this amount of time for a trigger event.  If a trigger event
	// does not arrive, then
	// the board will automatically trigger. Set the trigger timeout value to 0 to force the board
	// to wait forever for a
	// trigger event.
	//
	// IMPORTANT:
	// The trigger timeout value should be set to zero after appropriate trigger parameters have
	// been determined,
	// otherwise the board may trigger if the timeout interval expires before a hardware trigger
	// event arrives.

	double triggerTimeout_sec = 0;
	U32 triggerTimeout_clocks = (U32)(triggerTimeout_sec / 10.e-6 + 0.5);

	retCode = AlazarSetTriggerTimeOut(boardHandle, triggerTimeout_clocks);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarSetTriggerTimeOut failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// TODO: Configure AUX I/O connector as required
	
	retCode = AlazarConfigureAuxIO(boardHandle, AUX_OUT_TRIGGER, AUX_OUT_TRIGGER);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarConfigureAuxIO failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// Ignore Bad Clock when using K-Clock
	if (useKClock) {
		// (goodClock + badClock) <= triggerCycleTime(=0.000010)
		double triggerCycleTime, triggerPulseWidth;
		retCode = AlazarOCTIgnoreBadClock(m_hATSBoard, TRUE, secGoodClkDuration, secBadClkDuration, &triggerCycleTime, &triggerPulseWidth);
		printf("AlazarOCTIgnoreBadClock : %s, cycleTime : %lf, pulseWidth : %lf\n", AlazarErrorToText(retCode), triggerCycleTime, triggerPulseWidth);
	}

	return TRUE;
}

BOOL CATSDevice::configureAcquisition(HANDLE boardHandle) {
	RETURN_CODE retCode;
	CConfiguration& config = CConfiguration::GetInstance();
	const int nAScan = config.nAScan;
	const int nAScanPadding = config.nAScanPadding;
	const int nBScan = config.nBScan;
	const int nAcqBufCount = config.settingsAlazar.nAcqBufferCount;
	BOOL success = TRUE;

	//==========================================================================================================
	// Acquisition Setting
	//==========================================================================================================

	// There are no pre-trigger samples in NPT mode
	U32 preTriggerSamples = 0;

	// TODO: Select the number of post-trigger samples per record
	U32 postTriggerSamples = (nAScan + nAScanPadding - preTriggerSamples);

	// TODO: Specify the number of records per DMA buffer
	U32 recordsPerBuffer = nBScan;

	// TODO: Specify the total number of buffers to capture
	U32 buffersPerAcquisition = nAcqBufCount;

	// TODO: Select which channels to capture (A, B, or both)
	U32 channelMask = CHANNEL_A; // | CHANNEL_B;

	// TODO: Select if you wish to save the sample data to a file
	BOOL saveData = true;

	// Calculate the number of enabled channels from the channel mask
	int channelCount = 0;
	int channelsPerBoard = 2;
	for (int channel = 0; channel < channelsPerBoard; channel++)
	{
		U32 channelId = 1U << channel;
		if (channelMask & channelId)
			channelCount++;
	}

	// Get the sample size in bits, and the on-board memory size in samples per channel
	U8 bitsPerSample;
	U32 maxSamplesPerChannel;
	retCode = AlazarGetChannelInfo(boardHandle, &maxSamplesPerChannel, &bitsPerSample);
	if (retCode != ApiSuccess)
	{
		printf("Error: AlazarGetChannelInfo failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// Calculate the size of each DMA buffer in bytes
	float bytesPerSample = (float)((bitsPerSample + 7) / 8);
	U32 samplesPerRecord = preTriggerSamples + postTriggerSamples;
	U32 bytesPerRecord = (U32)(bytesPerSample * samplesPerRecord +
		0.5); // 0.5 compensates for double to integer conversion 
	U32 bytesPerBuffer = bytesPerRecord * recordsPerBuffer * channelCount;
	printf("samplesPerRecord : %d, recordsPerBuffer : %d, channelCount : %d\n", samplesPerRecord, recordsPerBuffer, channelCount);

	// Allocate memory for DMA buffers
	if (m_pAcqBuffers == nullptr) {
		m_pAcqBuffers = new U16 * [nAcqBufCount];
		for (int bufferIndex = 0; (bufferIndex < nAcqBufCount) && success; bufferIndex++)
		{
#ifdef _WIN32 // Allocate page aligned memory
			m_pAcqBuffers[bufferIndex] =
				(U16*)VirtualAlloc(NULL, bytesPerBuffer, MEM_COMMIT, PAGE_READWRITE);
#else
			BufferArray[bufferIndex] = (U16*)valloc(bytesPerBuffer);
#endif
			if (m_pAcqBuffers[bufferIndex] == NULL)
			{
				printf("Error: Alloc %u bytes failed\n", bytesPerBuffer);
				success = FALSE;
			}
		}
	}

	// Configure the record size
	if (success)
	{
		retCode = AlazarSetRecordSize(boardHandle, preTriggerSamples, postTriggerSamples);
		if (retCode != ApiSuccess)
		{
			printf("Error: AlazarSetRecordSize failed -- %s\n", AlazarErrorToText(retCode));
			success = FALSE;
		}
	}

	if (success)
	{
		U32 recordsPerAcquisition = 0x7FFFFFFF; // recordsPerBuffer * buffersPerAcquisition;

		U32 admaFlags = ADMA_EXTERNAL_STARTCAPTURE | ADMA_NPT | ADMA_FIFO_ONLY_STREAMING;

		retCode = AlazarBeforeAsyncRead(boardHandle, channelMask, (long)preTriggerSamples,
			samplesPerRecord, recordsPerBuffer, recordsPerAcquisition,
			admaFlags);

		if (retCode != ApiSuccess)
		{
			printf("Error: AlazarBeforeAsyncRead failed -- %s\n", AlazarErrorToText(retCode));
			success = FALSE;
		}
	}

	// Add the buffers to a list of buffers available to be filled by the board
	for (int bufferIndex = 0; (bufferIndex < nAcqBufCount) && success; bufferIndex++)
	{
		U16* pBuffer = m_pAcqBuffers[bufferIndex];
		retCode = AlazarPostAsyncBuffer(m_hATSBoard, pBuffer, bytesPerBuffer);
		if (retCode != ApiSuccess)
		{
			printf("Error: AlazarPostAsyncBuffer %u failed -- %s\n", bufferIndex,
				AlazarErrorToText(retCode));
			success = FALSE;
		}
	}

	m_nBufferIndex = 0;

	return success;
}