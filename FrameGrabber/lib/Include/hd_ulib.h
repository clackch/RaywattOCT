/*
//=====================================================================
//
// Filename:  HD_ULIB.H
//
//                  Copyright (C) 1999 by Foresight Imaging, LLC
//                                    All rights (world-wide) reserved.
//=====================================================================
*/


/*
//=====================================================================
// Definitions for YC grab mode functions
//=====================================================================
*/

/*
// YC image size
*/
#define YCM_IMAGE_WIDTH        640
#define YCM_IMAGE_HEIGHT       482


/*
// YC processing mode bit flags (HD_YCINFO.dwModes)
*/
#define YCM_DEFAULT            0x00000
#define YCM_NONE               0x00000
#define YCM_ALWAYS_INIT        0x00001
#define YCM_USE_SYNC_TRACKING  0x00002


/*
// YC processing data structures
*/
typedef struct
{
  DWORD            dwModes;  /* Processing mode; set by caller    */
  int              nPhase;   /* YC phase; set by eHP_YCGrabInit() */
  double           rPhase[6];/* YC phase; set by eHP_YCGrabInit() */
  double           iPhase[6];/* YC phase; set by eHP_YCGrabInit() */
} HD_YCINFO;


/*
// YC processing functions
*/

ERRTYPE  eHP_YCGrabInit( BoardHandle  bh,
                         RSET        *pYRSET,
                         RSET        *pCRSET,
                         HD_YCINFO   *pInfo  );

ERRTYPE  eHP_YCGrab(     BoardHandle  bh,
                         BYTE        *pImage,
                         RSET        *pYRSET,
                         RSET        *pCRSET,
                         HD_YCINFO   *pInfo  );
