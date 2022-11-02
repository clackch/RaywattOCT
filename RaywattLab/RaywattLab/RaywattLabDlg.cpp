
// RaywattLabDlg.cpp: 구현 파일
//

#include "pch.h"
#include "framework.h"
#include "RaywattLab.h"
#include "RaywattLabDlg.h"
#include "afxdialogex.h"
#include "Configuration.h"
#include "ATSDevice.h"
#include "SimulateDevice.h"
#include "LabImaging.h"
#include "OCTMeasurement.h"
#include "Calibration.h"
#include "DataWriter.h"
#include "DataReader.h"
#include "VideoWriter.h"
#include "TIFFWriter.h"
#include "ZaberController.h"
#include "MotorController.h"
#include "PiUsb.h"
#include "LaserController.h"
#include "Utility.h"
#include <opencv2/opencv.hpp>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CRaywattLabDlg 대화 상자

CRaywattLabDlg::CRaywattLabDlg(CWnd* pParent /*=nullptr*/)
	: CDialogEx(IDD_RAYWATTLAB_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);

	m_pThreadService = nullptr;
	m_pAcqDevice = nullptr;
	m_pSimDevice = nullptr;
	m_pImagingRealtime = nullptr;
	m_pImagingSimulate = nullptr;
	m_isRealtime = true;
	m_pDataWriter = nullptr;
	m_pFFTFile = nullptr;
	m_pDataReader = nullptr;
	m_pShutter = nullptr;

	m_pThreadCalibration = nullptr;
	m_pFrameBuffer = nullptr;

	m_strCalibPath = _T("");
	m_nCurCalibIndex = 0;
	m_pThreadPullback = nullptr;
	m_strCurCalibration = _T(".\\CALIBRATION.dat");
}

void CRaywattLabDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_LIST_PATIENT_DATA, m_listPatientData);
	DDX_Control(pDX, IDC_PICT_OCT_IMAGE, m_pictOCTImage);
	DDX_Radio(pDX, IDC_RADIO_IMAGE_CIRCLE, m_radioImageShape);
	DDX_Radio(pDX, IDC_RADIO_COLOR_BLACK, m_radioImageColor);
	DDX_Check(pDX, IDC_CHECK_HOT_COLOR, m_chkImageHotColor);
	DDX_Control(pDX, IDC_SLIDER_BRIGHTNESS, m_sliderBrightness);
	DDX_Control(pDX, IDC_SLIDER_CONTRAST, m_sliderContrast);
	DDX_Check(pDX, IDC_CHECK_INIT_MOTOR, m_chkInitMotor);
	DDX_Check(pDX, IDC_CHECK_INIT_STAGE, m_chkInitStage);
}

// private methods

int CRaywattLabDlg::initializeDevices() {
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);
	CZaberController* pInterferometer = CZaberController::GetInstance(ZABER_TYPE_INTERFEROMETER);

	if (m_pAcqDevice == nullptr) {
		m_pAcqDevice = new CATSDevice();
		m_pAcqDevice->SetImaging(m_pImagingRealtime);
		m_pAcqDevice->SetWriter(m_pDataWriter);
	}

	if (m_pAcqDevice->InitDevice() != NOERROR) {
		m_pAcqDevice->CleanUp();
		return E_FAIL;
	}

	if (m_chkInitStage) {
		if (pLinearStage->Open(config.zaber.pullback) == false) {
			m_pAcqDevice->CleanUp();
			return E_FAIL;
		}

		if (pInterferometer->Open(config.zaber.interferometer) == false) {
			pLinearStage->Close();
			m_pAcqDevice->CleanUp();
			return E_FAIL;
		}
	}

	if (m_chkInitMotor) {
		if (pMotor->Connect() == false) {
			pInterferometer->Close();
			pLinearStage->Close();
			m_pAcqDevice->CleanUp();
			return E_FAIL;
		}
		pMotor->SwitchOn();
	}

	CLaserController::GetInstance()->LaserOnOff(true);
	m_pAcqDevice->StartAcquisition();

	return NOERROR;
}
void CRaywattLabDlg::updatePatientDataList() {
	for (int i = m_listPatientData.GetCount(); i >= 0; i--) {
		m_listPatientData.DeleteString(i);
	}

	if (m_strPatientPath.IsEmpty()) return;

	AfxGetApp()->WriteProfileString(_T("RECENT_SETTING"), _T("PATIENT_PATH"), m_strPatientPath);
	GetDlgItem(IDC_EDIT_CURRENT_PATH)->SetWindowText(m_strPatientPath);

	CString strQuery = _T("");
	strQuery.Format(_T("%s\\*.bin"), m_strPatientPath);

	CFileFind fileFind;
	BOOL find = fileFind.FindFile(strQuery);
	while (find) {
		find = fileFind.FindNextFile();
		m_listPatientData.AddString(fileFind.GetFileName());
	}

	m_strPatientName = m_strPatientPath.Right(m_strPatientPath.GetLength() - m_strPatientPath.ReverseFind('\\') - 1);
	GetDlgItem(IDC_EDIT_CURRENT_PATIENT)->SetWindowText(m_strPatientName);
}

