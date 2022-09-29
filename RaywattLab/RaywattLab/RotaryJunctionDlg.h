#pragma once
#include "resource.h"
#include "CommonDlg.h"
#include "Utility.h"

// CRotaryJunctionDlg dialog

class CRotaryJunctionDlg : public CDialogEx, CCommonDlg
{
	DECLARE_DYNAMIC(CRotaryJunctionDlg)

private:
	bool m_isClickedBackward;
	bool m_isClickedForward;

	CThread* m_pThreadInterferometer;
public:
	CRotaryJunctionDlg(CWnd* pParent = nullptr);   // standard constructor
	virtual ~CRotaryJunctionDlg();

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_ROTARY_JUNCTION_DIALOG };
#endif

private:
	static UINT threadInterferometer(LPVOID param);
protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();

	DECLARE_MESSAGE_MAP()
public:
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	afx_msg void OnDestroy();
	afx_msg void OnBnClickedButtonZaberIdle();
	afx_msg void OnBnClickedButtonZaberMove();
	afx_msg void OnBnClickedButtonZaberPullback();
	afx_msg void OnBnClickedButtonMotorPerformRun();
	afx_msg void OnBnClickedButtonMotorStop();
	afx_msg void OnBnClickedButtonSaveSettings();
	afx_msg void OnShowWindow(BOOL bShow, UINT nStatus);
};
