/*
//=====================================================================
//
// Filename: ..\HDP\LIB\SRC\HDP_LIB.H
//
//                  Copyright (C) 1999-2011 by Foresight Imaging, LLC
//                                    All rights (world-wide) reserved.
//=====================================================================
*/


/*
//=====================================================================
//=================== Library Revision Declarations ===================
//=====================================================================
//
//
// Note:
//    If this file (HDP_LIB.h) is being included only for the revision
//   number, then use this code:
//      #define  _INC_HDP_LIB_U 1
//      #define  _INC_HDP_LIB   1
//      #include "hdp_lib.h"
//    This will avoid overhead including the compilation of other
//   include files (hp_ftime.h, gen_def.h).
//
//
//         *==========================================*
//         *                                          *
//         *              !!! Alert !!!               *
//         *                                          *
//         *  This Library Revision number must match *
//         * the revision number specified in the lib-*
//         * rary error message file (HDPERROR.DAT).  *
//         *  Also, this revision number is used by   *
//         * all executable HI*DEF library components *
//         * including static libraries, DLLs, and    *
//         * VxD's.                                   *
//         *  Additionally, when the revision number  *
//         * is changed:                              *
//         *   1) New version labels must be created  *
//         *     under PVCS;                          *
//         *   2) Check the "About" boxes and the     *
//         *     property pages for all components;   *
//         *   3) For release versions, the creation  *
//         *     time elements must be the version    *
//         *     number for each component.           *
//         *                                          *
//         *==========================================*
*/
#ifndef HDP_LIB_NREV
#define HDP_LIB_NREV  0x352
#define HDP_LIB_CREV  "3.5.235"
#define HDP_LIB_DATE  __DATE__
#endif


/*
//=====================================================================
//=================== Start of Universal Definitions ==================
//=====================================================================
*/
#ifndef _INC_HDP_LIB_U
#define _INC_HDP_LIB_U
#include "hp_ftime.h"
#include <stdint.h>

#if defined (UNIX)
#include "hlcommon.h"
#else
#if !defined (VTOOLSD)
#include <windef.h> 
#endif
#endif

/*
// Handle definitions.
*/
typedef short BoardHandle;
typedef void *ImageHandle;

typedef unsigned long HD_ABOSERIAL;

typedef struct {
  BoardHandle   bh;
  short         nResv;     /* for DWORD alignment */
  HD_ABOSERIAL  nABO;
  HANDLE        hAppEvent;
} HD_ABOHandle;

typedef struct {
  ImageHandle   ih;
  ERRTYPE       e;
  short         nResv;     /* for DWORD alignment */
  FINE_TIME     ftVEnd;
  unsigned long dwFieldN;
} HD_ABOSTAT;

typedef struct {
  HD_ABOHandle  hABO;
  HD_ABOSTAT    ABOStat;
} HD_ABOS;

typedef void (*HCB_ABO)(void *,long,long,HD_ABOS *);

typedef struct {
  HCB_ABO    cbABO;
  void       *pThis;
  long       lUser;
} HD_ABOCB;

typedef struct {
  long          lEvent;   /* Must be set to 0.   */
  long          lResv;    /* Must be set to 0.   */
  FINE_TIME     ftVEnd;   /* Set to 0 if unused. */
  unsigned long dwFieldN; /* Set to 0 if unused. */
} HD_ABORELEASE;

typedef struct {
  HD_ABOS       *pABOS;   /* NULL if operation is synchronous.    */
  HD_ABOCB      *pABOCB;  /* NULL for no callback.                */
  FINE_TIME     ftVEnd;   /* 0 if not specifying field by time.   */
  unsigned long dwFieldN; /* 0 if not specifying field by number. */
} HDOP_HDL;
#define HDOP_HNULL  (HDOP_HDL *)NULL

/*
// Typedef and defines for board types.
*/
typedef short HIDEF_TYPE;
#define HIDEF_PLUS     3
#define HIDEF_ACCURA   4
#define HIDEF_4        4
#define HIDEF_I25      5
#define HIDEF_I50      6
#define HIDEF_I60      7
#define HIDEF_I25MV    8
#define HIDEF_I50MV    9
#define HIDEF_I60MV    10
#define HIDEF_ICOLOR   11
#define HIDEF_ICOLORMV 12
#define HIDEF_IRGB25   13
#define HIDEF_IRGB50   14
#define HIDEF_IRGB60   15
#define HIDEF_IRGB25MV 16
#define HIDEF_IRGB50MV 17
#define HIDEF_IRGB60MV 18
#define HIDEF_I75      19
#define HIDEF_I75MV    20
#define HIDEF_IRGB75   21
#define HIDEF_IRGB75MV 22
#define HIDEF_IRGB165  23
#define HIDEF_IRGB200  24
#define HIDEF_I50HSN   25
#define HIDEF_IRGB170  26
#define HIDEF_IRGBI64_170 27
#define HIDEF_ACCUSTREAM_170 27
#define HIDEF_ACCUSTREAM_205A 28
#define HIDEF_ACCUSTREAM_50 29
#define HIDEF_ACCUSTREAM_75 30
#define HIDEF_ACCUSTREAM_VDR 31
#define HIDEF_ACCUSTREAM_50A 32
#define HIDEF_ACCUSTREAM_75A 33
#define HIDEF_ACCUSTREAM_170_PLUS 34
#define HIDEF_ACCUSTREAM_75_PLUS 35
#define HIDEF_ACCUSTREAM_50_PLUS 36
#define HIDEF_ACCUSTREAM_EXPRESS_170 37
#define HIDEF_ACCUSTREAM_EXPRESS_50 38
#define HIDEF_ACCUSTREAM_EXPRESS_75 39
#define HIDEF_ACCUSTREAM_EXPRESS_HD 40
#define HIDEF_ACCUSTREAM_EXPRESS_HD50 41
#define HIDEF_ACCUSTREAM_EXPRESS_HD75 42
#define HIDEF_ACCUSTREAM_EXPRESS_HDC 43
#define HIDEF_ACCUSTREAM_EXPRESS_HDC50 44
#define HIDEF_ACCUSTREAM_EXPRESS_HDC75 45
#define HIDEF_ACCUSTREAM_EXPRESS_1000 46
#define HIDEF_ACCUSTREAM_EXPRESS_1000_75 47
#define HIDEF_ACCUSTREAM_EXPRESS_1000_SDI 48
#define HIDEF_ACCUSTREAM_EXPRESS_2000 49
#define HIDEF_ACCUSTREAM_EXPRESS_2000_75 50
#define HIDEF_ACCUSTREAM_EXPRESS_2000_SDI 51

/*
// Defines and structures for nHP_Report().
*/
/* Library supports at most 8 boards in a system */
#define HDRC_RESCAN  0x01
#define HDRC_TEST    0x02
#define HDRC_RETEST  0x04
#define HDRC_NOCLAIM 0x08
#define HDRC_TEST_UNTESTED  0


#define CAMERA_STRING_SIZE (32)
#define MAX_CAMERA_MODES   (12)

#define CAMERA_HELP_FILE_SIZE (128)

/* Wanted to reduce this to 8, but broke the diag */
#define MAXBOARDCOUNT (15)

typedef struct tagCameraInfo {
  char szCameraName[CAMERA_STRING_SIZE];
  char aszCameraModes[MAX_CAMERA_MODES][CAMERA_STRING_SIZE];
  char szCurrentMode[CAMERA_STRING_SIZE];
  int  nNumModes;
  int  nExposureType;
  WORD wExposureFields;
  int  nIntegrationType;
  int  nSyncType;
  char szCameraHelpFile[CAMERA_HELP_FILE_SIZE];
} HD_CameraInfo;

typedef struct tagCableInfo {
  int  iPin;
  int  iUsage;
  char szFunction[CAMERA_STRING_SIZE];
  struct tagCableInfo *pNext;
} HD_CableInfo;

typedef struct {
  int          nCPSwap;
  DWORD        dwOSYN_CTL;     // Control for Sync Gen, offset 0x80
  DWORD        dwADIO_CTL;     // IO Control Enable, offset 0xA0
  DWORD        dwADIO_OUT;     // Camera IO Output Config, offset 0xAC
  DWORD        dwCAM_CTL;      // Camera control register, offset 0x88
  DWORD        dwCAM1_DLY;     // Exposure control #1 Delay, offset 0x8C
  DWORD        dwCAM1_TIM;     // Exposure control #1 duration, offset 0x90
  DWORD        dwCAM2_DLY;     // Exposure control #2 delay, offset 0x84
  DWORD        dwCAM2_TIM;     // Exposure control #2 duration offset, 0xB0
  DWORD        dwSTRB_DLY;     // Strobe Delay, offset 0x94
  DWORD        dwSTRB_TIM;     // Strobe Duration, offset 0x98
  DWORD        dwCT1_PSRC;     // HSync In Config, offset 0xA4
  DWORD        dwCT2_PSRC;     // Vsync In Config, offset 0xA8
  DWORD        dwOSYNC_H;      // Htotal for Sync Gen, offset 0xB8
  DWORD        dwOSYNC_HW;     // Generated HSync low time, offset 0x9C
  DWORD        dwOSYNC_V;      // Vtotal for Sync Gen, offset 0xBC
  DWORD        dwOSYNC_VW;     // Generated VSync low time, offset 0x7C
  DWORD        dwStrobePolarity;  // Strobe polarity, 0 = high to low, 1 = low to high
  DWORD        dwTriggerPolarity; // Trigger polarity, 0 = low to high, 1 = high to low
  DWORD        dwIntegratePeriod; // integration period, in fields.
  DWORD        dwVTotal;       // Actual camera VTotal.
  BOOL         bIntegrate;     // Integration flag
  DWORD        dwRETRIG_DLY;   // Retrigger delay, offset 0x78
  DWORD        dwActelRev;     // Actel revision #
  DWORD        dwOSYNC_INTG;   // Integration counter
} MV_HWSTATE;

