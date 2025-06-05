/*
//====================================================================
//
// $Workfile:   DlgIColorFilter.cpp  $
//
// $Revision:   1.2  $
//
// $Date:   23 Jan 2008 09:13:58  $
//
//    Copyright(c) 2001-2008 by Foresight Imaging, LLC
//    All rights (world-wide) reserved.
//====================================================================
*/

#include "stdafx.h"
#include "resource.h"
#include "VAInfo.h"
#include "DlgIColorFilter.h"
#include "VideoAdjust.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif


CDlgIColorFilter::CDlgIColorFilter(CWnd* pParent /*=NULL*/)
  : CDialog(CDlgIColorFilter::IDD, pParent)
{
  //{{AFX_DATA_INIT(CDlgIColorFilter)
  //}}AFX_DATA_INIT
  m_pDlgParent      = NULL;
  m_bNotchFilter    = FALSE;
  m_bLowPassFilter  = TRUE;
  m_bLumaPedestal   = FALSE;
  m_bLumaDecimation = FALSE;
  m_nLCombValue     = 0;
  m_nLCombEnable    = 0; // Auto
  m_nCCombValue     = 0;
  m_nCCombEnable    = 0; // Auto
  m_nLumaPeaking    = 0; // <nominal
  m_bInitialized    = FALSE;
  m_bWaitToApply    = FALSE;
  m_nVideoFormat    = 0; // NTSC
}


