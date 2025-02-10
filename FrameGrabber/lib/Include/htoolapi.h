/*
//=====================================================================
//
// Filename: HDP\tlib\src\HTOOLAPI.H
//
//                  Copyright (C) 1999 by Foresight Imaging, LLC
//                                    All rights (world-wide) reserved.
//=====================================================================
*/

#ifndef _H_HTOOLAPI
#define _H_HTOOLAPI

#include "hdp_lib.h"

/*
//=====================================================================
// Auto-SYNC tool support.
//=====================================================================
*/

/*
// Components of ASMODES structure.
*/
typedef WORD ASM_N;
#define ASM_N_SKIP      0
#define ASM_N_NONEED    1
#define ASM_N_INTEREST  2
#define ASM_N_REQUIRED  3
#define ASM_N_PREFERRED 4
#define ASM_N_N         5

typedef WORD ASM_A;
#define ASM_A_NOTAVAIL  0
#define ASM_A_NOTDONE   1
#define ASM_A_NOTCHOSEN 2
#define ASM_A_AVAILABLE 3

/*
//=====================================================================
//
// Structure for tracking and controlling each board/mode combination.
//
//  Several copies of structure ASMODES occur in each structure
// AS_CHANNEL.  Each ASMODES structure carries information about a
// possible use for the signal on a channel.  Additional details on how
// these structures are used by the applications programmer are pro-
// vided in ToolsAPI.wri.
//  Each AS_CHANNEL structure contains an ASMODES structure for each
// of these possible video signal uses:
//     1) video image information (mVideo); and
//     2) composite sync information suitable for each HI*DEF board
//       type and board/mode combination (mode[MODES_HIDEF_N]).
//
//  Element 'mode' is an array of ASMODES structures in AS_CHANNEL.
// Each of those elements specifies a particular used or combination
// of uses for a CSync signal on that channel.  Those uses are:
//    MODE_HDP_1PASS:  A HI*DEF Plus in single pass mode.
//    MODE_HDP_2PASS:  A HI*DEF Plus in dual pass mode.
//    MODE_HD4_1PASS:  A HI*DEF 4 in single pass mode.
//    MODE_HD4_2PASS:  A HI*DEF 4 in dual pass mode.
//    MODES_HDP:       A HI*DEF Plus in either mode.
//    MODES_HD4:       A HI*DEF 4 in either mode.
//
//  Note that element 'mVideo' is not used for the CSync only channels
// (CT1 to CT4 and SS).
//
//  There are three elements in this structure:
//    anNeed:   This value indicates what level of interest the appli-
//         cation has of this channel in the corresponding board/mode
//         combination.  The possible values are:
//              ASM_N_SKIP: Do not process for this mode at all.  This
//                   will cause the corresponding aaAvail value to be
//                   set to ASM_A_NOTAVAIL and for the corresponding
//                   'e' code to be set to HPERR_TOOL_SKIPPED.
//              ASM_N_NONEED: There is no need to support this
//                   board/mode combination.  Continue processing for
//                   it but do not bias signal analysis in its favor.
//                   If it is eliminated, log the cause only as a
//                   "verbose" notification.
//              ASM_N_INTEREST: This board/mode is of interest for this
//                   channel.  Fit the signal recognition to this
//                   board/mode if possible.  If it becomes eliminated,
//                   log the cause as a "normal" error notification.
//              ASM_N_REQUIRED: This board/mode is required for this
//                   channel.  Fit the signal recognition to this
//                   board/mode if possible.  If it becomes eliminated,
//                   log the cause as a "terse" error notification and
//                   stop processing for this channel.  Should this
//                   occur, the 'e' code for the remaining board/modes
//                   that have not been eliminated will be set to
//                   HPERR_TOOL_SKIPPED.
//              ASM_N_PREFERRED: This specifies all the same conditions
//                   as ASM_N_REQUIRED and one more, that this pass
//                   mode (single or dual) is preferred over the other
//                   for this channel.  This value can only be applied
//                   to one of the MODE_H* board/mode selections to
//                   indicate that for the particular board type
//                   (HI*DEF Plus or HI*DEF 4) the particular pass mode
//                   (single or dual) is preferred over the other.  If
//                   two conflicting (single and dual pass for the same
//                   board type) both specify ASM_N_PREFERRED, they
//                   will both be downgraded to ASM_N_REQUIRED.  Also,
//                   if ASM_N_PREFERRED is applied to one of the
//                   MODES_H* combination selections, it will be down-
//                   graded to ASM_N_REQUIRED.  When this mode is
//                   selected (and not downgraded), the final aaAvail
//                   value can be ASM_A_NOTAVAIL or ASM_A_AVAILABLE but
//                   never ASM_A_NOTCHOSEN.  This selection becomes
//                   noted in the RSET structure as preferred.
//           
//    aaAvail:  This value is returned by the Auto-SYNC functions to
//         indicate the availability of this board/mode combination for
//         this channel.  The possible values are:
//              ASM_A_NOTAVAIL:  This channel cannot be processed with
//                   this board/mode combination.  The cause for its
//                   elimination is specified in the 'e' value of this
//                   structure.
//              ASM_A_NOTDONE:  Auto-SYNC processing has not completed.
//                   The RSET structure is not ready but nor has this
//                   board/mode combination has not been eliminated for
//                   this channel.
//              ASM_A_NOTCHOSEN:  This signal can be processed for this
//                   board/mode combination but the other mode (dual
//                   pass or single pass) has been chosen.
//              ASM_A_AVAILABLE:  The RSET structure is complete and
//                   will support this board/mode combination.
//    e:     This value is zero unless aaAvail is ASM_A_NOTAVAIL.
//         Otherwise, it indicates the reason that this board/mode
//         combination became unavailable for this channel.
//=====================================================================
*/
typedef struct {
  ASM_N   anNeed;
  ASM_A   aaAvail;
  ERRTYPE e;
} ASMODES;

