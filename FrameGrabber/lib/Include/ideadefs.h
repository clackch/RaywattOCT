#ifndef INCLUDED_IDEADDEFS_H
#define INCLUDED_IDEADDEFS_H

// The ANSI-C int32_t data type is used rather than long because in 64 bit Linux
// long is 64 bits instead of 32 bits as in Windows.
#if defined _WIN32 && _MSC_VER < 1600
// required because MSVC < VS2010 aren't ANSI-C compliant
typedef __int32 int32_t; 
typedef unsigned long uint32_t; 
typedef __int64 int64_t; 
typedef unsigned __int64 uint64_t; 
#else 
#pragma warning (push)
#pragma warning (disable : 4005)
//#include <intsafe.h>
#include <stdint.h>
#pragma warning (pop)
#endif 

#if defined(LINUX)
typedef int32_t HRESULT;
#endif 


typedef enum IDEA_CONTROL_ID
{
  // -- Control Name -------------- Data type

  // Control Management         
  IDEA_ControlIDZero      = 0,  // Used to zero IDEA_CONTROL_ID entries
  IDEA_ControlIDIncrement = 1,  // Used to increment IDEA_CONTROL_ID entries

  IDEA_FirstControlInfo = 0,    // IDEA_CONTROL_INFO
  IDEA_NextControlInfo,         // IDEA_CONTROL_INFO
  IDEA_ControlInfo,             // IDEA_CONTROL_INFO
  IDEA_FirstSymbolInfo,         // IDEA_SYMBOL_INFO
  IDEA_NextSymbolInfo,          // IDEA_SYMBOL_INFO
  IDEA_SymbolInfo,              // IDEA_SYMBOL_INFO
  IDEA_ControlHelp,             // IDEA_CONTROL_HELP
  
  // Information
  IDEA_BoardReport,             // IDEA_BOARD_REPORT
  IDEA_BoardCapabilities,       // IDEA_BOARD_CAPS
  
  // Save/Restore all controls
  IDEA_AllControls,             // char *
  
  IDEA_FirstUserControl = 100,

  // Video Input Controls
  IDEA_VideoInput,              // int32_t
  IDEA_VideoInputStatus,        // int32_t
	IDEA_PriorityVideoInput,      // int32_t
  IDEA_VideoChannelNumber,      // int32_t
	IDEA_VideoType,               // int32_t

  // Video Format Controls
  IDEA_FirstFormatControl = 200,
  IDEA_Gain,                    // int32_t
  IDEA_BlackLevel,              // int32_t
  IDEA_Contrast,                // int32_t
  IDEA_Brightness,              // int32_t
  IDEA_Hue,                     // int32_t
  IDEA_Saturation,              // int32_t

  IDEA_HorizontalFrequency,     // int32_t
  IDEA_HorizontalTotal,         // int32_t
  IDEA_Phase,                   // int32_t
  IDEA_FinePhase,               // int32_t
  IDEA_HorizontalPosition,      // int32_t
  IDEA_ImageWidth,              // int32_t
  IDEA_PixelFrequency,          // int32_t

  IDEA_VerticalFrequency,       // int32_t
  IDEA_VerticalTotal,           // int32_t
  IDEA_VerticalPosition,        // int32_t
  IDEA_ImageHeight,             // int32_t
  IDEA_VerticalSyncType,        // int32_t
  IDEA_Interlace,               // int32_t
  IDEA_FramePeriod_ms,          // int32_t
	IDEA_FramePeriod,             // int32_t

  // Automatic controls - override capable
  IDEA_ClampPlacement,          // int32_t
  IDEA_ClampDuration,           // int32_t
  IDEA_ClampMode,               // int32_t
  IDEA_PixelType,               // int32_t

	IDEA_Gamma,                   // int32_t
	
	IDEA_LastFormatControl = 299,
  
  IDEA_VideoFormatFile,         // char* szFilePath[] - Format file for current input
	IDEA_VideoFormatFiles,        // IDEA_INDEXED_STRING
  IDEA_VideoFormat,             // IDEA_VIDEO_FORMAT
  IDEA_Calibration,             // IDEA_CALIBRATION

  IDEA_AutoDetect = 320,        // int32_t
  IDEA_AutoSwitch,              // int32_t

  // Format file info
  IDEA_ReferenceProgram = 340,  // int32_t
  IDEA_ReferenceBoard,          // int32_t
  IDEA_ReferenceBoardType,      // int32_t

  // Trigger Controls
  IDEA_TriggerMode = 400,       // int32_t
  IDEA_TriggerPolarity,         // int32_t
  IDEA_RetriggerDelayTime,      // int32_t
  IDEA_TriggerFilterTime,       // int32_t
  IDEA_TriggerState,            // int32_t

  // Events
  IDEA_EventSetup = 430,        // IDEA_EVENT_SETUP
  IDEA_EventInfo,               // IDEA_EVENT_INFO
  
  // IDEA Setup
  IDEA_BaseFolder = 500,        // char szFilePath[]
  IDEA_FormatFolder,            // char szFilePath[]
  IDEA_PicturesFolder,          // char szFilePath[]
  IDEA_VideosFolder,            // char szFilePath[]
  IDEA_ShowErrors,              // int32_t
  IDEA_LogErrors,               // IDEA_ERROR_LOGGING_SETUP

  // DVI and HDMI setup
  IDEA_DVI1_EDIDFile = 600,     // char szFilePath[]
  IDEA_DVI1_Equalizer,          // int32_t
  IDEA_HDMI1_EDIDFile,          // char szFilePath[]
  IDEA_HDMI1_Equalizer,         // int32_t

  // Compression controls
  IDEA_H264Profile = 700,       // char szText[]
  IDEA_H264Level,               // char szText[]
  IDEA_H264VariableBitRate,     // int32_t
  IDEA_H264KBitRate,            // int32_t
  IDEA_H264AverageKBitRate,     // int32_t
  IDEA_H264MaxKBitRate,         // int32_t
  IDEA_H264MinKBitRate,         // int32_t
  IDEA_H264OutputFrameRate,     // int32_t

  // WDM Controls
  IDEA_CaptureDataType = 800,   // int32_t
  IDEA_CaptureDimensions,       // int32_t
  IDEA_PreviewSameAsCapture,    // int32_t
  IDEA_PreviewDataType,         // int32_t
  IDEA_PreviewDimensions,       // int32_t
	IDEA_CaptureFrameRateSelect,  // int32_t
	IDEA_PreviewFrameRateSelect,  // int32_t
	IDEA_CaptureFramePeriod,			// int32_t
	IDEA_PreviewFramePeriod,			// int32_t
  
  // Streamer Controls
  IDEA_StreamerName = 900,
  IDEA_BoardName,


  // Execute Commands
  IDEA_HardReset = 1000,
  IDEA_SoftReset,
  IDEA_ForceTrigger,
  IDEA_ForceTriggerAll,
  IDEA_SaveVideoFormat,         // char szFilePath[]

  IDEA_Snap,
  IDEA_Stream,
  IDEA_StreamMode,
  IDEA_GetStreamBuffer,
  IDEA_ReleaseStreamBuffer,

  IDEA_UpdateFormat,

  IDEA_LastUserControl,

  IDEA_FirstPrivateControl = 8192,

  IDEA_SignalInfo,
  IDEA_EDIDData,
  IDEA_RetriggerDelay,

  IDEA_ProxyIdeaInterface,
  
  IDEA_RTSPStreaming,
  IDEA_MulticastActive,
  IDEA_UnicastActive,

  IDEA_LastPrivateControl,

  IDEA_DebugDialog,
  
  IDEA_ControlTableEnd = -1,
} IDEA_CONTROL_ID;


