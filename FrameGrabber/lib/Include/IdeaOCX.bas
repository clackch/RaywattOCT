Attribute VB_Name = "IdeaOCX"
Option Explicit

'Win32 events declarations
Type SECURITY_ATTRIBUTES
  nLength As Long
  lpSecurityDescriptor As Long
  bInheritHandle As Boolean
End Type

Public Declare Function CreateEvent Lib "kernel32" Alias "CreateEventA" _
        (lpEventAttributes As SECURITY_ATTRIBUTES, ByVal bManualReset As Long, _
        ByVal bInitialState As Long, ByVal lpName As String) As Long
Public Declare Function SetEvent Lib "kernel32" (ByVal hEvent As Long) As Long
Public Declare Function CloseHandle Lib "kernel32" (ByVal hObject As Long) As Long
Public Declare Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As Long)

'Image file types
Public Const IIF_IMAGE_FORMAT_UNDEFINED      As Long = 0
Public Const IIF_IMAGE_FORMAT_BMP            As Long = 1
Public Const IIF_IMAGE_FORMAT_TIFF           As Long = 2
Public Const IIF_IMAGE_FORMAT_JPEG_LOSSLESS  As Long = 3
Public Const IIF_IMAGE_FORMAT_DICOM          As Long = 4
Public Const IIF_IMAGE_FORMAT_JPEG           As Long = 5

'Pixel types
Public Const PT_MONO_GRAY_8             As Long = 0    '8 Bit Grayscale
Public Const PT_MONO_GRAY_10            As Long = 1    '10 Bit Grayscale
Public Const PT_MONO_RGB3CH             As Long = 2    '24 Bit RGB (3 Channel)
Public Const PT_COLOR_YCBCR_YONLY       As Long = 3    '8 Bit Grayscale
Public Const PT_COLOR_YCBCR             As Long = 4    'YCbCr 4:2:2
Public Const PT_COLOR_RGB32             As Long = 5    '32 Bit RGB
Public Const PT_COLOR_RGB24             As Long = 6    '24 Bit RGB
Public Const PT_COLOR_RGB16_565         As Long = 7    '16 Bit RGB (5-6-5)
Public Const PT_COLOR_RGB16_555         As Long = 8    '16 Bit RGB (5-5-5)
Public Const PT_COLOR_8_WEBSAFE_PALETTE As Long = 9    '8 Bit Web-Safe Palette
Public Const PT_YPBPR_YONLY             As Long = 10   '8 Bit Grayscale
Public Const PT_YPBPR_YCBCR             As Long = 11   'YCbCr 4:2:2
Public Const PT_YPBPR_RGB32             As Long = 12   '32 Bit RGB
Public Const PT_YPBPR_RGB24             As Long = 13   '24 Bit RGB
Public Const PT_YPBPR_RGB16_565         As Long = 14   '16 Bit RGB (5-6-5)
Public Const PT_YPBPR_RGB16_555         As Long = 15   '16 Bit RGB (5-5-5)
Public Const PT_YPBPR_8_WEBSAFE_PALETTE As Long = 16   '8 Bit Web-Safe Palette

'VESA detection return codes
Public Const VESA_SCAN_SUCCESS          As Long = 0
Public Const VESA_SCAN_NOT_RUN          As Long = -1
Public Const VESA_SCAN_FAILED           As Long = -2
Public Const VESA_SCAN_NO_BOARDS        As Long = -3
Public Const VESA_SCAN_INVALID_BOARD    As Long = -4
Public Const VESA_SCAN_TIMEOUT          As Long = -5
Public Const VESA_SCAN_NO_VIDEO         As Long = -6
Public Const VESA_SCAN_INVALID_VIDEO    As Long = -7
Public Const VESA_SCAN_NO_MATCH         As Long = -8

'IDEA_INFO_CONNECTION message parameters
Public Const IDEA_CONNECTION_LOST       As Long = -1
Public Const IDEA_CONNECTION_REGAINED   As Long = -2

'IDEA_INFO_DIALOG_ACTION message parameters
Public Const CONFIG_DIALOG_OK           As Long = 0
Public Const CONFIG_DIALOG_CANCEL       As Long = 1
Public Const CONFIG_DIALOG_APPLY        As Long = 2
Public Const DIALOG_UPDATE_DISPLAY      As Long = 100

'Flags for configuring dialogs
Public Const VESA_DIALOG_OFF            As Long = &H0
Public Const VESA_DIALOG_ON             As Long = &H1
Public Const VESA_DIALOG_SILENT         As Long = &H100

Public Const CABLE_TYPE_DIALOG_OFF      As Long = &H0
Public Const CABLE_TYPE_DIALOG_ON       As Long = &H1

Public Const AOI_DIALOG_OFF             As Long = &H0
Public Const AOI_DIALOG_ON              As Long = &H1
Public Const AOI_DIALOG_LIVE            As Long = &H100
Public Const AOI_DIALOG_STREAM          As Long = &H200

Public Const COMPRESS_OPTIONS_OFF       As Long = &H0
Public Const COMPRESS_OPTIONS_ON        As Long = &H1

Public Const DICOM_OPTIONS_OFF          As Long = &H0
Public Const DICOM_OPTIONS_ON           As Long = &H1

Public Const VIDEO_ADJUST_OFF           As Long = &H0
Public Const VIDEO_ADJUST_ON            As Long = &H1
Public Const VIDEO_ADJUST_HIDDEN        As Long = &H100

