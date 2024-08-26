// WBalThread.cpp : implementation file
//

#include "stdafx.h"
#include "hdp_lib.h"
#include "hdp_err.h"
#include "WBalThread.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CWBalThread - Working thread to perform White Balance operation

IMPLEMENT_DYNCREATE(CWBalThread, CWinThread)

CWBalThread::CWBalThread()
{
  m_pWBalData      = NULL;
  m_pnReturn       = NULL;
  m_ImageHandle    = 0;
  m_pRSET          = NULL;
  m_nInterval      = 500;    // Sleep 500ms while waiting to start
}

CWBalThread::~CWBalThread()
{
}

BOOL CWBalThread::InitInstance()
{
  ERRTYPE          e;
  //
  // This method will do the entire work for the thread
  // It won't exit until the exit conditions are met.
  //
  // First, be sure all the necessary variables are set.
  //
  while (TRUE) {
    if (   (m_pnReturn != NULL)
        && (m_pWBalData != NULL)
        && (m_pRSET != NULL)
        && (m_ImageHandle != 0))
      break;
    Sleep(m_nInterval);
  }
  //
  // Run the white balance operation.  If the return value is 0,
  // everything worked so set the return value to 1 (unless the
  // return value is non-zero, the caller won't know we're really
  // done.
  //
  e = eHD_TestWhiteBalance( m_ImageHandle, m_pRSET, m_pWBalData);
  if (e == 0)
    *m_pnReturn = 1;
  else if (e == HPERR_EECORRUPT)
    *m_pnReturn = -1;
  else
    *m_pnReturn = -2;
  if (*m_pnReturn == 0)
    *m_pnReturn = 1;
  //
  // Getting to here means the exit conditions have been met
  // By returning FALSE here, won't need to implement ExitInstance()
  //
  return FALSE;
}

int CWBalThread::ExitInstance()
{
  return 0;
}

BEGIN_MESSAGE_MAP(CWBalThread, CWinThread)
	//{{AFX_MSG_MAP(CWBalThread)
		// NOTE - the ClassWizard will add and remove mapping macros here.
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CWBalThread message handlers
