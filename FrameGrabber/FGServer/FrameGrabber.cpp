#include "FrameGrabber.h"

FrameGrabber::FrameGrabber() {
	lHeight = 0;
	lWidth = 0;
	m_BoardHandle = 0;
	m_ImageHandle = 0;

	portConnection = -1;
	oldPortConnection = -1;

	m_bInitialized = FALSE;
	m_bSyncValid = FALSE;

	m_dwCaptureFormatSelect = FormatRGB888_24;

	pIdeaInfo = NULL;
	sc.pRecvBuf = NULL;

	m_hVPLUT = HDLUT_BYPASS;
}

FrameGrabber::~FrameGrabber()
{
}

FGError FrameGrabber::InitBoard() {
	ERRTYPE e;
	int nBoardCount = 0;

	if (!m_bInitialized)
	{
		memset(&m_BoardReport, 0, sizeof(m_BoardReport));
		nHP_Report(0, sizeof(m_BoardReport), &m_BoardReport);
		nBoardCount = m_BoardReport.nBrdCount;

		if (nBoardCount == 0)
		{
			return FGError::BoardNotExisted;
		}
	}

	m_BoardHandle = bhHP_Claim(m_BoardReport.bra[0].wID, 0);

	m_bInitialized = TRUE;

	return FGError::Normal;
}

void FrameGrabber::CheckPortConnection() {
	pIdeaInfo = 0;
	if (eHP_GetInfoStruct(m_BoardHandle, &pIdeaInfo) == 0)
	{
		UpdateVideoSettingLong	uvsl;
		uvsl.lValue = 0;
		uvsl.pRSet = 0;
		char* my_char = new char[256];
		strcpy(my_char, "ContinuousGrabEnable");
		eHP_SetControlValue(m_BoardHandle, my_char, sizeof(uvsl), (void*)&uvsl);

		pIdeaInfo->hInfoEvent = CreateEvent(0, TRUE, FALSE, NULL);
		m_bSyncValid = bHP_CSyncDetect(m_BoardHandle);
		if (m_bSyncValid) {
			portConnection = 1;
			PLOGI.printf("Check: Port Connected");
		}
		else {
			portConnection = 0;
			PLOGI.printf("Check: Port Disconnected");
		}
	}
}

ERRTYPE FrameGrabber::ReadFormatFile(char* m_CHPFilePath) {
	ERRTYPE e = 0;
	DWORD dwBoardCaps;
	// read .chp 
	e = eHP_RSET_FRead(m_BoardHandle, m_CHPFilePath, &m_RSet, FALSE);

	if (e) {
		char errMsg[2048];
		DecodeError(errMsg, e);
		PLOGI.printf("m_BoardHandle init Fail %s", m_CHPFilePath);
		PLOGI.printf(errMsg);
		return e;
	}

	if (m_ImageHandle)
	{
		eHD_DeallocateAll(m_BoardHandle);
	}

	m_ImageHandle = ihHD_Allocate(m_BoardHandle, HDAL_DEFRAG, &m_RSet);
	strcpy(m_RSet.szCHPFile, m_CHPFilePath);
	
	e = eHD_RSET_Set(m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON);
	if (e) {
		char errMsg[2048];
		DecodeError(errMsg, e);
		PLOGI.printf("Unable to load hardware profile : %s", m_CHPFilePath);
		PLOGI.printf(errMsg);
		return e;
	}

	eHD_SetIHDMALUT(m_ImageHandle, m_hVPLUT);

	eHP_GetControlValue(m_BoardHandle, (char*)"BoardCaps", sizeof(dwBoardCaps), (void*)&dwBoardCaps);

	if ((dwBoardCaps & FSCAPS_RGB_WITH_MONO) != 0)
	{
		UV.lValue = -1;
		UV.pRSet = NULL;
		eHP_GetControlValue(m_BoardHandle, (char*)"MonoCapture", sizeof(UV), (void*)&UV);

		if (UV.lValue) // in mono mode
		{
			// Only 2 formats possible - FormatGray_8 or FormatYUY2_16
			if (m_dwCaptureFormatSelect != FormatYUY2_16)
				m_dwCaptureFormatSelect = FormatGray_8;
		}
		else
		{
			// Cannot convert RGB to FormatGray_8
			if (m_dwCaptureFormatSelect < FormatRGB555_16)
				m_dwCaptureFormatSelect = FormatRGB888_24;
		}
	}

	return e;
}

