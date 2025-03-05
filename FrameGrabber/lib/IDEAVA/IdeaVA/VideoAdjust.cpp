// VideoAdjust.cpp : implementation file
//

#include "stdafx.h"
#include "Resource.h"
#include "VAInfo.h"
#include "DlgIColorFilter.h"
#include "VideoAdjust.h"
#include "hdp_err.h"
#include "WhiteBalance.h"
#include "WBalThread.h"


#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CVideoAdjust dialog


CVideoAdjust::CVideoAdjust(CWnd* pParent /*=NULL*/)
	: CDialog(CVideoAdjust::IDD, pParent)
{
 	//{{AFX_DATA_INIT(CVideoAdjust)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
  m_State             = VA_STATE_UNINITIALIZED;
  m_pParent           = pParent;
  m_bRestrictResizing = FALSE;
  m_BoardHandle       = -1;
  m_pIColorFilters    = NULL;
  m_pRegisterSet      = NULL;
  m_SliderMax         = 100;
}

CVideoAdjust::~CVideoAdjust()
{
}

void CVideoAdjust::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CVideoAdjust)
		// NOTE: the ClassWizard will add DDX and DDV calls here
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CVideoAdjust, CDialog)
	//{{AFX_MSG_MAP(CVideoAdjust)
	ON_WM_HSCROLL()
	ON_WM_VSCROLL()
	ON_BN_CLICKED(IDC_SAVE_SETTINGS, OnSaveSettings)
	ON_BN_CLICKED(ID_FILTERS, OnFilters)
	ON_BN_CLICKED(IDC_WHITEBALANCE, OnWhiteBalance)
	ON_WM_CLOSE()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CVideoAdjust message handlers

BOOL CVideoAdjust::IsColorBoard() 
{
  switch( m_nBoardType ) {
    case HIDEF_ICOLOR:
    case HIDEF_ICOLORMV:
    case HIDEF_IRGB25:
    case HIDEF_IRGB50:
    case HIDEF_IRGB75:
    case HIDEF_IRGB25MV:
    case HIDEF_IRGB50MV:
    case HIDEF_IRGB75MV:
    case HIDEF_IRGB165:
    case HIDEF_IRGB170:
    case HIDEF_IRGB200:
    case HIDEF_IRGBI64_170:
    case HIDEF_ACCUSTREAM_VDR:
    case HIDEF_ACCUSTREAM_205A:
    case HIDEF_ACCUSTREAM_50A:
    case HIDEF_ACCUSTREAM_75A:
    case HIDEF_ACCUSTREAM_170_PLUS:
    case HIDEF_ACCUSTREAM_75_PLUS:
    case HIDEF_ACCUSTREAM_50_PLUS:
    case HIDEF_ACCUSTREAM_EXPRESS_170:
    case HIDEF_ACCUSTREAM_EXPRESS_50:
    case HIDEF_ACCUSTREAM_EXPRESS_75:
    case HIDEF_ACCUSTREAM_EXPRESS_HD:
    case HIDEF_ACCUSTREAM_EXPRESS_HD50:
    case HIDEF_ACCUSTREAM_EXPRESS_HD75:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
    case HIDEF_ACCUSTREAM_EXPRESS_1000:
    case HIDEF_ACCUSTREAM_EXPRESS_1000_75:
    case HIDEF_ACCUSTREAM_EXPRESS_1000_SDI:
    case HIDEF_ACCUSTREAM_EXPRESS_2000:
    case HIDEF_ACCUSTREAM_EXPRESS_2000_75:
    case HIDEF_ACCUSTREAM_EXPRESS_2000_SDI:
      return TRUE;
      break;
    default:
      break;
  }
	return FALSE;
}

BOOL CVideoAdjust::IsTVBoard() 
{
  switch( m_nBoardType ) {
    case HIDEF_ICOLOR:
    case HIDEF_ICOLORMV:
      return TRUE;
      break;
    case HIDEF_ACCUSTREAM_205A:
    case HIDEF_ACCUSTREAM_50A:
    case HIDEF_ACCUSTREAM_75A:
    case HIDEF_ACCUSTREAM_EXPRESS_HD:
    case HIDEF_ACCUSTREAM_EXPRESS_HD50:
    case HIDEF_ACCUSTREAM_EXPRESS_HD75:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
      {
        UpdateVideoSettingLong uvl;
        uvl.lValue = 0;
        uvl.pRSet  = 0;
        eHP_GetControlValue( m_BoardHandle, "SDTVMode", sizeof( uvl), &uvl );
        if( uvl.lValue )
          return TRUE;
      }
    default:
      break;
  }
	return FALSE;
}

BOOL CVideoAdjust::IsRGBBoard() 
{
  switch( m_nBoardType ) {
    case HIDEF_IRGB25:
    case HIDEF_IRGB50:
    case HIDEF_IRGB75:
    case HIDEF_IRGB25MV:
    case HIDEF_IRGB50MV:
    case HIDEF_IRGB75MV:
    case HIDEF_IRGB165:
    case HIDEF_IRGB170:
    case HIDEF_IRGB200:
    case HIDEF_IRGBI64_170:
    case HIDEF_ACCUSTREAM_205A:
    case HIDEF_ACCUSTREAM_50A:
    case HIDEF_ACCUSTREAM_75A:
    case HIDEF_ACCUSTREAM_VDR:
    case HIDEF_ACCUSTREAM_170_PLUS:
    case HIDEF_ACCUSTREAM_75_PLUS:
    case HIDEF_ACCUSTREAM_50_PLUS:
    case HIDEF_ACCUSTREAM_50:
    case HIDEF_ACCUSTREAM_75:
    case HIDEF_ACCUSTREAM_EXPRESS_170:
    case HIDEF_ACCUSTREAM_EXPRESS_50:
    case HIDEF_ACCUSTREAM_EXPRESS_75:
    case HIDEF_ACCUSTREAM_EXPRESS_HD:
    case HIDEF_ACCUSTREAM_EXPRESS_HD50:
    case HIDEF_ACCUSTREAM_EXPRESS_HD75:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
    case HIDEF_ACCUSTREAM_EXPRESS_1000:
    case HIDEF_ACCUSTREAM_EXPRESS_1000_75:
    case HIDEF_ACCUSTREAM_EXPRESS_1000_SDI:
    case HIDEF_ACCUSTREAM_EXPRESS_2000:
    case HIDEF_ACCUSTREAM_EXPRESS_2000_75:
    case HIDEF_ACCUSTREAM_EXPRESS_2000_SDI:
      return TRUE;
      break;
    default:
      break;
  }
	return FALSE;
}

BOOL CVideoAdjust::IsHighSpeedRGBBoard() 
{
  switch( m_nBoardType ) {
    case HIDEF_IRGB165:
    case HIDEF_IRGB170:
    case HIDEF_IRGB200:
    case HIDEF_IRGBI64_170:
    case HIDEF_ACCUSTREAM_205A:
    case HIDEF_ACCUSTREAM_VDR:
    case HIDEF_ACCUSTREAM_50A:
    case HIDEF_ACCUSTREAM_75A:
    case HIDEF_ACCUSTREAM_170_PLUS:
    case HIDEF_ACCUSTREAM_75_PLUS:
    case HIDEF_ACCUSTREAM_50_PLUS:
    case HIDEF_ACCUSTREAM_EXPRESS_170:
    case HIDEF_ACCUSTREAM_EXPRESS_50:
    case HIDEF_ACCUSTREAM_EXPRESS_75:
    case HIDEF_ACCUSTREAM_EXPRESS_HD:
    case HIDEF_ACCUSTREAM_EXPRESS_HD50:
    case HIDEF_ACCUSTREAM_EXPRESS_HD75:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
    case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
    case HIDEF_ACCUSTREAM_EXPRESS_1000:
    case HIDEF_ACCUSTREAM_EXPRESS_1000_75:
    case HIDEF_ACCUSTREAM_EXPRESS_1000_SDI:
    case HIDEF_ACCUSTREAM_EXPRESS_2000:
    case HIDEF_ACCUSTREAM_EXPRESS_2000_75:
    case HIDEF_ACCUSTREAM_EXPRESS_2000_SDI:
      return TRUE;
      break;
    default:
      break;
  }
	return FALSE;
}

void CVideoAdjust::ReportMsg(LPCTSTR pMsg, UINT nType) const
{
  if ( m_pReportMsg )
    (*m_pReportMsg)(m_pParent, pMsg, nType);
  else
    AfxMessageBox(pMsg, nType);
}

void CVideoAdjust::ShowVADialog( BoardHandle BoardHandle, ImageHandle ImageHandle,
               REPORTPROC pReportMsg, DISPLAYPROC pUpdateDisplay, BOOL bHidden ) 
{
  BOOL             bReturn;
  short            hdt;

  m_BoardHandle    = BoardHandle;
  m_ImageHandle    = ImageHandle;
  m_pReportMsg     = pReportMsg;
  m_pUpdateDisplay = pUpdateDisplay;

  hdt = 3;
  nHP_GetBoardInfo( m_BoardHandle, HPEE_BOARD_TYPE, 2, &hdt);
  m_nBoardType = hdt;

  if ( IsTVBoard() ) {
      bReturn = Create(IDD_ICOLOR_VIDEO_ADJUSTMENTS,   NULL);
  } else if ( IsRGBBoard() ) {
      bReturn = Create(IDD_IRGB_VIDEO_ADJUSTMENTS,   NULL);
  } else {
      bReturn = Create(IDD_IMONO_VIDEO_ADJUSTMENTS,   NULL);
  }
 	
  if(!bReturn)   //Create failed.
    ReportMsg("Error creating Dialog");
  else
    if (bHidden) {
      m_State = VA_STATE_HIDDEN;
      ShowWindow(SW_HIDE);
    } else {
      m_State = VA_STATE_NORMAL;
      ShowWindow(SW_SHOW);
    }
}