void CDlgIColorFilter::DoDataExchange(CDataExchange* pDX)
{
  CDialog::DoDataExchange(pDX);
  //{{AFX_DATA_MAP(CDlgIColorFilter)
  //}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CDlgIColorFilter, CDialog)
  //{{AFX_MSG_MAP(CDlgIColorFilter)
  ON_BN_CLICKED(IDC_ICOL_CHECK_AUTOCCOMB, OnIcolCheckAutoccomb)
  ON_BN_CLICKED(IDC_ICOL_CHECK_AUTOLCOMB, OnIcolCheckAutolcomb)
  ON_BN_CLICKED(IDC_ICOL_CHECK_MANCCOMB, OnIcolCheckManccomb)
  ON_BN_CLICKED(IDC_ICOL_CHECK_MANLCOMB, OnIcolCheckManlcomb)
  ON_BN_CLICKED(IDC_ICOL_CHECK_NOCCOMB, OnIcolCheckNoccomb)
  ON_BN_CLICKED(IDC_ICOL_CHECK_NOLCOMB, OnIcolCheckNolcomb)
  ON_BN_CLICKED(IDC_ICOL_CHECK_NOTCHFIL, OnIcolCheckNotchfil)
  ON_BN_CLICKED(IDC_ICOL_BTN_DEF_COMPHIRES, OnIcolBtnDefCompHires)
  ON_BN_CLICKED(IDC_ICOL_BTN_DEF_COMPNORM, OnIcolBtnDefCompNorm)
  ON_BN_CLICKED(IDC_ICOL_BTN_DEF_SVIDHIRES, OnIcolBtnDefSvidHires)
  ON_BN_CLICKED(IDC_ICOL_BTN_DEF_SVIDNORM, OnIcolBtnDefSvidNorm)
  ON_WM_CLOSE()
  ON_BN_CLICKED(IDC_ICOL_CHECK_LOWPASS, OnIcolCheckLowpass)
  ON_BN_CLICKED(IDC_ICOL_CHECK_LUMADEC, OnIcolCheckLumadec)
  ON_BN_CLICKED(IDC_ICOL_CHECK_LUMAPED, OnIcolCheckLumaped)
  ON_EN_CHANGE(IDC_ICOL_EDIT_MANCCOMB, OnChangeIcolEditManccomb)
  ON_EN_CHANGE(IDC_ICOL_EDIT_MANLCOMB, OnChangeIcolEditManlcomb)
  ON_CBN_SELCHANGE(IDC_ICOL_COMBO_LUMAPEAK, OnSelchangeIcolComboLumapeak)
  //}}AFX_MSG_MAP
END_MESSAGE_MAP()

//////////////////////////////////////////////////////////////////////
// CDlgIColorFilter message handlers

BOOL CDlgIColorFilter::OnInitDialog()
{
  BOOL             bNotch;

  CDialog::OnInitDialog();
  //
  // Notch & lowpass filter, luma pedestal and decimation filter
  //
  ((CButton *)GetDlgItem(IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( (m_bNotchFilter ? 1 : 0));
  ((CButton *)GetDlgItem(IDC_ICOL_CHECK_LOWPASS))
      ->SetCheck( (m_bLowPassFilter ? 1 : 0));
  ((CButton *)GetDlgItem(IDC_ICOL_CHECK_LUMAPED))
      ->SetCheck( (m_bLumaPedestal ? 1 : 0));
  ((CButton *)GetDlgItem(IDC_ICOL_CHECK_LUMADEC))
      ->SetCheck( (m_bLumaDecimation ? 1 : 0));
  //
  // LComb Filter
  //
  if ( (m_nLCombValue < 0) || (m_nLCombValue > 7) )
    m_nLCombValue = 0;
  SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
  ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
        ->SetRange(0, 7);
  ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
        ->SetPos( m_nLCombValue);
  switch( m_nLCombEnable)
  {
    case 2:  // Disable
      if ((m_nLCombValue != 3) && (m_nLCombValue != 4))
      {
        m_nLCombValue = 4;
        SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
        ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
            ->SetPos( m_nLCombValue);
      }
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
          ->SetCheck( 1);
      GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( FALSE);
      GetDlgItem( IDC_ICOL_SPIN_MANLCOMB)->EnableWindow( FALSE);
      break;

    case 1:  // Manual
      if ((m_nLCombValue == 3) || (m_nLCombValue == 4))
      {
        m_nLCombValue = 0;
        SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
        ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
            ->SetPos( m_nLCombValue);
      }
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
          ->SetCheck( 1);
      GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( TRUE);
      GetDlgItem( IDC_ICOL_SPIN_MANLCOMB)->EnableWindow( TRUE);
      //
      // Turning on the luma comb turns off the notch filter
      //
      bNotch = m_bNotchFilter;
      m_bNotchFilter = FALSE;
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
          ->SetCheck( 0);
      //
      // The notch filter change may have changed
      //
      if (bNotch != m_bNotchFilter)
        SaveIColorFilters();
      break;

    case 0:  // Auto
    default:
      m_nLCombEnable = 0; // Force the value
      if ((m_nLCombValue == 3) || (m_nLCombValue == 4))
      {
        m_nLCombValue = 0;
        SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
        ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
            ->SetPos( m_nLCombValue);
      }
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
          ->SetCheck( 1);
      GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( FALSE);
      GetDlgItem( IDC_ICOL_SPIN_MANLCOMB)->EnableWindow( FALSE);
      //
      // Turning on the luma comb turns off the notch filter
      //
      bNotch = m_bNotchFilter;
      m_bNotchFilter = FALSE;
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
          ->SetCheck( 0);
      //
      // The notch filter change may have changed
      //
      if (bNotch != m_bNotchFilter)
        SaveIColorFilters();
      break;
  }
  //
  // CComb Filter
  //
  if ( (m_nCCombValue < 0) || (m_nCCombValue > 7) )
    m_nCCombValue = 0;
  SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
  ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
        ->SetRange(0, 7);
  ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
        ->SetPos( m_nCCombValue);
  switch( m_nCCombEnable)
  {
    case 2:  // Disable
      if (m_nCCombValue != 7)
      {
        m_nCCombValue = 7;
        SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
        ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
            ->SetPos( m_nCCombValue);
      }
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
          ->SetCheck( 1);
      GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( FALSE);
      GetDlgItem( IDC_ICOL_SPIN_MANCCOMB)->EnableWindow( FALSE);
      break;

    case 1:  // Manual
      if (m_nCCombValue == 7)
      {
        m_nCCombValue = 0;
        SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
        ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
            ->SetPos( m_nCCombValue);
      }
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
          ->SetCheck( 1);
      GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( TRUE);
      GetDlgItem( IDC_ICOL_SPIN_MANCCOMB)->EnableWindow( TRUE);
      break;

    case 0:  // Auto
    default:
      m_nCCombEnable = 0; // Force the value
      if (m_nCCombValue == 7)
      {
        m_nCCombValue = 0;
        SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
        ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
            ->SetPos( m_nCCombValue);
      }
      ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOCCOMB))
          ->SetCheck( 1);
      GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( FALSE);
      GetDlgItem( IDC_ICOL_SPIN_MANCCOMB)->EnableWindow( FALSE);
      break;
  }
  //
  // Luma horiz peaking control
  //
  if ( (m_nLumaPeaking < 0) || (m_nLumaPeaking > 3))
    m_nLumaPeaking = 0;
  ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
        ->SetCurSel( m_nLumaPeaking);

  m_bInitialized = TRUE;
  return TRUE;  // return TRUE unless you set the focus to a control
                // EXCEPTION: OCX Property Pages should return FALSE
}


//
// Update the I-Color registers based on the current settings.
//
void CDlgIColorFilter::OnApplySettings()
{
  //
  // If the dialog elements aren't initialized, don't apply
  // the settings yet.
  //
  if (   (m_pDlgParent == NULL)
      || (!m_bInitialized)
      ||  m_bWaitToApply)
    return;
  //
  // First, we need to update member variables based
  // on the states of the current controls
  //
  // Notch & low pass filter, luma pedestal and decimation filter
  //
  m_bNotchFilter =
    (((CButton *)GetDlgItem(IDC_ICOL_CHECK_NOTCHFIL))->GetCheck() != 0);
  m_bLowPassFilter =
    (((CButton *)GetDlgItem(IDC_ICOL_CHECK_LOWPASS))->GetCheck() != 0);
  m_bLumaPedestal =
    (((CButton *)GetDlgItem(IDC_ICOL_CHECK_LUMAPED))->GetCheck() != 0);
  m_bLumaDecimation =
    (((CButton *)GetDlgItem(IDC_ICOL_CHECK_LUMADEC))->GetCheck() != 0);
  //
  // LComb Filter
  //
  m_nLCombValue = GetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB);
  if ( (m_nLCombValue < 0) || (m_nLCombValue > 7) )
    m_nLCombValue = 0;

  m_nLCombEnable = 0;
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
          ->GetCheck() == 1)
    m_nLCombEnable = 2;
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
          ->GetCheck() == 1)
    m_nLCombEnable = 1;
  //
  // CComb Filter
  //
  m_nCCombValue = GetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB);
  if ( (m_nCCombValue < 0) || (m_nCCombValue > 7) )
    m_nCCombValue = 0;

  m_nCCombEnable = 0;
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
          ->GetCheck() == 1)
    m_nCCombEnable = 2;
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
          ->GetCheck() == 1)
    m_nCCombEnable = 1;
  //
  // Luma horiz peaking control
  //
  m_nLumaPeaking = ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
        ->GetCurSel();
  if ( (m_nLumaPeaking < 0) || (m_nLumaPeaking > 3))
    m_nLumaPeaking = 0;
  //
  // Update the board registers
  //
  SaveIColorFilters();
}


