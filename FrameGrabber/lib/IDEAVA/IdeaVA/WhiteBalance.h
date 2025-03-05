#if !defined(AFX_WHITEBALANCE_H__BE0DD441_D45E_11D6_BF9B_0050BAD362D7__INCLUDED_)
#define AFX_WHITEBALANCE_H__BE0DD441_D45E_11D6_BF9B_0050BAD362D7__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "resource.h"

// WhiteBalance.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CWhiteBalance dialog

class CWhiteBalance : public CDialog
{
// Construction
public:
	CWhiteBalance(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(CWhiteBalance)
	enum { IDD = IDD_WHITE_BALANCE };
		// NOTE: the ClassWizard will add data members here
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CWhiteBalance)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:

	// Generated message map functions
	//{{AFX_MSG(CWhiteBalance)
		// NOTE: the ClassWizard will add member functions here
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_WHITEBALANCE_H__BE0DD441_D45E_11D6_BF9B_0050BAD362D7__INCLUDED_)