typedef enum IDEA_SYMBOL_ID
{
  IDEA_SymbolIndexUnknown = -1, // Indicates search for index.
  IDEA_SymbolIDZero       = 0,  // Used to zero IDEA_SYMBOL_ID entries
  IDEA_SymbolIDIncrement  = 1,  // Used to increment IDEA_SYMBOL_ID entries

  // ------  Symbols ------
  // General
  IDEA_Off = 0,
  IDEA_On = 1,
  
  // Video Input Names
	IDEA_VideoInput_DVIA = 50000,
	IDEA_VideoInput_DVID,
	IDEA_VideoInput_HDMI,
	IDEA_VideoInput_SDI,
	IDEA_VideoInput_SVideo,
	IDEA_VideoInput_Composite,
	IDEA_VideoInput_First = IDEA_VideoInput_DVIA,
	IDEA_VideoInput_Last = IDEA_VideoInput_Composite,

	IDEA_VideoType_Digital = IDEA_VideoInput_First + 100,
	IDEA_VideoType_RGB,
  IDEA_VideoType_RGBS,
  IDEA_VideoType_RGBHV,
  IDEA_VideoType_YUV,
	IDEA_VideoType_Mono,
	IDEA_VideoType_MonoOnRed,
  IDEA_VideoType_MonoOnGreen,
  IDEA_VideoType_MonoOnBlue,
	IDEA_VideoType_SDTV,
	IDEA_VideoType_First = IDEA_VideoType_Digital,
	IDEA_VideoType_Last = IDEA_VideoType_SDTV,
  
  IDEA_VideoInputStatus_Inactive = IDEA_VideoInput_First + 200,
  IDEA_VideoInputStatus_Active,
  
  // Pixel Type Names
  IDEA_PixelType_RGB = IDEA_VideoInput_First + 300,
	IDEA_PixelType_Mono,
	IDEA_PixelType_YUV,

	IDEA_PixelBits,

  
  IDEA_TriggerMode_Off = IDEA_VideoInput_First + 400,
  IDEA_TriggerMode_StartStopLow,
  IDEA_TriggerMode_StartStopHigh,
  IDEA_TriggerMode_RunWhileLow,
  IDEA_TriggerMode_RunWhileHigh,

  IDEA_TriggerPolarity_Falling,
  IDEA_TriggerPolarity_Rising,
  
  IDEA_VerticalSyncType_Normal = IDEA_VideoInput_First + 500,
  IDEA_VerticalSyncType_Block,

  IDEA_CaptureDataType_8BitYOnly = IDEA_VideoInput_First + 600,
  IDEA_CaptureDataType_16BitYUV422,
  IDEA_CaptureDataType_16BitRGB555,
  IDEA_CaptureDataType_24BitRGB,
  IDEA_CaptureDataType_32BitRGB,

  IDEA_CaptureDimensions_SameAsInput = IDEA_VideoInput_First + 700,
  IDEA_CaptureDimensions_HalfInput,
  IDEA_CaptureDimensions_QuarterInput,
  IDEA_CaptureDimensions_720x480,
  IDEA_CaptureDimensions_480p,
  IDEA_CaptureDimensions_1280x720,
  IDEA_CaptureDimensions_720p,
  IDEA_CaptureDimensions_1920x1080,
  IDEA_CaptureDimensions_1080p,
  IDEA_CaptureDimensions_1080i,
  IDEA_CaptureDimensions_640x480,
  IDEA_CaptureDimensions_768x576,
  IDEA_CaptureDimensions_800x600,
  IDEA_CaptureDimensions_1024x768,
  IDEA_CaptureDimensions_1280x1024,
  IDEA_CaptureDimensions_1600x1200,
  IDEA_CaptureDimensions_1920x1200,

  IDEA_PreviewSameAsCapture_Off = IDEA_VideoInput_First + 800,
  IDEA_PreviewSameAsCapture_On,

  IDEA_PreviewDataType_8BitYOnly = IDEA_VideoInput_First + 900,
  IDEA_PreviewDataType_16BitYUV422,
  IDEA_PreviewDataType_16BitRGB555,
	IDEA_PreviewDataType_16BitRGB565,
  IDEA_PreviewDataType_24BitRGB,
  IDEA_PreviewDataType_32BitRGB,

  IDEA_PreviewDimensions_SameAsInput = IDEA_VideoInput_First + 1000,
  IDEA_PreviewDimensions_HalfInput,
  IDEA_PreviewDimensions_QuarterInput,
  IDEA_PreviewDimensions_720x480,
  IDEA_PreviewDimensions_480p,
  IDEA_PreviewDimensions_1280x720,
  IDEA_PreviewDimensions_720p,
  IDEA_PreviewDimensions_1920x1080,
  IDEA_PreviewDimensions_1080p,
  IDEA_PreviewDimensions_1080i,
  IDEA_PreviewDimensions_640x480,
  IDEA_PreviewDimensions_768x576,
  IDEA_PreviewDimensions_800x600,
  IDEA_PreviewDimensions_1024x768,
  IDEA_PreviewDimensions_1280x1024,
  IDEA_PreviewDimensions_1600x1200,
  IDEA_PreviewDimensions_1920x1200,

  IDEA_SymbolTableEnd = -1,
} IDEA_SYMBOL_ID;


