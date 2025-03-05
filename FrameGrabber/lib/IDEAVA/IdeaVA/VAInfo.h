#if !defined(VAINFO_H_INCLUDED_)
#define VAINFO_H_INCLUDED_

#include "hdp_lib.h"

#define VA_STATE_UNINITIALIZED   0
#define VA_STATE_HIDDEN          1
#define VA_STATE_NORMAL          2

typedef VOID  (CALLBACK *DISPLAYPROC)( CWnd *pParent, BOOL bResize, BOOL bResnap );
typedef VOID  (CALLBACK *REPORTPROC)( CWnd *pParent, LPCTSTR pMsg, UINT nType );

typedef struct VAInfoTag {
  BoardHandle  BoardHandle;
  ImageHandle  ImageHandle;
  CWnd        *pParent;
  DISPLAYPROC  pUpdateDisplay;
  REPORTPROC   pReportMsg;
  BOOL         bHidden;
  UINT         UserQuitMessage;  // message to be posted back to application when ok/cancel is pressed
} VA_INFO;

typedef BOOL  (CALLBACK *SHOWVADIALOGPROC)( VA_INFO *VAInfo );
typedef VOID  (CALLBACK *CLOSEVADIALOGPROC)( VOID );
typedef VOID  (CALLBACK *RESTRICTRESIZINGPROC)( BOOL bRestrict );
typedef VOID  (CALLBACK *UPDATERSETPROC)( RSET *pRSET );
typedef VOID  (CALLBACK *SAVESETTINGSPROC)( CString szCHP, CString szComments, 
                                            CString szMyName, CString szMyVersion,
                                            BOOL bSilent );
typedef long  (CALLBACK *GETVASTATEPROC)( VOID );
typedef VOID  (CALLBACK *WHITEBALANCEPROC)( BOOL bSilent );

#endif // !defined(VAINFO_H_INCLUDED_)