void CDlgIColorFilter::OnOK()
{
  //
  // Don't accept an OK keypress if we're modeless
  //
}


void CDlgIColorFilter::OnCancel()
{
  //
  // Treat a cancel as a close
  //
  this->OnClose();
}


void CDlgIColorFilter::OnClose()
{
  if (m_pDlgParent == NULL)
    CDialog::OnClose();
  else
    m_pDlgParent->CloseIColorFilters( FALSE);
}


void CDlgIColorFilter::OnIcolCheckAutolcomb()
{
  OnIcolCheckAutolcomb(TRUE);
}


void CDlgIColorFilter::OnIcolCheckAutolcomb(BOOL bApply)
{
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
          ->GetCheck() == 1)
  {
    //
    // Apply all the changes at the end
    //
    m_bWaitToApply = TRUE;
    m_nLCombEnable = 0;
    GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( TRUE);
    m_nLCombValue = GetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB);
    if ((m_nLCombValue == 3) || (m_nLCombValue == 4))
    {
      //
      // Index 3 or 4 disables luma comb.  Can't leave these set
      //
      m_nLCombValue =  0;
      SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
      ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
          ->SetPos( m_nLCombValue);
    }
    //
    // Enabling Auto turns off the manual controls
    //
    GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( FALSE);
    GetDlgItem( IDC_ICOL_SPIN_MANLCOMB)->EnableWindow( FALSE);
    //
    // Turning on the luma comb turns off the notch filter
    //
    m_bNotchFilter = FALSE;
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
        ->SetCheck( 0);
    m_bWaitToApply = FALSE;
    if (bApply)
      OnApplySettings();
  }
}


void CDlgIColorFilter::OnIcolCheckManlcomb()
{
  OnIcolCheckManlcomb(TRUE);
}


void CDlgIColorFilter::OnIcolCheckManlcomb(BOOL bApply)
{
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
          ->GetCheck() == 1)
  {
    //
    // Apply all the changes at the end
    //
    m_bWaitToApply = TRUE;
    m_nLCombEnable = 1;
    //
    // Enabling Manual turns on the manual controls
    //
    GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( TRUE);
    GetDlgItem( IDC_ICOL_SPIN_MANLCOMB)->EnableWindow( TRUE);
    m_nLCombValue = GetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB);
    if ((m_nLCombValue == 3) || (m_nLCombValue == 4))
    {
      //
      // Index 3 or 4 disables luma comb.  Can't leave these set
      //
      m_nLCombValue =  0;
      SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
      ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
          ->SetPos( m_nLCombValue);
    }
    //
    // Turning on the luma comb turns off the notch filter
    //
    m_bNotchFilter = FALSE;
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
        ->SetCheck( 0);
    m_bWaitToApply = FALSE;
    if (bApply)
      OnApplySettings();
  }
}


void CDlgIColorFilter::OnIcolCheckNolcomb()
{
  OnIcolCheckNolcomb(TRUE);
}


void CDlgIColorFilter::OnIcolCheckNolcomb(BOOL bApply)
{
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
          ->GetCheck() == 1)
  {
    //
    // Apply all the changes at the end
    //
    m_bWaitToApply = TRUE;
    m_nLCombEnable = 2;
    m_nLCombValue =  4; // Index 3 or 4 disables luma comb.  Choose 4.
    GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( TRUE);
    SetDlgItemInt( IDC_ICOL_EDIT_MANLCOMB, m_nLCombValue);
    ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANLCOMB))
        ->SetPos( m_nLCombValue);
    //
    // Disabling comb turns off the manual controls
    //
    GetDlgItem( IDC_ICOL_EDIT_MANLCOMB)->EnableWindow( FALSE);
    GetDlgItem( IDC_ICOL_SPIN_MANLCOMB)->EnableWindow( FALSE);
    m_bWaitToApply = FALSE;
    if (bApply)
      OnApplySettings();
  }
}


