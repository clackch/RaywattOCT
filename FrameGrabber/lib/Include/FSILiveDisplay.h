#if !defined _FSI_LIVEDISLAY_H
#define _FSI_LIVEDISPLAY_H

#include <ddraw.h>

// structures and typedefs
typedef struct _tagVideoParameters
{
	DWORD dwMaxX;
	DWORD dwMaxY;
	DWORD dwPitch;
	BOOL	bInterlaced;
  DWORD dwFrameRate_ms;
  BOOL bForceDisplayUpdate;
} LiveVideoParameters;

typedef enum
{
  RENDERER_GDI,
  RENDERER_DEFAULT,
  RENDERER_DIRECTDRAW,
  RENDERER_DIRECTDRAW_COPY,
  RENDERER_DIRECT2D
} RENDERER_TYPE;

typedef struct LDInit
{
  HWND   hWnd;
  ImageHandle ih;
  BoardHandle bh;
  RSET        *prset;
  BOOL        bDecimation;
  BOOL        bScaleLiveVideo;
  COLORREF    ColorKey;
  int         nDecimateFrames;
  BOOL        bForceDisplayUpdate;
  int         nRenderer;
} LDParameters;

// functions
BOOL LDStartLiveDisplay( LDParameters *pParam );
BOOL LDStopLiveDisplay(HWND hWnd);
BOOL LDSetScrollPos( HWND hWnd, int nHorz, int nVert );
BOOL LDGetDisplayRect( HWND hWnd, RECT * );
BOOL LDRestartLiveDisplay( LDParameters *pParam );
int  LDGetDefaultRenderer();
#endif