void CRaywattLabDlg::initScopeViewLayout() {
	const int nPaddingBetweenScopeViews = 20;
	const int nPaddingBottom = 20;
	const int nPaddingLeft = 10;
	RECT rect;
	m_pictOCTImage.GetWindowRect(&rect);

	int x = rect.left;
	int y = rect.top;
	int width = (rect.right - rect.left);
	int height = (rect.bottom - rect.top);
	int heightScope = (height - nPaddingBetweenScopeViews - nPaddingBottom) / 2;

	x = x + width + nPaddingLeft;
	m_scopeView.MoveWindow(x, y, width, heightScope);
	y = y + heightScope + nPaddingBetweenScopeViews;
	m_scopeViewFFT.MoveWindow(x, y, width, heightScope);

	m_scopeView.ShowWindow(SW_SHOW);
	m_scopeViewFFT.ShowWindow(SW_SHOW);
}
void CRaywattLabDlg::updateBrightnessContrast() {
	const double rangeB[] = { 0.0f, 100.0f };
	const double rangeC[] = { 0.5f, 3.0f };
	const double rangeSliderB[] = { m_sliderBrightness.GetRangeMin(), m_sliderBrightness.GetRangeMax() };
	const double rangeSliderC[] = { m_sliderContrast.GetRangeMin(), m_sliderContrast.GetRangeMax() };
	const int posB = m_sliderBrightness.GetPos();
	const int posC = m_sliderContrast.GetPos();

	double brightness = ((double)posB / rangeSliderB[1]) * (rangeB[1] - rangeB[0]) + rangeB[0];
	double contrast = ((double)posC / rangeSliderC[1]) * (rangeC[1] - rangeC[0]) + rangeC[0];

	m_pImagingRealtime->SetBrightnessContrast(brightness, contrast);
	m_pImagingSimulate->SetBrightnessContrast(brightness, contrast);

	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("BRIGHTNESS"), posB);
	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("CONTRAST"), posC);
}
CString CRaywattLabDlg::generateFileName(CString strPath, CString strExtension, CString strPrefix) {
	CTime currentTime = CTime::GetCurrentTime();
	CString strFilePath = _T("");

	if (!strPrefix.IsEmpty()) {
		strFilePath.Format(_T("%s\\%s_%s%s"), strPath, strPrefix, currentTime.Format("%m%d_%H%M%S"), strExtension);
	}
	else {
		strFilePath.Format(_T("%s\\%s%s"), strPath, currentTime.Format("%m%d_%H%M%S"), strExtension);
	}
	return strFilePath;
}
void CRaywattLabDlg::findFileByExtension(CString strFolder, CString strExt, std::vector<CString>& vList) {
	CString strQuery = _T("");
	strQuery.Format(_T("%s\\*.%s"), strFolder, strExt);

	CFileFind fileFind;
	BOOL find = fileFind.FindFile(strQuery);
	while (find) {
		find = fileFind.FindNextFile();
		vList.push_back(fileFind.GetFilePath());
	}
	std::sort(vList.begin(), vList.end());
}
void CRaywattLabDlg::updateMeasurement(USHORT nPeakValue, int nPeakIndex, int nLineWidth, USHORT nNoisePower) {
	CString strBuffer = _T("");

	strBuffer.Format(_T("%d"), nPeakValue);
	GetDlgItem(IDC_EDIT_PEAK)->SetWindowText(strBuffer);
	strBuffer.Format(_T("%d"), nPeakIndex);
	GetDlgItem(IDC_EDIT_PEAK_INDEX)->SetWindowText(strBuffer);
	strBuffer.Format(_T("%d"), nLineWidth);
	GetDlgItem(IDC_EDIT_LINEWIDTH)->SetWindowText(strBuffer);
	strBuffer.Format(_T("%d"), nNoisePower);
	GetDlgItem(IDC_EDIT_NOISE)->SetWindowText(strBuffer);
}


