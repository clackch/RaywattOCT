#if !defined(AFX_WBALTHREAD_H__21874960_55EE_11D6_BF9B_0050BAD362D7__INCLUDED_)
#define AFX_WBALTHREAD_H__21874960_55EE_11D6_BF9B_0050BAD362D7__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// WBalThread.h : header file
//



/////////////////////////////////////////////////////////////////////////////
// CWBalThread thread

class CWBalThread : public CWinThread
{
	DECLARE_DYNCREATE(CWBalThread)
protected:
	CWBalThread();           // protected constructor used by dynamic creation

// Attributes
public:
  HD_WBALDATA     *m_pWBalData;
  ImageHandle      m_ImageHandle;
  RSET            *m_pRSET;
  int              m_nInterval;
  int             *m_pnReturn;

// Operations
public:

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CWBalThread)
	public:
	virtual BOOL InitInstance();
	virtual int ExitInstance();
	//}}AFX_VIRTUAL

// Implementation
protected:
	virtual ~CWBalThread();

	// Generated message map functions
	//{{AFX_MSG(CWBalThread)
		// NOTE - the ClassWizard will add and remove member functions here.
	//}}AFX_MSG

	DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_WBALTHREAD_H__21874960_55EE_11D6_BF9B_0050BAD362D7__INCLUDED_)