BOOL CVideoAdjust::OnInitDialog() 
{
  HD_MODELIST *phml;
  HD_MODE     *phm;
  int         nMode;
  long        lGain;
  long        lBlackLevel;
  long        lHue;
  long        lSaturation;
  DWORD       dwSpread;
  char        szValBuf[32];
  CStatic     *pVal;
  CSliderCtrl *pSlider;
  CSpinButtonCtrl *pSpin;
  UpdateVideoSetting UVSetting;

	CDialog::OnInitDialog();

  phml = pHD_ModeList();
  phm = phml->hm;

  eHD_RSET_Get( m_ImageHandle, &m_RSet );

  if ( IsTVBoard() )
  {
    long lBrightness;
    long lContrast;
    long lHue;
    long lSaturation;

    switch( m_nBoardType )
    {
      case HIDEF_ICOLOR:
      case HIDEF_ICOLORMV:        
        SaveIColorRegisters();
        break;
      default:
        break;
    }
    m_SliderMax = 255;
    UVSetting.pRSet = &m_RSet;
    eHP_GetControlValue( m_BoardHandle, "Brightness",
                         sizeof( UpdateVideoSetting ), &UVSetting );
    m_nOriginalBrightness = lBrightness = UVSetting.bValue;
    m_nBrightness = lBrightness;
    pSlider = (CSliderCtrl *)GetDlgItem( IDC_BRIGHTNESS_CTL );
    pVal = (CStatic *)GetDlgItem( IDC_BRIGHTNESS_VAL );
    pSlider->SetRange( 0, m_SliderMax, TRUE );
    pSlider->SetPos( m_SliderMax - m_nBrightness );
    wsprintf( szValBuf, "%d", m_nBrightness );
    pVal->SetWindowText( szValBuf );

    UVSetting.pRSet = &m_RSet;
    eHP_GetControlValue( m_BoardHandle, "Contrast",
                         sizeof( UpdateVideoSetting ), &UVSetting );
    m_nOriginalContrast = lContrast = UVSetting.bValue;
    m_nContrast = lContrast;
    pSlider = (CSliderCtrl *)GetDlgItem( IDC_CONTRAST_CTL );
    pVal = (CStatic *)GetDlgItem( IDC_CONTRAST_VAL );
    pSlider->SetRange( 0, m_SliderMax, TRUE );
    pSlider->SetPos( m_SliderMax - m_nContrast );
    wsprintf( szValBuf, "%d", m_nContrast );
    pVal->SetWindowText( szValBuf );

    UVSetting.pRSet = &m_RSet;
    eHP_GetControlValue( m_BoardHandle, "Hue",
                         sizeof( UpdateVideoSetting ), &UVSetting );
    m_nOriginalHue = lHue = UVSetting.bValue;
    m_nHue = lHue;
    pSlider = (CSliderCtrl *)GetDlgItem( IDC_HUE_CTL );
    pVal = (CStatic *)GetDlgItem( IDC_HUE_VAL );
    pSlider->SetRange( 0, m_SliderMax, TRUE );
    pSlider->SetPos( m_SliderMax - m_nHue );
    wsprintf( szValBuf, "%d", m_nHue );
    pVal->SetWindowText( szValBuf );

    UVSetting.pRSet = &m_RSet;
    eHP_GetControlValue( m_BoardHandle, "Saturation",
                         sizeof( UpdateVideoSetting ), &UVSetting );
    m_nOriginalSaturation = lSaturation = UVSetting.bValue;
    m_nSaturation = lSaturation;
    pSlider = (CSliderCtrl *)GetDlgItem( IDC_SATURATION_CTL );
    pVal = (CStatic *)GetDlgItem( IDC_SATURATION_VAL );
    pSlider->SetRange( 0, m_SliderMax, TRUE );
    pSlider->SetPos( m_SliderMax - m_nSaturation );
    wsprintf( szValBuf, "%d", m_nSaturation );
    pVal->SetWindowText( szValBuf );

    m_nWidth = m_nOriginalWidth = m_RSet.lRegs[HPR_WIDTH];
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_WIDTH );
    pVal = (CStatic *)GetDlgItem( IDC_WIDTH_VAL );
    pSpin->SetRange( 0, 2047 );
    pSpin->SetPos( m_nWidth );
    wsprintf( szValBuf, "%d", m_nWidth );
    pVal->SetWindowText( szValBuf );

    UVSetting.pRSet = &m_RSet;
    eHP_GetControlValue( m_BoardHandle, "HorizontalPosition",
                         sizeof( UpdateVideoSetting ), &UVSetting );
    m_nXPosition = m_nOriginalXPosition = UVSetting.bValue;
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_X_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_X_POSITION_VAL );
    pSpin->SetRange( 0, 1023 );
    pSpin->SetPos( m_nXPosition );
    wsprintf( szValBuf, "%d", m_nXPosition );
    pVal->SetWindowText( szValBuf );

    m_nHeight = m_nOriginalHeight = m_RSet.lRegs[HPR_HEIGHT];
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_HEIGHT );
    pVal = (CStatic *)GetDlgItem( IDC_HEIGHT_VAL );
    pSpin->SetRange( 0, 2047 );
    pSpin->SetPos( m_nHeight );
    wsprintf( szValBuf, "%d", m_nHeight );
    pVal->SetWindowText( szValBuf );

    m_nYPosition = m_nOriginalYPosition = m_RSet.lRegs[HPR_VBP] / 10;
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_Y_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_Y_POSITION_VAL );
    pSpin->SetRange( 0, 1023 );
    pSpin->SetPos( m_nYPosition );
    wsprintf( szValBuf, "%d", m_nYPosition );
    pVal->SetWindowText( szValBuf );

    switch( m_nBoardType )
    {
      case HIDEF_ACCUSTREAM_205A:
      case HIDEF_ACCUSTREAM_50A:
      case HIDEF_ACCUSTREAM_75A:
      case HIDEF_ACCUSTREAM_EXPRESS_HD:
      case HIDEF_ACCUSTREAM_EXPRESS_HD50:
      case HIDEF_ACCUSTREAM_EXPRESS_HD75:
      case HIDEF_ACCUSTREAM_EXPRESS_HDC:
      case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
      case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
      ((CButton *)GetDlgItem(ID_FILTERS))->EnableWindow(FALSE);
        break;
    }

  }
  else
  {
    switch (m_nBoardType) {
      case HIDEF_ACCURA:
        //
        // The demo currently supports single pass only.
        // It could be changed...
        //
        nMode = MODE_HD4_1PASS;
        break;
      case HIDEF_I25:
      case HIDEF_I25MV:
       nMode = MODE_HDI25;
       break;
      case HIDEF_I50:
      case HIDEF_I50MV:
      case HIDEF_I50HSN:
       nMode = MODE_HDI50;
       break;
      case HIDEF_I60:
      case HIDEF_I60MV:
        nMode = MODE_HDI60;
        break;
      case HIDEF_I75:
      case HIDEF_I75MV:
        nMode = MODE_HDI75;
        break;
      case HIDEF_IRGB25:
      case HIDEF_IRGB25MV:
       nMode = MODE_HDIRGB25;
       break;
      case HIDEF_IRGB50:
      case HIDEF_IRGB50MV:
       nMode = MODE_HDIRGB50;
       break;
      case HIDEF_IRGB75:
      case HIDEF_IRGB75MV:
        nMode = MODE_HDIRGB75;
        break;
      case HIDEF_IRGB165:
        nMode = MODE_HDIRGB165;
        break;
      case HIDEF_IRGB170:
        nMode = MODE_HDIRGB170;
        break;
      case HIDEF_IRGB200:
        nMode = MODE_HDIRGB200;
        break;
      case HIDEF_IRGBI64_170:
        nMode = MODE_HDIRGBI64_170;
        break;
      case HIDEF_ACCUSTREAM_205A:
        nMode = MODE_HDACCUSTREAM205A;
        break;
      case HIDEF_ACCUSTREAM_50A:
        nMode = MODE_HDACCUSTREAM50A;
        break;
      case HIDEF_ACCUSTREAM_75A:
        nMode = MODE_HDACCUSTREAM75A;
        break;

      case HIDEF_ACCUSTREAM_170_PLUS:
        nMode = MODE_HDACCUSTREAM170PLUS;
        break;

      case HIDEF_ACCUSTREAM_75_PLUS:
        nMode = MODE_HDACCUSTREAM75PLUS;
        break;

      case HIDEF_ACCUSTREAM_50_PLUS:
        nMode = MODE_HDACCUSTREAM50PLUS;
        break;

      case HIDEF_ACCUSTREAM_VDR:
        nMode = MODE_HDACCUSTREAMVDR;
        break;

      case HIDEF_ACCUSTREAM_EXPRESS_170:
        nMode = MODE_HDACCUSTREAMEXPRESS_170;
        break;
        
      case HIDEF_ACCUSTREAM_EXPRESS_50:
        nMode = MODE_HDACCUSTREAMEXPRESS_50;
        break;

      case HIDEF_ACCUSTREAM_EXPRESS_75:
        nMode = MODE_HDACCUSTREAMEXPRESS_75;
        break;

      case HIDEF_ACCUSTREAM_EXPRESS_HD:
        nMode = MODE_HDACCUSTREAMEXPRESSHD;
        break;
        
      case HIDEF_ACCUSTREAM_EXPRESS_HD50:
        nMode = MODE_HDACCUSTREAMEXPRESSHD50;
        break;
        
      case HIDEF_ACCUSTREAM_EXPRESS_HD75:
        nMode = MODE_HDACCUSTREAMEXPRESSHD75;
        break;
        
      case HIDEF_ACCUSTREAM_EXPRESS_HDC:
        nMode = MODE_HDACCUSTREAMEXPRESSHDC;
        break;
        
      case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
        nMode = MODE_HDACCUSTREAMEXPRESSHDC50;
        break;
        
      case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
        nMode = MODE_HDACCUSTREAMEXPRESSHDC75;
        break;

		  case HIDEF_ACCUSTREAM_EXPRESS_1000:
		    nMode = MODE_HDACCUSTREAMEXPRESS1000;
		    break;

      case HIDEF_ACCUSTREAM_EXPRESS_1000_75:
        nMode = MODE_HDACCUSTREAMEXPRESS1000_75;
        break;

      case HIDEF_ACCUSTREAM_EXPRESS_1000_SDI:
        nMode = MODE_HDACCUSTREAMEXPRESS1000_SDI;
        break;

      case HIDEF_ACCUSTREAM_EXPRESS_2000:
		    nMode = MODE_HDACCUSTREAMEXPRESS2000;
		    break;

      case HIDEF_ACCUSTREAM_EXPRESS_2000_75:
        nMode = MODE_HDACCUSTREAMEXPRESS2000_75;
        break;

      case HIDEF_ACCUSTREAM_EXPRESS_2000_SDI:
        nMode = MODE_HDACCUSTREAMEXPRESS2000_SDI;
        break;

      default:    // what the heck, use I25
        nMode = MODE_HDI25;
       	break;
    }

    m_SliderMax = 100;

    m_nBlackLevelMin = phm[nMode].lBLevelMin;
    m_nBlackLevelMax = phm[nMode].lBLevelMax;
    m_nGainMin = phm[nMode].lVideo_mVMin;
    m_nGainMax = phm[nMode].lVideo_mVMax;

    dwSpread = (m_nBlackLevelMax -  m_nBlackLevelMin ) / 100;
    m_nOriginalBlackLevel = lBlackLevel = m_RSet.lRegs[HPR_BLEVEL];
    m_nBrightness = abs( m_nBlackLevelMin - lBlackLevel )/dwSpread;
    pSlider = (CSliderCtrl *)GetDlgItem( IDC_BRIGHTNESS_CTL );
    pVal = (CStatic *)GetDlgItem( IDC_BRIGHTNESS_VAL );
    pSlider->SetRange( 0, m_SliderMax, TRUE );
    pSlider->SetPos( m_SliderMax - m_nBrightness );
    wsprintf( szValBuf, "%d", m_nBrightness );
    pVal->SetWindowText( szValBuf );

    dwSpread = (m_nGainMax -  m_nGainMin ) / 100;
    m_nOriginalGain = lGain = m_RSet.lRegs[HPR_GAIN];
    m_nContrast = abs( m_nGainMax - lGain )/dwSpread;
    pSlider = (CSliderCtrl *)GetDlgItem( IDC_CONTRAST_CTL );
    pVal = (CStatic *)GetDlgItem( IDC_CONTRAST_VAL );
    pSlider->SetRange( 0, m_SliderMax, TRUE );
    pSlider->SetPos( m_SliderMax - m_nContrast );
    wsprintf( szValBuf, "%d", m_nContrast );
    pVal->SetWindowText( szValBuf );

    m_nWidth = m_nOriginalWidth = m_RSet.lRegs[HPR_WIDTH];
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_WIDTH );
    pVal = (CStatic *)GetDlgItem( IDC_WIDTH_VAL );
    pSpin->SetRange( 0, 2047 );
    pSpin->SetPos( m_nWidth );
    wsprintf( szValBuf, "%d", m_nWidth );
    pVal->SetWindowText( szValBuf );

    m_nXPosition = m_nOriginalXPosition = m_RSet.lRegs[HPR_HBS];
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_X_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_X_POSITION_VAL );
    pSpin->SetRange( 0, 1023 );
    pSpin->SetPos( m_nXPosition );
    wsprintf( szValBuf, "%d", m_nXPosition );
    pVal->SetWindowText( szValBuf );

    m_nPhase = m_nOriginalPhase = m_RSet.lRegs[HPR_PHASE];
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_PHASE );
    pVal = (CStatic *)GetDlgItem( IDC_PHASE_VAL );
    pSpin->SetRange( 0, 255 );
    pSpin->SetPos( m_nPhase / 5 );
    wsprintf( szValBuf, "%d.%dns", m_nPhase / 10, m_nPhase % 10 );
    pVal->SetWindowText( szValBuf );

    if ( IsHighSpeedRGBBoard() ) {
      UpdateVideoSettingLong uvl;
      uvl.pRSet = &m_RSet;
      eHP_GetControlValue( m_BoardHandle, "FinePhaseAdjust", sizeof( uvl ), &uvl );
      m_nFinePhase = m_nOriginalFinePhase = uvl.lValue;
      pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_PHASE_FINE );
      pVal = (CStatic *)GetDlgItem( IDC_FINE_PHASE_VAL );
      pSpin->SetRange( 0, 31 );
      pSpin->SetPos( m_nFinePhase );
      wsprintf( szValBuf, "%d", m_nFinePhase );
      pVal->SetWindowText( szValBuf );
    }
    else
    {  // fine phase adjustment only for high speed RGB at this time.
      pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_PHASE_FINE );
      pVal = (CStatic *)GetDlgItem( IDC_FINE_PHASE_VAL );
      if( pSpin )
        pSpin->EnableWindow( FALSE );
      if( pVal )
        pVal->EnableWindow( FALSE );
    }

    m_nHeight = m_nOriginalHeight = m_RSet.lRegs[HPR_HEIGHT];
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_HEIGHT );
    pVal = (CStatic *)GetDlgItem( IDC_HEIGHT_VAL );
    pSpin->SetRange( 0, 2047 );
    pSpin->SetPos( m_nHeight );
    wsprintf( szValBuf, "%d", m_nHeight );
    pVal->SetWindowText( szValBuf );

    m_nYPosition = m_nOriginalYPosition = m_RSet.lRegs[HPR_VBP] / 10;
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_Y_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_Y_POSITION_VAL );
    pSpin->SetRange( 0, 1023 );
    pSpin->SetPos( m_nYPosition );
    wsprintf( szValBuf, "%d", m_nYPosition );
    pVal->SetWindowText( szValBuf );

    // enable or disable the ypbpr controls
    {
      UpdateVideoSettingLong UV;
 	    UV.lValue = -1;
	    UV.pRSet = NULL;
	    eHP_GetControlValue(m_BoardHandle, "YUVClamp", sizeof(UV), (void *) &UV);
      if( UV.lValue == 0)
      {  // Not in YPbPrMode so disable the controls
        GetDlgItem( IDC_STATIC_HUE )->EnableWindow(FALSE);
        GetDlgItem( IDC_HUE_CTL )->EnableWindow(FALSE);
        GetDlgItem( IDC_HUE_VAL )->EnableWindow(FALSE);
        GetDlgItem( IDC_STATIC_SAT )->EnableWindow(FALSE);
        GetDlgItem( IDC_SATURATION_CTL )->EnableWindow(FALSE);
        GetDlgItem( IDC_SATURATION_VAL )->EnableWindow(FALSE);
        GetDlgItem( IDC_CLAMP_PLACE )->EnableWindow(FALSE);
        GetDlgItem( IDC_CLAMP_PLACE_CTL )->EnableWindow(FALSE);
        GetDlgItem( IDC_CLAMP_DURATION )->EnableWindow(FALSE);
        GetDlgItem( IDC_CLAMP_DUR_CTL )->EnableWindow(FALSE);
        GetDlgItem( IDC_CP_VAL )->EnableWindow(FALSE);
        GetDlgItem( IDC_CD_VAL )->EnableWindow(FALSE);
      }
      else  // in YPbPR mode, load up the initial values
      {
 	      UV.lValue = -1;
	      UV.pRSet = &m_RSet;

        dwSpread = (m_nBlackLevelMax -  m_nBlackLevelMin ) / 100;
        m_nOriginalHue = lHue = m_RSet.lRegs[HPR_HUE];
        m_nHue = abs( m_nBlackLevelMin - lHue )/dwSpread;
        pSlider = (CSliderCtrl *)GetDlgItem( IDC_HUE_CTL );
        pVal = (CStatic *)GetDlgItem( IDC_HUE_VAL );
        pSlider->SetRange( 0, m_SliderMax, TRUE );
        pSlider->SetPos( m_SliderMax - m_nHue );
        wsprintf( szValBuf, "%d", m_nHue );
        pVal->SetWindowText( szValBuf );

        dwSpread = (m_nGainMax -  m_nGainMin ) / 100;
        m_nOriginalSaturation = lSaturation = m_RSet.lRegs[HPR_SATURATION];
        m_nSaturation = abs( m_nGainMax - lGain )/dwSpread;
        pSlider = (CSliderCtrl *)GetDlgItem( IDC_SATURATION_CTL );
        pVal = (CStatic *)GetDlgItem( IDC_SATURATION_VAL );
        pSlider->SetRange( 0, m_SliderMax, TRUE );
        pSlider->SetPos( m_SliderMax - m_nSaturation );
        wsprintf( szValBuf, "%d", m_nSaturation );
        pVal->SetWindowText( szValBuf );

        eHP_GetControlValue(m_BoardHandle, "ClampPlacement", sizeof(UV), (void *) &UV);
        m_nOriginalYPbPrClampPlacement = m_nYPbPrClampPlacement = UV.lValue;
        pSlider = (CSliderCtrl *)GetDlgItem( IDC_CLAMP_PLACE_CTL );
        pSlider->SetRange( 1, 255, TRUE );
        // because the sliders are oriented top to bottom, invert the value
        pSlider->SetPos( 256 - m_nOriginalYPbPrClampPlacement );
        pVal = (CStatic *)GetDlgItem( IDC_CP_VAL );
        wsprintf( szValBuf, "%d", m_nOriginalYPbPrClampPlacement );
        pVal->SetWindowText( szValBuf );


        eHP_GetControlValue(m_BoardHandle, "ClampDuration", sizeof(UV), (void *) &UV);
        m_nOriginalYPbPrClampDuration = m_nYPbPrClampDuration = UV.lValue;
        pSlider = (CSliderCtrl *)GetDlgItem( IDC_CLAMP_DUR_CTL );
        pSlider->SetRange( 1, 255, TRUE );
        pSlider->SetPos( 256 - m_nOriginalYPbPrClampDuration );
        pVal = (CStatic *)GetDlgItem( IDC_CD_VAL );
        wsprintf( szValBuf, "%d", m_nOriginalYPbPrClampDuration );
        pVal->SetWindowText( szValBuf );

      }

    }
  }

  if( strlen( m_RSet.szCHPFile ) < _MAX_PATH ) {
    m_szHardwareProfilePath = m_RSet.szCHPFile;
    ReadCHPHeader( (LPCTSTR)m_szHardwareProfilePath );
  } else
    m_szHardwareProfilePath = '\0';
  //
  // Show/hide video controls if resizing is disabled
  //
  RestrictVAResizing( m_bRestrictResizing);

  // Dont allow White balance for Accustream/IRGB White with
  // monochrome inputs
  if( IsMonochromeInput() )
  {
    CButton *pWB = (CButton *)GetDlgItem( IDC_WHITEBALANCE );
    pWB->EnableWindow(FALSE);
  }
	return TRUE;  // return TRUE unless you set the focus to a control
	              // EXCEPTION: OCX Property Pages should return FALSE
}