// This enum is based on the VARENUM in the Microsoft header wtypes.h
// The assignments to IDEA_DT_... come from the VT_... types so they match.
typedef enum IDEA_DATA_TYPE
{
  IDEA_DT_EMPTY	      = 0,       // Nothing
  IDEA_DT_I4	        = 3,       // 4 byte signed int
  IDEA_DT_R4	        = 4,       // 4 byte real
  IDEA_DT_R8	        = 5,       // 8 byte real
  IDEA_DT_BSTR	      = 8,       // OLE Automation string
  IDEA_DT_ERROR	      = 10,      // SCODE
  IDEA_DT_BOOL	      = 11,      // True=-1, False=0
	IDEA_DT_VARIANT     = 12,      // VARIANT *
	IDEA_DT_UNKNOWN     = 13,      // IUnknown *
  IDEA_DT_I1	        = 16,      // signed char
  IDEA_DT_UI1	        = 17,      // unsigned char
  IDEA_DT_UI2	        = 18,      // unsigned short
  IDEA_DT_UI4	        = 19,      // unsigned long
  IDEA_DT_I8	        = 20,      // signed 64-bit int
  IDEA_DT_UI8	        = 21,      // unsigned 64-bit int
  IDEA_DT_VOID	      = 24,      // C style void
  IDEA_DT_HRESULT	    = 25,      // Standard return type
  IDEA_DT_PTR	        = 26,      // pointer type
  IDEA_DT_LPSTR	      = 30,      // null terminated string
  IDEA_DT_LPWSTR	    = 31,      // wide null terminated string
  IDEA_DT_STORAGE	    = 67,      // Name of the storage
} IDEA_DATA_TYPE;