void FrameGrabber::CreateFromFG() {

	DWORD		dwBoardCaps;

	lHeight = m_RSet.lRegs[HPR_HEIGHT];
	lWidth = m_RSet.lRegs[HPR_WIDTH];

	eHP_GetControlValue(m_BoardHandle, (char*)"BoardCaps", sizeof(dwBoardCaps), (void*)&dwBoardCaps);

	if ((dwBoardCaps & FSCAPS_RGB_WITH_MONO) != 0)
	{
		UV.lValue = -1;
		UV.pRSet = NULL;
		eHP_GetControlValue(m_BoardHandle, (char*)"MonoCapture", sizeof(UV), (void*)&UV);
		if (UV.lValue) // in mono mode
		{
			wBitsPerPixel = 8;
		}
		else
		{
			wBitsPerPixel = 24;
		}
	}

	if ((dwBoardCaps & FSCAPS_PIXEL_YUV) == 0)
	{
		wBitsPerPixel = 24;
	}
	WORD wMode;
	ERRTYPE e;

	memset(&sc, 0, sizeof(SNAP_CONTROL));
	sc.stSize = sizeof(SNAP_CONTROL);
	switch (wBitsPerPixel)
	{
	case 16:
		// YCbCr needs to be converted to RGB to save it to BMP
		wMode = HDXFR_PIXEL_10 | HDXFR_YMODE_DIB;
		break;

	case 24:
		wMode = HDXFR_PIXEL_RGB888_24 | HDXFR_YMODE_DIB;
		break;

	case 8:
		wMode = HDXFR_PIXEL_8 | HDXFR_YMODE_DIB;
		break;

	default:
		wMode = HDXFR_PIXEL_RGB888_24 | HDXFR_YMODE_DIB;
		break;
	}

	sc.wFormat = wMode;
	sc.hLUT = 0;
	sc.bTrigger = FALSE;
}

void FrameGrabber::InitializeLiveStreamInfo() {
	int channel = 0;

	memset(&m_LiveStreamInfo, 0, sizeof(LIVESTREAM_INFO));
	m_LiveStreamInfo.dwSize = sizeof(LIVESTREAM_INFO);
	m_LiveStreamInfo.nDestinationWidth = m_RSet.lRegs[HPR_WIDTH];
	m_LiveStreamInfo.nDestinationHeight = m_RSet.lRegs[HPR_HEIGHT];
	m_LiveStreamInfo.dwNumberOfBuffers = STREAM_FRAMES;

	if (wBitsPerPixel == 24) {
		m_LiveStreamInfo.nDataType = IDEA_TYPE_RGB_24;
		channel = 3;
	}
	else {
		m_LiveStreamInfo.nDataType = IDEA_TYPE_MONO_8;
		channel = 1;
	}

	m_LiveStreamInfo.bDIBTarget = TRUE; // 상하 반전
	m_LiveStreamInfo.pBufferList = new void* [m_LiveStreamInfo.dwNumberOfBuffers];
	for (DWORD i = 0; i < m_LiveStreamInfo.dwNumberOfBuffers; ++i)
	{
		m_LiveStreamInfo.pBufferList[i] = new char[m_LiveStreamInfo.nDestinationWidth * m_LiveStreamInfo.nDestinationHeight * channel];
	}
	m_LiveStreamInfo.nDecimateFrames = 0;
	m_LiveStreamInfo.bFieldUpdate = FALSE;
	m_LiveStreamInfo.hLUT = m_hVPLUT;
	m_LiveStreamInfo.nTop = 0;
	m_LiveStreamInfo.nBottom = m_RSet.lRegs[HPR_HEIGHT];
	m_LiveStreamInfo.nLeft = 0;
	m_LiveStreamInfo.nRight = m_RSet.lRegs[HPR_WIDTH];
	m_LiveStreamInfo.hStartEvent = CreateEvent(0, TRUE, FALSE, NULL);
	m_LiveStreamInfo.hStopEvent = CreateEvent(0, TRUE, FALSE, NULL);
	m_LiveStreamInfo.hBufferEvent = CreateEvent(0, TRUE, FALSE, NULL);
	m_LiveStreamInfo.hErrorEvent = CreateEvent(0, TRUE, FALSE, NULL);
}


void FrameGrabber::DecodeError(char* szErrMsg, ERRTYPE e)
{
	char		szErrText[1024];
	int			nErrSize;

	if (e == 0) return;

	//
	// Call nHP_ErrMessage() to read from hdperror.dat
	//
	nErrSize = nHP_ErrMessage(e, 1023, szErrText);
	if (nErrSize > 0)
	{
		sprintf(szErrMsg, "%s %s\n", szErrMsg, szErrText);
	}

	sprintf(szErrMsg, "%s [Error Code = %d]", szErrMsg, e);
}