void CVideoAdjust::OnFilters()
{
  //
  // Open the I-Color filters dialog or bring it
  // to the front
  //
  if (m_pIColorFilters == NULL)
  {
    //
    // Create a new modeless dialog
    //
    m_pIColorFilters = new CDlgIColorFilter;
    if (m_pIColorFilters == NULL)
    {
      ReportMsg( "Unable to access I-Color filters.",
                     MB_ICONSTOP|MB_OK);
      return;
    }
    m_pIColorFilters->m_pDlgParent = this;
    if (m_pIColorFilters->LoadIColorFilters() == 0)
    {
      ReportMsg( "Unable to access I-Color filters.",
                     MB_ICONSTOP|MB_OK);
      delete m_pIColorFilters;
      m_pIColorFilters = NULL;
      return;
    }
    m_pIColorFilters->Create( IDD_ICOLOR_FILTERS, this);
    m_pIColorFilters->ShowWindow( SW_SHOWNORMAL );
  }
  else
    m_pIColorFilters->BringWindowToTop();
}


void CVideoAdjust::OnClose()
{
  m_State = VA_STATE_UNINITIALIZED;
  //
  // Close the I-Color Filters dialog, if open
  //
  CloseIColorFilters( FALSE);
  if (m_pRegisterSet != NULL)
  {
    delete m_pRegisterSet;
    m_pRegisterSet = NULL;
  }
  CDialog::OnClose();
}


