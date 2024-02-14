// RotaryJunctionDlg.cpp : implementation file
//

#include "pch.h"
#include "RotaryJunctionDlg.h"
#include "afxdialogex.h"
#include "Utility.h"
#include "ZaberController.h"
#include "MotorController.h"
#include "Configuration.h"
#include "PiUsb.h"


// CRotaryJunctionDlg dialog

IMPLEMENT_DYNAMIC(CRotaryJunctionDlg, CDialogEx)

CRotaryJunctionDlg::CRotaryJunctionDlg(CWnd* pParent /*=nullptr*/)
	: CDialogEx(IDD_ROTARY_JUNCTION_DIALOG, pParent)
{
	m_isClickedBackward = false;
	m_isClickedForward = false;
	m_pThreadInterferometer = NULL;
	m_pShutter = nullptr;
	m_isShutterOpened = true;
}

CRotaryJunctionDlg::~CRotaryJunctionDlg()
{
}

UINT CRotaryJunctionDlg::threadInterferometer(LPVOID param) {
	CRotaryJunctionDlg* pDlg = (CRotaryJunctionDlg*)param;
	CZaberController* pDelayLine = pDlg->m_pDelayLine;

	while (pDlg->m_pThreadInterferometer->isRun) {
		if (pDlg->m_isClickedBackward) {
			pDelayLine->RotateRelative(DELAYLINE_BACKWARD_POSITION);
		}
		else if (pDlg->m_isClickedForward) {
			pDelayLine->RotateRelative(DELAYLINE_FORWARD_POSITION);
		}
		Sleep(30);
	}

	return NOERROR;
}

void CRotaryJunctionDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BOOL CRotaryJunctionDlg::OnInitDialog() {
	CDialogEx::OnInitDialog();

	CConfiguration& config = CConfiguration::GetInstance();
	int result = 0;
	m_pShutter = piConnectShutter(&result, config.shutterSerial);
	if (result == PI_NO_ERROR) {
		piSetShutterState(PI_SHUTTER_OPEN, m_pShutter);
		m_isShutterOpened = true;
		GetDlgItem(IDC_BUTTON_CLOSE_SHUTTER)->EnableWindow(TRUE);
	}

	return TRUE;  // return TRUE  unless you set the focus to a control
}

BEGIN_MESSAGE_MAP(CRotaryJunctionDlg, CDialogEx)
	ON_BN_CLICKED(IDC_BUTTON_ZABER_IDLE, &CRotaryJunctionDlg::OnBnClickedButtonZaberIdle)
	ON_BN_CLICKED(IDC_BUTTON_ZABER_MOVE, &CRotaryJunctionDlg::OnBnClickedButtonZaberMove)
	ON_BN_CLICKED(IDC_BUTTON_ZABER_PULLBACK, &CRotaryJunctionDlg::OnBnClickedButtonZaberPullback)
	ON_BN_CLICKED(IDC_BUTTON_MOTOR_PERFORM_RUN, &CRotaryJunctionDlg::OnBnClickedButtonMotorPerformRun)
	ON_BN_CLICKED(IDC_BUTTON_MOTOR_STOP, &CRotaryJunctionDlg::OnBnClickedButtonMotorStop)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_SETTINGS, &CRotaryJunctionDlg::OnBnClickedButtonSaveSettings)
	ON_WM_DESTROY()
	ON_WM_SHOWWINDOW()
	ON_BN_CLICKED(IDC_BUTTON_CLOSE_SHUTTER, &CRotaryJunctionDlg::OnBnClickedButtonCloseShutter)
END_MESSAGE_MAP()


// CRotaryJunctionDlg message handlers

BOOL CRotaryJunctionDlg::PreTranslateMessage(MSG* pMsg) {
	if (pMsg->message == WM_KEYDOWN) {
		if (pMsg->wParam == VK_ESCAPE)
		{
			return TRUE;
		}
	}
	else if (pMsg->message == WM_LBUTTONDOWN) {
		m_isClickedBackward = isPointInComponent(this, IDC_BUTTON_MOVE_ZABER_BACKWARD, pMsg->pt);
		m_isClickedForward = isPointInComponent(this, IDC_BUTTON_MOVE_ZABER_FORWARD, pMsg->pt);
		if (m_isClickedBackward || m_isClickedForward) {
			CUtility::StartThread(threadInterferometer, m_pThreadInterferometer, this);
		}
	}
	else if (pMsg->message == WM_LBUTTONUP || pMsg->message == WM_MOUSELEAVE) {
		if (m_isClickedBackward || m_isClickedForward) {
			m_isClickedBackward = false;
			m_isClickedForward = false;
			CUtility::StopThread(m_pThreadInterferometer);
		}
	}

	return CDialogEx::PreTranslateMessage(pMsg);
}

