// IdeaVA.h : main header file for the IDEAVA DLL
//

#if !defined(AFX_IDEAVA_H__900C88C5_69A1_11D6_BF9B_0050BAD362D7__INCLUDED_)
#define AFX_IDEAVA_H__900C88C5_69A1_11D6_BF9B_0050BAD362D7__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"		// main symbols

class CVideoAdjust;
/////////////////////////////////////////////////////////////////////////////
// CIdeaVAApp
// See IdeaVA.cpp for the implementation of this class
//

class CIdeaVAApp : public CWinApp
{
private:
  static CIdeaVAApp   *StaticThis;
  CVideoAdjust        *m_pVideoAdjust;

public:
	CIdeaVAApp();
  ~CIdeaVAApp();
  void MyShowDialog( VA_INFO *pVAInfo );
  void MySaveSettings( CString szCHP, CString szComments, 
                       CString szMyName, CString szMyVersion, BOOL bSilent );
  void MyCloseDialog( void );
  void MyUpdateRSET( RSET *pRSET );
  void MyRestrictResizing( BOOL bRestrict );
  long MyGetState( void );
  void MyWhiteBalance( BOOL bSilent );
  /*__declspec( dllexport )*/  static BOOL CALLBACK ShowDialog( VA_INFO *pVAInfo );
  /* __declspec( dllexport ) */ static VOID CALLBACK SaveSettings( CString szCHP, CString szComments, 
                                             CString szMyName, CString szMyVersion,
                                             BOOL bSilent );
 /* __declspec( dllexport ) */ static VOID CALLBACK CloseDialog( void );
 /* __declspec( dllexport ) */ static VOID CALLBACK UpdateRSET( RSET *pRSET );
 /* __declspec( dllexport ) */ static VOID CALLBACK RestrictResizing( BOOL bRestrict );
 /* __declspec( dllexport ) */ static long CALLBACK GetVAState( void );
 /* __declspec( dllexport ) */ static VOID CALLBACK WhiteBalance( BOOL bSilent );

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CIdeaVAApp)
	//}}AFX_VIRTUAL

	//{{AFX_MSG(CIdeaVAApp)
		// NOTE - the ClassWizard will add and remove member functions here.
		//    DO NOT EDIT what you see in these blocks of generated code !
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};


/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_IDEAVA_H__900C88C5_69A1_11D6_BF9B_0050BAD362D7__INCLUDED_)
