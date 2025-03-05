#if !defined INCLUDE_IdeaDrvDefs
#define INCLUDE_IdeaDrvDefs

#if defined(WINDLL) || defined(_WINDLL)
#define KS_BITMAPINFOHEADER BITMAPINFOHEADER
#else
#include <ksmedia.h>
#endif

typedef struct
{
	char          szH264Profile[16];
	char          szH264Level[16];
	BOOL          bH264VariableBitRate;
	DWORD         dwH264KBitRate;
	DWORD         dwH264MinKBitRate;
	DWORD         dwH264AverageKBitRate;
	DWORD         dwH264MaxKBitRate;
	DWORD         dwOutputFrameRate;
	char          szH264GopStructure[16];
	BOOL          bH264OpenGop;
	DWORD         dwH264CodedPictureBufferDelay;
	BOOL          bH264DisableTimebaseCorrector;
} COMPRESSION_PARAMETERS;

typedef struct mailbox
{
  LONG bSync;
  LONG bCaptureRunning;
  LONG bPreviewRunning;
  LONG bCompressRunning;
  LONG bProxyInitialized;
  LONG bProxyInitFailed;
  LONG bCaptureMessageBitmapBusy;
  LONG bPreviewMessageBitmapBusy;

  ULONGLONG pCaptureMessageBitmap;
  DWORD dwCaptureMessageBitmapSize;
  DWORD dwCaptureMessageBitmapWidth;
  DWORD dwCaptureMessageBitmapHeight;
  DWORD dwCaptureMessageBitmapPitch;
  DWORD dwCaptureMessageBitmapBpp;
  DWORD dwCaptureMessageBitmapDataType;

  ULONGLONG pPreviewMessageBitmap;
  DWORD dwPreviewMessageBitmapSize;
  DWORD dwPreviewMessageBitmapWidth;
  DWORD dwPreviewMessageBitmapHeight;
  DWORD dwPreviewMessageBitmapPitch;
  DWORD dwPreviewMessageBitmapBpp;
  DWORD dwPreviewMessageBitmapDataType;

  ULONGLONG qwCommandToDll;
  ULONGLONG qwCommandReturn;
  ULONGLONG qwCommandParam1;
  ULONGLONG qwCommandParam2;
  ULONGLONG qwCommandParam3;
  ULONGLONG qwCommandParam4;
  ULONGLONG qwCommandParam5;
  ULONGLONG qwCommandParam6;
  
  KS_BITMAPINFOHEADER CaptureBmiHeader;
  KS_BITMAPINFOHEADER PreviewBmiHeader;
  ULONGLONG pCaptureDmaHeaders;
  ULONGLONG pPreviewDmaHeaders;
  
  LONG lTriggerMode;
	COMPRESSION_PARAMETERS CompressionParms;
  
} MAILBOX, *PMAILBOX;

#ifndef _INC_HD_DEVIO

typedef struct
{
	DWORD dwOffset;
	DWORD dwSize;
	union
	{
		BYTE byValue;
		WORD wValue;
		DWORD dwValue;
	};
} PCI_CONFIG_VALUE;



#define HIDEF_DEVICE_TYPE 0x00008100
#define HIDEF_DRVFUNC0    0x0800
#ifdef UNIX
#define HIDEF_CCODE(Function, Method) (HIDEF_DRVFUNC0 + Function)
#else
#define HIDEF_CCODE(Function, Method) \
  (unsigned long)(CTL_CODE(HIDEF_DEVICE_TYPE, HIDEF_DRVFUNC0 + Function, Method, FILE_ANY_ACCESS))
#endif

