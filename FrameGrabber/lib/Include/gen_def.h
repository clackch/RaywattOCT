/*
// *************************************************************************
//
// Copyright (c)  2011 Foresight Imaging, LLC
//
// This program and the information contained in it is confidential
// and proprietary to Imation Corp. and may not be distributed, reproduced,
// modified, or used in any manner without the prior written permission of
// Imation Corp.
//
// This software is based upon software developed by Foresight Imaging, LLC.,
// Chelmsford, MA.
//
// Header Name:  gen_def.h
//
//
// *************************************************************************
*/

#ifndef _INC_GEN_DEF
#define _INC_GEN_DEF

#if !defined (PSOS)
#include <stdlib.h>
#endif

/*
//=====================================================================
// Set Symbols (_ANYWIN and DOS_LARGE) for Operating environment.
//=====================================================================
*/
#undef DOS_LARGE

#if    defined(MSCWIN)||defined(WINDLL)||defined(_WIN32)||defined(_WIN)

#ifdef _ANYWIN
);message "*** Should not #Define _ANYWIN.";
#endif
#undef _ANYWIN
#define _ANYWIN

#else  /*  else not *WIN*   */

#ifdef _ANYWIN
);message "*** #Define WINDLL, MSCWIN, _WIN32, or _WIN, not _ANYWIN.";
#else  /*  else not _ANYWIN */
#if defined(MSDOS) && !defined(WCC)
#define DOS_LARGE
#define SIMPLY_DOS 1
#endif
#endif /* _ANYWIN           */

#endif  /* defined(*WIN*)   */


#if defined (UNIX)
#undef TARGET_ERROR
#if defined (_ANYWIN)||defined(_WINNT)||defined(_WINDOWS)
#define TARGET_ERROR
#endif
#if defined(MSDOS)||defined(_DOS)||defined(WCC)||defined(OS9)||defined(VME)
#define TARGET_ERROR
#endif
#if defined(TARGET_ERROR)
#error "Incompatible target symbols defined"
#endif
#endif  /* defined(UNIX) */


/*
//=====================================================================
//   Determine symbols for library declarations and include <windows.h>
// if need be.
//=====================================================================
*/
#undef EXPORT

#ifdef _ANYWIN

#include "windows.h"

#ifdef _WIN32

#if defined (MSC_VER)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif
#define DSGLOBAL
typedef long SDWORD;
#else     /* if _WIN32 ... else  */

#define EXPORT _export
#define DSGLOBAL  __loadds

#endif    /* if _WIN32 ... endif */

#define LIBTYPE   WINAPI
#define APPTYPE   WINAPI DSGLOBAL

#else     /* if _ANYWIN ... else  */

#define _INC_WINDOWS
#define _WINDOWS_
#undef  CALLBACK
#define CALLBACK

#define EXPORT
#define DSGLOBAL
#define LIBTYPE
#define APPTYPE
#define PASCAL
 
typedef struct {int left,top,right,bottom;} RECT;

#endif    /* if _ANYWIN ... endif */

#ifdef UNIX

#define far
#define FAR
#define near
#define NEAR
#define huge

#define _inp     inp
#define _inpw    inpw
#define _outp    outp
#define _outpw   outpw

typedef unsigned int   UINT;
typedef int            INT;

typedef int            BOOL;               /* Define types used by code */
typedef int            HANDLE;             /*   taken from elsewhere    */
typedef unsigned char  BYTE;               /*                           */
//typedef int16_t        WORD;               /*                           */
typedef unsigned short  WORD;               /*                           */
typedef u_int32_t      DWORD;              /* LINUX64 port              */
typedef int32_t        SDWORD;             /* LINUX64 port              */
typedef u_int64_t      ULONGLONG;          /* 11.24.2010 changes to live_vid */
typedef void far *     LPVOID;             /*                           */
typedef char far *     LPSTR;              /*                           */
typedef char far *     LPTSTR;             /*                           */
typedef const char far * LPCSTR;           /*                           */
typedef const char far * LPCTSTR;          /*                           */
typedef void           VOID;               /*                           */
typedef BYTE far *     LPBYTE;             /*                           */
typedef WORD far *     LPWORD;             /*                           */
typedef DWORD far *    LPDWORD;            /*                           */
typedef BYTE huge *    HPBYTE;             /*                           */
typedef WORD huge *    HPWORD;             /*                           */
typedef DWORD huge *   HPDWORD;            /*                           */
typedef long           LONG;               /*                           */
typedef unsigned long *LPOVERLAPPED;       /*                           */
typedef struct{int x, y;} POINT;
typedef void (far *FARPROC)(void);
typedef void *        PVOID;
typedef BYTE           UINT8;              /*                           */
typedef WORD           UINT16;             /*                           */
typedef DWORD          UINT32;             /*                           */
typedef long long      _int64;
typedef long long      __int64;
typedef u_int64_t     LARGE_INTEGER;
#define  strcmpi(a, b)   strcasecmp(a, b)
#define  _strcmpi(a, b)  strcasecmp(a, b)
#define  _strnicmp       strncasecmp
#define  sprintf_s       snprintf
#define  strcpy_s(a, b, c)   strncpy(a, c, b )
#define  lstrlen         strlen

/* New definitions made necessary by MyMessageBox */
#define MB_ICONSTOP (0x10)
#define MB_ICONINFORMATION (0x40)
#define MB_ICONERROR (0x10)
#define IDYES (6)
#define IDNO  (7)
#define MB_YESNO (4)
#define MB_OK  (0)
#define MB_OKCANCEL  (1)

#define ZeroMemory(a,b) memset(a, 0, b)

#define BI_RGB 0

#if 0
#pragma pack(1)
typedef struct tagBITMAPFILEHEADER {    /* bmfh */
    WORD    bfType;
    DWORD   bfSize;
    WORD    bfReserved1;
    WORD    bfReserved2;
    DWORD   bfOffBits;
} BITMAPFILEHEADER;

#pragma pack(1)
typedef struct tagBITMAPINFOHEADER {    /* bmih */
    DWORD   biSize;
    DWORD    biWidth;
    DWORD    biHeight;
    WORD    biPlanes;
    WORD    biBitCount;
    DWORD   biCompression;
    DWORD   biSizeImage;
    DWORD    biXPelsPerMeter;
    DWORD    biYPelsPerMeter;
    DWORD   biClrUsed;
    DWORD   biClrImportant;
} BITMAPINFOHEADER;
#endif
#endif


/*
//=====================================================================
//=====================================================================
*/


#define CNULL ((char *)0)
#define FNULL ((FILE *)0)
#define WNULL ((WORD *)0)
#define TRUE 1
#define FALSE 0

/*
//   Use of "BOOL" is to be avoided since it is non-length specific.
// BOOLs is an explicitly 16 bit version of BOOL and is therefore more
// portable among compiler environments.
//   Note that using "short", "long", etc instead of "int" will not
// always suffice since "enum"s are "int" and therefore always
// always development environment specific.
*/                             
typedef short          BOOLs;
typedef unsigned short ERRTYPE;

#if defined (DOS_LARGE) || defined (WCC)
typedef unsigned char  BYTE;
typedef unsigned short WORD;
typedef unsigned long  DWORD;
#define FAR
#endif

#if defined (WCC)
#define _rccoord rccoord
#define _timeb   timeb
#define _ftime   ftime
#define _snprintf   _bprintf
#define _vsnprintf  _vbprintf
#define _outp    outp
#define _outpw   outpw
#define _inp     inp
#define _inpw    inpw
#endif

#endif  /* if _INC_GEN_DEF ... endif */