/*
// Components of AS_CHANNEL structure.
*/
typedef short ASCH;
#define ASCH_NONE -1
#define ASCH_0     0
#define ASCH_CA1   0
#define ASCH_CA2   1
#define ASCH_CA3   2
#define ASCH_CA4   3
#define ASCH_CT1   4
#define ASCH_CT2   5
#define ASCH_CT3   6
#define ASCH_CT4   7
#define ASCH_SS    8
#define ASCH_N     9

typedef WORD ASCMASK;
#define ASCMASK_NONE  0x0000
#define ASCMASK_0     0x0001
#define ASCMASK_CA1   0x0001
#define ASCMASK_CA2   0x0002
#define ASCMASK_CA3   0x0004
#define ASCMASK_CA4   0x0008
#define ASCMASK_CA    0x000F
#define ASCMASK_CT1   0x0010
#define ASCMASK_CT2   0x0020
#define ASCMASK_CT3   0x0040
#define ASCMASK_CT4   0x0080
#define ASCMASK_CT    0x00F0
#define ASCMASK_SS    0x0100
#define ASCMASK_ALL   0x01FF

/*
// Defines for wVideoFlags in structure AS_CHANNEL.
*/
typedef unsigned short HDVF;
#define HDVF_INTERLACE    0x0001 /* Signal is interlaced.              */
#define HDVF_POS_HS       0x0002 /* HSync polarity is reversed (pos.). */
#define HDVF_POS_VS       0x0004 /* VSync polarity is reversed (pos.). */
#define HDVF_POS          0x0006 /* Reverse both HSync and VSync pol.  */
#define HDVF_VIDEO        0x0800 /* Some video or sync detected.       */
#define HDVF_HSYNC        0x1000 /* Horizontal sync pulses detected.   */
#define HDVF_VSYNC        0x2000 /* Vertical sync pulses detected.     */
#define HDVF_CSYNC        0x3000 /* Composite sync pulses detected.    */
#define HDVF_BLOCKSYNC    0x8000 /* Block sync used.                   */
#define HDVF_ALL          0xB807

#define HDVF_ON(v,f)  ( (v##wVideoFlags & HDVF_##f) == HDVF_##f)
#define HDVF_OFF(v,f) ( (v##wVideoFlags & HDVF_##f) != HDVF_##f)