#define IOCTL_HIDEF_0                     HIDEF_CCODE(0, METHOD_BUFFERED)
#define IOCTL_HIDEF_REPORT                HIDEF_CCODE(0, METHOD_BUFFERED)
#define IOCTL_HIDEF_CLAIM                 HIDEF_CCODE(7, METHOD_BUFFERED)
#define IOCTL_HIDEF_UNCLAIM               HIDEF_CCODE(8, METHOD_BUFFERED)
#define IOCTL_HIDEF_BASEPTRS              HIDEF_CCODE(13, METHOD_BUFFERED)
#define IOCTL_HIDEF_LOCK_DMA_BUFFER       HIDEF_CCODE(25, METHOD_BUFFERED)
#define IOCTL_HIDEF_UNLOCK_DMA_BUFFER     HIDEF_CCODE(26, METHOD_BUFFERED)
#define IOCTL_HIDEF_INITIALIZE            HIDEF_CCODE(28, METHOD_BUFFERED)
#define IOCTL_HIDEF_ENABLE_TRIGGER_EVENT  HIDEF_CCODE(32, METHOD_BUFFERED)
#define IOCTL_HIDEF_DISABLE_TRIGGER_EVENT HIDEF_CCODE(33, METHOD_BUFFERED)
#define IOCTL_HIDEF_GET_PCI_CONFIG        HIDEF_CCODE(37, METHOD_BUFFERED)
#define IOCTL_HIDEF_SET_PCI_CONFIG        HIDEF_CCODE(38, METHOD_BUFFERED)

#define IOCTL_HIDEF_ENABLE_DRIVER_EVENT   HIDEF_CCODE(39, METHOD_BUFFERED)
#define IOCTL_HIDEF_DISABLE_DRIVER_EVENT  HIDEF_CCODE(40, METHOD_BUFFERED)
#define IOCTL_HIDEF_ALLOCATE_MAILBOX      HIDEF_CCODE(41, METHOD_BUFFERED)
#define IOCTL_HIDEF_DEALLOCATE_MAILBOX    HIDEF_CCODE(42, METHOD_BUFFERED)
#define IOCTL_HIDEF_SET_RSET              HIDEF_CCODE(43, METHOD_BUFFERED)
#define IOCTL_HIDEF_GET_RSET              HIDEF_CCODE(44, METHOD_BUFFERED)
#define IOCTL_HIDEF_SET_PIN_FORMAT        HIDEF_CCODE(45, METHOD_BUFFERED)
#define IOCTL_HIDEF_GET_PIN_FORMAT        HIDEF_CCODE(46, METHOD_BUFFERED)
#define IOCTL_HIDEF_GET_PCI_CONFIG_VALUE  HIDEF_CCODE(47, METHOD_BUFFERED)
#define IOCTL_HIDEF_SET_PCI_CONFIG_VALUE  HIDEF_CCODE(48, METHOD_BUFFERED)
#define IOCTL_HIDEF_N                     HIDEF_CCODE(49, METHOD_BUFFERED)
#endif // !_INC_HD_DEVIO

typedef enum
{
  DRIVER_EVENT_DLL_COMMAND,
  DRIVER_EVENT_DLL_COMMAND_ACK,
  DRIVER_EVENT_RESERVED1,
  DRIVER_EVENT_RESERVED2
} DRIVER_EVENTS;

typedef enum
{
  DLLCMD_NONE,
  DLLCMD_CODEC_RESET,
  DLLCMD_CODEC_INIT,
  DLLCMD_CODEC_START,
  DLLCMD_CODEC_STOP,
  DLLCMD_SetTriggerMode,
  DLLCMD_SetTriggerPolarity,
	DLLCMD_SystemGoingToSleep,
	DLLCMD_SystemWakingUp,
	DLLCMD_LAST
} DLL_COMMANDS;

typedef enum
{
	TRIGGER_OFF							= 0,
	TRIGGER_START_STOP_LOW,
	TRIGGER_START_STOP_HIGH,
	TRIGGER_RUN_LOW,
	TRIGGER_RUN_HIGH
} TRIGGER_MODE;

#if !defined(_INC_HDP_LIB_U)

typedef struct
{
  DWORD     dwEventIndex;
  DWORD     bEventEnable;
  ULONGLONG qwEventHandle;
} DRIVER_EVENT;

#endif

#endif // INCLUDE_IdeaDrvDefs