typedef struct {
  HIDEF_TYPE      nType;     /* HIDEF_PLUS, HIDEF_ACCURA, etc.       */
  unsigned short  wID;       /* ISA Base IO address or PCI slot #.   */
  unsigned short  wIO;       /* Base IO address.                     */
  char            cID;       /* Character ID assigned in registry.   */
  char            cResv;     /* Forcing WORD alignment.              */
  char            szSerial[8]; /* ASCII Serial number, if available. */
  BoardHandle     bh;        /* Handle if claimed by this VM, else 0.*/
  short           bClaim;    /* True if claimed by any process.      */
  unsigned long   dwFeatures;/* Features available on this board.    */
} HD_sBoard;
typedef struct {
  unsigned long   e;
  unsigned short  wRev;         /* device driver internal revision number */
  short           nBrdCount;    /* nBrdCount is the actual number of boards in the system */
  short           nBrdList;     /* the number of .bra structures actually filled in, usually == nBrdCount */
  unsigned short  w32BitDriver; /* are we using a 32 or a 64 bit driver */
  unsigned long   dwFragDMA;
  unsigned long   dwFragHisto;
  HD_sBoard       bra[MAXBOARDCOUNT];
} HD_sReport;


/*
//   Error codes returned by bhHP_Claim(), bhHD_Claim(), and
// ihHD_Allocate().
//
//  HDALLOCATE_PITCH:  Unresolvable HI*DEF Plus 1024/2048 pitch
//      conflict.  The new image would have required that the HI*DEF
//      Plus board pitch be changed.
*/
#define HPCLAIM_NO_VXD   -2
#define HPCLAIM_NO_HDP   -3
#define HPCLAIM_TOOMANY  -4
#define HPCLAIM_IN_USE   -5
#define HPCLAIM_ALLOC    -6
#define HPCLAIM_PARAM    -7
#define HPCLAIM_DOSMEM   -8
#define HPCLAIM_VXD_REV  -9
#define HPCLAIM_PITCH   -10

/*
// Parameter "nFlags" to bhHD_Claim().
//
//   HDICI_x_y:
//    x:  CT:  The CT1 and CT2 specification.
//        CS:  The Composite Sync specification.
//        HF:  The horizontal frequency specification (+/- 2%).
//        HT:  The horizontal total specification (+/- 20%).
//    y:  ERR: Report an error if the RSET value is out of range.
//        USE: Use the ICI value (override the RSET value, if any).
//        CHK: Constrain the RSET value to the ICI limits.
//        IGN: Ignore the ICI value (use the RSET value).
*/
#define HDCLAIM_NO_INTR  0x0001
#define HDCLAIM_FORCE_SB 0x0002
#define HDCLAIM_SERVICE_CLAIM 0x0004

/*
// Register Set structure.
//   Library developers: see "GEN_RSET" notes in HP_REGS.c before
//  making changes to this enumeration.
*/
#define HPRFIELD(fld) (1<<(HPR_##fld&7))
#define HD_RSETINDEX_RPT(p,i) ((((BYTE *)p)[i/8] & (1<<(i&7))) != 0)
#define HD_RSETINDEX_CLR(p,i) ( ((BYTE *)p)[i/8] &= ~(1<<(i&7)) )
#define HD_RSETINDEX_SET(p,i) ( ((BYTE *)p)[i/8] |= (1<<(i&7)) )

typedef enum __RegIndex
{
  HPR_VCHAN=0,  HPR_CSYNC,    HPR_PCLOCK,   HPR_CLAMPPOS, HPR_CLAMPDUR,  // was HPR_RESV0,    HPR_RESV1,
  HPR_VTOTAL,   HPR_FR_MSEC,  HPR_INTERL,   HPR_VSTYPE,   HPR_VBP,
  HPR_HEIGHT,   HPR_CABLE,    HPR_DIGITAL,  HPR_HFREQ,    HPR_HTOTAL,
  HPR_HBS,      HPR_WIDTH,    HPR_HS_NSEC,  HPR_HBP,      HPR_SATURATION, // was HPR_RESV4
  HPR_GAIN,     HPR_BLEVEL,   HPR_BLACKREF, HPR_WHITEREF, HPR_FLAGS,
  HPR_PIXELSET, HPR_ZOOMDOWN, HPR_SDTV_IN,  HPR_BLANKFIL, HPR_PITCH,
  HPR_PASSMODE, HPR_DPDELAY,  HPR_PHASE2,   HPR_PHASE,    HPR_VATTR,
  HPR_CONTROL,  HPR_PIXELMODE,  HPR_HSTART,   HPR_VSTART,   HPR_CAMCTL,
  HPR_REFPROG,  HPR_REFBTYPE, HPR_REFBOARD, HPR_HUE, HPR_GAMMA_X100,
  HPR_SYNCFILTER,  HPR_PIXFREQ,  HPR_VERTFREQ, HPR_N,
	// Alternate uses, preserve legacy names
	HPR_INPUT_TYPE = HPR_DIGITAL,
  HPR_FINE_PHASE = HPR_PCLOCK
} REGINDEX;


enum _HPR_INPUT_TYPE
{
	HPR_INPUT_TYPE_ANALOG = 0,
	HPR_INPUT_TYPE_DVI,
	HPR_INPUT_TYPE_HDMI,
	HPR_INPUT_TYPE_SDI
};


typedef struct {
  unsigned char byaRegSpecd[8]; /* Bit=1 for each lRegs[] specified. */
  union
  {
    SDWORD          lRegs[HPR_N];

    struct
    {
      SDWORD lVideoChannel;
      SDWORD lSyncChannel;
      SDWORD lFinePhase;
      SDWORD lClampPosition;
      SDWORD lClampDuration;
      SDWORD lVerticalTotal;
      SDWORD lFrameTime_ms;
      SDWORD lInterlaced;
      SDWORD lVerticalSyncType;
      SDWORD lVerticalBackPorch;
      SDWORD lImageHeight;
      SDWORD lCableType;
			union
			{
				SDWORD lDigitalChannel;
				SDWORD lInputType;
			};
			SDWORD lHorizontalFrequency;
      SDWORD lHorizontalTotal;
      SDWORD lHorizontalBackSync;
      SDWORD lImageWidth;
      SDWORD lHorizontalSyncWidth_ns;
      SDWORD lHorizontalBackPorch;
      SDWORD lSaturation;
      SDWORD lGain;
      SDWORD lBlackLevel;
      SDWORD lBlackReference;
      SDWORD lWhiteReference;
      SDWORD lFlags;
      SDWORD lPixelSetValue;
      SDWORD lZoomDown;
      SDWORD lSDTVMode;
      SDWORD lBlankingFilter;
      SDWORD lPitch;
      SDWORD lPassMode;
      SDWORD lDualPassDelay;
      SDWORD lPhase2;
      SDWORD lPhase;
      SDWORD lVideoAttributes;
      SDWORD lControl;
      SDWORD lPixelMode;
      SDWORD lHorizontalStart;
      SDWORD lVerticalStart;
      SDWORD lCameraControl;
      SDWORD lReferenceProgram;
      SDWORD lReferenceBoardType;
      SDWORD lReferenceBoardSerial;
      SDWORD lHue;
			SDWORD lGammaX100;
      SDWORD lSyncFilter;
      SDWORD lPixelFrequency;
      SDWORD lVerticalFrequency;
    } lRegisters;
  };
  
  union
  {
    struct
    {
      HD_CableInfo*  pCableInfo;  /* machine vision cable information  */
      HD_CameraInfo* pCameraInfo; /* machine vision camera information */
      MV_HWSTATE*    pMVState;
    } ObsoleteMembers;

    struct
    {
      SDWORD lActiveVideoWidth; // Active video width in case image width is used for cropping
      SDWORD lActiveVideoHeight;     // Active video height in case image height is used for cropping
    };
  };

  BYTE          KS0127Regs[64];  /* I-Color A/D registers    */
  BYTE          AD9884Regs[14];  /* I-Color RGB A/D registers    */
  BYTE          AD9888Regs[26];  /* I-RGB 165,180 register set */
  BYTE          AD9887Regs[40];  /* I-RGB 170 register set, note the 'A' part has 40 regs, the non-'A' part has 32 */
  BYTE          ADV7401Regs[256]; /* AccuStream205A family has 9888 and 7401 */
  BYTE          ADV7441Regs[256]; /* AccuStreamHD family has ADV7441 */
  char          szCHPFile[MAX_PATH];  /* store the name of the chp file used for this rset */
  char          szEDIDFile[MAX_PATH];  /* Name of EDID file */
} RSET;
/*
// Defines for rset element [HPR_VCHAN] and [HPR_CSYNC].
*/
#define HDCHAN_CA1     0
#define HDCHAN_CA2     1
#define HDCHAN_CA3     2
#define HDCHAN_CA4     3
#define HDCHAN_SS12    4
#define HDCHAN_SS12H   5
#define HDCHAN_SS12V   6
#define HDCHAN_SS12I   7
#define HDCHAN_CT1     8
#define HDCHAN_CT2     9
#define HDCHAN_CT3    10
#define HDCHAN_CT4    11
#define HDCHAN_CT1I   12
#define HDCHAN_CT2I   13
#define HDCHAN_CT3I   14
#define HDCHAN_CT4I   15
#define HDCHAN_SSA1T2 16
#define HDCHAN_SSA2T2 17
#define HDCHAN_SSA3T2 18
#define HDCHAN_SSA4T2 19
#define HD_NUM_SYNC_SOURCES  20
#define HD_NUM_SYNC_LINES     8

/* Capture pixel modes for storing in the RSET - this
 * way RSET_Set can correctly set the RGB/Monochrome capture
 * mode as specified in the CHP file.  Store in HPR_PIXELMODE
 */
#define HDCAPTURE_RGB  (0)
#define HDCAPTURE_MONO (1)