void CDlgIColorFilter::OnChangeIcolEditManlcomb()
{
  OnChangeIcolEditManlcomb(TRUE);
}


void CDlgIColorFilter::OnChangeIcolEditManlcomb(BOOL bApply)
{
  if (bApply)
    OnApplySettings();
}


void CDlgIColorFilter::OnIcolCheckAutoccomb()
{
  OnIcolCheckAutoccomb(TRUE);
}


void CDlgIColorFilter::OnIcolCheckAutoccomb(BOOL bApply)
{
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOCCOMB))
          ->GetCheck() == 1)
  {
    //
    // Apply all the changes at the end
    //
    m_bWaitToApply = TRUE;
    m_nCCombEnable = 0;
    GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( TRUE);
    m_nCCombValue = GetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB);
    if (m_nCCombValue == 7)
    {
      //
      // Index 7 disables chroma comb.  Can't leave this set
      //
      m_nCCombValue =  0;
      SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
      ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
          ->SetPos( m_nCCombValue);
    }
    //
    // Enabling Auto turns off the manual controls
    //
    GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( FALSE);
    GetDlgItem( IDC_ICOL_SPIN_MANCCOMB)->EnableWindow( FALSE);
    m_bWaitToApply = FALSE;
    if (bApply)
      OnApplySettings();
  }
}


void CDlgIColorFilter::OnIcolCheckManccomb()
{
  OnIcolCheckManccomb(TRUE);
}


void CDlgIColorFilter::OnIcolCheckManccomb(BOOL bApply)
{
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
          ->GetCheck() == 1)
  {
    //
    // Apply all the changes at the end
    //
    m_bWaitToApply = TRUE;
    m_nCCombEnable = 1;
    GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( TRUE);
    m_nCCombValue = GetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB);
    if (m_nCCombValue == 7)
    {
      //
      // Index 7 disables chroma comb.  Can't leave this set
      //
      m_nCCombValue =  0;
      SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
      ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
          ->SetPos( m_nCCombValue);
    }
    //
    // Enabling Manual turns on the manual controls
    //
    GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( TRUE);
    GetDlgItem( IDC_ICOL_SPIN_MANCCOMB)->EnableWindow( TRUE);
    m_bWaitToApply = FALSE;
    if (bApply)
      OnApplySettings();
  }
}


void CDlgIColorFilter::OnIcolCheckNoccomb()
{
  OnIcolCheckNoccomb(TRUE);
}


void CDlgIColorFilter::OnIcolCheckNoccomb(BOOL bApply)
{
  if (((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
          ->GetCheck() == 1)
  {
    //
    // Apply all the changes at the end
    //
    m_bWaitToApply = TRUE;
    m_nCCombEnable = 2;
    m_nCCombValue =  7; // Filter index 7 -> no chroma comb
    GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( TRUE);
    SetDlgItemInt( IDC_ICOL_EDIT_MANCCOMB, m_nCCombValue);
    ((CSpinButtonCtrl*) GetDlgItem( IDC_ICOL_SPIN_MANCCOMB))
        ->SetPos( m_nCCombValue);
    //
    // Disabling comb turns off the manual controls
    //
    GetDlgItem( IDC_ICOL_EDIT_MANCCOMB)->EnableWindow( FALSE);
    GetDlgItem( IDC_ICOL_SPIN_MANCCOMB)->EnableWindow( FALSE);
    if (bApply)
      OnApplySettings();
  }
}


void CDlgIColorFilter::OnChangeIcolEditManccomb()
{
  OnChangeIcolEditManccomb(TRUE);
}


void CDlgIColorFilter::OnChangeIcolEditManccomb(BOOL bApply)
{
  if (bApply)
    OnApplySettings();
}


void CDlgIColorFilter::OnIcolCheckNotchfil()
{
  OnIcolCheckNotchfil(TRUE);
}


void CDlgIColorFilter::OnIcolCheckNotchfil(BOOL bApply)
{
  //
  // Notch filter won't engage if luma comb filter is enabled
  //
  if (m_nLCombEnable != 2)
  {
    m_bNotchFilter = FALSE;
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( 0);
  }
  else
  {
    m_bNotchFilter = !m_bNotchFilter;
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( (m_bNotchFilter ? 1 : 0));
  }
  if (bApply)
    OnApplySettings();
}


void CDlgIColorFilter::OnIcolCheckLowpass()
{
  OnIcolCheckLowpass(TRUE);
}


void CDlgIColorFilter::OnIcolCheckLowpass(BOOL bApply)
{
  m_bLowPassFilter = !m_bLowPassFilter;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LOWPASS))
    ->SetCheck( (m_bLowPassFilter ? 1 : 0));
  if (bApply)
    OnApplySettings();
}


