#ifndef FRAMEGRABBER_H
#define FRAMEGRABBER_H

#define STREAM_FRAMES 30

#include "FGServer.h"

enum FGError {
	Normal,
	BoardNotExisted, // 사용가능한 보드가 없는 경우
	FrameNotLoaded,
	PortError,
};

enum m_MonoFormatTypes
{
	FormatGray_8,
	FormatGrayYuy2_16
};

enum m_ColorFormatTypes
{
	FormatYOnly_8,
	FormatYUY2_16,
	FormatRGB555_16,
	FormatRGB888_24,
	FormatRGB888_32,
	FormatRGB888_Gray_On_Red,
	FormatRGB888_Gray_On_Green,
	FormatRGB888_Gray_On_Blue,
	FormatRGB_256
};

class FrameGrabber
{
public:
	FrameGrabber();
	~FrameGrabber();

	ERRTYPE ReadFormatFile(char* m_CHPFilePath);
	FGError InitBoard();
	void CreateFromFG();
	void CheckPortConnection();
	void InitializeLiveStreamInfo();
	void DecodeError(char* szErrMsg, ERRTYPE e);

	IDEA_INFO* pIdeaInfo;
	BOOL m_bSyncValid;
	SNAP_CONTROL sc;
	ImageHandle m_ImageHandle;
	LIVESTREAM_INFO m_LiveStreamInfo;

	short lWidth;
	short lHeight;
	char wBitsPerPixel = 0;

	int portConnection; // init : -1, Connected : 1, Disconnected : 0
	int oldPortConnection;
	bool boardConnection = 0;

	string chpFilePath = "C:\\Raywatt\\FrameGrabber\\chp\\";
	string chpFileName;

private:
	BOOL m_bInitialized;
	HD_sReport m_BoardReport;
	DWORD m_dwCaptureFormatSelect;
	RSET m_RSet;
	BoardHandle m_BoardHandle;
	UpdateVideoSettingLong UV;
	HD_LUTHandle m_hVPLUT;
};

#endif