Public Const CONFIG_DIALOG_OFF          As Long = &H0
Public Const CONFIG_DIALOG_ON           As Long = &H1
Public Const CONFIG_DIALOG_WIZARD       As Long = &H2
Public Const CONFIG_DIALOG_NO_LIVE      As Long = &H100
Public Const CONFIG_DIALOG_NO_SNAPS     As Long = &H200
Public Const CONFIG_DIALOG_NO_STREAMING As Long = &H400
Public Const CONFIG_DIALOG_NO_DICOM     As Long = &H800
Public Const CONFIG_DIALOG_NO_NAMES     As Long = &H1000

Public Const BOARD_SELECTION_OFF        As Long = &H0
Public Const BOARD_SELECTION_ON         As Long = &H1
Public Const BOARD_SELECTION_NO_NAMES   As Long = &H100

Public Const USER_BOARD_NAMES_OFF       As Long = &H0
Public Const USER_BOARD_NAMES_ON        As Long = &H1

Public Const WHITE_BALANCE_OFF          As Long = &H0
Public Const WHITE_BALANCE_ON           As Long = &H1
Public Const WHITE_BALANCE_SILENT       As Long = &H100

'Flags for SnappedImageOrientation property
Public Const ORIENTATION_NORMAL         As Long = &H0
Public Const ORIENTATION_VFLIP          As Long = &H1

'Constants for streaming or sequence acquire
Public Const FRAMES_RUN_UNTIL_CANCEL    As Long = -1

'Triggered streaming modes
Public Const TRIGGER_OFF                As Long = 0
Public Const TRIGGER_START_STOP_LOW     As Long = 1
Public Const TRIGGER_START_STOP_HIGH    As Long = 2
Public Const TRIGGER_RUN_LOW            As Long = 3
Public Const TRIGGER_RUN_HIGH           As Long = 4

'Streaming flags are:
' &H0001 - Notify application on each frame
' &H0002 - Notify application at the end of streaming
' &H0004 - Use a codec to compress stream (obsolete - not used)
' &H0008 - Start streaming on external trigger event (obsolete - use TriggeredStreaming property instead)
' &H0010 - Preview in a DirectDrawWindow (obsolete - not used)
' &H0020 - Write to AVI file (reserved for internal use only)
' &H0040 - Do not delete memory buffers
' &H0080 - Run in a separate thread (obsolete -- always uses a thread now)
' &H0100 - Terminate on trigger stop
' &H0200 - Terminate on dropped frame(s)
' &H0400 - Write to DICOM file (reserved for internal use only)
' &H0800 - Continuous capture mode (reserved for internal use only)
' &H1000 - Preserve log file since last run
' &H2000 - Don't save Data Set after streaming (DICOM, internal use only)
' &H4000 - Write to memory buffers only (reserved for internal use only)
' &H8000 - Don't add DICOM offset table to file (reserved for internal use only)
' &H10000 - Flip image vertically on streaming

Public Const STREAMING_FRAME_NOTIFY                As Long = &H1
Public Const STREAMING_TERMINATION_NOTIFY          As Long = &H2
Public Const STREAMING_RESV_0004                   As Long = &H4
Public Const STREAMING_RESV_0008                   As Long = &H8
Public Const STREAMING_RESV_0010                   As Long = &H10
Public Const STREAMING_WRITE_AVI                   As Long = &H20
Public Const STREAMING_PRESERVE_BUFFERS            As Long = &H40
Public Const STREAMING_RESV_0080                   As Long = &H80
Public Const STREAMING_TERMINATE_ON_TRIGGER_STOP   As Long = &H100
Public Const STREAMING_TERMINATE_ON_DROPPED_FRAME  As Long = &H200
Public Const STREAMING_WRITE_DICOM                 As Long = &H400
Public Const STREAMING_CONTINUOUS                  As Long = &H800
Public Const STREAMING_PRESERVE_LOG_FILE           As Long = &H1000
Public Const STREAMING_DICOM_DONT_SAVE_DS          As Long = &H2000
Public Const STREAMING_WRITE_MEMORY                As Long = &H4000
Public Const STREAMING_NO_DICOM_OFFSET_TABLE       As Long = &H8000&
Public Const STREAMING_FLIP_IMAGE_VERTICALLY       As Long = &H10000&

'Verbosity levels for reporting
Public Const VERBOSITY_SILENT           As Long = 0
Public Const VERBOSITY_DIALOGS          As Long = 1
Public Const VERBOSITY_ERRORS           As Long = 2
Public Const VERBOSITY_WARNINGS         As Long = 3
Public Const VERBOSITY_INFO             As Long = 4

'FourCC codes & conversion
Public Const DIB__FOURCC As Long = &H20424944   'MakeFourCC("DIB ")
Public Const MJPG_FOURCC As Long = &H47504A4D   'MakeFourCC("MJPG")
Public Const PIMJ_FOURCC As Long = &H4A4D4950   'MakeFourCC("PIMJ")
Public Const YUY2_FOURCC As Long = &H32595559   'MakeFourCC("YUY2")
Public Const PVW2_FOURCC As Long = &H32575650   'MakeFourCC("PVW2")
Public Const LEAD_FOURCC As Long = &H4441454C   'MakeFourCC("LEAD")
Public Const MSVC_FOURCC As Long = &H4356534D   'MakeFourCC("MSVC")

' Deinterlacing Modes
Public Const DEINTERLACE_NONE As Long = 0
Public Const DEINTERLACE_KEEP_ODD As Long = 1
Public Const DEINTERLACE_KEEP_EVEN As Long = 2


Public Function MakeFourCC(Str4 As String) As Long
    MakeFourCC = Asc(Mid(Str4, 1, 1)) + _
            (2 ^ 8) * Asc(Mid(Str4, 2, 1)) + _
            (2 ^ 16) * Asc(Mid(Str4, 3, 1)) + _
            (2 ^ 24) * Asc(Mid(Str4, 4, 1))
End Function