BOOL CVideoAdjust::DestroyWindow() 
{
  m_State = VA_STATE_UNINITIALIZED;
  //
  // Close the I-Color Filters dialog, if open
  //
  CloseIColorFilters( FALSE);
  if (m_pRegisterSet != NULL)
  {
    delete m_pRegisterSet;
    m_pRegisterSet = NULL;
  }
  return CDialog::DestroyWindow();
}


void CVideoAdjust::CloseIColorFilters(BOOL bApply)
{
  if (m_pIColorFilters == NULL)
    return;

  if (bApply)
    m_pIColorFilters->SaveIColorFilters();
  m_pIColorFilters->DestroyWindow();
  delete m_pIColorFilters;
  m_pIColorFilters = NULL;
}


void CVideoAdjust::OnVScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar) 
{
  char      szValBuf[32];

  CSliderCtrl *pScrollBrightness  = (CSliderCtrl *)GetDlgItem( IDC_BRIGHTNESS_CTL );
  CSliderCtrl *pScrollContrast    = (CSliderCtrl *)GetDlgItem( IDC_CONTRAST_CTL );
  CSliderCtrl *pScrollHue         = (CSliderCtrl *)GetDlgItem( IDC_HUE_CTL );
  CSliderCtrl *pScrollSaturation  = (CSliderCtrl *)GetDlgItem( IDC_SATURATION_CTL );
  CSliderCtrl *pScrollClampPlace  = (CSliderCtrl *)GetDlgItem( IDC_CLAMP_PLACE_CTL );
  CSliderCtrl *pScrollClampDur    = (CSliderCtrl *)GetDlgItem( IDC_CLAMP_DUR_CTL );
  CSliderCtrl *pSlider            = (CSliderCtrl *)pScrollBar;	
  CStatic     *pBrightnessVal     = (CStatic *)GetDlgItem( IDC_BRIGHTNESS_VAL );
  CStatic     *pContrastVal       = (CStatic *)GetDlgItem( IDC_CONTRAST_VAL );
  CStatic     *pHueVal            = (CStatic *)GetDlgItem( IDC_HUE_VAL );
  CStatic     *pSaturationVal     = (CStatic *)GetDlgItem( IDC_SATURATION_VAL );
  CStatic     *pClampPlaceVal     = (CStatic *)GetDlgItem( IDC_CP_VAL );
  CStatic     *pClampDurVal       = (CStatic *)GetDlgItem( IDC_CD_VAL );
  CSpinButtonCtrl *pSpinHeight    = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_HEIGHT );
  CSpinButtonCtrl *pSpinYPosition = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_Y_POSITION );
  CSpinButtonCtrl *pSpin          = (CSpinButtonCtrl *)pScrollBar;	
  CStatic         *pHeightVal     = (CStatic *)GetDlgItem( IDC_HEIGHT_VAL );
  CStatic         *pYPositionVal  = (CStatic *)GetDlgItem( IDC_Y_POSITION_VAL );

  if( pScrollBrightness == pSlider )  {
    wsprintf( szValBuf, "%d", m_SliderMax - pSlider->GetPos() );
    pBrightnessVal->SetWindowText( szValBuf );
    SetBrightness( m_SliderMax - pSlider->GetPos() );
  }
  else if( pScrollContrast == pSlider ) {
    wsprintf( szValBuf, "%d", m_SliderMax - pSlider->GetPos() );
    pContrastVal->SetWindowText( szValBuf );
    SetContrast( m_SliderMax - pSlider->GetPos() );
  }
  else if( pScrollHue == pSlider ) {
    wsprintf( szValBuf, "%d", m_SliderMax - pSlider->GetPos() );
    pHueVal->SetWindowText( szValBuf );
    SetHue( m_SliderMax - pSlider->GetPos() );
  }
  else if( pScrollSaturation == pSlider ) {
    wsprintf( szValBuf, "%d", m_SliderMax - pSlider->GetPos() );
    pSaturationVal->SetWindowText( szValBuf );
    SetSaturation( m_SliderMax - pSlider->GetPos() );
  }
  else if( pSpinHeight == pSpin ) {
    SetHeight( nPos );
    wsprintf( szValBuf, "%d", m_nHeight );
    pHeightVal->SetWindowText( szValBuf );
  }
  else if( pSpinYPosition == pSpin ) {
    SetYPosition( nPos );
    wsprintf( szValBuf, "%d", m_nYPosition );
    pYPositionVal->SetWindowText( szValBuf );
  }
  else if( pScrollClampPlace == pSlider ) {
    wsprintf( szValBuf, "%d", 256 - pSlider->GetPos() );
    pClampPlaceVal->SetWindowText( szValBuf );
    SetYPbPrClampPlacement( 256 - pSlider->GetPos() );
  }
  else if( pScrollClampDur == pSlider ) {
    wsprintf( szValBuf, "%d", 256 - pSlider->GetPos() );
    pClampDurVal->SetWindowText( szValBuf );
    SetYPbPrClampDuration( 256 - pSlider->GetPos() );
  }
	
	CDialog::OnVScroll(nSBCode, nPos, pScrollBar);
}


void CVideoAdjust::OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar) 
{
  char      szValBuf[32];

  CSpinButtonCtrl *pSpinWidth     = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_WIDTH );
  CSpinButtonCtrl *pSpinXPosition = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_X_POSITION );
  CSpinButtonCtrl *pSpinPhase     = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_PHASE );
  CSpinButtonCtrl *pSpinFinePhase = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_PHASE_FINE );
  CSpinButtonCtrl * pSpin         = (CSpinButtonCtrl *)pScrollBar;	
  CStatic *pWidthVal              = (CStatic *)GetDlgItem( IDC_WIDTH_VAL );
  CStatic *pXPositionVal          = (CStatic *)GetDlgItem( IDC_X_POSITION_VAL );
  CStatic *pPhaseVal              = (CStatic *)GetDlgItem( IDC_PHASE_VAL );
  CStatic *pFinePhaseVal          = (CStatic *)GetDlgItem( IDC_FINE_PHASE_VAL );

  if( pSpinWidth == pSpin )  {
    SetWidth( nPos );
    wsprintf( szValBuf, "%d", m_nWidth );
    pWidthVal->SetWindowText( szValBuf );
  }
  else if( pSpinXPosition == pSpin ) {
    SetXPosition( nPos );
    wsprintf( szValBuf, "%d", m_nXPosition );
    pXPositionVal->SetWindowText( szValBuf );
  }
  else if( pSpinPhase == pSpin ) {
    SetPhase( nPos * 5 );
    wsprintf( szValBuf, "%d.%dns", m_nPhase / 10, m_nPhase % 10 );
    pPhaseVal->SetWindowText( szValBuf );
  }
  else if( pSpinFinePhase == pSpin ) {
    SetFinePhase( nPos  );
    wsprintf( szValBuf, "%d", m_nFinePhase );
    pFinePhaseVal->SetWindowText( szValBuf );
  }


	CDialog::OnHScroll(nSBCode, nPos, pScrollBar);
}


void CVideoAdjust::OnOK() 
{
  m_State = VA_STATE_UNINITIALIZED;
  //
  // Close the I-Color Filters dialog, if open
  //
  CloseIColorFilters( FALSE);

  // notify the parent
  if( m_UserQuitMessage )
    ::PostMessage(m_pParent->m_hWnd, m_UserQuitMessage, 1, 0 );    // WPARAM == 1 = OK
	CDialog::OnOK();
}