/* Channel designations for Accustream SDTV Inputs */
/* these are store in RSET.lREGS[HPR_SDTV_IN]  */
/* HIRES is the high resolution input */
#define HDCHAN_HIRES     0
#define HDCHAN_COMPOSITE 1
#define HDCHAN_SVIDEO    2
// auto detect modes
#define ADV7401_NTSC     0
#define ADV7401_PAL      1
#define ADV7401_SECAM    3

typedef struct tagIColorChannel {
  RSET *pRSet;
  char szChannel[16];
} I_Color_Channel;

typedef struct tagUpdateRegister {
  BYTE bRegister;
  BYTE bValue;
  RSET *pRSet;
} UpdateRegister;

typedef struct tagUpdateRegisterSet {
  BYTE *pKS0127Registers;
  RSET *pRSet;
} UpdateRegisterSet;

typedef struct tagUpdateAD9884RegisterSet {
  BYTE *pAD9884Registers;
  RSET *pRSet;
} UpdateAD9884RegisterSet;

typedef struct tagUpdateAD9888RegisterSet {
  BYTE *pAD9888Registers;
  RSET *pRSet;
} UpdateAD9888RegisterSet;

typedef struct tagUpdateAD9887RegisterSet {
  BYTE *pAD9887Registers;
  RSET *pRSet;
} UpdateAD9887RegisterSet;

typedef struct tagUpdateVideoSetting {
  BYTE bValue;
  RSET *pRSet;
} UpdateVideoSetting;

typedef struct tagUpdateVideoLong {
  long lValue;
  RSET *pRSet;
} UpdateVideoSettingLong;

typedef struct tagUpdateFormatSetting {
  char  szFormat[7];
  RSET *pRSet;
} UpdateFormatSetting;

typedef struct tagFsiCableList {
  int iNumEntries;
  char **pList;
} FsiCableList;

typedef struct tagIRGBLookUpTables {
  BYTE *pRedLUT;
  BYTE *pGreenLUT;
  BYTE *pBlueLUT;
} IRGBLookUpTables;

typedef struct tagBoardIDString {
  int nType;
  char *pIDString;
} BoardIDString;



/*
// Defines for rset element [HPR_REFPROG].
*/
#define HCHPREFP_APP_0     0
#define HCHPREFP_APP_1     1
#define HCHPREFP_APP_2     2
#define HCHPREFP_AS_TOOL   3
#define HCHPREFP_AS_DEMO   4
#define HCHPREFP_AUTOSYNC  5
#define HCHPREFP_SETUP     6
/*
// Defines for rset element [HPR_PASSMODE].
*/
#define HD_PMODE_SINGLE    0
#define HD_PMODE_DUAL      1
#define HD_PMODE_DUALODD   2
/*
// Defines for rset element [HPR_INTERL].
*/
#define HD_INTERL_PROG             0
#define HD_INTERL_ON               1
#define HD_INTERL_ODDUP            2
#define HD_INTERL_ON_EVENODD       3
#define HD_INTERL_ODDUP_EVENODD    4
/*
// Defines for rset element [HPR_FLAGS].
*/
#define HPR_FLAGS_YPBPR        0x0001
#define HPR_FLAGS_ROTATE_LEFT  0x0010
#define HPR_FLAGS_ROTATE_RIGHT 0x0100
#define HPR_FLAGS_DISABLE_DVI_CABLE_EXTENDER 0x1000
#define HPR_FLAGS_DISABLE_HDMI_CABLE_EXTENDER 0x2000

/*
// Defines for IRGB170 - DVI modes
*/
#define HD_ACTIVE_INTERFACE_ANALOG      (0x00)
#define HD_ACTIVE_INTERFACE_DIGITAL     (0x01)
#define HD_ACTIVE_INTERFACE_UNSPECIFIED (0x02)
#define HD_DIGITAL_INTERFACE_DETECTED (0x08)
#define HD_ANALOG_HSYNC_DETECTED      (0x80)
#define HD_ANALOG_CSYNC_DETECTED      (0x40)
#define HD_ANALOG_VSYNC_DETECTED      (0x20)
// Jack 01-04-10...
#define HD_DIGITAL2_SYNC_DETECTED     (0x100)
#define HD_ACTIVE_INTERFACE_DIGITAL2   (0x04)
#define HD_SDI_SYNC_DETECTED           (0x200)
#define HD_ACTIVE_INTERFACE_SDI        (0x400)
// ...Jack 01-04-10
#define HD_DIGITAL_SYNC_DETECTED      (0x10)

typedef enum {
  LVM_PAUSE = 0,
  LVM_RUN,
  LVM_SNAP,
  LVM_STOP,
  LVM_TRIGGER_RUN,
  LVM_TRIGGER_STOP,
  LVM_TRIGGER_RUN_STOP,
  LVM_SUSPEND,
  LVM_RESTART,
  LVM_TRIGGER2_RUN,
  LVM_TRIGGER2_STOP,
  LVM_TRIGGER2_RUN_STOP
} LVM_;

#define TRIGGER_ON_FALLING_EDGE 0
#define TRIGGER_ON_RISING_EDGE  1

#endif /* #ifndef _INC_HDP_LIB_U */


/*
//=====================================================================
//============= Start of Library Environment Definitions ==============
//=====================================================================
*/
#ifndef _INC_HDP_LIB
#define _INC_HDP_LIB

#include <stdio.h>
#include "gen_def.h"
#if !defined(UNIX)
#include <winioctl.h>
#endif


#define HPE_NULL  ((HPE_CALLBACK)0)
#define HCBE_NULL ((HCB_ERROR)0)
#define HPOD_NULL ((HP_OpenData *)NULL)

#if defined( WINDLL ) || defined ( _WIN )
typedef void (EXPORT CALLBACK *HPE_CALLBACK)
  (BoardHandle,ERRTYPE,char far *);
typedef void (EXPORT CALLBACK *HCB_ERROR)
  (void *,BoardHandle,ERRTYPE,const char far *);
#else
#if !defined (WCC)
typedef void (far *HPE_CALLBACK)(BoardHandle,ERRTYPE,char *);
typedef void (far *HCB_ERROR)(void *,BoardHandle,ERRTYPE,const char *);
#else
typedef void (*HPE_CALLBACK)(BoardHandle,ERRTYPE,char *);
typedef void (*HCB_ERROR)(void *,BoardHandle,ERRTYPE,const char *);
#endif
#endif

/*
//  Codes for access to "BoardInfo" fields.
//
//  Note to Library Developers:
//    Do not change the HPEE_HD2_POTS value.  "0x8000" is used
//   explicitly in the HI*DEF II compatibility library to access
//   the settings of the simulated potentiometers.
*/
#define HPEE_PORT_ASC   0x0001
#define HPEE_PORT_BIN   0x0002
#define HPEE_PIXEL_BITS 0x0003
#define HPEE_BOARD_TYPE 0x0004
#define HPEE_PROM       0x00FF
#define HPEE_FORMAT_REV 0x1000
#define HPEE_PARTNO     0x2000
#define HPEE_BOARD_REV  0x3000
#define HPEE_SERIAL_NO  0x4000
#define HPEE_PHASE_ADJ  0x5000
#define HPEE_OVENS      0x5100
#define HPEE_HD2_POTS   0x8000
#define HPEE_RGB_POTS   0x9000
#define HPEE_EOD        0xFF00

/*
// Structure required for nHP_Open();
*/
#define HPO_NULL \
  {HPOD_NULL,HPE_NULL,HCBE_NULL,(void *)NULL,0,{0,0,0},0xFFFF,FNULL}

typedef struct _tag_HP_OpenData {
  struct _tag_HP_OpenData *psParent;   /* Ptr to previous Open Data. */
  HPE_CALLBACK fpcbHPE;      /* Error handling callback routine.     */
  HCB_ERROR pcbERR;          /* Revised error handling callback.     */
  void     *pObjError;       /* Object (or data) pointer for pcbERR()*/
  ERRTYPE  eError;           /* Last error code.                     */
  ERRTYPE  eErrorList[3];    /* Previous 3 error codes generated.    */
  ERRTYPE  eLibMax;          /* Max error # to library message list. */
  FILE     *fpErrSrc;        /* App message file (used if e>eLibMax).*/
} HP_OpenData;

/*
// Defines and structure for module memalloc.c
*/
#define HDAL_DEFRAG         0x000
#define HDAL_DEFRAG_STOP    0x001
#define HDAL_DEFRAG_MASK    0x001
#define HDAL_SYNCHR_OFF     0x000
#define HDAL_SYNCHR_ON      0x002
#define HDAL_DISABLE_ROTATE 0x010

/*
// Defines for the wControl in the eHD_* data transfer commands:
//
//  HDXFR_PIXEL_*    Specifies whether the transfer is 8 or 10 bits
//                  per pixel.  The default is ten bits.
//  HDXFR_FGMEM_*    Specifies whether the contents of the Frame
//                  Grabber (FG) memory associated with this image
//                  handle must be preserved after this call.  The
//                  default is to preserve.  Alternatively, it can be
//                  released so that FG memory may have an opportunity
//                  to defragment.
*/
// Retained for compatibility
// Tried to swap HDXFR_PIXEL_8 and HDXFR_PIXEL_10 from above to match
// old BPP_8 and BPP_10 in case NULL instead of symbol passed to functions.
// This caused IdeaDemo to stop working.
// Needs to be fixed in the driver basepci.h and in new 8 and 10 constants below.
#define HDXFR_PIXEL_8      0x0001 // 8 and 10 swapped from above to match
#define HDXFR_PIXEL_10     0x0000 // old BPP_8 and BPP_10 in case NULL passed to functions.
#define HDXFR_PIXEL_YUV    0x0002
#define HDXFR_PIXEL_RGB    0x0004
#define HDXFR_FGMEM_RETAIN 0x0000
// New
#define HDXFR_PIXEL_MONO_8      0x0001
#define HDXFR_PIXEL_YONLY_8     0x0001
#define HDXFR_PIXEL_MONO_10     0x0000
#define HDXFR_PIXEL_YCBCR422_16 0x0000
#define HDXFR_PIXEL_YUVGRAY_16  0x0002
#define HDXFR_PIXEL_RGB555_16   0x0003
#define HDXFR_PIXEL_RGB888_32   0x0004
#define HDXFR_PIXEL_RGB888_24   0x0005
#define HDXFR_PIXEL_RGB565_16   0x0006
#define HDXFR_PIXEL_RED_8       0x0008
#define HDXFR_PIXEL_GREEN_8     0x0009
#define HDXFR_PIXEL_BLUE_8      0x000a
// Jack 08-11-08...
#define HDXFR_PIXEL_YCBCR444_32 0x000b
#define HDXFR_PIXEL_YCBCR444_24 0x000c
// ...Jack 08-11-08
//+Joe 08.26.2010 - add swapped red/green xfers
#define HDXFR_PIXEL_BGR888_24 0x00d
#define HDXFR_PIXEL_BGR888_32 0x00e
#define HDXFR_PIXEL_BGR555_16 0x00f
#define HDXFR_PIXEL_MASK        0x000F