void CDlgIColorFilter::OnIcolCheckLumadec()
{
  OnIcolCheckLumadec(TRUE);
}


void CDlgIColorFilter::OnIcolCheckLumadec(BOOL bApply)
{
  m_bLumaDecimation = !m_bLumaDecimation;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMADEC))
    ->SetCheck( (m_bLumaDecimation ? 1 : 0));
  if (bApply)
    OnApplySettings();
}


void CDlgIColorFilter::OnIcolCheckLumaped()
{
  OnIcolCheckLumaped(TRUE);
}


void CDlgIColorFilter::OnIcolCheckLumaped(BOOL bApply)
{
  m_bLumaPedestal = !m_bLumaPedestal;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMAPED))
    ->SetCheck( (m_bLumaPedestal ? 1 : 0));
  if (bApply)
    OnApplySettings();
}


void CDlgIColorFilter::OnSelchangeIcolComboLumapeak()
{
  OnSelchangeIcolComboLumapeak(TRUE);
}


void CDlgIColorFilter::OnSelchangeIcolComboLumapeak(BOOL bApply)
{
  int              nSel;

  nSel = ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
              ->GetCurSel();
  if (nSel == m_nLumaPeaking)
    return; // No change
  if (bApply)
    OnApplySettings();
}


//
// Defaults for normal S-Video input video are:
//
// * Luma comb filter:    Auto (NTSC), DISABLE [=4] (PAL)
// * Chroma comb filter:  Auto
// * Low pass filter:     ENABLE
// * Notch filter:        DISABLE
// * Luma peaking:        Minimal [=0]
// * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
// * Luma decimation:     ON
//
void CDlgIColorFilter::OnIcolBtnDefSvidNorm()
{
  BOOL             bPAL;

  bPAL = (this->m_nVideoFormat == 1);
  //
  // Apply all the changes at the end
  //
  m_bWaitToApply = TRUE;
  if (bPAL)
  {
    //
    // * Luma comb filter:    DISABLE [=4] (PAL)
    //   (OnIcolCheckNolcomb() sets comb value to 4)
    //
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
        ->SetCheck( 0);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
        ->SetCheck( 0);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
        ->SetCheck( 1);
    OnIcolCheckNolcomb(FALSE);
  }
  else
  {
    //
    // * Luma comb filter:    Auto (NTSC)
    //
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
        ->SetCheck( 1);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
        ->SetCheck( 0);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
        ->SetCheck( 0);
    OnIcolCheckAutolcomb(FALSE);
  }
  //
  // * Chroma comb filter:  Auto
  //
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOCCOMB))
      ->SetCheck( 1);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
      ->SetCheck( 0);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
      ->SetCheck( 0);
  OnIcolCheckAutoccomb(FALSE);
  //
  // * Low pass filter:     ENABLE
  //
  m_bLowPassFilter = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LOWPASS))
      ->SetCheck( 1);
  //
  // * Notch filter:        DISABLE
  //
  m_bNotchFilter = FALSE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( 0);
  //
  // * Luma peaking:        Minimal [=0]
  //
  m_nLumaPeaking = 0;
  ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
        ->SetCurSel( m_nLumaPeaking);
  //
  // * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
  //
  if (bPAL)
  {
    m_bLumaPedestal = FALSE;
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMAPED))
        ->SetCheck( 0);
  }
  else
  {
    m_bLumaPedestal = TRUE;
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMAPED))
        ->SetCheck( 1);
  }
  //
  // * Luma decimation:     ON
  //
  m_bLumaDecimation = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMADEC))
      ->SetCheck( 1);
  //
  // Apply the changes
  //
  m_bWaitToApply = FALSE;
  OnApplySettings();
}