/*
// Defines for wCtrlFlags in structure AS_CHANNEL.
*/
typedef unsigned short HDCF;
#define HDCF_FRODD        0x0001 /* Force frame ordering to ODDUP      */
#define HDCF_FREVENODD    0x0002 /* Force ordering to ON_EVENODD       */
#define HDCF_FRODDEVENODD 0x0004 /* Force ordering to ODDUP_EVENODD    */
#define HDCF_FRALL        0x0007 /* All frame ordering modes           */
#define HDCF_VESAMODE     0x0010 /* Force test for VESA modes          */
#define HDCF_ALL          0x0017 /* All control flag values            */

#define HDCF_ON(c,f)  ( (c##wCtrlFlags & HDCF_##f) == HDCF_##f)
#define HDCF_OFF(c,f) ( (c##wCtrlFlags & HDCF_##f) != HDCF_##f)

/*
// Channel control word:
//   ENABLE:         Process for this channel.
//   HTOT_CLOCK:     Use the external clock to determine horz. total.
//   HTOT_LIMIT:     Use twMin & twMax to determine horizontal total.
*/
typedef WORD ASCMD;
#define ASCMD_ENABLE         0x0001
#define ASCMD_HTOT_CLOCK     0x0002
#define ASCMD_HTOT_LIMIT     0x0004

/*
//  Type ASPHASEL is used in the AS_CHANNEL structure to specify how
// the phase delay determination for that channel will be performed.
//  Type ASPDMETHOD is used in the ASPHASEL structure to specify which
// basic phase delay determination method will be used.  Listed below
// is a brief description of each method and how it uses the parameter
// values lParam and lHunt (also part of the ASPHASEL structure).
//
//  Before the phase detection method is applied, a range of HBS/Phase
// Delay combinations are determined.  These combination are the candi-
// date values.  The purpose of the phase delay determination method is
// to select the best combination from among those candidates.
//  In making this determination these candidates will be "surveyed".
// Several survey types are possible.  The fastest type of survey is a
// "fast measurement" survey - where about 17% of the candidates are
// examined and only summary information is collected.  Once the fast
// measurement survey is completed, a "measurement hunt" may be
// performed in an attempt to find a candidate with a better phase
// delay score.  These hunts can range from a fraction of a second to
// half a minute depending on the extent of the hunt (lHunt) and the
// video frame rate.  Setting lHunt to 254 or more, turns the measure-
// ment hunt into a "thorough measurement" survey (next).
//  Alternatively, a "thorough measurement" or "thorough examination"
// survey may be performed.  In both cases every candidate is surveyed.
// In the first case (thorough measurement) only summary information is
// collected.  In the second case (thorough examination) complete image
// data is collected and processed.  A thorough measurement survey will
// take tens of seconds depending on the video format.  A thorough
// examination survey will take from 1 to 5 minutes (per channel)
// depending on the video format.
//
//    ASPD_STDEV:  A fast measurement survey followed by a measurement
//      hunt is performed in an attempt to locate the candidate with
//      the lowest pixel value standard deviation.  This candidate
//      falls very close to the midpoint of the pixel edge.  The
//      candidate which falls lParam degrees beyond this edge is then
//      used.  A full pixel period represents 360 degrees.
//        In the default case, ASPD_STDEV is used with lParam set to
//      240 and lHunt set to 10.  This provides a fairly quick and
//      reasonably good selection with a wide range of image/format
//      combinations.
//
//    ASPD_SHARP:  A fast measurement survey followed by a measurement
//      hunt is performed in an attempt to locate the candidate with
//      the sharpest histogram peaks.  This method provides very good
//      results when the image consists entirely or almost (>90%)
//      entirely of an alternating-white-pixel-black-pixel pattern.
//      Such patterns include "resolve" which is mostly a large checker
//      board with one pixel per square and the 1 pixel "vgrill" which
//      is alternating black and white vertical stripes one pixel wide.
//        Note that a resolve-like pattern is very good for pixel
//      detection and phase delay determination but is very poor for
//      black white adjustments.
//
//        If lParam is from 5 to 630, it specifies that this method
//      is to be filtered using the standard deviation method.  This
//      filtering is based on the fact that the candidate that produces
//      the minimal pixel standard deviation (StDevMin) is in the midst
//      of the worse candidate values.  Those values need not be con-
//      sidered.  When filtering is on (5<=lParam<=630), a range of
//      candidates are eliminated.  The range starts 0.5nSec before
//      StDevMin.  The end of the range is a specified period of time
//      after StDevMin.  This time period is specified with lParam in
//      0.1 nSec units, but is never less than 0.5nSec nor greater than
//      half a pixel period.
//        Depending on the situation, specifying filtering may speed up
//      or slow down the phase delay determination process.  More
//      importantly, it makes the results of ASPD_SHARP more reliable
//      especially when a substantial portion of the image is not
//      resolve-like.
//
//    ASPD_MEAS5:  This method involves a thorough measurement survey
//      of the candidates and is therefore typically slower than
//      methods ASPD_STDEV and ASPD_SHARP.  However, it is also more
//      precise and reliable than those methods for most image/format
//      combinations.
//        Since this method does not employ "hunting", parameter lHunt
//      is not used and should be set to zero.  Parameter lParam
//      specifies filtering and is used exactly as it is with
//      ASPD_SHARP.  When filtering is used with this method, it
//      improves reliablility but has no affect on speed.
//
//    ASPD_EXAM5:  This method involves a thorough examination survey
//      of the candidates and is therefore the slowest of the methods.
//      This method requires a minimal amount of edge information in
//      the image and its selection process is precise and definitive.
//      The biggest drawback to using this method is that it is usually
//      slower than picking the phase delay value manually.
//       This method uses the image data to precisely measure the image
//      stability at each candidate value.  It then selects the most
//      stable.  The resulting selection will produce repeatable
//      results with resilience to changes in temperature and image
//      content.  Based on this criteria, its results are better than
//      can be selected by eye.
//       Neither lHunt nor lParam are used and both should be set to
//      zero.  Filtering does not improve the selection results of this
//      method and is not available for it.
*/
typedef enum {
  ASPD_STDEV=0, ASPD_SHARP, ASPD_MEAS5, ASPD_EXAM5
} ASPDMETHOD;
typedef struct {
  ASPDMETHOD  aspd;
  long        lParam;
  long        lHunt;
} ASPHASEL;

