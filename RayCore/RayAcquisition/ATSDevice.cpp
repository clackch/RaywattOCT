#include "Config.h"
#include "ATSDevice.h"

CATSDevice::CATSDevice(Setting setting)
	: m_setting(setting)
{
	m_hATSBoard = NULL;
	m_nBufferIndex = 0;
	m_pAcqBuffers = NULL;
	m_pCurBuffer = NULL;
	m_pPrevBuffer = NULL;
}
CATSDevice::~CATSDevice() {
	CleanUp();
}

int CATSDevice::InitDevice() {
	m_isInit = false;

	U32 systemId = 1;
	U32 boardId = 1;

	U8 major, minor, revision;
	AlazarGetSDKVersion(&major, &minor, &revision);
	PLOGI.printf("[Alazar] SDK Ver.%d.%d.%d\n", major, minor, revision);

	m_hATSBoard = AlazarGetBoardBySystemID(systemId, boardId);
	if (m_hATSBoard == NULL)
	{
		PLOGI.printf("Error: Unable to open board system Id %u board Id %u\n", systemId, boardId);
		return E_FAIL;
	}

	BoardTypes type = (BoardTypes)AlazarGetBoardKind(m_hATSBoard);

	if (type == 33 /*ATS9371*/) {
		PLOGI.printf("AlazarGetBoardKind: %d (ATS9371)", type);
		m_admaFlags = ADMA_EXTERNAL_STARTCAPTURE | ADMA_NPT | ADMA_FIFO_ONLY_STREAMING;
	}
	else {	/*ATS9364*/
		PLOGI.printf("AlazarGetBoardKind: %d (ATS9364)", type);
		m_admaFlags = ADMA_EXTERNAL_STARTCAPTURE | ADMA_NPT;
	}

	calibrateBoard(m_hATSBoard);
	if (false)
	{
		Setting setting = m_setting;
		m_setting.bUseKClock = false;
		m_setting.bUseDES = false;

		configureBoard(m_hATSBoard);
		configureAcquisition(m_hATSBoard);
		AlazarStartCapture(m_hATSBoard);
		int nCurFrame = 0, nTotalFrame = 0;
		acquire(nCurFrame, nTotalFrame);
		PLOGI.printf("acquire - curFrame: %d, totalFrame: %d", nCurFrame, nTotalFrame);
		stop();

		m_setting = setting;
	}

	int retry = 0;
	BOOL result = FALSE;
	do {
		result = configureBoard(m_hATSBoard);
		if (result) break;
		retry++;
	} while (retry < 10);

	m_isInit = result;

	return (result) ? NOERROR : E_FAIL;
}
int CATSDevice::CleanUp() {
	// Free all memory allocated
	if (m_pAcqBuffers != NULL) {
		for (int bufferIndex = 0; bufferIndex < m_setting.nBufferCount; bufferIndex++)
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

	return NOERROR;
}

int CATSDevice::start() {
	configureAcquisition(m_hATSBoard);

	// Arm the board system to wait for a trigger event to begin the acquisition
	RETURN_CODE retCode = AlazarStartCapture(m_hATSBoard);

	if (retCode != ApiSuccess)
	{
		PLOGI.printf("Error: AlazarStartCapture failed -- %s\n", AlazarErrorToText(retCode));
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
		PLOGI.printf("Error: AlazarAbortAsyncRead failed -- %s\n", AlazarErrorToText(retCode));
		
		return retCode;
	}

	return NOERROR;
}

char *CATSDevice::acquire(int& nCurFrame, int& nTotalFrame) {
	const int nBufferSize = m_setting.nAScan * m_setting.nBScan;
	const int nAcqBufCount = m_setting.nBufferCount;
	const U32 timeout_ms = m_setting.msTimeOut;
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
			PLOGI.printf("Error: AlazarWaitAsyncBufferComplete failed -- %s\n", AlazarErrorToText(retCode));
			return NULL;
		}

		m_pPrevBuffer = m_pCurBuffer;
	}
	
	m_nBufferIndex++;
	m_nBufferIndex = (m_nBufferIndex >= nAcqBufCount) ? 0 : m_nBufferIndex;

	return (char *)m_pCurBuffer;
}

