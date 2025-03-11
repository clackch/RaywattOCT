/*
// 1995Aug29 Scott  Releasable version.
// 1995May04 Scott  Copied from \IMATOOLS\INC\FINETIME.H for HD Plus.
//=====================================================================
//
// Filename: ..\HDP\GENERAL\SRC\HP_FTIME.H
//
//   This is a HI*DEF Plus version of "FINETIME.H".
//
//=====================================================================
*/

#ifndef _HP_FTIME
#define _HP_FTIME

#include "gen_def.h"

/*
//=====================================================================
//
//   From this point on, the declarations in this file (HP_FTIME.H)
// should be identical to those found in FINETIME.H.
//
//=====================================================================
*/

typedef struct {
  unsigned short date;
  short          nResv;     /* for DWORD alignment */
  DWORD  time;
} FINE_TIME;

typedef FINE_TIME *pFINE_TIME;

#ifdef __cplusplus
extern "C" {
#endif

pFINE_TIME    APPTYPE fine_delta(pFINE_TIME, SDWORD);
pFINE_TIME    APPTYPE fine_add(pFINE_TIME, FINE_TIME);
SDWORD        APPTYPE fine_subtract(pFINE_TIME, FINE_TIME);
DWORD         APPTYPE fine_now(pFINE_TIME);
pFINE_TIME    APPTYPE fine_later(pFINE_TIME, DWORD);
void          APPTYPE fine_abswait(FINE_TIME);
void          APPTYPE fine_wait(DWORD);
char *        APPTYPE fine_ascii_local(char *,FINE_TIME);

#ifdef __cplusplus
}
#endif

#endif /* _HP_FTIME */