typedef enum IDEA_CONTROL_TYPE
{
  IDEA_CT_NONE,
  IDEA_CT_CONTROL_MANAGEMENT,
  IDEA_CT_READ_ONLY,
  IDEA_CT_WRITE_ONLY,
  IDEA_CT_READ_WRITE,
  IDEA_CT_READ_OVERRIDE,
  IDEA_CT_EXECUTE
} IDEA_CONTROL_TYPE;

typedef enum IDEA_UNITS
{
  IDEA_U_None = 0,
  IDEA_U_ns,
  IDEA_U_us,
  IDEA_U_ms,
  IDEA_U_s,
  IDEA_U_mV,
  IDEA_U_V,
  IDEA_U_Hz,
  IDEA_U_KHz,
  IDEA_U_MHz,
  IDEA_U_Bits,
  IDEA_U_KBits,
  IDEA_U_MBits,
  IDEA_U_Pixels,
  IDEA_U_Lines,
  IDEA_U_Percent,
  IDEA_U_Bytes,
	IDEA_U_ReferenceTime  // Time expressed in 100ns units
} IDEA_UNITS;


// To retrieve an IDEA_CONTROL_INFO structure set nControlID to the IDEA_CONTROL_ID and
// call Get(IDEA_ControlInfo, sizeof(IDEA_CONTROL_INFO), IDEA_CONTROL_INFO *pStruct);
typedef struct
{
  int32_t           nStructSize;        // Size of this structure
  IDEA_CONTROL_ID   nControlID;         // From IDEA_CONTROL_ID enum.
  char              szControlName[32];  // Name string.
  IDEA_DATA_TYPE    nDataType;          // From IDEA_DATA_TYPE enum.
  int32_t           nDataSize;          // Size in bytes.
  IDEA_CONTROL_TYPE nControlType;       // From IDEA_CONTROL_TYPE enum.
  IDEA_UNITS        nUnits;             // From IDEA_UNITS enum.
  char              szUnits[32];        // Units string.
  int32_t           nMultiplier;        // Position of decimal point.
  int32_t           nMinValue;          // Minimum value
  int32_t           nMaxValue;          // Maximum value
  IDEA_SYMBOL_ID    nFirstSymbolID;     // Set if control has symbols
  int32_t           nMinStep;           // Minimum step value
  int32_t           nCommonStep;        // Common step value
} IDEA_CONTROL_INFO;


