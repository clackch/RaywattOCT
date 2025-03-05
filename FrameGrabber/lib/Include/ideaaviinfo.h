#if !defined(AviInfo_H_INCLUDED_)
#define AviInfo_H_INCLUDED_

#include "windows.h"
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


class CIdeaAviFile
{
public:
  virtual void        Destroy() = 0;
  virtual BOOL        ShowProgress() = 0;
  virtual BOOL        Open(PCTSTR pszFileName = 0, PBITMAPINFO pBitmapInfo = 0, int nFrameRate = 1) = 0;
  virtual BOOL        AddFrame(PBITMAPINFO pBitmapInfo, void *pBuffer, int nFrameNum) = 0;
  virtual void        Close() = 0;
  virtual void        AbortFlush() = 0;
  virtual BOOL        SelectCodec(CString csCodecFriendlyName) = 0;
  virtual BOOL        SelectAudio(CString csAudioFriendlyName) = 0;
  virtual BOOL        UpdateProgress() = 0;
  virtual BOOL        CodecCanCompress(CString csCodecFriendlyName, PBITMAPINFO pBitmapInfo) = 0;
  virtual BOOL        SetOptions(IDEAAVI_OPTIONS *pIdeaAVIOptions) = 0;
  virtual BOOL        SetCallback(void *pCallback, void *pCallbackContext) = 0;
};

typedef CIdeaAviFile* (CALLBACK *P_CREATE_INSTANCE_PROC)( CWnd* pParent );
typedef BOOL (CALLBACK *P_SET_PARENT_WINDOW_PROC)( HWND hWnd );
typedef BOOL (CALLBACK *P_SHOW_DIALOG_PROC)( IDEAAVI_OPTIONS *pIdeaAVIOptions );
typedef BOOL (CALLBACK *P_OPEN_AVIFILE_PROC)( LPCTSTR pszFileName, BITMAPINFO *pBitmapInfo, DWORD dwFrameRate );
typedef BOOL (CALLBACK *P_ADD_FRAME_PROC)(BITMAPINFO *pBitmapInfo, void *pBuffer, int nFrameNum);
typedef VOID (CALLBACK *P_CLOSE_AVIFILE_PROC)();
typedef VOID (CALLBACK *P_ABORT_FLUSH_PROC)();
typedef BOOL (CALLBACK *P_SHOW_PROGRESS_PROC)();
typedef BOOL (CALLBACK *P_CHOOSE_CODEC_PROC)(LPCTSTR pszCurrentCodecName, LPCTSTR pszSelectedCodecName, int nNameSize, BITMAPINFO *pBitmapInfo);
typedef BOOL (CALLBACK *P_CONFIGURE_CODEC_PROC)(LPCTSTR pszCodecName);
typedef BOOL (CALLBACK *P_CAN_CONFIGURE_CODEC_PROC)(LPCTSTR pszCodecName);
typedef BOOL (CALLBACK *P_CODEC_CAN_COMPRESS_PROC)(LPCTSTR pszCodecName, BITMAPINFO *pBitmapInfo);
typedef BOOL (CALLBACK *P_SET_CODEC_PROC)(LPCTSTR pszCodecName);
typedef BOOL (CALLBACK *P_SET_OPTIONS_PROC)(IDEAAVI_OPTIONS *pIdeaAVIOptions);
typedef BOOL (CALLBACK *P_SET_CALLBACK_PROC)(void *pCallback, void *pContext);
typedef BOOL (CALLBACK *P_CHOOSE_AUDIO_PROC)(LPCTSTR pszCurrentAudioName, LPCTSTR pszSelectedAudioName, int nNameSize );
typedef BOOL (CALLBACK *P_CONFIGURE_AUDIO_PROC)(LPCTSTR pszAudioName);
typedef BOOL (CALLBACK *P_CAN_CONFIGURE_AUDIO_PROC)(LPCTSTR pszAudioName);
typedef BOOL (CALLBACK *P_SET_AUDIO_PROC)(LPCTSTR pszAudioName);
typedef BOOL (CALLBACK *P_CAN_CAPTURE_AUDIO_PROC)();

#endif // !defined(AviInfo_H_INCLUDED_)