void CVideoAdjust::OnCancel() 
{
  m_State = VA_STATE_UNINITIALIZED;
  //
  // Close the I-Color Filters dialog, if open
  //
  CloseIColorFilters( FALSE);
  if ( IsTVBoard() ) {
    UpdateVideoSetting UVSetting;
    UpdateVideoSettingLong UVSettingLong;
    UVSetting.pRSet     = &m_RSet;
    UVSettingLong.pRSet = &m_RSet;
    // set the adjusted values to the original, because UpdateVarSet may be called.
    UVSetting.bValue = m_nBrightness = (BYTE)m_nOriginalBrightness;
    eHP_SetControlValue( m_BoardHandle, "Brightness", 
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = m_nContrast = (BYTE)m_nOriginalContrast;
    eHP_SetControlValue( m_BoardHandle, "Contrast", 
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = m_nHue = (BYTE)m_nOriginalHue;
    eHP_SetControlValue( m_BoardHandle, "Hue", 
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = m_nSaturation = (BYTE)m_nOriginalSaturation;
    eHP_SetControlValue( m_BoardHandle, "Saturation", 
                         sizeof(UVSetting), &UVSetting);
    UVSettingLong.lValue = m_nXPosition = (BYTE)m_nOriginalXPosition;
    eHP_SetControlValue( m_BoardHandle, "HorizontalPosition", 
                         sizeof(UVSettingLong), &UVSettingLong);

    m_RSet.lRegs[HPR_WIDTH]  = m_nOriginalWidth;
    HD_RSETINDEX_SET(&m_RSet, HPR_WIDTH);
    m_RSet.lRegs[HPR_HEIGHT] = m_nOriginalHeight;
    HD_RSETINDEX_SET(&m_RSet, HPR_HEIGHT);
    m_RSet.lRegs[HPR_VBP]    = m_nOriginalYPosition * 10;
    HD_RSETINDEX_SET(&m_RSet, HPR_VBP);
	    
    if (!m_bRestrictResizing)
      eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );

    if( m_nBoardType == HIDEF_ICOLOR ||
        m_nBoardType == HIDEF_ICOLORMV )
    {
      RestoreIColorRegisters();
    }
  
    UpdateImage(TRUE);
  } else {
    UpdateVideoSettingLong uvl;
    
    m_RSet.lRegs[HPR_GAIN]   = m_nOriginalGain;
    m_RSet.lRegs[HPR_BLEVEL] = m_nOriginalBlackLevel;
    m_RSet.lRegs[HPR_WIDTH]  = m_nOriginalWidth;
    m_RSet.lRegs[HPR_HBS]    = m_nOriginalXPosition;
    m_RSet.lRegs[HPR_PHASE]  = m_nOriginalPhase;
    m_RSet.lRegs[HPR_HEIGHT] = m_nOriginalHeight;
    m_RSet.lRegs[HPR_VBP]    = m_nOriginalYPosition * 10;

    if ( IsHighSpeedRGBBoard() ) {
      uvl.pRSet = &m_RSet;
      uvl.lValue = m_nOriginalFinePhase;
      eHP_SetControlValue( m_BoardHandle, "FinePhaseAdjust",
                           sizeof(uvl), &uvl);
    }

    eHP_GetControlValue(m_BoardHandle, "YUVClamp", sizeof(uvl), (void *) &uvl);
    if( uvl.lValue != 0)
    {
      uvl.pRSet     = &m_RSet;

      uvl.lValue =  m_nYPbPrSaturation = m_nOriginalYPbPrSaturation;
      eHP_SetControlValue( m_BoardHandle, "SaturationYPbPr", 
                           sizeof(uvl), &uvl);

      uvl.lValue =  m_nYPbPrClampPlacement = m_nOriginalYPbPrClampPlacement;
      eHP_SetControlValue( m_BoardHandle, "ClampPlacement", 
                           sizeof(uvl), &uvl);

      uvl.lValue =  m_nYPbPrClampDuration = m_nOriginalYPbPrClampDuration;
      eHP_SetControlValue( m_BoardHandle, "ClampDuration", 
                           sizeof(uvl), &uvl);
    }
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
    UpdateImage();
  }

  // notify the parent
  if( m_UserQuitMessage )
    ::PostMessage(m_pParent->m_hWnd, m_UserQuitMessage, 0, 0 );    // WPARAM == 0 = CANCEL

  CDialog::OnCancel();
}



void CVideoAdjust::SetBrightness( long b )
{
  BOOL             bChanged = FALSE;

  if (m_nBrightness != b)
    bChanged = TRUE;

  m_nBrightness = b;

  if ( IsTVBoard() ) {
    UpdateVideoSetting UVSetting;

    UVSetting.pRSet = &m_RSet;
    UVSetting.bValue = (BYTE)b;

    eHP_SetControlValue( m_BoardHandle, "Brightness", sizeof(UVSetting), &UVSetting);
  } else {
#if 1
    DWORD dwSpread = (m_nBlackLevelMax - m_nBlackLevelMin ) / 100;

    m_RSet.lRegs[HPR_BLEVEL] = m_nBlackLevelMin + (m_nBrightness * dwSpread );
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
#else
     const int hardwareDarkValue = -5000;
     const int hardwareLightValue = 5000;
     const int hardwareBrightValueRange = hardwareLightValue - hardwareDarkValue;
     const int darkValue = 1;
     const int lightValue = 100;
     const int brightValueRange = lightValue - darkValue;
     double brightValuePercent = (double)(b - 1) / (double)(brightValueRange);

     int newHardwareBrightness = 
       static_cast<int>((brightValuePercent * hardwareBrightValueRange) + hardwareDarkValue);
     m_RSet.lRegs[HPR_BLEVEL] = newHardwareBrightness;
     ERRTYPE error = eHD_RSET_Set(m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON);
#endif
  }

  if (bChanged)
    UpdateImage(FALSE);
}


void CVideoAdjust::SetContrast( long c )
{
  BOOL             bChanged = FALSE;

  if (m_nContrast != c)
    bChanged = TRUE;

  m_nContrast = c;

  if ( IsTVBoard() ) {
    UpdateVideoSetting UVSetting;

    UVSetting.pRSet = &m_RSet;
    UVSetting.bValue = (BYTE)c;
    eHP_SetControlValue( m_BoardHandle, "Contrast", sizeof(UVSetting), &UVSetting);
  } else {
#if 1
    DWORD dwSpread = (m_nGainMax -  m_nGainMin ) / 100;

    m_RSet.lRegs[HPR_GAIN] = m_nGainMax - (m_nContrast * dwSpread );
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
#else
     const int hardwareMinGain = 10000;  // 1.0V - signal amplitude in 100uV steps
     const int hardwareMaxGain = 5000;   // 0.5V
     const int hardwareGainRange = hardwareMinGain - hardwareMaxGain;
     const int MinGain = 1;
     const int MaxGain = 100;
     const int GainRange = MaxGain - MinGain;
     double GainPercent = (double)(c - 1) / (double)GainRange; 

     int newHardwareGain = 
       static_cast<int>(hardwareMinGain - (GainPercent * hardwareGainRange));
     m_RSet.lRegs[HPR_GAIN] = newHardwareGain;
     ERRTYPE error = eHD_RSET_Set(m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON);
#endif
  }

  if (bChanged)
    UpdateImage(FALSE);
}


void CVideoAdjust::SetHue( long h )
{
  BOOL             bChanged = FALSE;

  if (m_nHue != h)
    bChanged = TRUE;

  m_nHue = h;

  if ( IsTVBoard() ) {
    UpdateVideoSetting UVSetting;

    UVSetting.pRSet = &m_RSet;
    UVSetting.bValue = (BYTE)h;
    eHP_SetControlValue( m_BoardHandle, "Hue", sizeof(UVSetting), &UVSetting);
  }
  else
  {
    DWORD dwSpread = (m_nBlackLevelMax - m_nBlackLevelMin ) / 100;

    m_RSet.lRegs[HPR_HUE] = m_nBlackLevelMin + (m_nHue * dwSpread );
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
  }

  if (bChanged)
    UpdateImage(FALSE);
}


void CVideoAdjust::SetSaturation( long s )
{
  BOOL             bChanged = FALSE;

  if (m_nSaturation != s)
    bChanged = TRUE;
    
  m_nSaturation = s;

  if ( IsTVBoard() ) {
    UpdateVideoSetting UVSetting;

    UVSetting.pRSet = &m_RSet;
    UVSetting.bValue = (BYTE)s;
    eHP_SetControlValue( m_BoardHandle, "Saturation", sizeof(UVSetting), &UVSetting);
  }
  else
  {
    DWORD dwSpread   = (m_nGainMax -  m_nGainMin ) / 100;

    m_RSet.lRegs[HPR_SATURATION] = m_nGainMax - (m_nSaturation * dwSpread );
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
  }
  
  if (bChanged)
    UpdateImage(FALSE);
}

void CVideoAdjust::SetYPbPrClampPlacement( long s )
{
  BOOL             bChanged = FALSE;
  UpdateVideoSettingLong UVSetting;

  UVSetting.pRSet = &m_RSet;
  UVSetting.lValue = s;
  if (m_nYPbPrClampPlacement != s)
    bChanged = TRUE;
  m_nYPbPrClampPlacement = s;

  eHP_SetControlValue( m_BoardHandle, "ClampPlacement", 
                       sizeof(UVSetting), &UVSetting);

  if (bChanged)
    UpdateImage(FALSE);
}

void CVideoAdjust::SetYPbPrClampDuration( long s )
{
  BOOL             bChanged = FALSE;
  UpdateVideoSettingLong UVSetting;

  UVSetting.pRSet = &m_RSet;
  UVSetting.lValue = s;
  if (m_nYPbPrClampDuration != s)
    bChanged = TRUE;
  m_nYPbPrClampDuration = s;

  eHP_SetControlValue( m_BoardHandle, "ClampDuration", 
                       sizeof(UVSetting), &UVSetting);

  if (bChanged)
    UpdateImage(FALSE);
}

void CVideoAdjust::SetYPbPrSaturation( long s )
{
  BOOL             bChanged = FALSE;
  UpdateVideoSettingLong UVSetting;

  UVSetting.pRSet = &m_RSet;
  UVSetting.lValue = s;
  if (m_nYPbPrSaturation != s)
    bChanged = TRUE;
  m_nYPbPrSaturation = s;

  eHP_SetControlValue( m_BoardHandle, "SaturationYPbPr", 
                       sizeof(UVSetting), &UVSetting);

  if (bChanged)
    UpdateImage(FALSE);
}
long CVideoAdjust::SetWidth( long w )
{
  long lHBS = m_RSet.lRegs[HPR_HBS];
  long lHEND = lHBS + w;

  if( lHEND < m_RSet.lRegs[HPR_HTOTAL] ) {
    if (m_nWidth != w)
    {
      m_nWidth = w;
      m_RSet.lRegs[HPR_WIDTH] = w;
      eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
      UpdateImage(TRUE);  // use True because the output dimensions have changed..
    }
    return w;
  } else {
    return m_nWidth;
  }
}


long CVideoAdjust::SetXPosition( long p )
{
  BOOL             bChanged = FALSE;

  if ( IsTVBoard() ) {
    UpdateVideoSetting UVSetting;

    UVSetting.pRSet = &m_RSet;
    UVSetting.bValue = (BYTE)p;
    if (m_nXPosition != p)
      bChanged = TRUE;
    m_nXPosition = p;
    eHP_SetControlValue( m_BoardHandle, "HorizontalPosition", 
                         sizeof(UVSetting), &UVSetting);
    if (bChanged)
      UpdateImage(FALSE);
  } else {
    long lWIDTH = m_RSet.lRegs[HPR_WIDTH];
    long lHEND  = lWIDTH + p;

    if ( lHEND < m_RSet.lRegs[HPR_HTOTAL] ) {
      if (m_nXPosition != p) {
        m_nXPosition = p;
        m_RSet.lRegs[HPR_HBS] = p;
        eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
        UpdateImage(FALSE);
      }
      return p;
    } else {
      return m_nXPosition;
    }
  }
  return m_nXPosition;
}


void CVideoAdjust::SetPhase( long p )
{
  if (m_nPhase != p) {
    m_nPhase = p;
    m_RSet.lRegs[HPR_PHASE] = p;
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
    UpdateImage(FALSE);
  }
}

void CVideoAdjust::SetFinePhase( long p )
{
  if (m_nFinePhase != p) {
    UpdateVideoSettingLong uvl;
    m_nFinePhase = p;
    
    uvl.pRSet = &m_RSet;
    uvl.lValue = m_nFinePhase;
    eHP_SetControlValue( m_BoardHandle, "FinePhaseAdjust",
                       sizeof(uvl), &uvl);
    UpdateImage(FALSE);
  }
}


long CVideoAdjust::SetHeight( long h )
{
  long lVBP = m_RSet.lRegs[HPR_VBP] / 10;
  long lVEND = lVBP + h;

  if( lVEND < m_RSet.lRegs[HPR_VTOTAL] ) {
    if (m_nHeight != h)
    {
      m_nHeight = h;
      m_RSet.lRegs[HPR_HEIGHT] = h;
      eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
      UpdateImage(TRUE); // use TRUE because the output dimensions have changed
    }
    return h;
  } else {
    return m_nHeight;
  }
}


long CVideoAdjust::SetYPosition( long p )
{
  long lHEIGHT = m_RSet.lRegs[HPR_HEIGHT];
  long lVEND  = lHEIGHT + p;

  if( lVEND < m_RSet.lRegs[HPR_VTOTAL] ) {
    if (m_nYPosition != p)
    {
      m_nYPosition = p;
      m_RSet.lRegs[HPR_VBP] = p * 10;
      eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
      UpdateImage(FALSE);
    }
    return p;
  } else {
    return m_nYPosition;
  }
}

char* stristr(const char* string1, const char* string2)
{
  int c = tolower((unsigned char)*string2);
  
  if (c == '\0')
    return (char*)string1;
  
  for (; *string1; string1++)
  {
    if (tolower((unsigned char)*string1) == c)
    {
      for (size_t i = 0;;)
      {
        if (string2[++i] == '\0')
          return (char*)string1;
        
        if (tolower((unsigned char)string1[i]) != tolower((unsigned char)string2[i]))
          break;
      }
    }
  }

  return NULL;
}

//
// Extract creation data from header of a CHP file.
//
void CVideoAdjust::ReadCHPHeader(const char *szCHP) 
{
#define CHPLINE_SIZE 200
  FILE            *fpCHP;
  BOOL             b, bWhite, bBlank, bMdData, bCrData, bCmntData;
  char             c, *psz, *pszWh, *pszData, szLine[CHPLINE_SIZE+1];
  CString         *pszCLine;
	errno_t					err;
  //
  // Set defaults.
  //
  m_szCrTime[0]  = 0;
  m_szCreator[0] = 0;
  m_szCrVers[0]  = 0;
  m_szLibVers[0] = 0;
  m_szCrRels[0]  = 0;
  m_szComment    = "";
  m_szBoard[0]   = 0;
  m_szBoardSN[0] = 0;
  //
  // Ready CHP file.
  //
	if ((err = fopen_s(&fpCHP, szCHP, "rt")) != 0)
		return;
  
  //
  // Read the comment section one line at a time.
  //
  bMdData = FALSE;
  bCrData = FALSE;
  bCmntData = FALSE;
  for (;;)
  {
    psz = fgets(szLine,CHPLINE_SIZE,fpCHP);
    if (psz==(char *)NULL) 
      break;
    //
    // Reduce white space.
    //
    bWhite = TRUE;
    bBlank = TRUE;
    for (psz=pszWh=szLine; *psz; )
    {
      c = *psz++;
      if (c<=' ')
      {
        if (bWhite) 
          continue;
        c = ' ';
        bWhite = TRUE;
      }
      else
      {
        bWhite = FALSE;
        bBlank = FALSE;
      }
      *pszWh++ = c;
    }
    if (bBlank) 
      continue;
    if (bWhite) 
      pszWh--;
    *pszWh = 0;
    //
    // We're only interested in comment lines.  When we reach a line
    // that begins with a left bracket, we've reached the end of the
    // header.
    //
    if (szLine[0]=='[') 
      break;
    if (szLine[0]!=';') 
      continue;
    //
    // Check for a comment line
    //
    if (strstr(szLine,";::") == szLine)
    {
      //
      // Note: This line will be pre-truncated
      // if it is too long
      //
			strncpy_s(szLine, _countof(szLine), "   ", 3);
      pszCLine = new CString( szLine + 3, (int)strlen(szLine) - 3);
      pszCLine->TrimLeft();
      pszCLine->TrimRight();
      m_szComment += *pszCLine + "\x0D\x0A";
      delete pszCLine;
      continue;
    }
    //
    // Find the data, if any.
    //
    pszData = strchr(szLine,':');
    if (pszData==(char *)NULL) 
      continue;
    *pszData++ = 0;
    if (*pszData==' ') 
      pszData++;
    //
    // We're only interested in information related to CHP file
    // creation.
    //
    szLine[0]=' ';
		//_strupr_s(szLine, _countof(szLine));
    b = FALSE;
    if (stristr(szLine," CREAT")) 
      b=TRUE;
    if (stristr(szLine," GENERAT")) 
      b=TRUE;
    if (b)
    {
      bCrData = TRUE;
      bMdData = FALSE;
    }
    if (stristr(szLine," MODIF"))
    {
      bMdData = TRUE;
      bCrData = FALSE;
    }
    if (!bCrData) 
      continue;
    //
    // Save data from this line.
    //
    if (stristr(szLine," TIME")!=(char *)NULL)
    {
			strncpy_s(m_szCrTime, _countof(m_szCrTime), pszData, 30);
      m_szCrTime[29]=0;
      continue;
    }
    if (stristr(szLine," BY")!=(char *)NULL)
    {
			strncpy_s(m_szCreator, _countof(m_szCreator), pszData, 30);
      m_szCreator[29]=0;
      continue;
    }

    if (stristr(szLine,"BOARD TYPE")!=(char *)NULL)
    {
			strncpy_s(m_szBoard, _countof(m_szBoard), pszData, 40);
      m_szBoard[39]=0;
      continue;
    }
    if (stristr(szLine,"SERIAL NUMBER")!=(char *)NULL)
    {
			strncpy_s(m_szBoardSN, _countof(m_szBoardSN), pszData, 30);
      m_szBoardSN[29]=0;
      continue;
    }
    if (stristr(szLine,"LIBRARY VERSION")!=(char *)NULL)
    {
			strncpy_s(m_szLibVers, _countof(m_szLibVers), pszData, 10);
      m_szLibVers[9]=0;
      continue;
    }
    if (stristr(szLine," VERSION")!=(char *)NULL)
    {
			strncpy_s(m_szCrVers, _countof(m_szCrVers), pszData, 10);
      m_szCrVers[9]=0;
      continue;
    }
    if (stristr(szLine," RELEASE")!=(char *)NULL)
    {
			strncpy_s(m_szCrRels, _countof(m_szCrRels), pszData, 30);
      m_szCrRels[29]=0;
      continue;
    }
  }
  fclose(fpCHP);
  //
  // Fix bad version data from legacy CHP files
  //
  if (   (strlen(m_szLibVers) == 0)
      && (strlen(m_szCrVers) != 0))
  {
		strcpy_s(m_szLibVers, _countof(m_szLibVers), m_szCrVers);
  }
}

void CVideoAdjust::OnSaveSettings() 
{
  SaveVASettings("", "", "", "", FALSE);
}

void CVideoAdjust::SaveVASettings(CString szCHP, CString szComments, 
                                  CString szMyName, CString szMyVersion,
                                  BOOL bSilent) 
{
  FILE            *fpCHP;
  CString          szOut;
  char             szTime[30];
  time_t           t;
  char             szSN[9];
  long             lSerial;
  short            hdt;
	errno_t					 err;

  if (szCHP.IsEmpty())
    szCHP = m_szHardwareProfilePath;

  if (szMyName.IsEmpty())
    szMyName = "IDEA Video Adjustments Library";

  if (!bSilent) {    // Prompt the user for a target file
    CString          csFilt = "Common Hardware Profile (*.chp)|*.chp||";
    CFileDialog      fDlg( FALSE, "chp", szCHP, OFN_OVERWRITEPROMPT, csFilt, NULL);
    if (fDlg.DoModal() != IDOK) return;
    szCHP = fDlg.GetPathName();
  }
  //
  // Open the file for writing
  //
	if ((err = fopen_s(&fpCHP, szCHP, "wt")) != 0)
	{
		if (!bSilent) {
			szOut.Format("Unable to open CHP file '%s' for writing.", szCHP);
			ReportMsg(szOut, MB_ICONSTOP | MB_OK);
		}
		return;
	}

  //
  // If the file opened, save off the pathname for future use
  //
  m_szHardwareProfilePath = szCHP;
  //
  // Get the current time and application version string
  //
  time(&t);
	ctime_s(szTime, _countof(szTime), &t);

  if (szMyVersion.IsEmpty()) {
    char     achExeName[MAX_PATH];
    DWORD    dwLen;
    UINT     uLen; 
    LPCSTR   szData;
    PBYTE    pvVerInfo;
    char     achDisplayBuffer[256];
    DWORD    dwfvHandle;
    char     achQueryBuf[64];  // Must be in temp due to windows bug

    wsprintf( achExeName, "%s.EXE", AfxGetApp()->m_pszExeName );
    dwLen = GetFileVersionInfoSize( achExeName, &dwfvHandle  );
    uLen = (UINT)dwLen;
    if ( uLen > 0 ) {
      //
      // Version info is there, go get it
      //   
      pvVerInfo = new BYTE[uLen];
      GetFileVersionInfo( achExeName, NULL, uLen, pvVerInfo ); 
      lstrcpy( achQueryBuf,(LPCSTR)"\\StringFileInfo\\040904B0\\FileVersion" );
      VerQueryValue( pvVerInfo, (LPSTR)achQueryBuf, (LPVOID *)&szData, &uLen );
      if ( uLen > 0 ) {
        BYTE *p;

        wsprintf( achDisplayBuffer, "%s", szData );
        p = (BYTE *)achDisplayBuffer;
        //
        // Replace commas with periods
        //
        while ( *p ) {
          if ( *p == ',' ) 
            *p = '.';
          p++;
        }
        szMyVersion = achDisplayBuffer;
        delete pvVerInfo;
      }
    }
  }
  //
  // Retrieve the serial number
  //
  memset( szSN, 0, 9);
  nHP_GetBoardInfo( m_BoardHandle, HPEE_SERIAL_NO, 8, (void *) szSN);
  lSerial = lHD_EncodeSerial(szSN);
	sprintf_s(m_szBoardSN, _countof(m_szBoardSN), "%06ld", lSerial);
  //
  // Retrieve the board type
  //
  hdt = 3;
  nHP_GetBoardInfo( m_BoardHandle, HPEE_BOARD_TYPE, 2, &hdt);
  switch( hdt) {
    case HIDEF_ACCURA:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "Accura      ");
		}
    break;

    case HIDEF_I25:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-25        ");
		}
    break;

    case HIDEF_I50:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-50        ");
		}
    break;

    case HIDEF_I60:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-60        ");
		}
    break;

    case HIDEF_I75:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-75        ");
		}
		break;

		case HIDEF_I25MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-25 MV     ");
		}
		break;

		case HIDEF_I50MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-50 MV     ");
		}
		break;

		case HIDEF_I50HSN:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-50 HSN    ");
		}
		break;

		case HIDEF_I60MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-60 MV     ");
		}
		break;

		case HIDEF_I75MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-75 MV     ");
		}
		break;

		case HIDEF_ICOLOR:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-Color     ");
		}
		break;

		case HIDEF_ICOLORMV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-Color MV  ");
		}
		break;

		case HIDEF_IRGB25:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 25    ");
		}
		break;

		case HIDEF_IRGB50:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 50    ");
		}
		break;

		case HIDEF_IRGB60:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 60    ");
		}
		break;

		case HIDEF_IRGB75:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 75    ");
		}
		break;

		case HIDEF_IRGB25MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 25 MV ");
		}
		break;

		case HIDEF_IRGB50MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 50 MV ");
		}
		break;

		case HIDEF_IRGB60MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 60 MV ");
		}
		break;

		case HIDEF_IRGB75MV:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 75 MV ");
		}
		break;

		case HIDEF_IRGB165:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 165   ");
		}
		break;

		case HIDEF_IRGB170:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 170DVI");
		}
		break;

		case HIDEF_IRGB200:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "I-RGB 200   ");
		}
		break;

		case HIDEF_IRGBI64_170:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 170");
		}
		break;

		case HIDEF_ACCUSTREAM_205A:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 205A");
		}
		break;

		case HIDEF_ACCUSTREAM_50A:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 50A");
		}
		break;

		case HIDEF_ACCUSTREAM_75A:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 75A");
		}
		break;

		case HIDEF_ACCUSTREAM_VDR:
		{
				strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream VDR");
		}
		break;

    case HIDEF_ACCUSTREAM_170_PLUS:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 170+");
		}
    break;

		case HIDEF_ACCUSTREAM_75_PLUS:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 75+");
		}
		break;

		case HIDEF_ACCUSTREAM_50_PLUS:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream 50+");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_170:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 170");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_50:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 50");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_75:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 75");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_HD:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express HD+");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_HD50:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express SD 50+");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_HD75:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express HD 75+");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_HDC:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express HD+C");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_HDC50:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express SD 50+C");
		}
		break;

		case HIDEF_ACCUSTREAM_EXPRESS_HDC75:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express HD 75+C");
		}
		break;
		
		case HIDEF_ACCUSTREAM_EXPRESS_1000:
			 strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 1000");
			 break;

    case HIDEF_ACCUSTREAM_EXPRESS_1000_75:
      strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 1000 LF");
      break;

    case HIDEF_ACCUSTREAM_EXPRESS_1000_SDI:
      strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 1000 SDI");
      break;

    case HIDEF_ACCUSTREAM_EXPRESS_2000:
			 strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 2000");
			 break;

    case HIDEF_ACCUSTREAM_EXPRESS_2000_75:
      strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 2000 LF");
      break;

    case HIDEF_ACCUSTREAM_EXPRESS_2000_SDI:
      strcpy_s(m_szBoard, _countof(m_szBoard), "AccuStream Express 2000 SDI");
      break;

    default:
		{
			strcpy_s(m_szBoard, _countof(m_szBoard), "???         ");
		}
		break;
  }
  //
  // Write the header text
  //
  fprintf(fpCHP,
      ";\n"
      ";----------------------------------------------------------\n"
      ";              IDEA Common Hardware Profile\n"
      ";----------------------------------------------------------\n"
      ";\n"
      ";\n"
      ";              Filename: %s\n"
      ";\n"
      ";         Creation Time: %s\n"
      ";    Time Last Modified: %s"
      ";\n"
      ";          Generated by: %s\n"
      ";               Version: %s\n"
      ";       Library Version: %s\n"
      ";          Release Date: %s\n"
      ";\n"
      ";      Last Modified by: %s\n"
      ";               Version: %s\n"
      ";            Board Type: %s\n"
      ";   Board Serial Number: %s\n"
//      ";       Library Version: "HDP_LIB_CREV"\n"
//      ";          Release Date: "HDP_LIB_DATE"\n"
      ";\n"
      ";  A product of Foresight Imaging, LLC.\n"
      ";\n",
      (LPCTSTR)m_szHardwareProfilePath,
      m_szCrTime, szTime, m_szCreator, m_szCrVers,
      m_szLibVers,
      m_szCrRels, (LPCTSTR)szMyName, (LPCTSTR)szMyVersion,
      m_szBoard, m_szBoardSN
      );
  //
  // Write any user-defined comments
  //
  if ( !(m_szComment.IsEmpty() && szComments.IsEmpty()) )
    fprintf(fpCHP,
      ";  Comments:\n"
      ";\n"
      );
  if ( !m_szComment.IsEmpty() )
    WriteCommentString( fpCHP, m_szComment);
  if ( !szComments.IsEmpty() )
    WriteCommentString( fpCHP, szComments);
  //
  // Close the file header
  //
  fprintf(fpCHP,
      ";----------------------------------------------------------\n"
      ";\n"
      );
  fclose(fpCHP);
  //
  // Write the RSET data
  //
  eHP_RSET_FWrite(m_BoardHandle,
                  (char *)(LPCTSTR)m_szHardwareProfilePath, &m_RSet);
}


