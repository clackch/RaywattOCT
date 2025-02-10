/*
//=====================================================================
// Filename: include\MVision.h
//
//                  Copyright (C) 1999 by Foresight Imaging, LLC
//                                    All rights (world-wide) reserved.
//=====================================================================
*/

#ifndef _H_MVISION
#define _H_MVISION

#include <stdio.h>

#define MAXICILINE  300
#define MAX_ICI_CHP  29
#define SGC_EXTERNAL_CLOCK  0x00
#define SGC_40MHZ_CLOCK     0x01
#define SGC_HSYNC_ENABLE    0x04
#define SGC_HSYNC_INVERT    0x10
#define SGC_VSYNC_ENABLE    0x08
#define SGC_VSYNC_INVERT    0x20


typedef enum {
  PULSE_ZERO,    PULSE_ONE,     PULSE_OFF,     PULSE_ON,
  PULSE_INV,     PULSE_DBL,     PULSE_DBLINV
} PULSE_GATE;
typedef enum {
  ICISIG_NONE=0, ICISIG_CAM1,   ICISIG_CAM2,   ICISIG_GENHS,
  ICISIG_GENVS,  ICISIG_ZERO,   ICISIG_ONE,    ICISIG_N
} ICISIG;
typedef enum {
  ICITRIG_NONE,  ICITRIG_TRIG0, ICITRIG_CAM1,  ICITRIG_CAM2
} ICITRIG;

#if 0
typedef struct {
  int          nVideo;       /* Video continuity type.       */
  char  szCHP[MAX_ICI_CHP+1];/* Designated CHP file.         */
} ICI_CHANNEL;
#endif
// Bitmaps to show each how each IO Line is being used
#define PIN_UNASSIGNED     (0x0000)
#define PIN_IO1            (0x0001)
#define PIN_IO2            (0x0002)
#define PIN_IO3            (0x0003)
#define PIN_IO4            (0x0004)
#define PIN_IO5            (0x0005)
#define PIN_IO6            (0x0006)
#define PIN_IO7            (0x0007)
#define PIN_IO8            (0x0009)
#define PIN_P12V           (0x000A)
#define PIN_STROBE_LO      (0x000B)
#define PIN_STROBE_HI      (0x000C)
#define PIN_TRIGGER_IN     (0x000D)
#define PIN_TRIGGER_OUT    (0x000F)
#define PIN_VIDEO_CA2      (0x0010)
#define PIN_VIDEO_CA3      (0x0011)
#define PIN_VIDEO_CA4      (0x0012)
#define PIN_DIGITAL_OUT    (0x0013)
#define PIN_GND            (0x0014)

#define PIN_USAGE_UNASSIGNED    (0x0000)
#define PIN_USAGE_HSYNC_IN_HI   (0x0001)
#define PIN_USAGE_HSYNC_IN_LO   (0x0002)
#define PIN_USAGE_HSYNC_OUT_HI  (0x0003)
#define PIN_USAGE_HSYNC_OUT_LO  (0x0004)
#define PIN_USAGE_VSYNC_IN_HI   (0x0005)
#define PIN_USAGE_VSYNC_IN_LO   (0x0006)
#define PIN_USAGE_VSYNC_OUT_HI  (0x0007)
#define PIN_USAGE_VSYNC_OUT_LO  (0x0008)

// Jack 4-25-00...
//#define PIN_USAGE_EXP_HI_P1     (0x0009)
//#define PIN_USAGE_EXP_LO_P1     (0x000A)
//#define PIN_USAGE_EXP_HI_P2     (0x000B)
//#define PIN_USAGE_EXP_LO_P2     (0x000C)
#define PIN_USAGE_EXP_HI        (0x0009)
#define PIN_USAGE_EXP_LO        (0x000A)
#define PIN_USAGE_EXP2_HI       (0x000B)
#define PIN_USAGE_EXP2_LO       (0x000C)
// ...Jack 4-25-00
#define PIN_USAGE_EXP_HI_DOUBLE (0x000D)
#define PIN_USAGE_EXP_LO_DOUBLE (0x000E)
#define PIN_USAGE_INPUT         (0x000F)
#define PIN_USAGE_OUTPUT        (0x0010)
#define PIN_USAGE_INTEGRATE_HI  (0x0011)
#define PIN_USAGE_INTEGRATE_LO  (0x0012)
// Jack M40...
#define PIN_USAGE_VALID_HI      (0x0013)
#define PIN_USAGE_VALID_LO      (0x0014)
// ...Jack M40

#if 0
typedef struct {
  PULSE_GATE   pg;
  int          nUnits;       /* 0: nSec;  1: Lines  */
  int          nDelay;
  int          nDelayCount;
  int          nWidth;
  int          nWidthCount;
} ICI_PULSE;
#endif

