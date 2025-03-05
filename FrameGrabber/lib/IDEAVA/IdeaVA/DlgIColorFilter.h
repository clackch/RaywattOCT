/*
//====================================================================
//
// $Workfile:   DlgIColorFilter.h  $
//
// $Revision:   1.0  $
//
// $Date:   24 May 2002 10:45:54  $
//
//====================================================================
*/

#if !defined(AFX_DLGICOLORFILTER_H__9C1340A3_F900_11D3_8B90_00E09807DDAD__INCLUDED_)
#define AFX_DLGICOLORFILTER_H__9C1340A3_F900_11D3_8B90_00E09807DDAD__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

class CVideoAdjust;

class CDlgIColorFilter : public CDialog
{
//
// Construction
//
public:
  CDlgIColorFilter(CWnd* pParent = NULL);   // standard constructor
//
// Dialog Data
//
  //{{AFX_DATA(CDlgIColorFilter)
  enum { IDD = IDD_ICOLOR_FILTERS };
  //}}AFX_DATA
  CVideoAdjust    *m_pDlgParent;
  BOOL             m_bInitialized;
  BOOL             m_bWaitToApply;
  BOOL             m_bNotchFilter;
  BOOL             m_bLowPassFilter;
  BOOL             m_bLumaPedestal;
  BOOL             m_bLumaDecimation;
  int              m_nLCombEnable;     // 0=Auto, 1=Manual, 2=Disabled
  int              m_nLCombValue;      // 0..7
  int              m_nCCombEnable;     // 0=Auto, 1=Manual, 2=Disabled
  int              m_nCCombValue;      // 0..7
  int              m_nLumaPeaking;     // 0=<nominal...3=maximum peaking
  int              m_nVideoFormat;     // 0=NTSC, 1=PAL
//
// Overrides
//
  // ClassWizard generated virtual function overrides
  //{{AFX_VIRTUAL(CDlgIColorFilter)
  protected:
  virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
  //}}AFX_VIRTUAL
//
// Implementation
//
public:
  int  LoadIColorFilters();
  int  SaveIColorFilters();

protected:
  void OnApplySettings();
  void OnIcolCheckAutolcomb(BOOL bApply);
  void OnIcolCheckManlcomb(BOOL bApply);
  void OnIcolCheckNolcomb(BOOL bApply);
  void OnChangeIcolEditManlcomb(BOOL bApply);

  void OnIcolCheckAutoccomb(BOOL bApply);
  void OnIcolCheckManccomb(BOOL bApply);
  void OnIcolCheckNoccomb(BOOL bApply);
  void OnChangeIcolEditManccomb(BOOL bApply);

  void OnIcolCheckNotchfil(BOOL bApply);
  void OnIcolCheckLowpass(BOOL bApply);
  void OnIcolCheckLumadec(BOOL bApply);
  void OnIcolCheckLumaped(BOOL bApply);
  void OnSelchangeIcolComboLumapeak(BOOL bApply);

  // Generated message map functions
  //{{AFX_MSG(CDlgIColorFilter)
  virtual BOOL OnInitDialog();
  virtual void OnOK();
  virtual void OnCancel();
  afx_msg void OnIcolCheckAutoccomb();
  afx_msg void OnIcolCheckAutolcomb();
  afx_msg void OnIcolCheckManccomb();
  afx_msg void OnIcolCheckManlcomb();
  afx_msg void OnIcolCheckNoccomb();
  afx_msg void OnIcolCheckNolcomb();
  afx_msg void OnIcolCheckNotchfil();
  afx_msg void OnIcolBtnDefCompHires();
  afx_msg void OnIcolBtnDefCompNorm();
  afx_msg void OnIcolBtnDefSvidHires();
  afx_msg void OnIcolBtnDefSvidNorm();
  afx_msg void OnClose();
  afx_msg void OnIcolCheckLowpass();
  afx_msg void OnIcolCheckLumadec();
  afx_msg void OnIcolCheckLumaped();
  afx_msg void OnChangeIcolEditManccomb();
  afx_msg void OnChangeIcolEditManlcomb();
  afx_msg void OnSelchangeIcolComboLumapeak();
  //}}AFX_MSG
  DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_DLGICOLORFILTER_H__9C1340A3_F900_11D3_8B90_00E09807DDAD__INCLUDED_)

//////////////////////////////////////////////////////////////////////