/*
* threadService
*/
UINT CRaywattLabDlg::threadService(LPVOID param) {
	CRaywattLabDlg* pDlg = (CRaywattLabDlg*) param;
	CThread* pThread = pDlg->m_pThreadService;

	while (pThread->isRun) {
		std::tuple<int, WPARAM, LPARAM> popMsgThread = pDlg->popMessage();
		int popMsg = std::get<0>(popMsgThread);
		WPARAM wParam = std::get<1>(popMsgThread);
		LPARAM lParam = std::get<2>(popMsgThread);

		pDlg->PostMessage(popMsg, wParam, lParam);
		Sleep(5);
	}

	return NOERROR;
}
UINT CRaywattLabDlg::threadSaveCalibration(LPVOID param) {
	CRaywattLabDlg* pDlg = (CRaywattLabDlg*)param;
	CThread *pThread = pDlg->m_pThreadCalibration;
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	int from, step, count = 0;

	CString strValue = _T("");
	pDlg->GetDlgItem(IDC_EDIT_CALIBRATION_FROM)->GetWindowText(strValue);
	from = _wtoi(strValue);
	pDlg->GetDlgItem(IDC_EDIT_CALIBRATION_STEP)->GetWindowText(strValue);
	step = _wtoi(strValue);
	pDlg->GetDlgItem(IDC_EDIT_CALIBRATION_COUNT)->GetWindowText(strValue);
	count = _wtoi(strValue);

	pLinearStage->Move(from);
	while (pDlg->m_pThreadCalibration->isRun && !pLinearStage->GetZaberStatus()) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	for (int frame = 0; frame < count && pDlg->m_pThreadCalibration->isRun; frame++) {
		int position = from + (frame * step);
		pLinearStage->Move(position);
		while (pDlg->m_pThreadCalibration->isRun && !pLinearStage->GetZaberStatus()) {
			Sleep(DELAY_FOR_STOP_THREAD);
		}

		pDlg->PostMessage(WM_SAVE_CALIBRATION_FRAME, position);
		CUtility::SuspendThread(pDlg->m_pThreadCalibration);
	}

	pDlg->PostMessage(WM_SAVE_CALIBRATION_DONE);
	while (pDlg->m_pThreadCalibration->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}
UINT CRaywattLabDlg::threadPullback(LPVOID param) {
	CRaywattLabDlg* pDlg = (CRaywattLabDlg*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	if (pZaber->IsOpen()) {
		pZaber->SetSpeed(config.zaber.pullbackSpeed);

		// Start Recording OCT
		pDlg->m_pDataWriter->StartRecording();

		// Pullback Linear Stage
		pZaber->MoveRelative(config.zaber.pullbackDistance * -1);
		while (pDlg->m_pThreadPullback->isRun) {
			if (pZaber->GetZaberStatus()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}

		// Stop Recording OCT
		pDlg->m_pDataWriter->StopRecording();

		CString strPrefix = _T("");
		strPrefix.Format(_T("%dalines_%drpm_%dmm_%dmms"), config.nBScan, config.motor.velocity, config.zaber.pullbackDistance, config.zaber.pullbackSpeed);
		CString strFileName = pDlg->generateFileName(pDlg->m_strPatientPath, _T(".bin"), strPrefix);

		CDataWriter* pDataManager = pDlg->m_pDataWriter;
		const int nNumOfSamples = pDataManager->GetNumOfSamples();

		pDataManager->StartSave(strFileName.GetBuffer());
		for (int nFrame = 0; nFrame < nNumOfSamples && pDlg->m_pThreadPullback->isRun; nFrame++) {
			pDataManager->WriteFrame(nFrame);
		}
		pDataManager->StopSave();
	}

	pDlg->PostMessage(WM_PULLBACK_DONE);

	// wait for StopThread
	while (pDlg->m_pThreadPullback->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}


BEGIN_MESSAGE_MAP(CRaywattLabDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_WM_DESTROY()
	ON_MESSAGE(WM_PROCESS_OCT_DONE, &CRaywattLabDlg::OnMsgProcessOCTDone)
	ON_MESSAGE(WM_SAVE_CALIBRATION_FRAME, &CRaywattLabDlg::OnMsgSaveCalibrationFrame)
	ON_MESSAGE(WM_SAVE_CALIBRATION_DONE, &CRaywattLabDlg::OnMsgSaveCalibrationDone)
	ON_MESSAGE(WM_PULLBACK_DONE, &CRaywattLabDlg::OnMsgPullbackDone)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_DATA_FOLDER, &CRaywattLabDlg::OnBnClickedButtonOpenDataFolder)
	ON_BN_CLICKED(IDC_BUTTON_LOAD_SELECTED_DATA, &CRaywattLabDlg::OnBnClickedButtonLoadSelectedData)
	ON_BN_CLICKED(IDC_BUTTON_PLAY_LOADED_DATA, &CRaywattLabDlg::OnBnClickedButtonPlayLoadedData)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_VIDEO, &CRaywattLabDlg::OnBnClickedButtonSaveVideo)
	ON_BN_CLICKED(IDC_RADIO_IMAGE_CIRCLE, &CRaywattLabDlg::OnBnClickedRadioImageCircle)
	ON_BN_CLICKED(IDC_RADIO_IMAGE_RECTANGLE, &CRaywattLabDlg::OnBnClickedRadioImageRectangle)
	ON_BN_CLICKED(IDC_RADIO_COLOR_BLACK, &CRaywattLabDlg::OnBnClickedRadioColorBlack)
	ON_BN_CLICKED(IDC_RADIO_COLOR_WHITE, &CRaywattLabDlg::OnBnClickedRadioColorWhite)
	ON_BN_CLICKED(IDC_CHECK_HOT_COLOR, &CRaywattLabDlg::OnBnClickedCheckHotColor)
	ON_BN_CLICKED(IDC_CHECK_CLOSE_SHUTTER, &CRaywattLabDlg::OnBnClickedCheckCloseShutter)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_BRIGHTNESS, &CRaywattLabDlg::OnNMCustomdrawSliderBrightness)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_CONTRAST, &CRaywattLabDlg::OnNMCustomdrawSliderContrast)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_DATA, &CRaywattLabDlg::OnBnClickedButtonSaveData)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_ROTARY_JUNCTION, &CRaywattLabDlg::OnBnClickedButtonOpenRotaryJunction)
	ON_BN_CLICKED(IDC_BUTTON_ADMIN_INITIALIZE, &CRaywattLabDlg::OnBnClickedButtonAdminInitialize)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_CALIBRATION, &CRaywattLabDlg::OnBnClickedButtonSaveCalibration)
	ON_BN_CLICKED(IDC_BUTTON_NEXT_CALIB, &CRaywattLabDlg::OnBnClickedButtonChangeCalibration)
	ON_BN_CLICKED(IDC_CHECK_BACKGROUND_SUBTRACT, &CRaywattLabDlg::OnBnClickedCheckBackgroundSubtract)
	ON_BN_CLICKED(IDC_CHECK_BACKGROUND_FFT_SUBTRACT, &CRaywattLabDlg::OnBnClickedCheckBackgroundImageSubtract)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_CALIB_FOLDER, &CRaywattLabDlg::OnBnClickedButtonOpenCalibFolder)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_TIF, &CRaywattLabDlg::OnBnClickedButtonSaveTif)
	ON_BN_CLICKED(IDC_BUTTON_MEASURE, &CRaywattLabDlg::OnBnClickedButtonMeasure)
	ON_BN_CLICKED(IDC_CHECK_INIT_MOTOR, &CRaywattLabDlg::OnBnClickedCheckInitMotor)
	ON_BN_CLICKED(IDC_CHECK_INIT_STAGE, &CRaywattLabDlg::OnBnClickedCheckInitStage)
	ON_BN_CLICKED(IDC_BUTTON_PULLBACK, &CRaywattLabDlg::OnBnClickedButtonPullback)
END_MESSAGE_MAP()

