// WhiteBalance.cpp : implementation file
//

#include "stdafx.h"
#include "WhiteBalance.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CWhiteBalance dialog


CWhiteBalance::CWhiteBalance(CWnd* pParent /*=NULL*/)
	: CDialog(CWhiteBalance::IDD, pParent)
{
	//{{AFX_DATA_INIT(CWhiteBalance)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
}


void CWhiteBalance::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CWhiteBalance)
		// NOTE: the ClassWizard will add DDX and DDV calls here
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CWhiteBalance, CDialog)
	//{{AFX_MSG_MAP(CWhiteBalance)
		// NOTE: the ClassWizard will add message map macros here
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CWhiteBalance message handlers
