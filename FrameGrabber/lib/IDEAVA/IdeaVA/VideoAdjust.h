/*
//=====================================================================
//
// $Workfile:   VideoAdjust.h  $
//
// $Revision:   1.8  $
//
// $Date:   28 Sep 2012 14:15:54  $
//
//
//    Copyright(c) 2001-2008 by Foresight Imaging, LLC
//    All rights (world-wide) reserved.
//=====================================================================
*/


#if !defined(AFX_VIDEOADJUST_H__739026AC_6961_4864_B7EB_351755E53183__INCLUDED_)
#define AFX_VIDEOADJUST_H__739026AC_6961_4864_B7EB_351755E53183__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#include "DlgIColorFilter.h"

/*
// Register Definitions for the KS0127, taken
// right from the specification.  Used only for
// the I-Color board.
*/
#define KS0127_STAT     (0x00)
#define KS0127_CMDA     (0x01)
#define KS0127_CMDB     (0x02)
#define KS0127_CMDC     (0x03)
#define KS0127_CMDD     (0x04)
#define KS0127_HAVB     (0x05)
#define KS0127_HAVE     (0x06)
#define KS0127_HS1B     (0x07)
#define KS0127_HS1E     (0x08)
#define KS0127_HS2B     (0x09)
#define KS0127_HS2E     (0x0A)
#define KS0127_AGC      (0x0B)
#define KS0127_HXTRA    (0x0C)
#define KS0127_CDEM     (0x0D)
#define KS0127_PORTAB   (0x0E)
#define KS0127_LUMA     (0x0F)
#define KS0127_CON      (0x10)
#define KS0127_BRT      (0x11)
#define KS0127_CHROMA   (0x12)
#define KS0127_CHROMB   (0x13)
#define KS0127_DEMOD    (0x14)
#define KS0127_SAT      (0x15)
#define KS0127_HUE      (0x16)
#define KS0127_VERTIA   (0x17)
#define KS0127_VERTIB   (0x18)
#define KS0127_VERTIC   (0x19)
#define KS0127_HSCLL    (0x1A)
#define KS0127_HSCLH    (0x1B)
#define KS0127_VSCLL    (0x1C)
#define KS0127_VSCLH    (0x1D)
#define KS0127_OFMTA    (0x1E)
#define KS0127_OFMTB    (0x1F)
#define KS0127_VBICTL   (0x20)
#define KS0127_CCDAT2   (0x21)
#define KS0127_CCDAT1   (0x22)
#define KS0127_VBIL30   (0x23)
#define KS0127_VBIL74   (0x24)
#define KS0127_VBIL118  (0x25)
#define KS0127_VBIL1512 (0x26)
#define KS0127_TTFRAM   (0x27)
#define KS0127_TESTA    (0x28)
#define KS0127_UVOFFH   (0x29)
#define KS0127_UVOFFL   (0x2A)
#define KS0127_UGAIN    (0x2B)
#define KS0127_VGAIN    (0x2C)
#define KS0127_VAVB     (0x2D)
#define KS0127_VAVE     (0x2E)
#define KS0127_CTRACK   (0x2F)
#define KS0127_POLCTL   (0x30)
#define KS0127_REFCOD   (0x31)
#define KS0127_INVALY   (0x32)
#define KS0127_INVALU   (0x33)
#define KS0127_INVALV   (0x34)
#define KS0127_UNUSEY   (0x35)
#define KS0127_UNUSEU   (0x36)
#define KS0127_UNUSEV   (0x37)
#define KS0127_RSRV1    (0x38)
#define KS0127_RSRV2    (0x39)
#define KS0127_SHS1A    (0x3A)
#define KS0127_SHS1B    (0x3B)
#define KS0127_SHS1C    (0x3C)
#define KS0127_CMDE     (0x3D)
#define KS0127_VSDEL    (0x3E)
#define KS0127_CMDF     (0x3F)
#define KS0127_REGISTER_SIZE  64


/////////////////////////////////////////////////////////////////////////////
// CVideoAdjust dialog

class CVideoAdjust : public CDialog
{
// Construction
public:
	CVideoAdjust(CWnd* pParent = NULL);   // standard constructor
  ~CVideoAdjust();                      // standard destructor

// Dialog Data
	//{{AFX_DATA(CVideoAdjust)
	enum { IDD = IDD_IRGB_VIDEO_ADJUSTMENTS };
	//}}AFX_DATA