LRESULT CRaywattLabDlg::OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam) {
	CLabImaging* pImaging = (m_isRealtime) ? m_pImagingRealtime : m_pImagingSimulate;
	cv::Mat image = (m_radioImageShape == 0) ? pImaging->GetCircleImage() : pImaging->GetRectangleImage();
	Ipp16u* scopeData = pImaging->GetScopeData();
	Ipp16u* scopeFFTData = pImaging->GetScopeFFTData();

	CConfiguration& config = CConfiguration::GetInstance();
	const int nScopeLength = config.getScopeLength();
	const int nOutputLength = config.nOutputLength;

	drawToPictureBox(m_pictOCTImage, image.cols, image.rows, (char*)image.data);

	m_scopeView.SetChannelBuffer(0, scopeData, nScopeLength);
	m_scopeView.SetChannelBuffer(1, scopeData + nScopeLength, nScopeLength);
	m_scopeViewFFT.SetChannelBuffer(0, scopeFFTData, nOutputLength);
	m_scopeViewFFT.SetChannelBuffer(1, scopeFFTData + nOutputLength, nOutputLength);

	if (m_pDataWriter->IsRecording()) {
		Ipp16u* pFFTBuffer = new Ipp16u[nOutputLength];
		memcpy(pFFTBuffer, scopeFFTData, sizeof(Ipp16u) * nOutputLength);
		m_vFFTData.push_back(pFFTBuffer);
	}

	return NOERROR;
}

LRESULT CRaywattLabDlg::OnMsgSaveCalibrationFrame(WPARAM wParam, LPARAM lParam) {
	int position = (int)wParam;

	// To-Do : how to save frame? call DataWriter::Push directly?
	CConfiguration& config = CConfiguration::GetInstance();
	int nFrameSize = config.nBufferSize * sizeof(unsigned short);

	memcpy(m_pFrameBuffer, m_pImagingRealtime->GetFringesBuffer(), nFrameSize);

	CString strFilePath = _T("");
	strFilePath.Format(_T("%s_%dmm.bin"), m_strCalibrationPrefix, position);
	HANDLE hFile = CreateFile(
		strFilePath, GENERIC_WRITE,
		FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
		FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
	if (hFile != INVALID_HANDLE_VALUE) {
		DWORD dwBytesWrote = 0;
		WriteFile(hFile, m_pFrameBuffer, nFrameSize, &dwBytesWrote, nullptr);
		CloseHandle(hFile);
	}

	CUtility::ResumeThread(m_pThreadCalibration);

	return NOERROR;
}

LRESULT CRaywattLabDlg::OnMsgSaveCalibrationDone(WPARAM wParam, LPARAM lParam) {
	const int nIDs[] = { IDC_BUTTON_LOAD_SELECTED_DATA, IDC_EDIT_CALIBRATION_FROM, IDC_EDIT_CALIBRATION_STEP, IDC_EDIT_CALIBRATION_COUNT, IDC_BUTTON_SAVE_CALIBRATION };

	CUtility::StopThread(m_pThreadCalibration);

	for (int i = 0; i < 5; i++) {
		GetDlgItem(nIDs[i])->EnableWindow(TRUE);
	}

	return NOERROR;
}
LRESULT CRaywattLabDlg::OnMsgPullbackDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadPullback);
	GetDlgItem(IDC_BUTTON_PULLBACK)->EnableWindow(TRUE);
	updatePatientDataList();

	return NOERROR;
}



// CRaywattLabDlg 메시지 처리기

BOOL CRaywattLabDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
	CConfiguration& config = CConfiguration::GetInstance();
	if (!config.IsInit()) {
		config.Initialize(_T(".\\raywattLab.ini"));
	}

	initToggleButton(m_btnLoadData, IDC_BUTTON_LOAD_SELECTED_DATA, _T("Load"), _T("Unload"));
	initToggleButton(m_btnPlayData, IDC_BUTTON_PLAY_LOADED_DATA, _T("Play"), _T("Pause"));
	initToggleButton(m_btnSaveData, IDC_BUTTON_SAVE_DATA, _T("Save Data"), _T("Done"));
	initToggleButton(m_btnSaveVideo, IDC_BUTTON_SAVE_VIDEO, _T("Save AVI"), _T("Done"));
	initToggleButton(m_btnOpenRotaryJunction, IDC_BUTTON_OPEN_ROTARY_JUNCTION, _T("Open"), _T("Close"));

	m_strPatientPath = AfxGetApp()->GetProfileString(_T("RECENT_SETTING"), _T("PATIENT_PATH"), _T(""));
	updatePatientDataList();

	int nScopeLength = config.getScopeLength();
	m_scopeView.Create(this, 0);
	m_scopeView.AddChannel(_T("Data 1"), nScopeLength);
	m_scopeView.AddChannel(_T("Data 2"), nScopeLength);

	int nOutputLength = config.nOutputLength;
	m_scopeViewFFT.Create(this, 0);
	m_scopeViewFFT.AddChannel(_T("FFT Data 1"), nOutputLength);
	m_scopeViewFFT.AddChannel(_T("FFT Data 2"), nOutputLength);

	initScopeViewLayout();

	m_radioImageShape = 0;
	m_radioImageColor = 0;
	m_chkImageHotColor = TRUE;
	m_chkInitMotor = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("INIT_MOTOR"), TRUE);
	m_chkInitStage = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("INIT_STAGE"), TRUE);
	
	UpdateData(FALSE);

	int brightness = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("BRIGHTNESS"), 0);
	int contrast = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("CONTRAST"), 0);

	m_sliderBrightness.SetRange(0, 100);
	m_sliderBrightness.SetPos(brightness);

	m_sliderContrast.SetRange(0, 100);
	m_sliderContrast.SetPos(contrast);

	int goodClockStart = 0;
	int goodClockEnd = config.nAScan;

	m_pImagingRealtime = new CLabImaging(this);
	m_pImagingRealtime->Initialize(m_strCurCalibration, ".\\BACKGROUND.bin");
	m_pImagingRealtime->SetColor(m_chkImageHotColor);
	m_pImagingRealtime->SetGoodClockRange(goodClockStart, goodClockEnd);
	m_pImagingRealtime->Start();

	m_pImagingSimulate = new CLabImaging(this);
	m_pImagingSimulate->Initialize(m_strCurCalibration, ".\\BACKGROUND.bin");
	m_pImagingSimulate->SetColor(m_chkImageHotColor);
	m_pImagingRealtime->SetGoodClockRange(goodClockStart, goodClockEnd);
	m_pImagingSimulate->Start();

	m_pDataWriter = new CDataWriter();
	m_pDataWriter->Initialize(config.nBufferSize * sizeof(unsigned short));

	m_pDataReader = new CDataReader();

	m_pFrameBuffer = new char[config.nBufferSize * sizeof(unsigned short)];

	updateBrightnessContrast();

	int result = 0;
	m_pShutter = piConnectShutter(&result, config.shutterSerial);
	if (result == PI_NO_ERROR) {
		piSetShutterState(PI_SHUTTER_OPEN, m_pShutter);
		GetDlgItem(IDC_CHECK_CLOSE_SHUTTER)->EnableWindow(TRUE);
	}

	m_dlgRotaryJunction.Create(IDD_ROTARY_JUNCTION_DIALOG);

	GetDlgItem(IDC_EDIT_PREFIX)->SetWindowText(_T("1"));

	m_strCalibPath = AfxGetApp()->GetProfileString(_T("RECENT_SETTING"), _T("CALIB_PATH"), _T(""));
	GetDlgItem(IDC_EDIT_CALIB_PATH)->SetWindowText(m_strCalibPath);

	m_vCalibList.clear();
	m_nCurCalibIndex = 0;
	findFileByExtension(m_strCalibPath, _T("dat"), m_vCalibList);

	CUtility::StartThread(threadService, m_pThreadService, this);

	return TRUE;  // return TRUE  unless you set the focus to a control
}