#if 0
/*
//  Many of the ICI_SPEC fields have up to three states that must be
// maintained.  The first is the state designated in the ICI file.  The
// next is the state as modified through API calls.  The third is the
// state as reconfigured to accomodate hardware limitations.
//  To distinguish among these states, the following suffixes are used
// with the field names:
//  _D: The state designated in the ICI file.
//  _A: The state that is visible to the API.
//  _H: The state constrained to hardware limitations and/or conflict-
//     ing current use (ICI or non-ICI use).
//  _0: A combination of _D and _A, used when the field cannot be modi-
//     fied through the API once it has been loaded from an ICI file.
//  _C: (Current) A combination of _A and _H when the API is allowed to
//     modify the field but further changes are never required to
//     accomodate hardware limitations or formatting.
*/
typedef struct _ICI_SPEC {
  struct _ICI_SPEC *pNext;   /* Pointer to next ICI_SPEC structure. */
  char         *pszICI;      /* Full pathname of ICI file.          */
  char         szName[12];   /* Assigned board name.                */

  char         szSerial[8];  /* Serial number (0's if not known).   */
  int          nSlot;        /* Slot number (-1 if not known).      */
  int          nBdType;      /* Board type (I-50 or I-25).          */

  char         szGAT[32];    /* Gate file name.                     */
  ICI_CHANNEL  cha[4];       /* Specified CA1-4 configuration.      */

  int          nPMask_D;     /* ICI parameters spec'd in descriptor.*/
  int          nPMask_A;     /* ICI parameters per API (HDICI_*).   */
  long         lCSync_0;     /* CSync must be from this source.     */
  int          nHFreq_0;     /* HFreq must be within 2% of this.    */
  int          nHTotal_0;    /* HTotal must be within 10 of this.   */

  PULSE_GATE   pgTrig0_0;    /* Initial TrigZero pulse processing.  */
  PULSE_GATE   pgTrig0_H;    /* Current TrigZero pulse processing.  */
  ICISIG       isiga_H[ICISIG_N]; /* Map of available DOut signals. */
  ICI_PULSE    iplsCAM1_0;   /* CamPulse1 Config. per ICI File.     */
  ICI_PULSE    iplsCAM1_H;   /* CamPulse1 Config. now used by H/W.  */
  ICI_PULSE    iplsCAM2_0;   /* CamPulse2 Config. per ICI File.     */
  ICI_PULSE    iplsCAM2_H;   /* CamPulse2 Config. now used by H/W.  */
  ICI_PULSE    iplsSTRB;     /* Strobe pulse Configuration.         */
  ICITRIG      itrgTrig_D;   /* Initial CT4 (trigger) source.       */
  ICITRIG      itrgTrig_A;   /* API-known CT4 (trigger) source.     */
  ICITRIG      itrgTrig_H;   /* Hardware CT4 (trigger) source.      */
  struct {
    PULSE_GATE pg_0;         /* Initial HSync Pulse Generation.     */
    PULSE_GATE pg_H;         /* Current HSync Pulse Generation.     */
    int        nAlign;       /* 1: Align to TrigZero pulse.         */
    int        nClock;       /* 1: 40MHz; 2: Ext Clock; 3: PLL.     */
    int        nPeriodCount;
    int        nWidthCount;
  } hs;
  struct {
    PULSE_GATE pg_0;         /* Initial VSync Pulse Generation.     */
    PULSE_GATE pg_H;         /* Current VSync Pulse Generation.     */
    int        nHSync;       /* HSync clock  0: Live; 1: Generated. */
    ICITRIG    itrg;         /* VSync trigger event.                */
    int        nSingleShot;  /* 0: Cycles; 1: Single Shot.          */
    int        nPeriodCount; /* In lines (per nHSync).              */
    int        nWidthCount;  /* In lines (per nHSync).              */
  } vs;
  int          nDOHC_D;      /* Initial high-current digital output.*/
  int          nDOHC_C;      /* High-current digital output state.  */
  ICISIG       isigaDOut_D[8];/* Initial digital output states.     */
  ICISIG       isigaDOut_A[8];/* Operational digital output states. */
  ICISIG       isigaDOut_H[8];/* Current digital output states.     */
  int          nCT1_D;       /* Default CT1 assignment.             */
  int          nCT1_A;       /* Operation CT1 assignment.           */
  int          nCT1_H;       /* Current CT1 assignment.             */
  int          nCT2_D;       /* Default CT2 assignment.             */
  int          nCT2_A;       /* Operation CT2 assignment.           */
  int          nCT2_H;       /* Current CT2 assignment.             */

  int          nCPSwap_H;    /* Flags swap of CameraPulse assignment*/
} ICI_SPEC;

/*
*/
typedef int ICI_ErrCB(
  void *p, unsigned short e, struct _ICI_FILE *picf, char *pszError
);
typedef struct _ICI_FILE {
  long      lOpenFlag;
  FILE      *fp;
  int       nReadLineNo;
  int       nThisLineNo;
  int       nLastLineNo;
  char      *pszPath;
  ICI_ErrCB *pErrCB;
  void      *pErrObj;
} ICI_FILE;
#endif
/*
//=====================================================================
*/
/*
// Prototypes of function defined in module IVT_Text.c.
*/
//+Joe 09-08-2001 void ICIopen(ICI_FILE *pif, char *pszPath);
//+Joe 09-08-2001 void ICIclose(ICI_FILE *picf);
//+Joe 09-08-2001 void ReadICILine(ICI_FILE *picf, int nLSize, char *pszLine);
int  nKeyWord(char *pszTable,char *pszWord);
int  nICICharType(char c);
int  nHexChar(char c);
char *pszICI_Tokenize(char *pszLine);
void TListWords(char *pszTList);
char *pszTokenN(char *pszTokens, int nToken);
int  nStrToFixP(char *pszN, int nPlaces, int *pnN, int *pnChars);
int  nICILineType(char *pszTokens);
//+Joe 09-08-2001 char *pszMajorICILine(ICI_FILE *picf, int nLSize, char *pszLine);


#endif  /* _H_MVISION */