//
// Defaults for Hi-Resolution S-Video input video are:
//
// * Luma comb filter:    Auto (NTSC), DISABLE[=4] (PAL)
// * Chroma comb filter:  Auto
// * Low pass filter:     ENABLE
// * Notch filter:        DISABLE
// * Luma peaking:        Maximum[=3] (NTSC), Nominal[=1] (PAL)
// * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
// * Luma decimation:     ON
//
void CDlgIColorFilter::OnIcolBtnDefSvidHires()
{
  BOOL             bPAL;

  bPAL = (this->m_nVideoFormat == 1);
  //
  // Apply all the changes at the end
  //
  m_bWaitToApply = TRUE;
  //
  // * Luma comb filter:    Auto (NTSC), DISABLE[=4] (PAL)
  //   (OnIcolCheckNolcomb() sets comb value to 4)
  //
  if (bPAL)
  {
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
        ->SetCheck( 0);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
        ->SetCheck( 0);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
        ->SetCheck( 1);
    OnIcolCheckNolcomb(FALSE);
  }
  else
  {
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
        ->SetCheck( 1);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
        ->SetCheck( 0);
    ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
        ->SetCheck( 0);
    OnIcolCheckNolcomb(FALSE);
  }
  //
  // * Chroma comb filter:  Auto
  //
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOCCOMB))
      ->SetCheck( 1);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
      ->SetCheck( 0);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
      ->SetCheck( 0);
  OnIcolCheckAutoccomb(FALSE);
  //
  // * Low pass filter:     ENABLE
  //
  m_bLowPassFilter = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LOWPASS))
      ->SetCheck( 1);
  //
  // * Notch filter:        DISABLE
  //
  m_bNotchFilter = FALSE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( 0);
  //
  // * Luma peaking:        Maximum[=3] (NTSC), Nominal[=1] (PAL)
  //
  if (bPAL)
    m_nLumaPeaking = 1;
  else
    m_nLumaPeaking = 3;
  ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
        ->SetCurSel( m_nLumaPeaking);
  //
  // * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
  //
  if (bPAL)
    m_bLumaPedestal = FALSE;
  else
    m_bLumaPedestal = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMAPED))
      ->SetCheck( (m_bLumaPedestal ? 1 : 0));
  //
  // * Luma decimation:     ON
  //
  m_bLumaDecimation = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMADEC))
      ->SetCheck( 1);
  //
  // Apply the changes
  //
  m_bWaitToApply = FALSE;
  OnApplySettings();
}


//
// Defaults for normal Composite input video are:
//
// * Luma comb filter:    Auto
// * Chroma comb filter:  Auto
// * Low pass filter:     ENABLE
// * Notch filter:        DISABLE
// * Luma peaking:        Increased[=2] (NTSC), Minimal[=0] (PAL)
// * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
// * Luma decimation:     ON (NTSC), OFF (PAL)
//
void CDlgIColorFilter::OnIcolBtnDefCompNorm()
{
  BOOL             bPAL;

  bPAL = (this->m_nVideoFormat == 1);
  //
  // Apply all the changes at the end
  //
  m_bWaitToApply = TRUE;
  //
  // * Luma comb filter:    Auto
  //
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
      ->SetCheck( 1);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
      ->SetCheck( 0);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
      ->SetCheck( 0);
  OnIcolCheckAutolcomb(FALSE);
  //
  // * Chroma comb filter:  Auto
  //
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOCCOMB))
      ->SetCheck( 1);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
      ->SetCheck( 0);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
      ->SetCheck( 0);
  OnIcolCheckAutoccomb(FALSE);
  //
  // * Low pass filter:     ENABLE
  //
  m_bLowPassFilter = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LOWPASS))
      ->SetCheck( 1);
  //
  // * Notch filter:        DISABLE
  //
  m_bNotchFilter = FALSE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( 0);
  //
  // * Luma peaking:        Increased[=2] (NTSC), Minimal[=0] (PAL)
  //
  if (bPAL)
    m_nLumaPeaking = 0;
  else
    m_nLumaPeaking = 2;
  ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
        ->SetCurSel( m_nLumaPeaking);
  //
  // * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
  //
  if (bPAL)
    m_bLumaPedestal = FALSE;
  else
    m_bLumaPedestal = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMAPED))
      ->SetCheck( (m_bLumaPedestal ? 1 : 0));
  //
  // * Luma decimation:     ON (NTSC), OFF (PAL)
  //
  if (bPAL)
    m_bLumaDecimation = FALSE;
  else
    m_bLumaDecimation = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMADEC))
      ->SetCheck( (m_bLumaDecimation ? 1 : 0));
  //
  // Apply the changes
  //
  m_bWaitToApply = FALSE;
  OnApplySettings();
}