// 대화 상자에 최소화 단추를 추가할 경우 아이콘을 그리려면
//  아래 코드가 필요합니다.  문서/뷰 모델을 사용하는 MFC 애플리케이션의 경우에는
//  프레임워크에서 이 작업을 자동으로 수행합니다.

void CRaywattLabDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // 그리기를 위한 디바이스 컨텍스트입니다.

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// 클라이언트 사각형에서 아이콘을 가운데에 맞춥니다.
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// 아이콘을 그립니다.
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

void CRaywattLabDlg::OnDestroy() {
	CDialogEx::OnDestroy();

	CLaserController::GetInstance()->LaserOnOff(false);

	// stop threads
	if (m_pAcqDevice != nullptr) {
		m_pAcqDevice->StopAcquisition();
	}
	if (m_pSimDevice != nullptr) {
		m_pSimDevice->StopAcquisition();
	}
	if (m_pImagingRealtime != nullptr) {
		m_pImagingRealtime->Stop();
	}
	if (m_pImagingSimulate != nullptr) {
		m_pImagingSimulate->Stop();
	}
	if (m_pDataWriter != nullptr) {
		m_pDataWriter->StopRecording();
	}
	CUtility::StopThread(m_pThreadCalibration);
	CUtility::StopThread(m_pThreadPullback);
	CUtility::StopThread(m_pThreadService);

	// free memories
	if (m_pAcqDevice != nullptr) {
		delete m_pAcqDevice;
	}
	if (m_pSimDevice != nullptr) {
		delete m_pSimDevice;
	}
	if (m_pImagingRealtime != nullptr) {
		delete m_pImagingRealtime;
	}
	if (m_pImagingSimulate != nullptr) {
		delete m_pImagingSimulate;
	}
	if (m_pDataWriter != nullptr) {
		delete m_pDataWriter;
	}
	if (m_pDataReader != nullptr) {
		delete m_pDataReader;
	}
	if (m_pFrameBuffer != nullptr) {
		delete[] m_pFrameBuffer;
	}

	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);
	CZaberController* pInterferometer = CZaberController::GetInstance(ZABER_TYPE_INTERFEROMETER);

	pMotor->StopMotor();
	pMotor->SwitchOff();
	pMotor->Disconnect();

	pLinearStage->Close();
	pInterferometer->Close();

	if (m_pShutter != nullptr) {
		piDisconnectShutter(m_pShutter);
		m_pShutter = nullptr;
	}
}


// 사용자가 최소화된 창을 끄는 동안에 커서가 표시되도록 시스템에서
//  이 함수를 호출합니다.
HCURSOR CRaywattLabDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}


BOOL CRaywattLabDlg::PreTranslateMessage(MSG* pMsg) {
	if (pMsg->message == WM_KEYDOWN) {
		if (pMsg->wParam == VK_ESCAPE || pMsg->wParam == VK_RETURN)
		{
			return TRUE;
		}
	}

	return CDialogEx::PreTranslateMessage(pMsg);
}


void CRaywattLabDlg::OnBnClickedButtonAdminInitialize()
{
	int result = initializeDevices();

	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("INIT_MOTOR"), m_chkInitMotor);
	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("INIT_STAGE"), m_chkInitStage);

	if (result == NOERROR) {
		GetDlgItem(IDC_BUTTON_ADMIN_INITIALIZE)->EnableWindow(FALSE);
		GetDlgItem(IDC_BUTTON_SAVE_DATA)->EnableWindow(TRUE);
		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(TRUE);
	}
	else {
		AfxMessageBox(_T("[FAILED] Please check the device connection"));
	}
}