#define HDXFR_YMODE_DIB    0x0100
#define HDXFR_YMODE_SKIP   0x00F0
#define HDXFR_YMODE_SKIP_SHIFT 4
#define HDXFR_YMODE_DI_ODD  0x1000
#define HDXFR_YMODE_DI_EVEN 0x2000
#define HDXFR_XMODE_SKIP    0x0200
#define HDXFR_SCALE         0x4000
#define HDXFR_NO_DMA        0x8000

/*
//  Defines for the wControl argument to routines eHD_RSET_Set() and
// eHD_SetChannel():
//
//  HDSET_SYNCHR_OFF (the default) Specifies that the RSET data will be
//                  retained for use with the next board operation(s)
//                  using this image handle.  The board will not
//                  immediately (runtime-sychronously) be set according
//                  to this RSET.
//  HDSET_SYNCHR_ON  Specifies that in addition to retaining the RSET
//                  data for later use, the board will be immediately
//                  set with this RSET data.  The set up operation will
//                  be completed before eHD_RSET_Set() returns (ie,
//                  synchronously).
//                   Specifying HDSET_SYNCHR_ON is the prefered way of
//                  preparing to use bHP_CSyncDetect() when image
//                  handles are in use.  The setting will remain in
//                  effect until a board operation such as SET or GRAB
//                  is performed or until all image handles on the
//                  board have been deallocated.
*/
#define HDSET_SYNCHR_OFF   0x0000
#define HDSET_SYNCHR_ON    0x0002
#define HDSET_SET_ALWAYS   0x0004
#define HDSET_IGNORE_ERRORS   0x0008

/*
// Defines and structures for pHD_ModeList().
//
//  The first set of defines list the HI*DEF hardware configuration
// modes (MODE_HD*) and specify how many of these modes there are
// (MODE_HIDEF_N).
//  The second set provides symbols for certain mode combinations.
*/
#define MODE_HDP_1PASS 0
#define MODE_HDP_2PASS 1
#define MODE_HD4_1PASS 2
#define MODE_HD4_2PASS 3
#define MODE_HDI25     4
#define MODE_HDI50     5
#define MODE_HDI60     6
#define MODE_HDI75     7 
#define MODE_HDIRGB25  8
#define MODE_HDIRGB50  9
#define MODE_HDIRGB75  10
#define MODE_HDIRGB165 11
#define MODE_HDIRGB200 12
#define MODE_HDIRGB170 13
#define MODE_HDIRGBI64_170 14
#define MODE_HDACCUSTREAM50 15
#define MODE_HDACCUSTREAM75 16
#define MODE_HDACCUSTREAMVDR 17
#define MODE_HDACCUSTREAM205A 18
#define MODE_HDACCUSTREAM50A 19
#define MODE_HDACCUSTREAM75A 20
#define MODE_HDACCUSTREAM170PLUS 21
#define MODE_HDACCUSTREAM75PLUS 22
#define MODE_HDACCUSTREAM50PLUS 23
#define MODE_HDACCUSTREAMEXPRESS_170 24
#define MODE_HDACCUSTREAMEXPRESS_50 25
#define MODE_HDACCUSTREAMEXPRESS_75 26
#define MODE_HDACCUSTREAMEXPRESSHD 27
#define MODE_HDACCUSTREAMEXPRESSHD50 28
#define MODE_HDACCUSTREAMEXPRESSHD75 29
#define MODE_HDACCUSTREAMEXPRESSHDC 30
#define MODE_HDACCUSTREAMEXPRESSHDC50 31
#define MODE_HDACCUSTREAMEXPRESSHDC75 32
#define MODE_HDACCUSTREAMEXPRESS1000 33
#define MODE_HDACCUSTREAMEXPRESS1000_75 34
#define MODE_HDACCUSTREAMEXPRESS1000_SDI 35
#define MODE_HDACCUSTREAMEXPRESS2000 36
#define MODE_HDACCUSTREAMEXPRESS2000_75 37
#define MODE_HDACCUSTREAMEXPRESS2000_SDI 38

#define MODE_HIDEF_N 39
#define MODES_HDP     (MODE_HIDEF_N+0)
#define MODES_HD4     (MODE_HIDEF_N+1)
#define MODES_HIDEF_N (MODE_HIDEF_N+2)
typedef struct {
  char       szModeName[32];  /* Title of mode.                     */
  HIDEF_TYPE hdt;             /* HI*DEF board type.                 */
  short      nPasses;         /* 1: single pass; 2: dual pass mode. */
  DWORD      dwaHFreq[10];    /* Horz. freq. ranges (zero-filled).  */
  DWORD      dwaPFreq[8];     /* Pixel freq. ranges (zero-filled).  */
  DWORD      dwaMinHBPns[8];  /* Min. HBS nSec for each HFreq range.*/
  SDWORD       lHTotMin;        /* Minimum horizontal total.          */
  SDWORD       lHTotMax;        /* Maximum horizontal total.          */
  SDWORD       lHTotRes;        /* Horizontal total resolution.       */
  SDWORD       lHFP_nSecMin;    /* Min. horz. front porch (nSec).     */
  SDWORD       lHFPPixelMin;    /* Min. horz. front porch (pixels).   */
  SDWORD       lHBl_nSecMin;    /* Min. horz. blanking time (nSec).   */
  SDWORD       lHBlPixelMin;    /* Min. horz. blanking time (pixels). */
  SDWORD       lHBS_nSecMin;    /* Min. HSync + back porch (nSec).    */
  SDWORD       lHBSPixelMin;    /* Min. HSync + back porch (pixels).  */
  SDWORD       lWidthMax;       /* Maximum image width.               */
  SDWORD       lWidthRes;       /* Image width resolution.            */
  SDWORD       lPitchMax;       /* Maximum pitch.                     */
  SDWORD       lPitchRes;       /* Pitch resolution.                  */
  SDWORD       lFGCapacity;     /* Frame grabber capacity (pixels).   */
  SDWORD       lPD_pSec;        /* Approx. phase delay unit (in pSec).*/
  SDWORD       lMinPixDetHF;    /* Min. HFreq for A/S pixel detection.*/
  SDWORD       lVideo_mVMin;    /* Min. video (non-sync) gain (100uV).*/
  SDWORD       lVideo_mVMax;    /* Max. video (non-sync) gain (100uV).*/
  SDWORD       lBLevelMin;      /* Min. Black Level (gain mV/10000).  */
  SDWORD       lBLevelMax;      /* Max. Black Level (gain mV/10000).  */
  SDWORD       lBPAdjustment;   /* Adj to BP to normallize delay from board to board, 
                                 also the dual-buffer adjustment */ 
  SDWORD       lSBPAdjustment;  /* Adj to BP in Single Buffer mode */
// Jack 10-28-08...
  SDWORD       lSyncDelayDeltaCS; /* Composite Sync delay difference in ns from */
                                /* AccuStream 170 to normallize delay from board to board. */
  long       lSyncDelayDeltaSS; /* Separated Sync delay difference in ns from */
                                /* AccuStream 170 to normallize delay from board to board. */
// ...Jack 10-28-08
} HD_MODE;
typedef struct {
  short      nSize;           /* Size (bytes) of this structure.    */
  WORD       wRev;            /* The library revision number.       */
  short      nModes;          /* The number of modes (MODE_HIDEF_N).*/
  short      nModeSize;       /* Size of each HD_MODE structure.    */
  HD_MODE    hm[MODE_HIDEF_N];/* A description of each mode.        */
} HD_MODELIST;


/*
//  The maximum wait time (in milliseconds) that the HDPL library uses
// for most video operations.  This is a useable parameter to routine
// eHP_CommandReady() and eHP_CommandDone().
//  Note that a normal time-out period for a grab or set command should
// be three times the frame period plus 3 milliseconds.  For example,
// RS-170 frame period 33.33msec, so a good minimum timeout period for
// RS-170 grabs and sets would be 103.
*/
#define VIDEOTIMEOUT   3075

/*
//  Command codes for parameter for routines eHP_Command() and
// eHP_Command().
*/
#define HPCMD_IDLE          0x00
#define HPCMD_SET           0x01
#define HPCMD_GRAB          0x02
#define HPCMD_XFER          0x03
#define HPCMD_ALT_IDLE      0x04
#define HPCMD_CONT_SET      0x05
#define HPCMD_CONT_GRAB     0x06
#define HPCMD_CONT_XFER     0x07
#define HPCMD_GRAB_ON_CT4R  0x0A
#define HPCMD_GRAB_ON_TRIG  0x0A
#define HPCMD_GRAB_ON_CT4F  0x0E
#define HPCMD_GRAB_TO_CT4R  0x12
#define HPCMD_GRAB_TO_CT4F  0x16


