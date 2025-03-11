/*
//=====================================================================
//
// Filename: ideaocx.h
//
//                  Copyright (C) 2002 by Foresight Imaging, LLC
//                                    All rights (world-wide) reserved.
//=====================================================================
*/

/*
//
// NOTE::: YOU MUST HAVE EVENT SINKS IN ORDER TO RECEIVE EVENTS
//         FROM THE OCX !!!!!!!!!!!!!!!
// Format is:  (Object, Object Id, Event ID, Callback Function, List of Parameters )
// The Object Id must match the Id used in the Create call  ---V
//                  m_pIdeaFG->Create( " ", 0, rcVideo, this, 0x100 );
//
BEGIN_EVENTSINK_MAP(CClassName, CDialog)
  ON_EVENT( CClassName, 0x100, 1, OnNotificationMessage, VTS_I2 VTS_I4 )
  ON_EVENT( CClassName, 0x100, 2, OnSnap, VTS_I2 )
  ON_EVENT( CClassName, 0x100, 3, OnStreamingTermination, VTS_I2 VTS_I4 )
  ON_EVENT( CClassName, 0x100, 4, OnSequenceTermination, VTS_I2 VTS_I4 )
  ON_EVENT( CClassName, 0x100, 5, OnFrameReady, VTS_I4)
END_EVENTSINK_MAP()
*/

#if !defined (_FSI_IDEAOCX_H_)
#define _FSI_IDEAOCX_H_

/* Image file types */
typedef enum  {
  IIF_IMAGE_FORMAT_UNDEFINED      = 0x00000000,
  IIF_IMAGE_FORMAT_BMP            = 0x00000001,
  IIF_IMAGE_FORMAT_TIFF           = 0x00000002,
  IIF_IMAGE_FORMAT_JPEG_LOSSLESS  = 0x00000003,
  IIF_IMAGE_FORMAT_DICOM          = 0x00000004,
  IIF_IMAGE_FORMAT_JPEG           = 0x00000005,

  IIF_IMAGE_FORMAT_LARGEST        = 0xFFFFFFFF
} iifImageFormat;

/* Pixel types */
typedef enum {
  PT_MONO_GRAY_8 = 0,
  PT_MONO_GRAY_10,
  PT_MONO_RGB3CH,
  PT_COLOR_YCBCR_YONLY,
  PT_COLOR_YCBCR,
  PT_COLOR_RGB32,
  PT_COLOR_RGB24,
  PT_COLOR_RGB16_565,
  PT_COLOR_RGB16_555,
  PT_COLOR_8_WEBSAFE_PALETTE,
  PT_YPBPR_YONLY,
  PT_YPBPR_YCBCR,
  PT_YPBPR_RGB32,
  PT_YPBPR_RGB24,
  PT_YPBPR_RGB16_565,
  PT_YPBPR_RGB16_555,
  PT_YPBPR_8_WEBSAFE_PALETTE,
  PT_NUM_PIXEL_TYPES
} ePixelType;

#define STR_MONO_GRAY_8             "8 Bit Grayscale"
#define STR_MONO_GRAY_10            "10 Bit Grayscale"
#define STR_MONO_RGB3CH             "24 Bit RGB (3 Channel)"
#define STR_COLOR_YCBCR_YONLY       "8 Bit Grayscale"
#define STR_COLOR_YCBCR             "YCbCr 4:2:2"
#define STR_COLOR_RGB32             "32 Bit RGB"
#define STR_COLOR_RGB24             "24 Bit RGB"
#define STR_COLOR_RGB16_565         "16 Bit RGB (5-6-5)"
#define STR_COLOR_RGB16_555         "16 Bit RGB (5-5-5)"
#define STR_COLOR_8_WEBSAFE_PALETTE "8 Bit Web-Safe Palette"
#define STR_YPBPR_YONLY             "8 Bit Grayscale"
#define STR_YPBPR_YCBCR             "YCbCr 4:2:2"
#define STR_YPBPR_RGB32             "32 Bit RGB"
#define STR_YPBPR_RGB24             "24 Bit RGB"
#define STR_YPBPR_RGB16_565         "16 Bit RGB (5-6-5)"
#define STR_YPBPR_RGB16_555         "16 Bit RGB (5-5-5)"
#define STR_YPBPR_8_WEBSAFE_PALETTE "8 Bit Web-Safe Palette"

