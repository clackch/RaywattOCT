/*
//=====================================================================
//
// Filename: M10_DEMO.h
//
// This module contains code specific to the October 1998 Show.
//
//=====================================================================
*/
#ifndef  _H_M10_DEMO
#define  _H_M10_DEMO

#include "hdp_lib.h"

#define CAMC_P08_DELAY    0x40
#define CAMC_P08_ENABLE   0x20
#define CAMC_P17_POLARITY 0x10
#define CAMC_P17_ENABLE   0x08
#define CAMC_CT4_POLARITY 0x04
#define CAMC_CT4_ENABLE   0x02
#define CAMC_P17_DELAY    0x01
#define CAMC_M10          0x7F

/*
//=====================================================================
//
// eM10_ihDemoGrab: Special one-frame, camera-triggered grab.
// eM10_ihDemoLamp: Special lamp control.
//
//=====================================================================
*/
#ifdef __cplusplus
extern "C" {
#endif

ERRTYPE APPTYPE eM10_ihDemoGrab(ImageHandle ih, short nLineDelay);
ERRTYPE APPTYPE eM10_ihDemoLamp(ImageHandle ih, short nLampState);

#ifdef __cplusplus
}
#endif

#endif /* _H_M10_DEMO */