/*
//  Declarations and definitions for LUT Handles.  There are two static
// LUT's.  The first converts 0-255 to 0-255.  The second converts
// 0-255 to 255-0.
*/
typedef long HD_LUTHandle;
#define HDLUT_BASE      0x00000100
#define HDLUT_BYPASS    0x00000000
#define HDLUT_NEUTRAL   (HDLUT_BASE+0)
#define HDLUT_INVERTED  (HDLUT_BASE+1)
#define HDLUT_APPBASE   (HDLUT_BASE+2)

/*
//=====================================================================
// Live Video declarations.
//=====================================================================
*/

#define HDLV_DEST_VOID     0
#define HDLV_DEST_VIRTUAL  1
#define HDLV_DEST_VPE      2
/*
// nFlag setting for routine eHD_InitLiveVideo().
*/
#define HDLV_DEFRAG       HDAL_DEFRAG
#define HDLV_DEFRAG_STOP  HDAL_DEFRAG_STOP
#define HDLV_DEFRAG_MASK  HDAL_DEFRAG_MASK
/*
// Options for using the DMA header are:
//   HDLV_DMA_HDR_NONE:  There is no DMA header in use.
//   HDLV_DMA_HDR_MARK:  Mark the DMA header with the field number once
//                     the transfer image data from a new frame has
//                     completed.
//   HDLV_DMA_HDR_CHECK: Before transfering data into the buffer, check
//                     the header to make certain that the buffer is
//                     available for receiving data.  If it is not, the
//                     Live Video operation terminates.  If it is, the
//                     new frame number is recorded to the header, the
//                     DMA is performed, and finally, the high-order
//                     bit in the header is set to indicate that the
//                     buffer is available.
*/
#define HDLV_DMA_HDR_MASK   0x00C
#define HDLV_DMA_HDR_NONE   0x000
#define HDLV_DMA_HDR_MARK   0x004
#define HDLV_DMA_HDR_CHECK  0x008

typedef struct {
  DWORD   dwFrameN;
  void    *pBuffer;
  DWORD   dwBufferN;
// Jack 11-26-08...
// Pad structure to guarantee QWORD alignment regardless of OS
  void    *pReserved;
// ...Jack 11-26-08
} HDVID_HEADER;

typedef struct {
  ImageHandle ih;      /* Capture from this image handle.           */
  int         nCycle;  /* <2: DMA every frame; >1: DMA every Nth.   */
  int         nPhase;  /* 0<=nPhase<nCycle.  Which Nth frame to DMA.*/
  int         nDest;   /* Destination is HDLV_DEST_*.               */
  union {
    struct {
      void    *pDest;   /* Destination address.                      */
      int     nMode;   /* Pixel type: HDXFR_PIXEL_* (10, 8, or YUV).*/
                       /* Y modes HDXFR_YMODE_DIB, HDXFR_YMODE_SKIP */
      int     nPitch;   /* If >=4, Destination buffer pitch.         */
      int     nFlags;   /* Information about how header is used.     */
      HDVID_HEADER *pHdr;/* Ptr to buffer arbitration header or NULL.*/
      HD_LUTHandle hLUT;/* If >HDLUT_NEUTRAL, the DMA LUT to be used.*/
    } DMA;
    struct {
      int     nType;    /* 0:VIP; 1:VMI.                             */
      int     mSyncPol; /* Other TBD VPE parameters.                 */
    } VPE;
  } u;
  long        nLft, nTop, nRgt, nBot; /* Frame coordinates to DMA.  */
} HD_LIVEVIDEO;
/*
// Memory marker definitions.
*/
#define HDVID_FRAME_NUMBER_MASK    (0x0000FFFF)
#define HDVID_READY_FRAME_FLAG     (0x80000000)
#define HDVID_DROPPED_FRAME_FLAG   (0x40000000)
#define HDVID_SIGNALLED_FRAME_FLAG (0x01000000)
#define HDVID_REPORTED_FRAME_FLAG  (0x02000000)
#define HDVID_MESSAGE_FRAME_FLAG   (0x04000000)

#if defined (LINUX)
#define HD_LVBUF_RELEASE(p)   (p)->dwFrameN=0
#define HD_LVBUF_SET_READY(p) ((p)->dwFrameN|=HDVID_READY_FRAME_FLAG)
#define HD_LVBUF_READY(p)     (((p)->dwFrameN&HDVID_READY_FRAME_FLAG)!=0)
#define HD_LVBUF_EMPTY(p)     (((p)->dwFrameN)==0)
#define HD_LVBUF_SIGNAL(p)    (p)->dwFrameN|=HDVID_SIGNALLED_FRAME_FLAG
#define HD_LVBUF_SIGNALLED(p) (((p)->dwFrameN&HDVID_SIGNALLED_FRAME_FLAG)!=0)
#define HD_LVBUF_REPORT(p)    (p)->dwFrameN|=HDVID_REPORTED_FRAME_FLAG
#define HD_LVBUF_REPORTED(p) (((p)->dwFrameN&HDVID_REPORTED_FRAME_FLAG)!=0)
#define HD_LVBUF_DROPPED_FRAME(p) (((p)->dwFrameN&HDVID_DROPPED_FRAME_FLAG)!=0)
#define HD_LVBUF_FRAME_NUMBER(p)   ((p)->dwFrameN&HDVID_FRAME_NUMBER_MASK)
#else

#ifndef InterlockedOr
LONG FORCEINLINE InterlockedOr(LONG volatile *Destination,LONG Value)
{
  LONG Old;

  do
  {
    Old=*Destination;
  } while(InterlockedCompareExchange(Destination,(Old|Value),Old)!=Old);
  
  return(Old);
}
#endif //InterlockedOr

#define HD_LVBUF_RELEASE(p) (InterlockedAnd((long*)(&(p)->dwFrameN), 0))
#define HD_LVBUF_SET_READY(p) (InterlockedOr((long*)(&(p)->dwFrameN), HDVID_READY_FRAME_FLAG))
#define HD_LVBUF_SET_MESSAGE_READY(p) (InterlockedOr((long*)(&(p)->dwFrameN), HDVID_READY_FRAME_FLAG | HDVID_MESSAGE_FRAME_FLAG))
#define HD_LVBUF_READY(p)     (((p)->dwFrameN&HDVID_READY_FRAME_FLAG)!=0)
#define HD_LVBUF_EMPTY(p)     (((p)->dwFrameN)==0)
#define HD_LVBUF_SIGNAL(p)    (InterlockedOr((long*)(&(p)->dwFrameN), HDVID_SIGNALLED_FRAME_FLAG))
#define HD_LVBUF_SIGNALLED(p) (((p)->dwFrameN&HDVID_SIGNALLED_FRAME_FLAG)!=0)
#define HD_LVBUF_REPORT(p)    (InterlockedOr((long*)(&(p)->dwFrameN), HDVID_REPORTED_FRAME_FLAG))
#define HD_LVBUF_REPORTED(p) (((p)->dwFrameN&HDVID_REPORTED_FRAME_FLAG)!=0)

#define HD_LVBUF_DROPPED_FRAME(p) (((p)->dwFrameN&HDVID_DROPPED_FRAME_FLAG)!=0)
#define HD_LVBUF_FRAME_NUMBER(p)   ((p)->dwFrameN&HDVID_FRAME_NUMBER_MASK)
#endif

// For new control call to get the video header list
typedef struct tagVideoHeaderList
{
  int nNumHeaders;
  HDVID_HEADER *pHeaders;
} VideoHeaderList;

/*
// Snap Control structure for eHD_SnapToBuffer
*/
typedef struct {
  size_t  stSize;      /* size of this structure */
  BYTE   *pRecvBuf;    /* Buffer to receive the image */
  WORD    wFormat;     /* HDXFR_PIXEL_8, HDXFR_PIXEL_RGB etc. */
                       /* could also include HDXFR_YMODE_DIB, and YSKIP */
  HANDLE  hCompleteEvent;    /* App defined event handle */
	union
	{
		int     nDecimateX;         /* Decimation factor, X direction */
		int     nDestinationWidth;  /* Width to scale output to if hardware scaler */
  };
	union
	{
		int     nDecimateY;  /* Decimation factor, Y direction */
		int     nDestinationHeight;  /* Height to scale output to if hardware scaler */
	};
  long    hLUT;        /* LUT handle */
  union
  {
    BOOL   bTrigger;    /* 0 = No trigger, 1 = Wait on external trigger */
    int    nTrigger;    /* 0 = No trigger, 1 = Primary trigger, 2 = Secondary trigger */
  };
  int     nTrigType;   /* trigger on: 1=rising edge, 0=falling edge */
  ERRTYPE eRetCode;    /* error code returned */
  RECT    *pAOIRect;   /* Point to an AOI rectangle, in absolute coordinates */
} SNAP_CONTROL;

/*
//  Structure used by the WhiteBalance functions.
*/
typedef struct {
  DWORD  dwMode;                /* 0=Use new values, 1=Restore orig    */
  DWORD  dwCtrlFlags;           /* 0=Default, 0x1 = YPbPr video        */
  BYTE   byOrigRedGainCalib;    /* Orig red gain calib value           */
  BYTE   byOrigGreenGainCalib;  /* Orig green gain calib value         */
  BYTE   byOrigBlueGainCalib;   /* Orig blue gain calib value          */
  BYTE   byOrigRedOffsetCalib;  /* Orig red offset calib value         */
  BYTE   byOrigGreenOffsetCalib;/* Orig green offset calib value       */
  BYTE   byOrigBlueOffsetCalib; /* Orig blue offset calib value        */
  BYTE   byOrigRedGainReg;      /* Orig red gain register value        */
  BYTE   byOrigGreenGainReg;    /* Orig green gain register value      */
  BYTE   byOrigBlueGainReg;     /* Orig blue gain register value       */
  BYTE   byOrigRedOffsetReg;    /* Orig red offset register value      */
  BYTE   byOrigGreenOffsetReg;  /* Orig green offset register value    */
  BYTE   byOrigBlueOffsetReg;   /* Orig blue offset register value     */
  BYTE   byRedGainCalib;        /* New red channel gain calib value    */
  BYTE   byGreenGainCalib;      /* New green channel gain calib value  */
  BYTE   byBlueGainCalib;       /* New blue channel gain calib value   */
  BYTE   byRedOffsetCalib;      /* New red channel offset calib value  */
  BYTE   byGreenOffsetCalib;    /* New green channel offset calib value*/
  BYTE   byBlueOffsetCalib;     /* New blue channel offset calib value */
  int    nCurrentStep;          /* Current step in white balance       */
  int    nTotalSteps;           /* Total # of steps to perform         */
} HD_WBALDATA;