/* VESA detection return codes */
#define VESA_SCAN_SUCCESS                  0
#define VESA_SCAN_NOT_RUN                 -1
#define VESA_SCAN_FAILED                  -2
#define VESA_SCAN_NO_BOARDS               -3
#define VESA_SCAN_INVALID_BOARD           -4
#define VESA_SCAN_TIMEOUT                 -5
#define VESA_SCAN_NO_VIDEO                -6
#define VESA_SCAN_INVALID_VIDEO           -7
#define VESA_SCAN_NO_MATCH                -8

/* IDEA_INFO_CONNECTION message parameters */
#define IDEA_CONNECTION_LOST              -1
#define IDEA_CONNECTION_REGAINED          -2

/* IDEA_INFO_DIALOG_ACTION message parameters */
typedef enum {
  CONFIG_DIALOG_OK                       = 0,
  CONFIG_DIALOG_CANCEL                   = 1,
  CONFIG_DIALOG_APPLY                    = 2,
  DIALOG_UPDATE_DISPLAY                  = 100
} IDEA_INFO_DIALOG_ACTIONS;

/* Flags for configuring dialogs */
#define VESA_DIALOG_OFF                    0x0000
#define VESA_DIALOG_ON                     0x0001
#define VESA_DIALOG_SILENT                 0x0100

#define CABLE_TYPE_DIALOG_OFF              0x0000
#define CABLE_TYPE_DIALOG_ON               0x0001

#define AOI_DIALOG_OFF                     0x0000
#define AOI_DIALOG_ON                      0x0001
#define AOI_DIALOG_LIVE                    0x0100
#define AOI_DIALOG_STREAM                  0x0200
#define AOI_DIALOG_SNAP                    0x0400
#define AOI_DIALOG_NOTIFY                  0x8000

#define COMPRESS_OPTIONS_OFF               0x0000
#define COMPRESS_OPTIONS_ON                0x0001

#define DICOM_OPTIONS_OFF                  0x0000
#define DICOM_OPTIONS_ON                   0x0001

#define VIDEO_ADJUST_OFF                   0x0000
#define VIDEO_ADJUST_ON                    0x0001
#define VIDEO_ADJUST_HIDDEN                0x0100

#define CONFIG_DIALOG_OFF                  0x0000
#define CONFIG_DIALOG_ON                   0x0001
#define CONFIG_DIALOG_WIZARD               0x0002
#define CONFIG_DIALOG_NO_LIVE              0x0100
#define CONFIG_DIALOG_NO_SNAPS             0x0200
#define CONFIG_DIALOG_NO_STREAMING         0x0400
#define CONFIG_DIALOG_NO_DICOM             0x0800
#define CONFIG_DIALOG_NO_NAMES             0x1000

#define BOARD_SELECTION_OFF                0x0000
#define BOARD_SELECTION_ON                 0x0001
#define BOARD_SELECTION_NO_NAMES           0x0100

#define USER_BOARD_NAMES_OFF               0x0000
#define USER_BOARD_NAMES_ON                0x0001

#define WHITE_BALANCE_OFF                  0x0000
#define WHITE_BALANCE_ON                   0x0001
#define WHITE_BALANCE_SILENT               0x0100

/* Flags for SnappedImageOrientation property */
#define ORIENTATION_NORMAL                 0x00000000
#define ORIENTATION_VFLIP                  0x00000001

/* Constants for streaming or sequence acquire */
#define FRAMES_RUN_UNTIL_CANCEL            -1

/* Triggered streaming modes */
typedef enum {
  TRIGGER_OFF = 0,
  TRIGGER_START_STOP_LOW,
  TRIGGER_START_STOP_HIGH,
  TRIGGER_RUN_LOW,
  TRIGGER_RUN_HIGH
} TRIGGER_MODE;