BOOL CATSDevice::calibrateBoard(HANDLE boardHandle)
{
	RETURN_CODE retCode = ApiSuccess;

	const U32 preTriggerSamples = 4096;
	const U32 postTriggerSamples = 4096;
	const U32 samplesPerRecord = preTriggerSamples + postTriggerSamples;

	// 리소스들
	U16* pAcqBuffer = nullptr;
	OVERLAPPED  ovl = {};
	HANDLE      hEvent = nullptr;
	bool        asyncPrimed = false;  // AlazarBeforeAsyncRead 호출 완료 여부
	bool        asyncReading = false;  // AlazarAsyncRead 호출 완료 여부

	auto cleanup = [&]() {
		// 비동기 읽기 중이면 중단
		if (asyncPrimed || asyncReading) {
			AlazarAbortAsyncRead(boardHandle);
		}
		// 이벤트 핸들 정리
		if (hEvent) {
			CloseHandle(hEvent);
			hEvent = nullptr;
		}
		// 버퍼 정리
		if (pAcqBuffer) {
			VirtualFree(pAcqBuffer, 0, MEM_RELEASE);
			pAcqBuffer = nullptr;
		}
		};

	// 1) 버퍼 할당
	pAcqBuffer = (U16*)VirtualAlloc(nullptr, samplesPerRecord * sizeof(U16),
		MEM_COMMIT, PAGE_READWRITE);
	if (!pAcqBuffer) {
		PLOGI.printf("Error: VirtualAlloc failed\n");
		cleanup();
		return FALSE;
	}

	// 2) 레코드 크기
	retCode = AlazarSetRecordSize(boardHandle, preTriggerSamples, postTriggerSamples);
	PLOGI.printf("AlazarSetRecordSize -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 3) ADC 모드 기본
	retCode = AlazarSetParameterUL(boardHandle, CHANNEL_A, SET_ADC_MODE, ADC_MODE_DEFAULT);
	PLOGI.printf("SET_ADC_MODE DEFAULT -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 4) 캡처 클록: 내부 200MSPS (테스트용)
	retCode = AlazarSetCaptureClock(boardHandle,
		INTERNAL_CLOCK,
		SAMPLE_RATE_1000MSPS,
		CLOCK_EDGE_RISING,
		0);
	PLOGI.printf("SetCaptureClock INTERNAL 1000MSPS -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 5) 채널 A 입력
	retCode = AlazarInputControlEx(boardHandle,
		CHANNEL_A,
		DC_COUPLING,
		INPUT_RANGE_PM_400_MV,
		IMPEDANCE_50_OHM);
	PLOGI.printf("AlazarInputControlEx -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 6) 트리거: 외부 TTL, 양의 엣지
	retCode = AlazarSetTriggerOperation(boardHandle,
		TRIG_ENGINE_OP_J,
		TRIG_ENGINE_J, TRIG_EXTERNAL, TRIGGER_SLOPE_POSITIVE, 150,
		TRIG_ENGINE_K, TRIG_DISABLE, TRIGGER_SLOPE_POSITIVE, 128);
	PLOGI.printf("AlazarSetTriggerOperation -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	retCode = AlazarSetExternalTrigger(boardHandle, DC_COUPLING, ETR_TTL);
	PLOGI.printf("AlazarSetExternalTrigger -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 7) 트리거 지연/타임아웃
	retCode = AlazarSetTriggerDelay(boardHandle, 0);
	PLOGI.printf("AlazarSetTriggerDelay -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	retCode = AlazarSetTriggerTimeOut(boardHandle, 0); // 무기한 대기
	PLOGI.printf("AlazarSetTriggerTimeOut -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 8) AUX I/O (필요 시)
	retCode = AlazarConfigureAuxIO(boardHandle, AUX_OUT_TRIGGER, AUX_OUT_TRIGGER);
	PLOGI.printf("AlazarConfigureAuxIO -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 9) 비동기 읽기 준비 (pre-trigger 포함)
	retCode = AlazarBeforeAsyncRead(boardHandle,
		CHANNEL_A,
		(long)(-1 * (int)preTriggerSamples),  // pre-trigger 샘플 포함
		samplesPerRecord,
		1, 1,                                 // recordsPerBuffer, buffersPerAcq
		m_admaFlags);
	PLOGI.printf("AlazarBeforeAsyncRead -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }
	asyncPrimed = true;

	// 10) OVERLAPPED 준비
	hEvent = CreateEvent(nullptr, TRUE, FALSE, nullptr);
	if (!hEvent) {
		PLOGI.printf("Error: CreateEvent failed (GetLastError=%lu)\n", GetLastError());
		cleanup();
		return FALSE;
	}
	ZeroMemory(&ovl, sizeof(ovl));
	ovl.hEvent = hEvent;

	// 11) 비동기 읽기 요청
	retCode = AlazarAsyncRead(boardHandle, pAcqBuffer, samplesPerRecord, &ovl);
	PLOGI.printf("AlazarAsyncRead -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }
	asyncReading = true;

	// 12) 캡처 시작
	retCode = AlazarStartCapture(boardHandle);
	PLOGI.printf("AlazarStartCapture -- %s\n", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) { cleanup(); return FALSE; }

	// 13) 트리거 대기 (타임아웃 0이므로 HW 트리거를 실제로 넣어야 함)
	AlazarTriggered(boardHandle);

	// (여기서 필요하면 WaitForSingleObject(ovl.hEvent, ...) 를 사용해도 되고,
	// 캡처 파이프라인 정책에 맞춰 별도 완료 확인 로직을 넣어도 됩니다.)

	// 14) 정리
	cleanup();
	return TRUE;
}


BOOL CATSDevice::configureBoard(HANDLE boardHandle)
{
	RETURN_CODE retCode;
	const int   nAScan = m_setting.nAScan;        // ex) 1152
	const int   nLaserSpeed = m_setting.nLaserSpeed;   // A-line rate (Hz)
	const int   nTriggerDelaySample = m_setting.nTriggerDelaySample;
	const bool  useKClock = false;                   // k-clock 강제 OFF
	const bool  useDES = m_setting.bUseDES;

	// 목표 샘플링 레이트(시간균등 fringe)
	double samplesPerSecTarget = (double)nAScan * (double)nLaserSpeed;
	PLOGI.printf("target samples/sec : %.3f\n", samplesPerSecTarget);

	// ADC 모드
	retCode = AlazarSetParameterUL(boardHandle, CHANNEL_A, SET_ADC_MODE,
		useDES ? ADC_MODE_DES : ADC_MODE_DEFAULT);
	PLOGI.printf("ADC Mode (%s) -- %s",
		useDES ? "DES" : "NORMAL", AlazarErrorToText(retCode));
	if (retCode != ApiSuccess) return FALSE;

	// 샘플클록: 내부 + 프리셋 중 근접값 선택
	U32 srcClock = INTERNAL_CLOCK;
	U32 rateId = SAMPLE_RATE_1000MSPS;

	retCode = AlazarSetCaptureClock(boardHandle, srcClock, rateId,
		CLOCK_EDGE_RISING, 0);
	if (retCode != ApiSuccess) {
		PLOGI.printf("Error: AlazarSetCaptureClock failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// 입력 설정 (게인/임피던스/결합)
	retCode = AlazarInputControlEx(boardHandle, CHANNEL_A, DC_COUPLING,
		INPUT_RANGE_PM_400_MV, IMPEDANCE_50_OHM);
	if (retCode != ApiSuccess) {
		PLOGI.printf("Error: AlazarInputControlEx failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// 외부 트리거 (SOS), 양의 엣지
	retCode = AlazarSetTriggerOperation(boardHandle,
		TRIG_ENGINE_OP_J,
		TRIG_ENGINE_J, TRIG_EXTERNAL, TRIGGER_SLOPE_POSITIVE, 150,
		TRIG_ENGINE_K, TRIG_DISABLE, TRIGGER_SLOPE_POSITIVE, 128);
	if (retCode != ApiSuccess) {
		PLOGI.printf("Error: AlazarSetTriggerOperation failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	retCode = AlazarSetExternalTrigger(boardHandle, DC_COUPLING, ETR_TTL);
	if (retCode != ApiSuccess) {
		PLOGI.printf("Error: AlazarSetExternalTrigger failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// 라인 시작 보정
	retCode = AlazarSetTriggerDelay(boardHandle, nTriggerDelaySample);
	if (retCode != ApiSuccess) {
		PLOGI.printf("Error: AlazarSetTriggerDelay failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// 트리거 타임아웃(0=무기한 대기)
	retCode = AlazarSetTriggerTimeOut(boardHandle, 0);
	if (retCode != ApiSuccess) {
		PLOGI.printf("Error: AlazarSetTriggerTimeOut failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	return TRUE;
}


BOOL CATSDevice::configureAcquisition(HANDLE boardHandle) {
	RETURN_CODE retCode;
	const int nAScan = m_setting.nAScan;
	const int nBScan = m_setting.nBScan;
	const int nAcqBufCount = m_setting.nBufferCount;
	BOOL success = TRUE;
	
	CleanUp();

	//==========================================================================================================
	// Acquisition Setting
	//==========================================================================================================

	// There are no pre-trigger samples in NPT mode
	U32 preTriggerSamples = 0;

	// TODO: Select the number of post-trigger samples per record
	U32 postTriggerSamples = nAScan;

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
		PLOGI.printf("Error: AlazarGetChannelInfo failed -- %s\n", AlazarErrorToText(retCode));
		return FALSE;
	}

	// Calculate the size of each DMA buffer in bytes
	float bytesPerSample = (float)((bitsPerSample + 7) / 8);
	U32 samplesPerRecord = preTriggerSamples + postTriggerSamples;
	U32 bytesPerRecord = (U32)(bytesPerSample * samplesPerRecord +
		0.5); // 0.5 compensates for double to integer conversion 
	U32 bytesPerBuffer = bytesPerRecord * recordsPerBuffer * channelCount;
	PLOGI.printf("samplesPerRecord : %d, recordsPerBuffer : %d, channelCount : %d\n", samplesPerRecord, recordsPerBuffer, channelCount);

	// Allocate memory for DMA buffers
	if (m_pAcqBuffers == nullptr) {
		size_t count = static_cast<size_t>(nAcqBufCount);
		if (count > (1024ULL * 1024 * 1024 * 2) / sizeof(U16)) {
			return FALSE;
		}

		m_pAcqBuffers = new U16 * [count];
		for (int bufferIndex = 0; (bufferIndex < count) && success; bufferIndex++)
		{
#ifdef _WIN32 // Allocate page aligned memory
			m_pAcqBuffers[bufferIndex] =
				(U16*)VirtualAlloc(NULL, bytesPerBuffer, MEM_COMMIT, PAGE_READWRITE);
#else
			BufferArray[bufferIndex] = (U16*)valloc(bytesPerBuffer);
#endif
			if (m_pAcqBuffers[bufferIndex] == NULL)
			{
				PLOGI.printf("Error: Alloc %u bytes failed\n", bytesPerBuffer);
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
			PLOGI.printf("Error: AlazarSetRecordSize failed -- %s\n", AlazarErrorToText(retCode));
			success = FALSE;
		}
	}

	if (success)
	{
		U32 recordsPerAcquisition = 0x7FFFFFFF; // recordsPerBuffer * buffersPerAcquisition;

		retCode = AlazarBeforeAsyncRead(boardHandle, channelMask, (long)preTriggerSamples,
			samplesPerRecord, recordsPerBuffer, recordsPerAcquisition,
			m_admaFlags);

		if (retCode != ApiSuccess)
		{
			PLOGI.printf("Error: AlazarBeforeAsyncRead failed -- %s\n", AlazarErrorToText(retCode));
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
			PLOGI.printf("Error: AlazarPostAsyncBuffer %u failed -- %s\n", bufferIndex,
				AlazarErrorToText(retCode));
			success = FALSE;
		}
	}

	m_nBufferIndex = 0;
	m_pPrevBuffer = NULL;

	return success;
}