/*
//=====================================================================
// Video Port declarations.
//=====================================================================
*/
typedef enum {
  HD_VP_OFF, HD_VP_VIP, HD_VP_VMI
} HD_VPType;

typedef struct {
  HD_VPType  VPType;           /* VIP, VMI, or OFF           */
  BYTE       byHSPolarity;     /* Horizontal Sync Polarity   */
  BYTE       byVSPolarity;     /* Vertical Sync Polarity     */
  BYTE       byActivePolarity; /* Active Polarity            */
  BYTE       byFieldPolarity;  /* Field Polarity             */
  DWORD      dwHRefDelay;      /* Horizontal Reference Delay */
  DWORD      dwVRefDelay;      /* Vertical Reference Delay   */
  HD_LUTHandle hVPLUT;         /* LUT handle for Video Port  */
} HD_VPInfo;


//+04.27.2007 remove obsolete struct pointer
//+ this should flag an obvious error to any application that
//+ still attempts to use it.
typedef struct {
  BYTE *pVPInfo;
} HD_SSConfig;


typedef enum tagIOLines { IO_0, IO_1, IO_2, IO_3, IO_4, IO_5, IO_6, IO_7, DIGITAL_OUT } IOLines;
typedef enum tagIOLevel { IO_High, IO_Low, IO_Toggle } IOLevel;
#define IO_BLOCK    (-1)
#define IO_NO_BLOCK (-2)
typedef struct tagSetIO
{
  IOLines Line;
  IOLevel Level;
  int     Duration;
  int     Action;
} SetIO;

/*
//  On the HI*DEF Plus, the gain register can be set for an analogue
// signal voltage range of 600 to 2500 millivolts.  600 represents
// the minimum gain specification and produces a maximal gain register
// setting.  2500 represents the maximum gain specification and
// produces a minimal gain register setting.
*/
#define HDP_MINSIG_MV 600
#define HDP_MAXSIG_MV 2500



// Defines for bFieldUpdate, TRUE, FALSE, and
#define DI_ODD  (0x40) /* de-interlace, using odd field */
#define DI_EVEN (0x80) /* de-interlace, using even field */

typedef struct STREAMING_STATUS_Tag
{
  int           nState;
	DWORD         dwTotalFrames;
	DWORD         dwDroppedFrames;
	DWORD         dwFramesPerSecond;
	DWORD         dwTotalMilliseconds;
	ULONGLONG     qwTotalBytes;
  ULONGLONG     qwBytesPerSecond;
  void          *pBufferToRelease;   
} STREAMING_STATUS;

typedef void (CALLBACK *P_STREAMING_STATUS_CALLBACK)( void *pContext, STREAMING_STATUS *pStatus );

typedef struct {
  DWORD  dwSize;
  void   *pDestination;
  DWORD  dwPitch;
  BOOL   bFieldUpdate;
  // For hardware scaler on Express and newer boards
  // Values greater than 16 taken as output width
  union
  {
		int    nDecimateX;
		int    nDestinationWidth;
  };
  // For hardware scaler on Express and newer boards
  // Values greater than 16 taken as output height
  union
  {
		int    nDecimateY;
		int    nDestinationHeight;
  };
  int    nDecimateFrames;
  long   hLUT;
  int    nTop;
  int    nBottom;
  int    nLeft;
  int    nRight;
  int    nMode;

  // Structure Extension - entries can be set to zero to disable
  void        **pBufferList;
  DWORD         dwNumberOfBuffers;
  int           nDataType;
  BOOL          bDIBTarget;
  HDVID_HEADER *pVidHeaders;
  HANDLE        hBufferEvent;
  HANDLE        hStartEvent;
  HANDLE        hStopEvent;
  HANDLE        hErrorEvent;
	P_STREAMING_STATUS_CALLBACK pfnStatusCallback;
	void          *pStatusCallbackContext;
  int           nFrameRate;                         // Output frame rate
} LIVEDISPLAY_INFO;

typedef struct {
  DWORD         dwSize;            // Size of this structure
  void        **pBufferList;
  DWORD         dwNumberOfBuffers;
  int           nDataType;
  BOOL          bDIBTarget;
  // For hardware scaler on Express and newer boards
  // Values greater than 16 taken as output width
  union
  {
		int    nDecimateX;
		int    nDestinationWidth;
  };
  // For hardware scaler on Express and newer boards
  // Values greater than 16 taken as output height
  union
  {
		int    nDecimateY;
		int    nDestinationHeight;
  };
  int           nDecimateFrames;
  BOOL          bFieldUpdate;
  long          hLUT;
  int           nTop;
  int           nBottom;
  int           nLeft;
  int           nRight;
  int           nMode;
  HD_LIVEVIDEO *pLiveVideoDescriptors;
  HDVID_HEADER *pVidHeaders;
  HANDLE        hBufferEvent;
  HANDLE        hStartEvent;
  HANDLE        hStopEvent;
  HANDLE        hErrorEvent;

  // Structure Extension 3.4.1
  char         *pszAVIFileName;                     // File name to stream to - no need to handle streaming events
  ULONGLONG     qwMemoryBytesForBuffering;          // Bytes of system memory to use for buffering
  int           nPercentFullToStartDroppingFrames;  // Threshold in system buffer space used before frame dropping begins
	P_STREAMING_STATUS_CALLBACK pfnStatusCallback;
	void          *pStatusCallbackContext;
  int           nFrameRate;                         // Output frame rate

  // Structure Extension 3.4.2
  DWORD         dwTriggerStartFrameDelay;           // Frame count after start trigger where frame delivery begins
  DWORD         dwTriggerStopFrameDelay;            // Frame count after stop trigger where frame delivery ends

  // Structure Extension 3.5.1
  DWORD         dwGpuType;                          // IDEA_GPU_NONE, IDEA_GPU_NVIDIA, IDEA_GPU_AMD
  DWORD         dwGpuRenderType;                    // IDEA_RENDERER_OPENGL, IDEA_RENDERER_DX9, IDEA_RENDERER_DX11, IDEA_RENDERER_CUDA
  BOOL          bUseTexture;                        // Use a texture or a buffer
} LIVESTREAM_INFO;


typedef struct COMPRESSION_INFO_Tag{
  DWORD  dwSize;
  char   *pszFileName;
  void   **pBufferList;
  DWORD  dwNumberOfBuffers;
  DWORD  dwBufferSize;
  HDVID_HEADER *pHeaders;
  int    nDataType;
  int    nTop;
  int    nBottom;
  int    nLeft;
  int    nRight;
	int    nDestinationWidth;
	int    nDestinationHeight;
  int    nMode;

  HANDLE        hBufferEvent;
  HANDLE        hStartEvent;
  HANDLE        hStopEvent;
  HANDLE        hErrorEvent;
  char          szH264Profile[16];
  char          szH264Level[16];
  BOOL          bH264VariableBitRate;
  DWORD         dwH264KBitRate;
  DWORD         dwH264MinKBitRate;
  DWORD         dwH264AverageKBitRate;
  DWORD         dwH264MaxKBitRate;
  DWORD         dwOutputFrameRate;
	P_STREAMING_STATUS_CALLBACK pfnStatusCallback;
	void          *pStatusCallbackContext;

  // Structure Extension 3.4.1
  char          szH264GopStructure[16];
  BOOL          bH264OpenGop;
  DWORD         dwH264CodedPictureBufferDelay;
  BOOL          bH264DisableTimebaseCorrector;

} COMPRESSION_INFO;


typedef enum {
  IDEA_TYPE_MONO_8,
  IDEA_TYPE_MONO_10,
  IDEA_TYPE_MONO_16,
  IDEA_TYPE_YONLY_8,
  IDEA_TYPE_YONLY_16,
  IDEA_TYPE_YCBCR_16,
  IDEA_TYPE_RGB_32,
  IDEA_TYPE_RGB555_16,
  IDEA_TYPE_RGB565_16,
  IDEA_TYPE_RGB_24,
  IDEA_TYPE_BGR_24,
  IDEA_TYPE_BGR555_16,
  IDEA_TYPE_BGR_32
} IDEA_TYPE;

typedef enum {
	IDEA_INPUT_TYPE_UNDEFINED = 0,
	IDEA_INPUT_TYPE_HDMI,
	IDEA_INPUT_TYPE_DVI,
	IDEA_INPUT_TYPE_ANALOG,
	IDEA_INPUT_TYPE_SVIDEO_COMPOSITE
} GENERAL_VIDEO_INPUT_TYPE;


/*
  Structure for GetInfoStruct(BoardHandle, InfoEvent* pInfo);
*/

typedef struct {
  DWORD   dwSize;
  HANDLE  hInfoEvent;
  void    *pInfoObject;
  DWORD   dwInfoCode;
  DWORD   dwInfoExtra;
  char    szInfo[256];
  BOOL    bNewInfo;
  BOOL    bFirstEvent;
} IDEA_INFO;