typedef struct {
  WORD   wHTOTAL;
  WORD   wWidth;
} TOTWID;

/*
// Structures for using and controlling the pixel detection feature.
//
//  There is one of these structures for each channel.  However, this
// information is only used for analogue channels.
//
//  PD stands for pixel detection.
*/
#define HD_PIXDLIST 6
typedef struct {
  TOTWID   tw;         /* One of the most likely HTotals and widths. */
  double   dScore;     /* Score for tw.wWidth.                       */
} PIXPEAK;
typedef struct {
  BOOLs    bEnable;    /* Set by app to select PD for this channel.  */
  ERRTYPE  e;          /* Non-zero code if PD was not performed.     */
  double   dMaxW2H;    /* Maximum width-to-height ratio. */
  double   dMinW2H;    /* Minimum width-to-height ratio. */
  PIXPEAK  peaks[HD_PIXDLIST]; /* Likely widths by descending scores.*/
} PIXELD;

/*
// Structure for control and survey of an analogue channel
//
//  There is one of these structures for each channel.  This structure
// consists of a combination of:
//   1) Data which is set by the user to control the sync sources board
//     survey function (HD_SyncSurvey()).  These values are marked
//     with the digit "1".
//   2) Data which is set or changed by HD_SyncSurvey().  These values
//     are marked with the letter "S".
//   3) Data which can be changed before calling HD_VideoMeasure().
//     These values are marked with the digit "2".
//   4) Data which is set or changed by HD_VideoMeasure().  These
//     values are marked with the letter "M".
//   5) Data which is set or changed before calling HD_VideoAlign().
//     These values are marked with the digit "3".
//   6) Data which is set or changed by HD_VideoAlign().  These values
//     are marked with the letter "V".
//   7) Data which is used only with the analogue channels (CA1-4) are
//     marked with the letter "A".
*/
typedef struct {
  short    nThisSize;   /* 1------ Number of bytes in this structure.*/
  ASCMD    ascmd;       /* 1S2M3V- Control specifications.           */
  ASPHASEL asph;        /* ----3-A Phase delay determination method. */
  ASMODES  mVideo;      /* 1S2M--- Do we need/have video (non-Sync). */
  ASMODES  mode[MODES_HIDEF_N];/* 1S-M-V- Target board mode states.  */
  DWORD    dwHorFreq;   /* -S----- Horizontal frequency (Hz).        */
  short    nHL_Field;   /* -S----- # half lines per video field.     */
  short    nMaxLines;   /* -S----- Max lines/frame excluding blanking*/
  DWORD    dwHPulse_nsec;/*-S----- Width of HSync pulse (nsec).      */
  DWORD    dwVPeriod;   /* -S----- Vertical period in 100nsec units. */
  HDVF     wVideoFlags; /* -S----- Video characteristics flags.      */
  ASCMASK  ascmBestCS;  /* 1S----- Preferred CSync sources.          */
  ASCMASK  ascmHDP_CS;  /* -S----- CSync sources allowed with HD+.   */
  ASCMASK  ascmHD4_CS;  /* -S----- CSync sources allowed with HD4.   */
  ASCH     aschCS;      /* -S2M--A Composite sync source assignment. */
  TOTWID   twMin;       /* --2M3-A The min. permitted HTOTAL & width.*/
  TOTWID   twMax;       /* --2M3-A The max. permitted HTOTAL & width.*/
  TOTWID   tw;          /* --2M3-A The preferred HTOTAL & width.     */
  WORD     wHeight;     /* ---M--A Measured image height.            */
  PIXELD   PD;          /* --2M--A Pixel detection control structure.*/
  RSET     *prset;      /* 1----VA RSET output.  Contents is -S-M-VA.*/
  HDCF     wCtrlFlags;  /* --2M--- Control flags                     */
  short    nRGBCableType;/* 1------ RGB cable type index (0-based)   */
} AS_CHANNEL;
/*
// Components of AS_RUNSTATE structure.
*/
typedef short ASLV;     /* Log report verbosity. */
#define ASLV_CRITICAL -1
#define ASLV_TERSE     0
#define ASLV_NORMAL    1
#define ASLV_VERBOSE   2
#define ASLV_ASB       3
typedef short ASTAGE;   /* Which Auto-SYNC stages have completed. */
#define ASTAGE_INIT    0
#define ASTAGE_SURVEY  2
#define ASTAGE_MEASURE 4
#define ASTAGE_ALIGN   6
typedef void HCB_LOG(void *pThis, long lUser, ASLV aslv, char *pszMsg);
typedef void HCB_BUSY(void *pThis, short nPercent);
typedef BOOL HCB_ASB(void *pThis,
  long lUser, ASCH chan, long lNBytes, BYTE *pbyData
);
/*
//  Structure for maintaining Auto-SYNC state for a board and connect-
// ing to execution time Auto-SYNC reporting.
*/
typedef struct {
  /*
  //  These first five elements should not be modified by the applica-
  // tion code.
  */
  short       nThisSize; /* Number of bytes in this structure.       */
  DWORD       dwID;      /* Runtime AS_RUNSTATE structure ID ("ARun")*/
  BoardHandle bh;        /* Board handle for board to Auto-SYNC.     */
  void        *pResv;    /* Reserved for exclusive use of Auto-SYNC. */
  ASTAGE      astage;    /* How much Auto-SYNC processing is done.   */
  /*
  //  eHD_ASInitiate() sets up defaults for these next elements which
  // can be changed by your application code.
  */
  ASLV        aslv;      /* Level of detail to include in Log msgs.  */
  long        lUser;     /* Long variable for any application use.   */
  HCB_BUSY    *Spinning; /* Auto-SYNC progress reporting (% done).   */
  void        *pObjSpin; /* Object (or data) pointer for Spinning(). */
  HCB_LOG     *LogReport;/* Called with text data for log reporting. */
  void        *pObjLogR; /* Object (or data) pointer for LogReport().*/
  HCB_ASB     *bRawSync; /* *.ASB diagnostic data pipe.              */
  void        *pObjRawS; /* Object (or data) pointer for RawSync().  */
  /*
  //  eHD_ASInitiate() allocates these nine structures (per the
  // ASCMASK argument) and eHD_ASTerminate() frees them.  Treat these
  // pointers as read-only.
  */
  AS_CHANNEL  *pasc[ASCH_N]; /* Array of pointers to channel states. */
} AS_RUNSTATE;