void CRaywattLabDlg::OnBnClickedButtonOpenDataFolder()
{
	BROWSEINFO BrInfo;
	TCHAR szBuffer[MAX_PATH];

	::ZeroMemory(&BrInfo, sizeof(BROWSEINFO));
	::ZeroMemory(szBuffer, MAX_PATH);

	BrInfo.hwndOwner = GetSafeHwnd();
	BrInfo.lpszTitle = _T("Select Patient Data Folder");
	BrInfo.ulFlags = BIF_NEWDIALOGSTYLE | BIF_EDITBOX | BIF_RETURNONLYFSDIRS;
	LPITEMIDLIST pItemIdList = ::SHBrowseForFolder(&BrInfo);
	::SHGetPathFromIDList(pItemIdList, szBuffer);

	m_strPatientPath.Format(_T("%s"), szBuffer);

	updatePatientDataList();
}


void CRaywattLabDlg::OnBnClickedButtonLoadSelectedData()
{
	int result = NOERROR;
	bool dataLoaded = m_btnLoadData.pushed;
	bool dataPlayed = m_btnPlayData.pushed;
	bool videoSaving = m_btnSaveVideo.pushed;

	if (dataLoaded) {
		result = m_pSimDevice->StopAcquisition();
		m_isRealtime = true;

		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(TRUE);
	}
	else {
		m_isRealtime = false;

		CString strFilePath = _T("");
		CString strFileName = _T("");
		int nSelected = m_listPatientData.GetCurSel();
		m_listPatientData.GetText(nSelected, strFileName);
		strFilePath.Format(_T("%s/%s"), m_strPatientPath, strFileName);

		m_pDataReader->Initialize(strFilePath.GetBuffer());
		if (m_pSimDevice == nullptr) {
			m_pSimDevice = new CSimulateDevice(m_pDataReader);
			m_pSimDevice->SetImaging(m_pImagingSimulate);
		}
		result = m_pSimDevice->InitDevice();
		((CSimulateDevice*)m_pSimDevice)->SetPause(!dataPlayed);
		result = m_pSimDevice->StartAcquisition();

		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(FALSE);
	}

	if (result == NOERROR) {
		toggleButton(this, m_btnLoadData);
		dataLoaded = m_btnLoadData.pushed;

		GetDlgItem(IDC_BUTTON_PLAY_LOADED_DATA)->EnableWindow(dataLoaded);

		if (dataPlayed) {
			toggleButton(this, m_btnPlayData);
		}

		if (videoSaving) {
			toggleButton(this, m_btnSaveVideo);
		}
		GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->EnableWindow(TRUE);
		GetDlgItem(IDC_BUTTON_SAVE_TIF)->EnableWindow(dataLoaded);
	}
}


void CRaywattLabDlg::OnBnClickedButtonPlayLoadedData()
{
	int result = NOERROR;
	bool dataPlayed = m_btnPlayData.pushed;

	if (dataPlayed) {
		((CSimulateDevice*)m_pSimDevice)->SetPause(true);
	}
	else {
		if (((CSimulateDevice*)m_pSimDevice)->IsPaused()) {
			((CSimulateDevice*)m_pSimDevice)->SetPause(false);
		}
		else {
			printf("start simulation : %d\n", result);
		}
	}

	if (result == NOERROR) {
		toggleButton(this, m_btnPlayData);
		dataPlayed = m_btnPlayData.pushed;

		GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->EnableWindow(TRUE);
	}
}


void CRaywattLabDlg::OnBnClickedButtonSaveData()
{
	int result = NOERROR;
	bool dataSaving = m_btnSaveData.pushed;
	bool dataPlayed = m_btnPlayData.pushed;

	CConfiguration& config = CConfiguration::GetInstance();
	const int nOutputLength = config.nOutputLength;

	if (dataSaving) {
		CString strPrefix = _T("");
		GetDlgItem(IDC_EDIT_PREFIX)->GetWindowText(strPrefix);
		CString strFileName = generateFileName(m_strPatientPath, _T(".bin"), strPrefix);
		m_pDataWriter->StopRecording();
		GetDlgItem(IDC_BUTTON_SAVE_DATA)->SetWindowText(_T("Saving"));

		// Write Raw, FFT Data
		m_pImagingRealtime->Stop();
		m_pDataWriter->StartSave(strFileName.GetBuffer());
		int nNumOfSamples = m_pDataWriter->GetNumOfSamples();
		strFileName.Replace(_T(".bin"), _T(".csv"));
		CStringA fftName(strFileName);
		m_pFFTFile = fopen(fftName, "w+");

		COCTMeasurement measurement;
		USHORT nMaxPeak = 0, nNoisePower = 0;
		int nMaxIndex, nMaxWidth = 0;
		for (int idx = 0; idx < nNumOfSamples; idx++) {
			m_pDataWriter->WriteFrame(idx);
			m_pImagingRealtime->Process(m_pDataWriter->GetSample(idx));

			USHORT* pFFTData = m_pImagingRealtime->GetScopeFFTData();
			USHORT nPeakValue;
			int nPeakIndex, nLineWidth;
			measurement.CalculateAxialResolution(pFFTData, nPeakValue, nPeakIndex, nLineWidth);
			if (nPeakValue > nMaxPeak) {
				nMaxPeak = nPeakValue;
				nMaxIndex = nPeakIndex;
				nMaxWidth = nLineWidth;
				measurement.CalculateNoisePower(pFFTData, nPeakIndex, nNoisePower);
			}
			if (m_pFFTFile != nullptr) {
				for (int i = 0; i < nOutputLength; i++) {
					fprintf(m_pFFTFile, "%d,", pFFTData[i]);
				}
				fprintf(m_pFFTFile, "\n");
			}
		}
		m_pDataWriter->StopSave();
		m_pImagingRealtime->Start();
		if (m_pFFTFile != nullptr) {
			fclose(m_pFFTFile);
			m_pFFTFile = nullptr;
		}

		updateMeasurement(nMaxPeak, nMaxIndex, nMaxWidth, nNoisePower);
		updatePatientDataList();
	}
	else {
		result = m_pDataWriter->StartRecording();
		if (result != NOERROR) {
			AfxMessageBox(_T("Failed to save raw file"));
			return;
		}
	}

	if (result == NOERROR) {
		toggleButton(this, m_btnSaveData);
		dataSaving = m_btnSaveData.pushed;

		GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->EnableWindow(!dataSaving);
	}
}