typedef enum {
  IDEA_INFO_NONE,
  IDEA_INFO_ERROR,
  IDEA_INFO_WARNING,
  IDEA_INFO_CONNECTION,
  IDEA_INFO_SYNC,
  IDEA_INFO_LOCK,
  IDEA_INFO_DROPPED_FRAMES,
  IDEA_INFO_TRIGGER_START,
  IDEA_INFO_TRIGGER_STOP,
  IDEA_INFO_VESA_SCAN_READY,
  IDEA_INFO_VESA_SCAN_START,
  IDEA_INFO_VESA_SCAN_DONE,
  IDEA_INFO_DIALOG_ACTION,
  IDEA_INFO_LIVE_DISPLAY_START,
  IDEA_INFO_LIVE_DISPLAY_STOP,
  IDEA_INFO_LIVE_DISPLAY_SURFACE_GONE,
  IDEA_INFO_END
} IDEA_INFO_EVENT;

typedef struct {
  DWORD dwInfoSize;
  DWORD dwMode;     // 0 = Read Values, 1 = Adjust for max pix freq before read
  DWORD dwStatus;   // Reserved
  DWORD dwChannel;
  BOOL  bChannelValid;
  BOOL  bSyncValid;
  BOOL  bDigital;
  BOOL  bInterlaced;
  BOOL  bPositiveHSync;
  BOOL  bPositiveVSync;
  DWORD dwSyncChannel;
  DWORD dwMaxLines;
  DWORD dwHFreq;
  DWORD dwVFreq;
  DWORD dwPFreq;
  DWORD dwHTotal;
  DWORD dwVTotal;
  DWORD dwWidth;
  DWORD dwHeight;
} SIGNAL_INFO;

typedef struct {
	DWORD nDigital;
	DWORD nVChan;
	DWORD nCSync;
	DWORD nSDTVIn;
} SIGNAL_PARMS;

typedef struct {
  DWORD dwInfoSize;
  BOOL  bDVIClockActive;
  BOOL  bHDMIClockActive;
  BOOL  bAnalogCompositeSyncActive;
  BOOL  bAnalogHorizontalSyncActive;
  BOOL  bAnalogVerticalSyncActive;
  BOOL  bSDTVSyncActive;
  DWORD dwSDTVFormat;
  BOOL  bSDIActive;
} SYNC_INFO;

typedef void (CALLBACK* P_LOG_MESSAGE_CALLBACK)(void* pContext, char* pMessage);

typedef struct
{
  char* pszLogFile;
  BOOL bLogMessages;
  BOOL bSuppressMessageBoxes;

  // Structure extension for 3.5.1
  DWORD dwStructSize;
  P_LOG_MESSAGE_CALLBACK pfnMessageCallback;
  void* pMessageCallbackContext;
} LOG_MESSAGES;

typedef struct {
  char* pszLogFile;
  BOOL bLogTemperature;
  int  nTemperatureLogSeconds;
} LOG_TEMPERATURE;

typedef struct
{
  BoardHandle bh;
  char* pszSourceModule;
  char* pszFunction;
  char* pszDebugString;
} LOG_DEGUG_MESSAGE;

typedef struct {
  int nImageWidth;
  int nImageHeight;
} SCALE_IMAGE;

typedef struct {
  int    nDevice;
  HANDLE hDevice;
} DEVICE_OPEN_CLOSE;

typedef struct {
  DWORD     dwEventIndex;
  DWORD     bEventEnable;
  ULONGLONG qwEventHandle;
} DRIVER_EVENT;


// Trigger setup info
// Used in SetControlValue(bh, "TriggerSetup", &TRIGGER_SETUP_INFO);
typedef struct
{
  int     nStructSize;        // Size of this struct.
  int     nTriggerSelect;     // 0 = Default, 1 = Primary, 2 = Secondary (AccuStream 1000 and newer).
  BOOL    bEnable;            // Enable = 1, Disable = 0.
  HANDLE  hEvent;             // Event handle.
  BOOL    bRisingEdge;        // Rising edge = 1, falling edge = 0;
  BOOL    bOneShot;           // Trigger event is fired once then disabled.
  int     nRetriggerDelay_us; // Microseconds until next trigger is allowed.  0 = Use current.
  int     nTriggerFilter_us;  // Microseconds of minimum trigger pulse width. 0 = Use current.
} TRIGGER_SETUP_INFO;


/* Board Capabilities */
#define FSCAPS_USES_AD9884          (0x00000000)  // Place holder so code will build
#define FSCAPS_YUV_CAPTURE          (0x00000001)
#define FSCAPS_2PPC_FREQ            (0x00000002)
#define FSCAPS_USES_ADV7842         (0x00000004)
#define FSCAPS_USES_AD9887          (0x00000008)
#define FSCAPS_USES_AD9888          (0x00000010)
#define FSCAPS_USES_ADV7441         (0x00000040)
#define FSCAPS_DIGITAL_INPUT        (0x00000080)
#define FSCAPS_TRIGGER_POLARITY     (0x00000100)
#define FSCAPS_ONBOARD_YCBCR_CONV   (0x00000400)
#define FSCAPS_SWAP_RGB             (0x00000800)
#define FSCAPS_RGB_PIXEL            (0x00001000)
#define FSCAPS_USES_LUT             (0x00002000)
#define FSCAPS_CLOCK_SKEW           (0x00004000)
#define FSCAPS_COMPUTED_GAIN        (0x00008000)
#define FSCAPS_RGB_I2C              (0x00010000)
#define FSCAPS_SINGLE_BUFFER_SIZE   (0x00020000)  
#define FSCAPS_RGB_WITH_MONO        (0x00040000)
#define FSCAPS_DECIMATE_STREAM_X    (0x00080000)
#define FSCAPS_VESA_SCAN            (0x00100000)
#define FSCAPS_PLX_DESCRIPTORS      (0x00200000)
#define FSCAPS_NO_CONTINUOUS_GRAB   (0x01000000)
#define FSCAPS_STREAM_MONO_OR_YCBCR (0x02000000)
#define FSCAPS_NO_SYNC_FILTER       (0x04000000)
#define FSCAPS_PIXEL_YUV            (0x08000000)
#define FSCAPS_STREAM_MONO_ONLY     (0x10000000)
#define FSCAPS_FINE_PHASE           (0x20000000)
#define FSCAPS_USES_ADV7401         (0x40000000)
#define FSCAPS_DVI_FRAME            (0x80000000)

