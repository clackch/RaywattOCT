#if !defined(AviInfo_H_INCLUDED_)
#define AviInfo_H_INCLUDED_

#include "hdp_lib.h"

typedef struct AviInfoTag {
  char    *pszAviFile;
} AVI_INFO;

typedef struct
{
  int  nUsePercentOfAvailable;
  int  nDropFramesAtPercentFull;
  BOOL bRateMatch;
  BOOL bShowWriteProgress;
  BOOL bCaptureAudio;  //+Audio
} IDEAAVI_OPTIONS;


typedef BOOL (CALLBACK *P_SET_PARENT_WINDOW_PROC)( HWND hWnd );
typedef BOOL (CALLBACK *P_SHOW_DIALOG_PROC)( IDEAAVI_OPTIONS *pIdeaAVIOptions );
typedef int (CALLBACK *P_OPEN_AVIFILE_PROC)( LPCTSTR pszFileName, BITMAPINFO *pBitmapInfo, DWORD dwFrameRate, LPCSTR pszCodecName );
typedef BOOL (CALLBACK *P_ADD_FRAME_PROC)(int Handle, BITMAPINFO *pBitmapInfo, void *pBuffer, int nFrameNum);
typedef VOID (CALLBACK *P_CLOSE_AVIFILE_PROC)(int Handle);
typedef VOID (CALLBACK *P_ABORT_FLUSH_PROC)(int Handle);
typedef BOOL (CALLBACK *P_SHOW_PROGRESS_PROC)(int Handle);
typedef BOOL (CALLBACK *P_CHOOSE_CODEC_PROC)(LPCTSTR pszCurrentCodecName, LPCTSTR pszSelectedCodecName, int nNameSize, BITMAPINFO *pBitmapInfo);
typedef BOOL (CALLBACK *P_CONFIGURE_CODEC_PROC)(LPCTSTR pszCodecName);
typedef BOOL (CALLBACK *P_CAN_CONFIGURE_CODEC_PROC)(LPCTSTR pszCodecName);
typedef BOOL (CALLBACK *P_CODEC_CAN_COMPRESS_PROC)(LPCTSTR pszCodecName, BITMAPINFO *pBitmapInfo);
typedef BOOL (CALLBACK *P_SET_CODEC_PROC)(int Handle, LPCTSTR pszCodecName);
typedef BOOL (CALLBACK *P_SET_OPTIONS_PROC)(int Handle, IDEAAVI_OPTIONS *pIdeaAVIOptions);
typedef BOOL (CALLBACK *P_SET_CALLBACK_PROC)(int Handle, void *pCallback, void *pContext);
//+Audio
typedef BOOL (CALLBACK *P_CHOOSE_AUDIO_PROC)(LPCTSTR pszCurrentAudioName, LPCTSTR pszSelectedAudioName, int nNameSize );
typedef BOOL (CALLBACK *P_CONFIGURE_AUDIO_PROC)(LPCTSTR pszAudioName);
typedef BOOL (CALLBACK *P_CAN_CONFIGURE_AUDIO_PROC)(LPCTSTR pszAudioName);
typedef BOOL (CALLBACK *P_SET_AUDIO_PROC)(int Handle, LPCTSTR pszAudioName);
typedef BOOL (CALLBACK *P_CAN_CAPTURE_AUDIO_PROC)();
//-Audio

#endif // !defined(AviInfo_H_INCLUDED_)