void CRaywattLabDlg::OnBnClickedButtonSaveVideo()
{
	if (!m_btnLoadData.pushed || m_btnPlayData.pushed) return;

	GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->SetWindowText(_T("Saving.."));

	CString strDataPath = _T("");
	CString strDataFile = _T("");
	int nSelected = m_listPatientData.GetCurSel();
	m_listPatientData.GetText(nSelected, strDataFile);
	strDataPath.Format(_T("%s/%s"), m_strPatientPath, strDataFile);

	CString strAviPath = strDataPath;
	strAviPath.Replace(_T(".bin"), _T(".avi"));

	CLabImaging* pImaging = new CLabImaging(this);
	pImaging->Initialize(m_strCurCalibration, ".\\BACKGROUND.bin");
	pImaging->SetColor(m_chkImageHotColor);

	CDataReader* pReader = new CDataReader();
	pReader->Initialize(strDataPath.GetBuffer());

	CConfiguration& config = CConfiguration::GetInstance();
	CVideoWriter videoWriter;
	videoWriter.StartRecording(strAviPath, config.nCircleSize, config.nCircleSize);
	for (int i = 0; i < pReader->GetNumOfSamples(); i++) {
		pImaging->Process(pReader->GetSample(i));
		videoWriter.PushToBuffer(pImaging->GetCircleImage());
	}
	videoWriter.StopRecording();

	delete pReader;
	delete pImaging;

	CString strMessage = _T("");
	CString strFileName = strAviPath.Right(strAviPath.GetLength() - strAviPath.ReverseFind('\\') - 1);
	strMessage.Format(_T("%s saved."), strFileName);
	AfxMessageBox(strMessage);
	GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->SetWindowText(_T("Save Video"));
}


void CRaywattLabDlg::OnBnClickedButtonSaveTif()
{
	if (!m_btnLoadData.pushed || m_btnPlayData.pushed) return;

	GetDlgItem(IDC_BUTTON_SAVE_TIF)->SetWindowText(_T("Saving.."));

	CString strDataPath = _T("");
	CString strDataFile = _T("");
	int nSelected = m_listPatientData.GetCurSel();
	m_listPatientData.GetText(nSelected, strDataFile);
	strDataPath.Format(_T("%s/%s"), m_strPatientPath, strDataFile);

	CString strTifPath = strDataPath;
	strTifPath.Replace(_T(".bin"), _T(".tif"));
	CTIFFWriter tiffWriter(strTifPath);

	CLabImaging* pImaging = new CLabImaging(this);
	pImaging->Initialize(m_strCurCalibration, ".\\BACKGROUND.bin");
	pImaging->SetColor(m_chkImageHotColor);

	CDataReader* pReader = new CDataReader();
	pReader->Initialize(strDataPath.GetBuffer());

	for (int i = 0; i < pReader->GetNumOfSamples(); i++) {
		tiffWriter.SaveFrame(pImaging, pReader->GetSample(i));
	}

	delete pReader;
	delete pImaging;

	CString strMessage = _T("");
	CString strFileName = strTifPath.Right(strTifPath.GetLength() - strTifPath.ReverseFind('\\') - 1);
	strMessage.Format(_T("%s saved."), strFileName);
	AfxMessageBox(strMessage);
	GetDlgItem(IDC_BUTTON_SAVE_TIF)->SetWindowText(_T("Save TIF"));
}


void CRaywattLabDlg::OnBnClickedRadioImageCircle()
{
	UpdateData(TRUE);
}


void CRaywattLabDlg::OnBnClickedRadioImageRectangle()
{
	UpdateData(TRUE);
}


void CRaywattLabDlg::OnBnClickedRadioColorBlack()
{
	UpdateData(TRUE);
	bool bInvert = (m_radioImageColor != 0);
	if (m_pImagingRealtime != nullptr) m_pImagingRealtime->SetInvert(bInvert);
	if (m_pImagingSimulate != nullptr) m_pImagingSimulate->SetInvert(bInvert);
}


void CRaywattLabDlg::OnBnClickedRadioColorWhite()
{
	UpdateData(TRUE);
	bool bInvert = (m_radioImageColor != 0);
	if (m_pImagingRealtime != nullptr) m_pImagingRealtime->SetInvert(bInvert);
	if (m_pImagingSimulate != nullptr) m_pImagingSimulate->SetInvert(bInvert);
}


void CRaywattLabDlg::OnBnClickedCheckHotColor()
{
	UpdateData(TRUE);
	if (m_pImagingRealtime != nullptr) m_pImagingRealtime->SetColor(m_chkImageHotColor);
	if (m_pImagingSimulate != nullptr) m_pImagingSimulate->SetColor(m_chkImageHotColor);
}


void CRaywattLabDlg::OnBnClickedCheckCloseShutter()
{
	if (m_pShutter == nullptr) return;

	int closeShutter = ((CButton*)GetDlgItem(IDC_CHECK_CLOSE_SHUTTER))->GetCheck();
	if (closeShutter) {
		piSetShutterState(PI_SHUTTER_CLOSED, m_pShutter);
	}
	else {
		piSetShutterState(PI_SHUTTER_OPEN, m_pShutter);
	}
}


void CRaywattLabDlg::OnNMCustomdrawSliderBrightness(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);
	updateBrightnessContrast();
	*pResult = 0;
}


void CRaywattLabDlg::OnNMCustomdrawSliderContrast(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);
	updateBrightnessContrast();
	*pResult = 0;
}