/*
//=====================================================================
//================== Start of Function Prototypes. ====================
//=====================================================================
*/
#ifdef __cplusplus
extern "C" {
#endif

/*
// Function prototypes for module H95START:
*/
BoardHandle APPTYPE bhHP_Claim(WORD wIOAddr, DWORD dw4KMem);
BoardHandle APPTYPE bhHD_Claim(WORD wIOAddr, int nFlags);
void     APPTYPE HP_UnClaim(BoardHandle bh);
short    APPTYPE nHP_ClaimAll(DWORD dw4KMem,short nMaxBoards);
ERRTYPE  APPTYPE eHP_Open(BoardHandle bh,HP_OpenData FAR *pOD);
ERRTYPE  APPTYPE eHP_Close(HP_OpenData *pOD);
short    APPTYPE nHP_Report(
  WORD wChecks,short nRptSize,HD_sReport *phrpt
);
ERRTYPE  APPTYPE eHP_CheckHandle(BoardHandle bh);
ERRTYPE  APPTYPE eHD_GetJBCPath(short nType, char *pszJBC);
BOOL             eHP_DriverClose(void);
BOOL     APPTYPE eHP_DriverCloseBH( WORD wDevice );


/*
// Function prototypes for module HP_REGS:
*/
void     APPTYPE HD_DecodeSerial(char *pszSerial, long lSerial);
long     APPTYPE lHD_EncodeSerial(char *pszSerial);
ERRTYPE  APPTYPE eHP_SetGain   (BoardHandle bh, WORD wGain);
ERRTYPE  APPTYPE eHP_SetBlackLevel(BoardHandle bh, short nBlackLevel);
ERRTYPE  APPTYPE eHP_SetChannel(BoardHandle bh, WORD wChannel);
ERRTYPE  APPTYPE eHP_RSET_Check(BoardHandle bh, RSET *prset);
ERRTYPE  APPTYPE eHP_RSET_Set  (BoardHandle bh, RSET *prset);
ERRTYPE  APPTYPE eHD_RSET_Set  (
  ImageHandle ih, RSET *prset, WORD wControl
);
ERRTYPE  APPTYPE eHD_RSET_Get  (ImageHandle ih, RSET *prset);
ERRTYPE  APPTYPE eHD_RSET_Check(
  BoardHandle bh, RSET* prset, short nMode
);
ERRTYPE  APPTYPE eHP_RSET_FRead(BoardHandle bh,char FAR *pszCHP,
  RSET *prset, BOOLs bStrict);
ERRTYPE  APPTYPE eHP_RSET_FWrite(BoardHandle bh,char FAR *pszCHP,
  RSET *prset);
ERRTYPE  APPTYPE eHD_SetChannel(
  ImageHandle ih, long lVChan, long lCChan, WORD wControl
);

ERRTYPE APPTYPE eHD_TestWhiteBalance(ImageHandle  ih,
                                     RSET        *prset,
                                     HD_WBALDATA *pWBD);
ERRTYPE APPTYPE eHD_SetWhiteBalance(ImageHandle ih,
                                    RSET        *prset,
                                    HD_WBALDATA *pWBD);
ERRTYPE APPTYPE eHD_SetInfoEvent(BoardHandle bh, HANDLE hSyncEvent);
ERRTYPE APPTYPE eHD_ResetInfoEvent(BoardHandle bh);
ERRTYPE APPTYPE eHD_AutoDetect(ImageHandle ih, BoardHandle bh, char* pCHPFile, GENERAL_VIDEO_INPUT_TYPE* pInputSelected, BOOL* pbMatchFound, BOOL bIgnoreSVideo_Composite, char* logger);

/*
// Function prototypes for module HP_EE:
*/
short    APPTYPE nHP_GetBoardInfo(BoardHandle bh, unsigned short nFld,
  short nMaxBytes, void FAR *pFld);
HD_MODELIST *pHD_ModeList(void);

/*
// Function prototypes for module HP_VIDEO:
*/
BOOLs    APPTYPE bHP_CSyncDetect(BoardHandle bh);
BOOLs    APPTYPE bHP_CommandReady(BoardHandle bh);
ERRTYPE  APPTYPE eHP_CommandReady(BoardHandle bh, WORD wTimeout_mS);
BOOLs    APPTYPE bHP_CommandDone(BoardHandle bh);
ERRTYPE  APPTYPE eHP_CommandDone(BoardHandle bh, WORD wTimeout_mS);
ERRTYPE  APPTYPE eHP_Command(BoardHandle bh, WORD wCommand);
ERRTYPE  APPTYPE eHD_Command(
  ImageHandle ih, WORD wCommand, HDOP_HDL *phh
);
BOOLs    APPTYPE bHD_CommandDone(ImageHandle ih);
ERRTYPE  APPTYPE eHD_CommandDone(ImageHandle ih, WORD);

/*
// Function prototypes for module HP_XFER:
*/
ERRTYPE  APPTYPE eHP_FBFastRead(BoardHandle bh, WORD wColumn,
           WORD wLine, short nWide, short nHigh, WORD far *fpwBuf);
ERRTYPE  APPTYPE eHP_FBFastWrite(BoardHandle bh, WORD wColumn,
           WORD wLine, short nWide, short nHigh, WORD far *fpwBuf);
ERRTYPE  APPTYPE eHP_FBHisto(BoardHandle bh, WORD wColumn,
           WORD wLine, short nWide, short nHigh, DWORD far *fpdwHisto);
ERRTYPE  APPTYPE eHD_FBFastReadPixel(
  ImageHandle ih, WORD wControl, HDOP_HDL *phh,
  WORD wColumn, WORD wLine, short nWide, short nHigh,
  WORD far *fpwBuf
);
ERRTYPE  APPTYPE eHD_FBHistoPixel(
  ImageHandle ih, WORD wControl, HDOP_HDL *phh,
  WORD wColumn, WORD wLine, short nWide, short nHigh,
  DWORD far *fpdwHisto
);

ERRTYPE APPTYPE eHP_FBHistoPixel(BoardHandle bh, WORD wControl,
  WORD wColumn, WORD wLine, short nWide, short nHigh,
  DWORD far *pdwHisto
);

/*
// Function prototypes for module HP_ERROR:
*/
char     *pszEXE(void);
char     *pszExePath(size_t nSize, char *ca);
void     APPTYPE _HP_Error(BoardHandle bh, WORD wError);
short    APPTYPE nHP_ErrMessage(ERRTYPE e, short nSize, char *pszEMsg);


/*
// Function prototypes for module MEMALLOC:
*/
ImageHandle APPTYPE ihHD_Allocate(BoardHandle, WORD, RSET*);
ERRTYPE  APPTYPE eHD_Deallocate(ImageHandle ih);
ERRTYPE  APPTYPE eHD_DeallocateAll(BoardHandle);

/*
// Function prototypes for module HD_ASYNC:
*/
ERRTYPE  APPTYPE eHD_SetSyncSource(
  BoardHandle bh, long lSyncSource, RSET *pRSet, HDOP_HDL *pHDOP);
ERRTYPE APPTYPE eHD_ConfigSyncSource(
  BoardHandle bh, long lSyncSource, HD_SSConfig *pSSConfig,
  HDOP_HDL *pHDOP);
ERRTYPE  APPTYPE eHD_TrackSync(
  BoardHandle bh, long lSyncSource, BOOL bHold, HDOP_HDL *pHDOP);

ERRTYPE  APPTYPE eHP_GetInfoStruct(BoardHandle, IDEA_INFO** pInfo);

/*
// Function prototypes for module MVISION:
*/
ERRTYPE  APPTYPE eHD_TrigZeroPulse(BoardHandle bh, int nOper);
void     APPTYPE HD_EncodeCSync(char *pszCS, long lCS);
ERRTYPE  APPTYPE eHD_SetBinaryLines(
  BoardHandle bh, long lOutMask, long *plOutPins
);
ERRTYPE  APPTYPE eHD_GetDefaultCHP(
  BoardHandle bh, int nVChan, int *pnType, int *pnBytes, char *pszCHP
);
ERRTYPE  APPTYPE eHD_LoadCableSpec(RSET *pRSet, char *pCablePath );
ERRTYPE APPTYPE eHP_SetIntegrationPeriod( BoardHandle bh, WORD nFrames );
ERRTYPE APPTYPE eHP_SetTriggerDuration( BoardHandle bh, short nLines );
ERRTYPE APPTYPE eHP_SetTriggerDelay( BoardHandle bh, short nLines );
ERRTYPE APPTYPE eHP_SetTriggerPolarity( BoardHandle bh, short nPolarity );
ERRTYPE APPTYPE eHP_GetCurrentMVMode( BoardHandle bh, char *pszMode );
ERRTYPE APPTYPE eHP_GetCurrentMVCamera( BoardHandle bh, char *pszCamera );
ERRTYPE APPTYPE eHP_SetIRGBCableSync( BoardHandle bh, WORD wSyncType  );
ERRTYPE APPTYPE eHP_GetBoardIDString( BoardIDString *BoardID );
/*
// Function Prototypes And Defines for module KS0127
*/
#define HD_OUTPUT_YCbCr   (0)
#define HD_OUTPUT_RGB888  (6)

ERRTYPE APPTYPE eHP_SetControlValue( BoardHandle bh, char *szControl, size_t InSize, void *pInput );
ERRTYPE APPTYPE eHP_GetControlValue( BoardHandle bh, char *szControl, size_t OutSize, void *pOutput );

/*
// Function prototypes for module LIVE_VID.c:
*/
ERRTYPE APPTYPE eHD_LiveVideoInit(
  int nBuffers, HD_LIVEVIDEO *pLV, int nMaxFrames, int nFlags,
  HDOP_HDL *pHDOP
);

ERRTYPE eHD_SnapToBuffer( ImageHandle ih, SNAP_CONTROL *p );

ERRTYPE APPTYPE eHD_LiveDisplayInit(
  ImageHandle   ih,
  LIVEDISPLAY_INFO *pLiveDisplayInfo
);

ERRTYPE APPTYPE eHD_LiveDisplayMode(
  ImageHandle   ih, int nMode
);

ERRTYPE APPTYPE eHD_LiveStreamInit(
  ImageHandle   ih,
  LIVESTREAM_INFO *pLiveStreamInfo
);

ERRTYPE APPTYPE eHD_LiveStreamMode(
  ImageHandle   ih, int nMode
);

ERRTYPE APPTYPE eHD_LiveStreamClose(
  ImageHandle   ih,
  LIVESTREAM_INFO *pLiveStreamInfo
);

ERRTYPE APPTYPE eHP_EnableTriggerEvent( BoardHandle bh, HANDLE hEvent, BOOL bOneShot );

ERRTYPE APPTYPE eHP_DisableTriggerEvent( BoardHandle bh, HANDLE hEvent );

ERRTYPE  APPTYPE eHD_LiveVideoMode(
  ImageHandle ih, int nMode
);

HD_LUTHandle hHD_MakeLUT(
  HD_LUTHandle   hLUT,
  DWORD          dwCtrl,
  BYTE           *pbyLUT );

ERRTYPE eHD_SetIHDMALUT(
  ImageHandle    hImage,
  HD_LUTHandle   hLUT );

ERRTYPE eHD_GetIHDMALUT(
  ImageHandle   ih,
  DWORD         *pdwCtrl,
  HD_LUTHandle  *phLUT,
  BYTE          *pbyDstLUT );

ERRTYPE eHD_GetLUT(
  HD_LUTHandle  hLUT,
  DWORD         *pdwCtrl,
  BYTE          *pbyDstLUT );

int APPTYPE eHD_GetMaxBuffers(ImageHandle ih, WORD wSuggest, WORD wPixelType );

ERRTYPE APPTYPE eHD_ViewCamRegs( BoardHandle bh );
ERRTYPE  APPTYPE eHD_ViewAD9888Regs( BoardHandle bh ); // NOTE TO JOE - DONT CHECK THIS IN !!!!

ERRTYPE APPTYPE eHD_GetStreamBuffer( ImageHandle ih, HDVID_HEADER **pBufferHeader );
ERRTYPE APPTYPE eHD_ReleaseStreamBuffer( ImageHandle ih, HDVID_HEADER *pBufferHeader );
DWORD eHP_GetBoardCaps( int nType );
ERRTYPE eHP_LoadEdid( BoardHandle bh, char *pszEdidPath );


//+D2D
ERRTYPE APPTYPE eHD_GetDisplayBuffer( ImageHandle ih, HDVID_HEADER **pBufferHeader );
ERRTYPE APPTYPE eHD_ReleaseDisplayBuffer( ImageHandle ih, HDVID_HEADER *pBufferHeader );
ERRTYPE APPTYPE eHD_LiveDisplayClose(ImageHandle ih, LIVEDISPLAY_INFO *pLiveDisplayInfo);
//-D2D

ERRTYPE APPTYPE eHD_CompressionInit( ImageHandle ih, COMPRESSION_INFO *pCompressionInfo );
ERRTYPE APPTYPE eHD_CompressionMode( ImageHandle ih, int nMode );
ERRTYPE APPTYPE eHD_CompressionClose( ImageHandle ih, COMPRESSION_INFO *pCompressionInfo );
ERRTYPE APPTYPE eHD_GetCompressionBuffer( ImageHandle ih, HDVID_HEADER **pBufferHeader );
ERRTYPE APPTYPE eHD_ReleaseCompressionBuffer( ImageHandle ih, HDVID_HEADER *pBufferHeader );
ERRTYPE APPTYPE eHD_GetCompressionStatus(ImageHandle ih, STREAMING_STATUS *pStatus);

#ifdef __cplusplus
}
#endif

#endif   /* #ifndef _INC_HDP_LIB */