//
// Write comment text to the specified CHP file.
//
void CVideoAdjust::WriteCommentString( FILE *fpCHP, CString &szComment)
{
  //
  // Writes a comment string out to the CHP file one line at
  // a time.  Wraps at 120 chars.
  //
  CString          szLine;
  char             cCurrent;
  int              nColumn = 0;
  int              nCharIndex = 0;
  int              nCommentLength = szComment.GetLength();

  while ( nCharIndex < nCommentLength)
  {
    cCurrent = szComment[nCharIndex];
    //
    // End of line or wrap after column 120
    //
    if (  (cCurrent == '\n')
        ||(nColumn == 120))
    {
      fprintf( fpCHP, ";::    %s\n", (LPCTSTR)szLine);
      szLine = "";
      if (nColumn != 120)
        nCharIndex++;
      nColumn = 0;
    }
    else
    {
      szLine += cCurrent;
      nCharIndex++;
      nColumn++;
    }
  }
  if (nColumn != 0)
    fprintf( fpCHP, ";::    %s\n", (LPCTSTR)szLine);
}


//
// The application has indicated a change in whether
// it is running in a manner that precludes size changes.
// Show/hide controls based on this.  Although it has no
// sizing dependencies, the white balance control is also
// shown/hidden by this function because it has the same
// limitation (i.e., don't run it while in live display).
//
void CVideoAdjust::RestrictVAResizing(BOOL bRestrict)
{
  CStatic         *pVal;
  CSpinButtonCtrl *pSpin;
  CButton         *pButton;
  int              nShow;
  //
  // NB: If you always want to restrict resizing, force
  //     the value of m_bRestrictResizing to be TRUE.
  //
  if (m_State == VA_STATE_UNINITIALIZED) return;
  if (!::IsWindow(m_hWnd)) return;
  m_bRestrictResizing = bRestrict;
  nShow = (m_bRestrictResizing ? SW_HIDE : SW_SHOW);

  if ( IsTVBoard() ) {
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_WIDTH );
    pVal = (CStatic *)GetDlgItem( IDC_WIDTH_VAL );
    if (pSpin) pSpin->ShowWindow(nShow);
    if (pVal)  pVal->ShowWindow(nShow);

    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_X_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_X_POSITION_VAL );
    if (pSpin) pSpin->ShowWindow(SW_SHOW);
    if (pVal)  pVal->ShowWindow(SW_SHOW);

    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_HEIGHT );
    pVal = (CStatic *)GetDlgItem( IDC_HEIGHT_VAL );
    if (pSpin) pSpin->ShowWindow(nShow);
    if (pVal)  pVal->ShowWindow(nShow);

    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_Y_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_Y_POSITION_VAL );
    if (pSpin) pSpin->ShowWindow(SW_SHOW);
    if (pVal)  pVal->ShowWindow(SW_SHOW);

    pVal = (CStatic *)GetDlgItem( IDC_DIMENSION_TXT);
    if (pVal)  pVal->ShowWindow( nShow);
    pVal = (CStatic *)GetDlgItem( IDC_POSITION_TXT);
    if (pVal)  pVal->ShowWindow( nShow);
  } else {
    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_WIDTH );
    pVal = (CStatic *)GetDlgItem( IDC_WIDTH_VAL );
    if (pSpin) pSpin->ShowWindow(nShow);
    if (pVal)  pVal->ShowWindow(nShow);

    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_X_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_X_POSITION_VAL );
    if (pSpin) pSpin->ShowWindow(SW_SHOW);
    if (pVal)  pVal->ShowWindow(SW_SHOW);

    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_HEIGHT );
    pVal = (CStatic *)GetDlgItem( IDC_HEIGHT_VAL );
    if (pSpin) pSpin->ShowWindow(nShow);
    if (pVal)  pVal->ShowWindow(nShow);

    pSpin = (CSpinButtonCtrl *)GetDlgItem( IDC_SPIN_Y_POSITION );
    pVal = (CStatic *)GetDlgItem( IDC_Y_POSITION_VAL );
    if (pSpin) pSpin->ShowWindow(SW_SHOW);
    if (pVal)  pVal->ShowWindow(SW_SHOW);

    pVal = (CStatic *)GetDlgItem( IDC_DIMENSION_TXT);
    if (pVal)  pVal->ShowWindow( nShow);
    pVal = (CStatic *)GetDlgItem( IDC_POSITION_TXT);
    if (pVal)  pVal->ShowWindow( nShow);

    pButton = (CButton *)GetDlgItem( IDC_WHITEBALANCE);
    if (pButton) pButton->ShowWindow( nShow);
  }
}