void CRaywattLabDlg::OnBnClickedButtonOpenRotaryJunction()
{
	bool dlgVisible = m_dlgRotaryJunction.IsWindowVisible();

	if (dlgVisible) {
		m_dlgRotaryJunction.ShowWindow(SW_HIDE);
	}
	else {
		m_dlgRotaryJunction.ShowWindow(SW_SHOW);
	}
	toggleButton(this, m_btnOpenRotaryJunction);
}


void CRaywattLabDlg::OnBnClickedButtonSaveCalibration()
{
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	if (m_pAcqDevice == nullptr || !m_pAcqDevice->IsInit() || !pLinearStage->IsOpen()) {
		AfxMessageBox(_T("[FAILED] Do initialize first"));
		return;
	}

	const int editTextIDs[] = { IDC_EDIT_CALIBRATION_FROM, IDC_EDIT_CALIBRATION_STEP, IDC_EDIT_CALIBRATION_COUNT };
	CString strBuffer = _T("");
	for (int i = 0; i < 3; i++) {
		GetDlgItem(editTextIDs[i])->GetWindowText(strBuffer);
		if (strBuffer.IsEmpty()) {
			AfxMessageBox(_T("[FAILED] Input calibration parameters"));
			return;
		}
	}

	GetDlgItem(IDC_BUTTON_LOAD_SELECTED_DATA)->EnableWindow(FALSE);
	for (int i = 0; i < 3; i++) {
		GetDlgItem(editTextIDs[i])->EnableWindow(FALSE);
	}
	GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(FALSE);

	m_strCalibrationPrefix = generateFileName(m_strPatientPath, _T(""));
	CUtility::StartThread(threadSaveCalibration, m_pThreadCalibration, this);
}

void CRaywattLabDlg::OnBnClickedButtonChangeCalibration()
{
	if (m_vCalibList.empty()) return;

	CString strCurFile = m_vCalibList.at(m_nCurCalibIndex);
	CCalibration* calibration = new CCalibration();
	calibration->Initialize(strCurFile.GetBuffer());
	m_pImagingRealtime->ChangeCalibration(calibration);
	m_pImagingSimulate->ChangeCalibration(calibration);
	m_strCurCalibration = strCurFile.GetBuffer();

	GetDlgItem(IDC_EDIT_CUR_CALIBRATION)->SetWindowText(strCurFile.Right(strCurFile.GetLength() - m_strCalibPath.GetLength() - 1));

	m_nCurCalibIndex++;
	if (m_nCurCalibIndex >= m_vCalibList.size()) {
		m_nCurCalibIndex = 0;
	}
}


void CRaywattLabDlg::OnBnClickedCheckBackgroundSubtract()
{
	int subtract = ((CButton*)GetDlgItem(IDC_CHECK_BACKGROUND_SUBTRACT))->GetCheck();

	m_pImagingRealtime->SetBackgroundSubtract(subtract);
	m_pImagingSimulate->SetBackgroundSubtract(subtract);
}


void CRaywattLabDlg::OnBnClickedCheckBackgroundImageSubtract()
{
	int subtract = ((CButton*)GetDlgItem(IDC_CHECK_BACKGROUND_FFT_SUBTRACT))->GetCheck();

	m_pImagingRealtime->SetBackgroundFFTSubtract(subtract);
	m_pImagingSimulate->SetBackgroundFFTSubtract(subtract);
}

void CRaywattLabDlg::OnBnClickedButtonOpenCalibFolder()
{
	BROWSEINFO BrInfo;
	TCHAR szBuffer[MAX_PATH];

	::ZeroMemory(&BrInfo, sizeof(BROWSEINFO));
	::ZeroMemory(szBuffer, MAX_PATH);

	BrInfo.hwndOwner = GetSafeHwnd();
	BrInfo.lpszTitle = _T("Select Calibration File Folder");
	BrInfo.ulFlags = BIF_NEWDIALOGSTYLE | BIF_EDITBOX | BIF_RETURNONLYFSDIRS;
	LPITEMIDLIST pItemIdList = ::SHBrowseForFolder(&BrInfo);
	::SHGetPathFromIDList(pItemIdList, szBuffer);

	m_strCalibPath.Format(_T("%s"), szBuffer);

	AfxGetApp()->WriteProfileString(_T("RECENT_SETTING"), _T("CALIB_PATH"), m_strCalibPath);
	GetDlgItem(IDC_EDIT_CALIB_PATH)->SetWindowText(m_strCalibPath);

	m_vCalibList.clear();
	m_nCurCalibIndex = 0;
	findFileByExtension(m_strCalibPath, _T("dat"), m_vCalibList);
}


void CRaywattLabDlg::OnBnClickedButtonMeasure()
{
	CLabImaging* pImaging = (m_btnLoadData.pushed) ? m_pImagingSimulate : m_pImagingRealtime;

	COCTMeasurement measurement;
	USHORT* pFFTData = pImaging->GetScopeFFTData();

	USHORT nPeakValue, nNoisePower;
	int nPeakIndex, nLineWidth;

	measurement.CalculateAxialResolution(pFFTData, nPeakValue, nPeakIndex, nLineWidth);
	measurement.CalculateNoisePower(pFFTData, nPeakIndex, nNoisePower);

	updateMeasurement(nPeakValue, nPeakIndex, nLineWidth, nNoisePower);
}


void CRaywattLabDlg::OnBnClickedCheckInitMotor()
{
	UpdateData(TRUE);
}


void CRaywattLabDlg::OnBnClickedCheckInitStage()
{
	UpdateData(TRUE);
}


void CRaywattLabDlg::OnBnClickedButtonPullback()
{
	GetDlgItem(IDC_BUTTON_PULLBACK)->EnableWindow(FALSE);
	CUtility::StartThread(threadPullback, m_pThreadPullback, this);
}