//
// Defaults for hi-resolution Composite input video are:
//
// * Luma comb filter:    Auto
// * Chroma comb filter:  Auto
// * Low pass filter:     ENABLE
// * Notch filter:        DISABLE
// * Luma peaking:        Maximum[=3] (NTSC), Nominal[=1] (PAL)
// * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
// * Luma decimation:     ON (NTSC), OFF (PAL)
//
void CDlgIColorFilter::OnIcolBtnDefCompHires()
{
  BOOL             bPAL;

  bPAL = (this->m_nVideoFormat == 1);
  //
  // Apply all the changes at the end
  //
  m_bWaitToApply = TRUE;
  //
  // * Luma comb filter:    Auto
  //
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOLCOMB))
      ->SetCheck( 1);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANLCOMB))
      ->SetCheck( 0);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOLCOMB))
      ->SetCheck( 0);
  OnIcolCheckAutolcomb(FALSE);
  //
  // * Chroma comb filter:  Auto
  //
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_AUTOCCOMB))
      ->SetCheck( 1);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_MANCCOMB))
      ->SetCheck( 0);
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOCCOMB))
      ->SetCheck( 0);
  OnIcolCheckAutoccomb(FALSE);
  //
  // * Low pass filter:     ENABLE
  //
  m_bLowPassFilter = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LOWPASS))
      ->SetCheck( 1);
  //
  // * Notch filter:        DISABLE
  //
  m_bNotchFilter = FALSE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_NOTCHFIL))
      ->SetCheck( 0);
  //
  // * Luma peaking:        Maximum[=3] (NTSC), Nominal[=1] (PAL)
  //
  if (bPAL)
    m_nLumaPeaking = 1;
  else
    m_nLumaPeaking = 3;
  ((CComboBox*) GetDlgItem( IDC_ICOL_COMBO_LUMAPEAK))
        ->SetCurSel( m_nLumaPeaking);
  //
  // * Luma pedestal:       ENABLE (NTSC), DISABLE (PAL)
  //
  if (bPAL)
    m_bLumaPedestal = FALSE;
  else
    m_bLumaPedestal = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMAPED))
      ->SetCheck( (m_bLumaPedestal ? 1 : 0));
  //
  // * Luma decimation:     ON (NTSC), OFF (PAL)
  //
  if (bPAL)
    m_bLumaDecimation = FALSE;
  else
    m_bLumaDecimation = TRUE;
  ((CButton*) GetDlgItem( IDC_ICOL_CHECK_LUMADEC))
      ->SetCheck( (m_bLumaDecimation ? 1 : 0));
  //
  // Apply the changes
  //
  m_bWaitToApply = FALSE;
  OnApplySettings();
}


//
// Load the I-Color filter dialog with the current I-Color
// board settings.  Return 0 if unable to read the
// settings, otherwise returns non-zero.
//
int CDlgIColorFilter::LoadIColorFilters()
{
  ERRTYPE          e;
  BYTE            *pRegisterSet;
  UpdateRegisterSet
                   stRegSet;
  int              nFormat;
  char             szFormat[255];

  if (m_pDlgParent == NULL)
    return 0;
  if (m_pDlgParent->m_BoardHandle < 0)
    return 0;
  //
  // The dialog needs to know the video format; this is used
  // to optimize the behavior of the S-Video/Composite video
  // buttons.
  //
  nFormat = 0;  // Default to NTSC
  e = eHP_GetControlValue( m_pDlgParent->m_BoardHandle,
                           "VideoFormat",
                           sizeof( szFormat),
                           &szFormat);
  if (e == 0)
  {
    if (_strcmpi( szFormat, "PAL") == 0)
      nFormat = 1;
  }
  if ((nFormat < 0) || (nFormat > 1))
    nFormat = 0;
  //
  // Allocate and get the register set
  //
  pRegisterSet = (BYTE *) new BYTE[KS0127_REGISTER_SIZE];
  if (pRegisterSet == (BYTE*) 0)
  {
    return 0;
  }
  memset( pRegisterSet, 0, KS0127_REGISTER_SIZE);
  stRegSet.pKS0127Registers = pRegisterSet;
  stRegSet.pRSet = &m_pDlgParent->m_RSet;

  e = eHP_GetControlValue( m_pDlgParent->m_BoardHandle,
                           "KS0127RegisterSet",
                           sizeof( UpdateRegisterSet),
                           (void*) &stRegSet);
  if (e)
  {
    if (pRegisterSet != NULL)
      delete pRegisterSet;
    return 0;
  }

  m_nVideoFormat = nFormat;
  m_bLowPassFilter = (pRegisterSet[KS0127_DEMOD] & 0x20 )==0;
  m_bLumaPedestal = ( pRegisterSet[KS0127_LUMA] & 0x10 )>0;
  m_bNotchFilter = ( pRegisterSet[KS0127_LUMA] & 0x4 )>0;
  m_nLumaPeaking = (pRegisterSet[KS0127_LUMA] & 0x3);

  if (pRegisterSet[KS0127_VERTIA] & 0x80)
  {
    //
    // Luma comb is either off or uses manual coefficients
    //
    m_nLCombEnable = 1;
    m_nLCombValue = (pRegisterSet[KS0127_VERTIA] & 0x70)>>4;
    if (   (m_nLCombValue == 3)
        || (m_nLCombValue == 4))
      m_nLCombEnable = 2;   // Coeff. index 3 & 4 disable filter
  }
  else
  {
    //
    // Luma comb auto selects coefficients
    //
    m_nLCombEnable = 0;
    m_nLCombValue = (pRegisterSet[KS0127_VERTIA] & 0x70)>>4;
  }

  if ( pRegisterSet[KS0127_VERTIB] & 0x8 )
    m_bLumaDecimation = FALSE; // Decimation filter is reversed
  else
    m_bLumaDecimation = TRUE;

  if (pRegisterSet[KS0127_VERTIC] & 0x80)
  {
    //
    // Chroma comb is either off or uses manual coefficients
    //
    m_nCCombEnable = 1;
    m_nCCombValue = (pRegisterSet[KS0127_VERTIC] & 0x70)>>4;
    if (m_nCCombValue == 7)
      m_nCCombEnable = 2; // Coeff index 7 disables filter
  }
  else
  {
    //
    // Chroma comb auto selects coefficients
    //
    m_nCCombEnable = 0;
    m_nCCombValue = (pRegisterSet[KS0127_VERTIC] & 0x70)>>4;
  }
  //
  // Deallocate the handle and register set
  //
  if (pRegisterSet != NULL)
    delete pRegisterSet;
  return 1;
}