void CRotaryJunctionDlg::OnShowWindow(BOOL bShow, UINT nStatus)
{
	__super::OnShowWindow(bShow, nStatus);

	if (bShow) {
		CZaberController* pPullback = m_pPullback;
		CZaberController* pDelayLine = m_pDelayLine;
		CMotorController* pMotorCtrl = CMotorController::GetInstance();

		bool zaberConnected = (pPullback != nullptr && pPullback->IsOpen());
		if (zaberConnected) {
			GetDlgItem(IDC_BUTTON_ZABER_IDLE)->EnableWindow(TRUE);
			GetDlgItem(IDC_BUTTON_ZABER_MOVE)->EnableWindow(TRUE);
			GetDlgItem(IDC_BUTTON_ZABER_PULLBACK)->EnableWindow(TRUE);
		}

		bool motorConnected = (pMotorCtrl != nullptr && pMotorCtrl->IsConnected());
		if (motorConnected) {
			GetDlgItem(IDC_BUTTON_MOTOR_PERFORM_RUN)->EnableWindow(TRUE);
			GetDlgItem(IDC_BUTTON_MOTOR_STOP)->EnableWindow(TRUE);
		}

		bool interferometerConnected = (pDelayLine != nullptr && pDelayLine->IsOpen());
		if (interferometerConnected) {
			GetDlgItem(IDC_BUTTON_MOVE_ZABER_BACKWARD)->EnableWindow(TRUE);
			GetDlgItem(IDC_BUTTON_MOVE_ZABER_FORWARD)->EnableWindow(TRUE);
		}
		m_isClickedBackward = false;
		m_isClickedForward = false;


		CConfiguration &config = CConfiguration::GetInstance();
		CString strBuffer = _T("");

		GetDlgItem(IDC_EDIT_PULLBACK_ZABER_PORT)->SetWindowText(config.stepMotor.port);
		GetDlgItem(IDC_EDIT_INTERFEROMETER_ZABER_PORT)->SetWindowText(config.laserModule.port);

		strBuffer.Format(_T("%d"), config.stepMotor.pullbackDistance);
		GetDlgItem(IDC_EDIT_ZABER_DISTANCE)->SetWindowText(strBuffer);

		strBuffer.Format(_T("%d"), config.stepMotor.pullbackSpeed);
		GetDlgItem(IDC_EDIT_ZABER_VELOCITY)->SetWindowText(strBuffer);

		strBuffer.Format(_T("%d"), config.bldcMotor.velocityPullback);
		GetDlgItem(IDC_EDIT_MOTOR_VELOCITY)->SetWindowText(strBuffer);
	}
}

void CRotaryJunctionDlg::OnDestroy()
{
	__super::OnDestroy();

	CUtility::StopThread(m_pThreadInterferometer);

	if (m_pShutter != nullptr) {
		piDisconnectShutter(m_pShutter);
		m_pShutter = nullptr;
	}
}


void CRotaryJunctionDlg::OnBnClickedButtonZaberIdle()
{
	m_pPullback->Idle();	
}


void CRotaryJunctionDlg::OnBnClickedButtonZaberMove()
{
	CString strPosition = _T("");
	int nPosition = 0;

	GetDlgItem(IDC_EDIT_ZABER_POSITION)->GetWindowText(strPosition);
	nPosition = _ttoi(strPosition);

	m_pPullback->MoveAbsolute(nPosition);
}


void CRotaryJunctionDlg::OnBnClickedButtonZaberPullback()
{
	CString strVelocity = _T("");
	CString strDistance = _T("");
	int nVelocity = 0;
	int nDistance = 0;

	GetDlgItem(IDC_EDIT_ZABER_VELOCITY)->GetWindowText(strVelocity);
	nVelocity = _ttoi(strVelocity);
	GetDlgItem(IDC_EDIT_ZABER_DISTANCE)->GetWindowText(strDistance);
	nDistance = _ttoi(strDistance);

	m_pPullback->Pull(nVelocity, nDistance);
}


void CRotaryJunctionDlg::OnBnClickedButtonMotorPerformRun()
{
	CMotorController* pMotorCtrl = CMotorController::GetInstance();
	CString strVelocity = _T("");
	int nVelocity = 0;

	GetDlgItem(IDC_EDIT_MOTOR_VELOCITY)->GetWindowText(strVelocity);
	nVelocity = _ttoi(strVelocity);

	pMotorCtrl->PerformRun(nVelocity);

	strVelocity.Format(_T("%d"), nVelocity);
	GetDlgItem(IDC_EDIT_MOTOR_VELOCITY)->SetWindowText(strVelocity);
}


void CRotaryJunctionDlg::OnBnClickedButtonMotorStop()
{
	CMotorController* pMotorCtrl = CMotorController::GetInstance();

	pMotorCtrl->StopMotor();
}

void CRotaryJunctionDlg::OnBnClickedButtonSaveSettings()
{
	CConfiguration& config = CConfiguration::GetInstance();
	CString strBuffer = _T("");

	GetDlgItem(IDC_EDIT_ZABER_DISTANCE)->GetWindowText(strBuffer);
	config.stepMotor.pullbackDistance = _ttoi(strBuffer);

	GetDlgItem(IDC_EDIT_ZABER_VELOCITY)->GetWindowText(strBuffer);
	config.stepMotor.pullbackSpeed = _ttoi(strBuffer);

	GetDlgItem(IDC_EDIT_MOTOR_VELOCITY)->GetWindowText(strBuffer);
	config.bldcMotor.velocityPullback = _ttoi(strBuffer);

	config.SaveStepMotorSettings();
	config.SaveBLDCMotorSettings();
}


void CRotaryJunctionDlg::OnBnClickedButtonCloseShutter()
{
	if (m_pShutter == nullptr) return;

	m_isShutterOpened = true;
	if (m_isShutterOpened) {
		piSetShutterState(PI_SHUTTER_CLOSED, m_pShutter);
		GetDlgItem(IDC_BUTTON_CLOSE_SHUTTER)->SetWindowText(_T("Open"));
	}
	else {
		piSetShutterState(PI_SHUTTER_OPEN, m_pShutter);
		GetDlgItem(IDC_BUTTON_CLOSE_SHUTTER)->SetWindowText(_T("Close"));
	}
	m_isShutterOpened = !m_isShutterOpened;
}