/*
// Streaming flags are:
//   0x0001 - Notify application on each frame
//   0x0002 - Notify application at the end of streaming
//   0x0004 - Use a codec to compress stream (obsolete - not used)
//   0x0008 - Start streaming on external trigger event (obsolete - use TriggeredStreaming property instead)
//   0x0010 - Preview in a DirectDrawWindow (obsolete - not used)
//   0x0020 - Write to AVI file (reserved for internal use only)
//   0x0040 - Do not delete memory buffers
//   0x0080 - Run in a separate thread (obsolete -- always uses a thread now)
//   0x0100 - Terminate on trigger stop
//   0x0200 - Terminate on dropped frame(s)
//   0x0400 - Write to DICOM file (reserved for internal use only)
//   0x0800 - Continuous capture mode (reserved for internal use only)
//   0x1000 - Preserve log file since last run
//   0x2000 - Don't save Data Set after streaming (DICOM, internal use only)
//   0x4000 - Write to memory buffers only (reserved for internal use only)
//   0x8000 - Don't add DICOM offset table to file (reserved for internal use only)
//   0x10000 - Flip image vertically on streaming

*/
#define STREAMING_FRAME_NOTIFY                0x0001
#define STREAMING_TERMINATION_NOTIFY          0x0002
#define STREAMING_RESV_0004                   0x0004
#define STREAMING_RESV_0008                   0x0008
#define STREAMING_RESV_0010                   0x0010
#define STREAMING_WRITE_AVI                   0x0020
#define STREAMING_PRESERVE_BUFFERS            0x0040
#define STREAMING_RESV_0080                   0x0080
#define STREAMING_TERMINATE_ON_TRIGGER_STOP   0x0100
#define STREAMING_TERMINATE_ON_DROPPED_FRAME  0x0200
#define STREAMING_WRITE_DICOM                 0x0400
#define STREAMING_CONTINUOUS                  0x0800
#define STREAMING_PRESERVE_LOG_FILE           0x1000
#define STREAMING_DICOM_DONT_SAVE_DS          0x2000
#define STREAMING_WRITE_MEMORY                0x4000
#define STREAMING_NO_DICOM_OFFSET_TABLE       0x8000
#define STREAMING_FLIP_IMAGE_VERTICALLY       0x10000

/* Verbosity levels for reporting */
typedef enum {
  VERBOSITY_SILENT                       = 0,
  VERBOSITY_DIALOGS                      = 1,
  VERBOSITY_ERRORS                       = 2,
  VERBOSITY_WARNINGS                     = 3,
  VERBOSITY_INFO                         = 4
} VERBOSITY;

/* FourCC codes */
#define DIB__FOURCC (DWORD)0x20424944   // mmioFOURCC('D','I','B',' ')
#define MJPG_FOURCC (DWORD)0x47504A4D   // mmioFOURCC('M','J','P','G')
#define PIMJ_FOURCC (DWORD)0x4A4D4950   // mmioFOURCC('P','I','M','J')
#define YUY2_FOURCC (DWORD)0x32595559   // mmioFOURCC('Y','U','Y','2')
#define PVW2_FOURCC (DWORD)0x32575650   // mmioFOURCC('P','V','W','2')
#define LEAD_FOURCC (DWORD)0x4441454C   // mmioFOURCC('L','E','A','D')
#define MSVC_FOURCC (DWORD)0x4356534D   // mmioFOURCC('M','S','V','C')

/* DeInterlacing Modes */
#define DEINTERLACE_NONE (0)
#define DEINTERLACE_KEEP_ODD (1)
#define DEINTERLACE_KEEP_EVEN (2)

typedef enum
{
  RENDERER_GDI,
  RENDERER_DEFAULT,
  RENDERER_DIRECTDRAW,
  RENDERER_DIRECTDRAW_COPY,
  RENDERER_DIRECT2D
} RENDERER_TYPE;

#endif
