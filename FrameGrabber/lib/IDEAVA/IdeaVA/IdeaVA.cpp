// IdeaVA.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"
#include "VAInfo.h"
#include "IdeaVA.h"
#include "VideoAdjust.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

//
//	Note!
//
//		If this DLL is dynamically linked against the MFC
//		DLLs, any functions exported from this DLL which
//		call into MFC must have the AFX_MANAGE_STATE macro
//		added at the very beginning of the function.
//
//		For example:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// normal function body here
//		}
//
//		It is very important that this macro appear in each
//		function, prior to any calls into MFC.  This means that
//		it must appear as the first statement within the 
//		function, even before any object variable declarations
//		as their constructors may generate calls into the MFC
//		DLL.
//
//		Please see MFC Technical Notes 33 and 58 for additional
//		details.
//

/////////////////////////////////////////////////////////////////////////////
// CIdeaVAApp

BEGIN_MESSAGE_MAP(CIdeaVAApp, CWinApp)
	//{{AFX_MSG_MAP(CIdeaVAApp)
		// NOTE - the ClassWizard will add and remove mapping macros here.
		//    DO NOT EDIT what you see in these blocks of generated code!
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

CIdeaVAApp *CIdeaVAApp::StaticThis = NULL;

/////////////////////////////////////////////////////////////////////////////
// CIdeaVAApp construction

CIdeaVAApp::CIdeaVAApp()
{
	// TODO: add construction code here,
  StaticThis     = this;
  m_pVideoAdjust = NULL;
	// Place all significant initialization in InitInstance
}

CIdeaVAApp::~CIdeaVAApp()
{
  if ( m_pVideoAdjust ) delete m_pVideoAdjust;
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CIdeaVAApp object

CIdeaVAApp theApp;


/*__declspec( dllexport )*/BOOL CALLBACK CIdeaVAApp::ShowDialog( VA_INFO *pVAInfo )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  StaticThis->MyShowDialog( pVAInfo );
  return TRUE;
}


void CIdeaVAApp::MyShowDialog( VA_INFO *pVAInfo ) 
{
  if ( m_pVideoAdjust ) delete m_pVideoAdjust;
  m_pVideoAdjust = (CVideoAdjust *)new CVideoAdjust(pVAInfo->pParent);
  m_pVideoAdjust->m_UserQuitMessage = pVAInfo->UserQuitMessage;
  m_pVideoAdjust->ShowVADialog(pVAInfo->BoardHandle, pVAInfo->ImageHandle, 
          pVAInfo->pReportMsg, pVAInfo->pUpdateDisplay, pVAInfo->bHidden );
}


/*__declspec( dllexport ) */ VOID CALLBACK CIdeaVAApp::SaveSettings( CString szCHP, CString szComments, 
                                                       CString szMyName, CString szMyVersion,
                                                       BOOL bSilent )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  StaticThis->MySaveSettings(szCHP, szComments, szMyName, szMyVersion, bSilent);
}


void CIdeaVAApp::MySaveSettings( CString szCHP, CString szComments, 
                                 CString szMyName, CString szMyVersion,
                                 BOOL bSilent )
{
  if ( m_pVideoAdjust ) 
    m_pVideoAdjust->SaveVASettings(szCHP, szComments, szMyName, szMyVersion, bSilent);
}


/* __declspec( dllexport ) */ VOID CALLBACK CIdeaVAApp::CloseDialog( void )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  StaticThis->MyCloseDialog();
}


void CIdeaVAApp::MyCloseDialog( void )
{
  if ( m_pVideoAdjust ) {
    m_pVideoAdjust->DestroyWindow();
    delete m_pVideoAdjust;
    m_pVideoAdjust = NULL;
  }
}


/*__declspec( dllexport ) */ VOID CALLBACK CIdeaVAApp::UpdateRSET( RSET *pRSET )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  StaticThis->MyUpdateRSET( pRSET );
}


void CIdeaVAApp::MyUpdateRSET( RSET *pRSET )
{
  if ( m_pVideoAdjust ) m_pVideoAdjust->UpdateVARSET(pRSET);
}


/*__declspec( dllexport ) */ VOID CALLBACK CIdeaVAApp::RestrictResizing( BOOL bRestrict )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  StaticThis->MyRestrictResizing( bRestrict );
}


void CIdeaVAApp::MyRestrictResizing( BOOL bRestrict )
{
  if ( m_pVideoAdjust ) m_pVideoAdjust->RestrictVAResizing(bRestrict);
}


/*__declspec( dllexport ) */ long CALLBACK  CIdeaVAApp::GetVAState( void )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  return StaticThis->MyGetState();
}


long CIdeaVAApp::MyGetState( void )
{
  if ( m_pVideoAdjust ) {
    return m_pVideoAdjust->m_State;
  } else {
    return VA_STATE_UNINITIALIZED;
  }
}


/* __declspec( dllexport ) */ VOID CALLBACK CIdeaVAApp::WhiteBalance( BOOL bSilent )
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
  StaticThis->MyWhiteBalance(bSilent);
}


void CIdeaVAApp::MyWhiteBalance( BOOL bSilent )
{
  if ( m_pVideoAdjust )
    m_pVideoAdjust->RunWhiteBalance(bSilent);
}