//
// Updates I-Color board settings with values from the member
// variables of the I-Color filter dialog.  Returns
// 0 if unable to write to board, otherwise returns non-zero.
//
int CDlgIColorFilter::SaveIColorFilters()
{
  ERRTYPE          e;
  BYTE            *pRegisterSet;
  UpdateRegisterSet
                   stRegSet;

  if (m_pDlgParent == NULL)
    return 0;
  if (m_pDlgParent->m_BoardHandle < 0)
    return 0;
  //
  // Allocate and get the register set
  //
  pRegisterSet = (BYTE *) new BYTE[KS0127_REGISTER_SIZE];
  if (pRegisterSet == (BYTE*) 0)
    return 0;

  memset( pRegisterSet, 0, KS0127_REGISTER_SIZE);
  stRegSet.pKS0127Registers = pRegisterSet;
  stRegSet.pRSet = &m_pDlgParent->m_RSet;

  e = eHP_GetControlValue( m_pDlgParent->m_BoardHandle,
                           "KS0127RegisterSet",
                           sizeof( UpdateRegisterSet),
                           (void*) &stRegSet);

  if (e)
  {
    if (pRegisterSet != NULL)
      delete pRegisterSet;
    return 0;
  }

  pRegisterSet[KS0127_DEMOD] &= 0xDF;
  if (!m_bLowPassFilter)
    pRegisterSet[KS0127_DEMOD] |= 0x20;

  pRegisterSet[KS0127_LUMA] &= 0xEF;
  if (m_bLumaPedestal)
    pRegisterSet[KS0127_LUMA] |= 0x10;
  pRegisterSet[KS0127_LUMA] &= 0xFB;
  if (m_bNotchFilter)
    pRegisterSet[KS0127_LUMA] |= 0x4;
  pRegisterSet[KS0127_LUMA] &= 0xFC;
  pRegisterSet[KS0127_LUMA]|= (m_nLumaPeaking & 0x3);

  pRegisterSet[KS0127_VERTIA] &= 0x8F;
  pRegisterSet[KS0127_VERTIA] |= ((m_nLCombValue<<4) & 0x70);
  switch( m_nLCombEnable)
  {
    case 2: // Disabled
      pRegisterSet[KS0127_VERTIA] &= 0x7F;
      pRegisterSet[KS0127_VERTIA] |= 0x80;
      //
      // Select coefficient 3 or 4 to disable filter
      //
      pRegisterSet[KS0127_VERTIA] &= 0x8F;
      pRegisterSet[KS0127_VERTIA] |= ((0x4<<4) & 0x70);
      break;

    case 1: // Manual
      pRegisterSet[KS0127_VERTIA] &= 0x7F;
      pRegisterSet[KS0127_VERTIA] |= 0x80;
      break;

    case 0: // Auto
    default:
      pRegisterSet[KS0127_VERTIA] &= 0x7F;
      break;
  }

  pRegisterSet[KS0127_VERTIB] &= 0xF7;
  if (!m_bLumaDecimation)
    pRegisterSet[KS0127_VERTIB] |= 0x8;

  pRegisterSet[KS0127_VERTIC] &= 0x8F;
  pRegisterSet[KS0127_VERTIC] |= ((m_nCCombValue<<4) & 0x70);
  switch( m_nCCombEnable)
  {
    case 2: // Disabled
      pRegisterSet[KS0127_VERTIC] &= 0x7F;
      pRegisterSet[KS0127_VERTIC] |= 0x80;
      //
      // Select coefficient 7 to disable filter
      //
      pRegisterSet[KS0127_VERTIC] &= 0x8F;
      pRegisterSet[KS0127_VERTIC] |= 0x70;
      break;

    case 1: // Manual
      pRegisterSet[KS0127_VERTIC] &= 0x7F;
      pRegisterSet[KS0127_VERTIC] |= 0x80;
      break;

    case 0: // Auto
    default:
      pRegisterSet[KS0127_VERTIC] &= 0x7F;
      break;
  }
  //
  // Write back the register set
  //
  e = eHP_SetControlValue( m_pDlgParent->m_BoardHandle,
                           "KS0127RegisterSet",
                           sizeof( UpdateRegisterSet),
                           (void*) &stRegSet);
  //
  // Deallocate the handle and register set
  //
  if (pRegisterSet != NULL)
    delete pRegisterSet;

  if (e)
    return 0;
  else
  {
    // Update the current image (resnap, etc.) as needed
    m_pDlgParent->UpdateImage(FALSE);
    return 1;
  }
}
//////////////////////////////////////////////////////////////////////