  long            m_State;
  BoardHandle     m_BoardHandle;
  ImageHandle     m_ImageHandle;
  CWnd           *m_pParent;
  REPORTPROC      m_pReportMsg;
  DISPLAYPROC     m_pUpdateDisplay;
  RSET            m_RSet;
  int             m_nBoardType;
  int             m_nBrightness;
  int             m_nContrast;
  int             m_nHue;
  int             m_nSaturation;
  int             m_nWidth;
  int             m_nHeight;
  int             m_nXPosition;
  int             m_nYPosition;
  int             m_nPhase;
  int             m_nFinePhase;
	int             m_nBlackLevelMin;
  int             m_nBlackLevelMax;
  int             m_nGainMin;
  int             m_nGainMax;
  int             m_nYPbPrSaturation;
  int             m_nYPbPrClampPlacement;
  int             m_nYPbPrClampDuration;
  int             m_nOriginalGain;
  int             m_nOriginalBlackLevel;
  int             m_nOriginalBrightness;
  int             m_nOriginalContrast;
  int             m_nOriginalHue;
  int             m_nOriginalSaturation;
  int             m_nOriginalWidth;
  int             m_nOriginalXPosition;
  int             m_nOriginalPhase;
  int             m_nOriginalFinePhase;
  int             m_nOriginalHeight;
  int             m_nOriginalYPosition;
  int             m_nOriginalYPbPrSaturation;
  int             m_nOriginalYPbPrClampPlacement;
  int             m_nOriginalYPbPrClampDuration;
  CString         m_szHardwareProfilePath;
  CString         m_szComment;
  char            m_szBoard[40];
  char            m_szBoardSN[30];
  char            m_szCrTime[30];
  char            m_szCreator[30];
  char            m_szCrVers[10];
  char            m_szCrRels[30];
  char            m_szLibVers[10];
  BOOL            m_bRestrictResizing;
  CDlgIColorFilter
                 *m_pIColorFilters;
  BYTE           *m_pRegisterSet;
  int             m_SliderMax;
  UINT           m_UserQuitMessage;


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CVideoAdjust)
	public:
	virtual BOOL DestroyWindow();
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
public:
  void ShowVADialog(  BoardHandle BoardHandle, ImageHandle ImageHandle,
                      REPORTPROC pReportMsg, DISPLAYPROC pUpdateDisplay, BOOL bHidden );
  void SaveVASettings(CString szCHP, CString szComments, 
                      CString szMyName, CString szMyVersion, BOOL bSilent);
  void RunWhiteBalance(BOOL bSilent);
  BOOL OnInitDialog(); 
  void SetBrightness( long b );
  void SetContrast( long c );
  long SetWidth( long w );
  long SetXPosition( long p );
  void SetPhase( long p );
  void SetFinePhase( long p );
  long SetHeight( long h );
  long SetYPosition( long p );
  void SetHue( long h );
  void SetSaturation( long s );
  void SetYPbPrSaturation( long s );
  void SetYPbPrClampPlacement( long s );
  void SetYPbPrClampDuration( long s );
  void WriteCommentString( FILE *fpCHP, CString &szComment);
  void ReadCHPHeader(const char *szCHP) ;
  void RestrictVAResizing( BOOL bRestrict);
  void UpdateImage(BOOL bResize = TRUE);
  void UpdateVARSET( RSET *pRSET);
  BOOL HasSizeChanged();
  void CloseIColorFilters(BOOL bApply);
  void SaveIColorRegisters();
  void RestoreIColorRegisters();
  BOOL IsColorBoard();
  BOOL IsTVBoard();
  BOOL IsRGBBoard();
  BOOL IsHighSpeedRGBBoard();
  BOOL IsMonochromeInput();

protected:

	// Generated message map functions
	//{{AFX_MSG(CVideoAdjust)
	afx_msg void OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	afx_msg void OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);
	virtual void OnCancel();
	afx_msg void OnSaveSettings();
	virtual void OnOK();
	afx_msg void OnFilters();
	afx_msg void OnClose();
  afx_msg void OnWhiteBalance();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()

  void ReportMsg(LPCTSTR pMsg, UINT nType = MB_OK) const;
};


//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_VIDEOADJUST_H__739026AC_6961_4864_B7EB_351755E53183__INCLUDED_)