//
// Force an update of the image display.  The application
// will ignore updates during live video display.
//
void CVideoAdjust::UpdateImage(BOOL bResize /* = TRUE */)
{
  if ( m_pUpdateDisplay )
    (*m_pUpdateDisplay)( m_pParent, bResize, TRUE );

  if (bResize && (!m_bRestrictResizing)) {
    //
    // Restore the local RSET
    //
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
  }
}


//
// Update the passed RSET to have the same size parameters
// (and whatever else is desirable) as the parameters for
// this dialog.
//
void CVideoAdjust::UpdateVARSET( RSET *pRSET)
{
  UpdateVideoSetting UVSetting;

  if (pRSET == NULL)
    return;

  pRSET->lRegs[HPR_GAIN]   = m_RSet.lRegs[HPR_GAIN];
  pRSET->lRegs[HPR_BLEVEL] = m_RSet.lRegs[HPR_BLEVEL];
  pRSET->lRegs[HPR_PHASE]  = m_RSet.lRegs[HPR_PHASE];
  pRSET->lRegs[HPR_WIDTH]  = m_RSet.lRegs[HPR_WIDTH];
  pRSET->lRegs[HPR_HEIGHT] = m_RSet.lRegs[HPR_HEIGHT];
  pRSET->lRegs[HPR_VBP]    = m_RSet.lRegs[HPR_VBP];

  if ( IsTVBoard() ) {
    UVSetting.pRSet = pRSET;
    UVSetting.bValue = (BYTE)m_nXPosition;
    eHP_SetControlValue( m_BoardHandle, "HorizontalPosition",
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = (BYTE)m_nBrightness;
    eHP_SetControlValue( m_BoardHandle, "Brightness",
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = (BYTE)m_nContrast;
    eHP_SetControlValue( m_BoardHandle, "Contrast",
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = (BYTE)m_nHue;
    eHP_SetControlValue( m_BoardHandle, "Hue",
                         sizeof(UVSetting), &UVSetting);
    UVSetting.bValue = (BYTE)m_nSaturation;
    eHP_SetControlValue( m_BoardHandle, "Saturation",
                         sizeof(UVSetting), &UVSetting);
  } else {
    pRSET->lRegs[HPR_HBS] = m_RSet.lRegs[HPR_HBS];
  }
}


//
// See if size has changed
//
BOOL CVideoAdjust::HasSizeChanged()
{
  return(   (m_nWidth     != m_nOriginalWidth)
         || (m_nHeight    != m_nOriginalHeight));
}


//
// Save a copy of the I-Color registers (in case of a
// cancel request).
//
void CVideoAdjust::SaveIColorRegisters()
{
  ERRTYPE          e;
  UpdateRegisterSet
                   stRegSet;

  if (m_BoardHandle < 0)
    return;
  //
  // Allocate and get the register set
  //
  if (m_pRegisterSet != NULL)
  {
    delete m_pRegisterSet;
    m_pRegisterSet = NULL;
  }
  m_pRegisterSet = (BYTE *) new BYTE[KS0127_REGISTER_SIZE];
  if (m_pRegisterSet == (BYTE*) 0)
    return;
  memset( m_pRegisterSet, 0, KS0127_REGISTER_SIZE);
  stRegSet.pKS0127Registers = m_pRegisterSet;
  stRegSet.pRSet = &m_RSet;

  e = eHP_GetControlValue( m_BoardHandle,
                           "KS0127RegisterSet",
                           sizeof( UpdateRegisterSet),
                           (void*) &stRegSet);
  if (e)
  {
    if (m_pRegisterSet != NULL)
    {
      delete m_pRegisterSet;
      m_pRegisterSet = NULL;
    }
  }
}


//
// Restore the saved I-Color registers.  Changes made to
// the register settings (including positional and color
// settings) will be lost.
//
void CVideoAdjust::RestoreIColorRegisters()
{
  UpdateRegisterSet
                   stRegSet;

  if (   (m_BoardHandle < 0)
      || (m_pRegisterSet == NULL))
    return;
  //
  // Write back the register set
  //
  stRegSet.pKS0127Registers = m_pRegisterSet;
  stRegSet.pRSet = &m_RSet;
  eHP_SetControlValue( m_BoardHandle,
                           "KS0127RegisterSet",
                           sizeof( UpdateRegisterSet),
                           (void*) &stRegSet);
  //
  // Update the current image (resnap, etc.) as needed
  //
  UpdateImage(FALSE);
  if (m_pRegisterSet != NULL)
  {
    delete m_pRegisterSet;
    m_pRegisterSet = NULL;
  }
}


//
// Doing a white balance during continuous capture causes
// contention problems for the registers due to RSET_Set calls.
// Don't call this function while doing a live display or
// streaming operation.
//
void CVideoAdjust::OnWhiteBalance()
{
  RunWhiteBalance(FALSE);
}

void CVideoAdjust::RunWhiteBalance(BOOL bSilent)
{
  int              nReturn = 0;
  HD_WBALDATA      balanceData;
  ERRTYPE          e = 0;
  CProgressCtrl   *pProgress     = NULL;
  CWBalThread     *pWBalThread   = NULL;
  CWhiteBalance   *pWhiteBalance = NULL;

  if (m_BoardHandle < 0) return;
  //
  // Provide introductory information.  Recommend a grill pattern
  // with sections of total black and total white in the image.
  //
  EnableWindow(FALSE);
  if (!bSilent)
    if (AfxMessageBox( "White balancing adjusts the RGB gain and offset "
                       "calibration values from the I-RGB board defaults.  "
                       "It requires an image with pure white and pure black.  "
                       "A vertical grill pattern is ideal for this operation."
                       "\n\nYou should set the brightness and contrast to "
                       "appropriate values before white balancing; repeat "
                       "this operation if you change those values significantly.\n\n"
                       "White balance can take up to several minutes to run, "
                       "depending on the image size.  Once the changes are "
                       "accepted, they are not reset by clicking 'Cancel' in the "
                         "video adjustment dialog.  Are you ready to proceed?",
                       MB_ICONQUESTION|MB_YESNO) != IDYES)
      goto WhiteBalExit;
  //
  // Spawn a thread to run the white balance operation.  If the
  // return value is 0, everything worked.
  //
  if (!bSilent) {
    pWhiteBalance = (CWhiteBalance *)new CWhiteBalance;
    pWhiteBalance->Create(IDD_WHITE_BALANCE, this);
    pWhiteBalance->ShowWindow(SW_SHOWNORMAL);
    pProgress = (CProgressCtrl*)pWhiteBalance->GetDlgItem(IDC_WHITEBAL_PROGRESS);
  }
  if (pProgress != NULL) {
    pProgress->SetPos(0);
    Invalidate();
    UpdateWindow();
  }
  memset( &balanceData, 0, sizeof(HD_WBALDATA));
  pWBalThread = (CWBalThread*) AfxBeginThread( RUNTIME_CLASS(CWBalThread),
                                     THREAD_PRIORITY_NORMAL,
                                     0,
                                     0,
                                     NULL);
  pWBalThread->m_ImageHandle = m_ImageHandle;
  pWBalThread->m_pRSET       = &m_RSet;
  pWBalThread->m_pWBalData   = &balanceData;
  pWBalThread->m_pnReturn    = &nReturn;
  //
  // While waiting for the thread to finish the white balance,
  // handle the progress bar.
  //
  while (TRUE) {
    int            nPercent;
    //
    // Thread is done if the return value changes (negative for errors,
    // positive for successful run)
    //
    if (nReturn != 0) break;

    if (balanceData.nTotalSteps < 1)
      nPercent = 0;
    else
      nPercent = (balanceData.nCurrentStep * 100)
                   / balanceData.nTotalSteps;
    if (nPercent > 100)
      nPercent = 100;
    if (pProgress != NULL)
      pProgress->SetPos( nPercent);
    if (pWhiteBalance) pWhiteBalance->UpdateWindow();
    Sleep(100);
  }
  if (pProgress) pProgress->SetPos( 100);

  if (nReturn < 0) {
    if (nReturn == -1) {
      if (!bSilent)
        ReportMsg("The white balance operation failed.  The EEPROM "
                    "default values could not be read.",
                    MB_ICONSTOP|MB_OK);
      goto WhiteBalExit;
    } else {
      if (!bSilent)
        ReportMsg("The white balance operation failed.  You may want to "
                    "change the video pattern and try again.",
                    MB_ICONSTOP|MB_OK);
      goto WhiteBalExit;
    }
  }
  //
  // Update gain and offset calibration and register values
  //
  balanceData.dwMode = 0;
  e = eHD_SetWhiteBalance( m_ImageHandle, &m_RSet, &balanceData);
  if (e) {
    if (!bSilent)
      ReportMsg("Unable to update calibration values.",
                  MB_ICONSTOP|MB_OK);
    goto WhiteBalExit;
  }
  //
  // NOTE: We should be OK without updating the file's reference
  //       board and serial number values, but we're going to do
  //       it anyway.
  //
  if (m_BoardHandle > 0) {
    char           szSN[9];
    short          hdt;
    long           lBoardType, lSerial;

    lBoardType = 0;
    lSerial    = 0;
    //
    // Retrieve the board serial number and type
    //
    memset( szSN, 0, 9);
    nHP_GetBoardInfo( m_BoardHandle, HPEE_SERIAL_NO, 8, (void *) szSN);
    lSerial = lHD_EncodeSerial(szSN);
    hdt = 3;
    nHP_GetBoardInfo( m_BoardHandle, HPEE_BOARD_TYPE, 2, &hdt);
    lBoardType = (long) hdt;

    m_RSet.lRegs[HPR_REFBTYPE] = lBoardType;
    HD_RSETINDEX_SET( &m_RSet, HPR_REFBTYPE);
    m_RSet.lRegs[HPR_REFBOARD] = lSerial;
    HD_RSETINDEX_SET( &m_RSet, HPR_REFBOARD);
  }
  if (!e) {
    //
    // Update the current image (resnap, etc.) as needed
    //
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
    UpdateImage(FALSE);
  }
  if (bSilent) goto WhiteBalExit;
  //
  // Ask the user to keep the changes.
  //
  if (e || (AfxMessageBox("Do you want to keep the changes?",
                          MB_ICONQUESTION|MB_YESNO)
            != IDYES)) {
    //
    // Restore original calibration & registers.  Don't
    // refresh the gain and offset right now.
    //
    balanceData.dwMode = 1;
    e = eHD_SetWhiteBalance( m_ImageHandle, &m_RSet, &balanceData);
    eHD_RSET_Set( m_ImageHandle, &m_RSet, HDSET_SYNCHR_ON );
    UpdateImage();
  }
WhiteBalExit:
  if (pWhiteBalance) delete pWhiteBalance;
  EnableWindow(TRUE);
}

BOOL CVideoAdjust::IsMonochromeInput() 
{
  if ( !IsHighSpeedRGBBoard() ) return FALSE;

  UpdateVideoSettingLong uv;
  uv.lValue = -1;
  uv.pRSet = &m_RSet;
  eHP_GetControlValue(m_BoardHandle, "MonoCapture", sizeof(uv), (void *)&uv);
  if ( uv.lValue ) return TRUE;
  return FALSE;
}