/*
//=====================================================================
// Other tool support.
//=====================================================================
*/
typedef struct {
  long    lPix10TooLow;   /* Max 10-bit pixel value too low for edge*/
  long    lMinHiPixels;   /* Minimum pixels>lPix10TooLow for edge.  */
  long    lEdgeHBS;       /* HBS where left edge was found.         */
  long    lEdgePhase;     /* Left edge phase value (0.5nSec units). */
  long    lFullPhase;     /* Pixel period in 0.5nSec with margin.   */
} HPT_EDGE;

typedef struct {
  int     nChannel;       /* Video channel (e.g., ASCH_CA1)         */
  DWORD   dwHFreq;        /* Horz Frequency measured in Hz          */
  DWORD   dwVFreq;        /* Vert Frequency measured in .01Hz       */
  DWORD   dwVTotal;       /* Vertical Total                         */
  BOOL    bInterlaced;    /* TRUE if video is interlaced            */
  BOOL    bPosHSync;      /* TRUE if HSync polarity reversed (pos)  */
  BOOL    bPosVSync;      /* TRUE if VSync polarity reversed (pos)  */
} AS_VESAINFO;

/*
//=====================================================================
//================== Start of Function Prototypes. ====================
//=====================================================================
*/
#ifdef __cplusplus
extern "C" {
#endif

/*
//=====================================================================
// Auto-SYNC routine prototypes for Tools API.
//=====================================================================
*/
/*
// Claim Auto-SYNC resources.
*/
ERRTYPE APPTYPE eHD_ASInitiate(
  BoardHandle bh,      /* Board to Auto-SYNC.                     */
  ASCMASK     ascm,    /* Which channels to allocate for.         */
  AS_RUNSTATE **ppasr  /* Returns ptr to Auto-SYNC data pointer.  */
);
/*
// Board/Channel Survey.
*/
ERRTYPE APPTYPE eHD_SyncSurvey(
  AS_RUNSTATE *pasr    /* Auto-SYNC control & response data.      */
);
/*
// Measure video.
*/
void APPTYPE HD_VideoMeasure(
  AS_RUNSTATE *pasr    /* Auto-SYNC control & response data.      */
);
/*
// Video alignment.
*/
void APPTYPE HD_VideoAlign(
  AS_RUNSTATE *pasr    /* Auto-SYNC control & response data.      */
);
/*
// Release Auto-SYNC resources.
*/
ERRTYPE APPTYPE eHD_ASTerminate(
  AS_RUNSTATE **ppasr  /* Returns NULL in Auto-SYNC data pointer. */
);

/*
//=====================================================================
// Other routine prototypes for HI*DEF Tools API.
//=====================================================================
*/
/*
// Check board and force it to an initial state.
*/
ERRTYPE APPTYPE eHP_Arrest(BoardHandle bh);
/*
// Precisely locate the left edge.
*/
ERRTYPE APPTYPE eHPT_LeftEdgePhase(
  BoardHandle bh, RSET *prset,
  BYTE byThreshold, long lNHighPix, HPT_EDGE *phe
);
/*
// Fine-tune phase delay.
*/
ERRTYPE APPTYPE eHPT_PhaseAdjust(
  BoardHandle bh, RSET *prset, HPT_EDGE *phe, ASPHASEL *pasph
);
/*
// Final Black/White adjustment subroutine for a 
// particular board & channel.
*/
ERRTYPE APPTYPE eHPT_FinalBWAdjust(
  ASCH asch, BoardHandle bh, RSET *prset, DWORD *pdwHisto,
  WORD wStartX, WORD wStartY, WORD wImageWidth, WORD wImageHeight,
  double dLoPct, double dHiPct, long lLoTarg, long lHiTarg
);
/*
// Combine video and sync data (used if HD_VideoMeasure()
// is omitted, such as during VESA scanning.
*/
void APPTYPE HD_CombineVideoAndSyncs(AS_RUNSTATE *pasr);
/*
// Retrieve information about the input video signal for
// VESA scanning.
*/
void APPTYPE HD_GetVESAInfo(AS_RUNSTATE *pasr, AS_VESAINFO *pVESA);

#ifdef __cplusplus
}
#endif

#endif /* _H_HTOOLAPI */
