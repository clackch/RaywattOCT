#include "FGServer.h"

enum FGError {
	Normal,
	BoardNotExisted, // 사용가능한 보드가 없는 경우
	FrameNotLoaded,
	PortError,
};

enum PacketType {
	Image,
	Command,
	Nothing
};

enum CommandType {
	FGUnknown,
	FGStarted,
	FGStopped,
	FGAskPort,
	FGAskBoard,
	FGAskDeviceInfo,
	FGAngioConnected, // Port
	FGAngioDisconnected, // Port
	FGBoardExist,
	FGBoardNotExist,
	FGDeviceInfo,
	FGChpFile,
	FGSuccessChangeChp,
	FGFailChangeChp,
	FGNothing,
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
	// Construction
public:
	FrameGrabber();
	~FrameGrabber();

	ERRTYPE ReadFormatFile(char* m_CHPFilePath);

	short origin_Height;
	short origin_Width;

	short		lHeight;
	short		lWidth;
	char	wBitsPerPixel = 24;
	short		HeightPorch = 0;
	short		WidthPorch = 0;
	int currentIMG = 0;

	ImageHandle m_ImageHandle;
	bool portConnection;
	unsigned char* pRecvBuf;
	unsigned char* originIMG;
	bool boardConnection;
	std::string chpFilePath = "C:\\Raywatt\\FrameGrabber\\chp\\";
	std::string chpFileName;


	private:
		BOOL m_bInitialized;
		HD_sReport m_BoardReport;
		DWORD m_dwCaptureFormatSelect;
		RSET m_RSet;
		BoardHandle m_BoardHandle;
};