// To retrieve an IDEA_SYMBOL_INFO structure set nControlID to the IDEA_CONTROL_ID,
// set nSymbolID to the IDEA_SYMBOL_ID and nIndex to zero if the IDEA_SYMBOL_ID
// is known or set nIndex to the symbol index and nSymbolID to zero if only the
// index is known.
// call Get(IDEA_SymbolInfo, sizeof(IDEA_SYMBOL_INFO), IDEA_SYMBOL_INFO *pStruct);
typedef struct
{
  int32_t           nStructSize;          // Size of this structure
  IDEA_SYMBOL_ID    nSymbolID;            // From IDEA_CONTROL_INFO nFirstSymbolID.
  char              szSymbolName[32];     // Name string.
  IDEA_CONTROL_ID   nControlID;           // From IDEA_CONTROL_ID enum.
  int32_t           nIndex;               // Index in parameter list.
} IDEA_SYMBOL_INFO;


// To retrieve an IDEA_CONTROL_HELP string set nControlID to the IDEA_CONTROL_ID or,
// nSymbolID to the IDEA_SYMBOL_ID, nBufferSize to the size of the destination buffer
// and pszText to the buffer.  To get the required buffer size set nBufferSize to zero and
// the Get() method will return the size in nBufferSize.
// call Get(IDEA_ControlHelp, sizeof(IDEA_CONTROL_HELP), IDEA_CONTROL_HELP *pStruct);
typedef struct
{
  int32_t          nStructSize;  // Size of this structure
  union
  {
    IDEA_CONTROL_ID   nControlID;   // From IDEA_CONTROL_ID enum.
    IDEA_SYMBOL_ID    nSymbolID;    // From IDEA_SYMBOL_ID enum.
  };
  int32_t          nBufferSize;  // Must be set large enough to receive the string
  char             *pszText;     // Help string pointer.
} IDEA_CONTROL_HELP;


// To retrieve an IDEA_INDEXED_STRING structure set nControlID to the IDEA_CONTROL_ID,
// and nIndex to the entry in the table of strings.
// call Get(IDEA_<ControlID>, sizeof(IDEA_INDEXED_STRING), IDEA_INDEXED_STRING* pStruct);
typedef struct
{
	int32_t           nStructSize;          // Size of this structure
	int32_t           nIndex;               // Index in parameter list.
	char              szString[256];        // String (256 characters max).
} IDEA_INDEXED_STRING;


typedef enum IDEA_TRIGGER_MODE
{
	IDEA_TRIGGER_Off							= 0,
	IDEA_TRIGGER_StartStopLow,
	IDEA_TRIGGER_StartStopHigh,
	IDEA_TRIGGER_RunWhileLow,
	IDEA_TRIGGER_RunWhileHigh
} IDEA_TRIGGER_MODE;


typedef struct
{
  int32_t nStructSize;  // Size of this structure
  int32_t nInput;
  int32_t bActive;
  int32_t bStatusChanged;
  int32_t bInputAvailable;
} IDEA_INPUT_STATUS;


typedef enum IDEA_EVENT_TYPE
{
  IDEA_EVENT_TYPE_Sync,
  IDEA_EVENT_TYPE_Format,
  IDEA_EVENT_TYPE_Trigger,
} IDEA_EVENT_TYPE;


typedef struct
{
  int32_t          nStructSize;
  IDEA_EVENT_TYPE  nEventType;
  void             *pEventData;
} IDEA_EVENT_INFO;


typedef struct
{
  int32_t  nStructSize;  // Size of this structure
  int32_t  nRGBInputs;
  int32_t  nYPbPrInputs;
  int32_t  nSDTVCompositeInputs;
  int32_t  nSDTVSVideoInputs;
  int32_t  nDVIInputs;
  int32_t  nHDMIInputs;
	int32_t  nSDIInputs;
	int32_t  nAudioInputs;
  int32_t  nTriggerInputs;
  BOOL     bCanScaleVideo;
  BOOL     bCanDoH264;
  int32_t  nMaxDMASpeed_MBytesPerSec;
} IDEA_BOARD_CAPS;

typedef void (CALLBACK *P_IDEA_EVENT_CALLBACK)( void *pCallbackContext, IDEA_EVENT_INFO *pEventInfo );


typedef struct
{
  int32_t               nStructSize;        // Size of this structure
  HANDLE                hEvent;             // Event handle
  P_IDEA_EVENT_CALLBACK pfnCallback;        // Event callback function
  void                  *pCallbackContext;  // Context (this pointer) of the callback
} IDEA_EVENT_SETUP;

typedef struct
{
  int32_t               nStructSize;        // Size of this structure
  int32_t               nDataSize;          // Size of controls buffer, 0 to allocate on Get()
  char                  *pControlsBuffer;   // Pointer to controls buffer, 0 to allocate on Get()
  int32_t               bDeleteBuffer;      // Delete buffer after Set()
} IDEA_ALL_CONTROLS;

typedef struct
{
  int32_t               nStructSize;        // Size of this structure
  int32_t               nDataSize;          // Size of snapshot buffer, 0 to self allocate
  char*                 pSnapBuffer;        // Pointer to snapshot buffer, 0 if self allocating
  int32_t               nPixelType;         // IDEA_PixelType_RGBPixels, IDEA_PixelType_YPbPrPixels, IDEA_PixelType_MonochromePixels,
  int32_t               nWidth;             // Image Width, 0 if default
  int32_t               nHeight;            // Image Height, 0 if default
  int32_t               nLeft;              // Image X offset, 0 if default
  int32_t               nTop;               // Image Y offset, 0 if default
} IDEA_SNAP_SETUP;

typedef struct
{
  int32_t               nStructSize;        // Size of this structure
  int32_t               nNumberOfBuffers;   // Number of streaming buffers to use, 0 for default of 30
  int32_t               nPixelType;         // IDEA_PixelType_RGBPixels, IDEA_PixelType_YPbPrPixels, IDEA_PixelType_MonochromePixels,
  int32_t               nWidth;             // Image Width, 0 if default
  int32_t               nHeight;            // Image Height, 0 if default
  int32_t               nLeft;              // Image X offset, 0 if default
  int32_t               nTop;               // Image Y offset, 0 if default
  int32_t               nMode;              // Mode 0 = Stop, 1 = Run, 2 = Pause
} IDEA_STREAM_SETUP;

// Severity = 1, Customer = 0 indicates error.
// Allows negative values down to 0xE0000000 or -536,870,912 for return from GetValue()
#define IS_IDEA_ERROR( Status ) ( ( ( ( uint32_t )( Status ) ) & 0xA0000000 ) == 0x80000000 )

#endif  // #ifndef INCLUDED_IDEADDEFS_H
