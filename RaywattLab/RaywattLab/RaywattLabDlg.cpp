
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
#include "RJController.h"
#include "LaserController.h"
#include "LookUpTable.h"
#include "Utility.h"
#include "plog/Initializers/RollingFileInitializer.h"

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

	m_pRJController = nullptr;
	m_pLaserModule = nullptr;

	m_pThreadCalibration = nullptr;
	m_pFrameBuffer = nullptr;

	m_strCalibPath = _T("");
	m_nCurCalibIndex = 0;
	m_pThreadPullback = nullptr;
	m_strCurCalibration = _T(".\\CALIBRATION.dat");

	m_bInitialized = false;
	m_bStartAcquisition = false;
}

void CRaywattLabDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_LIST_PATIENT_DATA, m_listPatientData);
	DDX_Control(pDX, IDC_PICT_OCT_IMAGE, m_pictOCTImage);
	DDX_Radio(pDX, IDC_RADIO_IMAGE_CIRCLE, m_radioImageShape);
	DDX_Radio(pDX, IDC_RADIO_COLOR_BLACK, m_radioImageColor);
	DDX_Check(pDX, IDC_CHECK_HOT_COLOR, m_chkImageHotColor);
	DDX_Check(pDX, IDC_CHECK_SHOW_GUIDE, m_chkShowGuide);
	DDX_Control(pDX, IDC_SLIDER_BRIGHTNESS, m_sliderBrightness);
	DDX_Control(pDX, IDC_SLIDER_CONTRAST, m_sliderContrast);
	DDX_Control(pDX, IDC_SLIDER_LOWLEVEL, m_sliderLowLevel);
	DDX_Control(pDX, IDC_SLIDER_HIGHLEVEL, m_sliderHighLevel);
	DDX_Control(pDX, IDC_SLIDER_FRAME, m_sliderFrame);
}

// private methods

void CRaywattLabDlg::setLogger(TCHAR* logRootPath) {

	time_t timer = time(nullptr);
	tm t;
	errno_t err = localtime_s(&t, &timer);

	char rootPath[MAX_PATH];
	WideCharToMultiByte(CP_ACP, 0, logRootPath, MAX_PATH, rootPath, MAX_PATH, nullptr, nullptr);

	char logFile[_MAX_PATH];
	sprintf(logFile, "%s\\lab_%d-%02d-%02d.log", rootPath, (t.tm_year + 1900), (t.tm_mon + 1), t.tm_mday);
	printf("plog::init - %s\n", logFile);

#ifdef DEBUG
	plog::init(plog::debug, logFile);
#else
	plog::init(plog::info, logFile);
#endif
}
int CRaywattLabDlg::initializeDevices() {
	CConfiguration& config = CConfiguration::GetInstance();

	if (m_chkInitStage || m_chkInitMotor) {
		if (m_pRJController->Connect(config.bldcMotor.port) == false) {
			return E_FAIL;
		}
		m_pRJController->Set(eStepMotorIndex::Both, config.stepMotor.pullbackSpeed);
		m_pRJController->SwitchOn();
	}

	if (m_pAcqDevice == nullptr) {
		m_pAcqDevice = new CATSDevice(config.acquisition);
		m_pAcqDevice->SetImaging(m_pImagingRealtime);
		m_pAcqDevice->SetWriter(m_pDataWriter);
	}

	if (m_pAcqDevice->InitDevice() != NOERROR) {
		m_pRJController->Disconnect();
		m_pLaserModule->Disconnect();
		m_pAcqDevice->CleanUp();
		return E_FAIL;
	}

	return NOERROR;
}
int CRaywattLabDlg::finalizeDevices() {
	if (m_pAcqDevice != nullptr)
	{
		m_pAcqDevice->CleanUp();
	}
	m_pRJController->Disconnect();
	m_pLaserModule->Disconnect();

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
	strQuery.Format(_T("%s\\*.*"), m_strPatientPath.GetBuffer(), m_strPatientPath.GetBuffer());

	CFileFind fileFind;
	BOOL find = fileFind.FindFile(strQuery);
	while (find) {
		find = fileFind.FindNextFile();
		if (fileFind.IsDots() || fileFind.IsDirectory()) continue;
		
		CString strFileName = fileFind.GetFileName();

		CString strExt = strFileName.Right(strFileName.GetLength() - strFileName.ReverseFind('.') - 1);
		if (strExt.Compare(_T("bin")) == 0 || strExt.Compare(_T("oct")) == 0) {
			m_listPatientData.AddString(strFileName);
		}
	}

	m_strPatientName = m_strPatientPath.Right(m_strPatientPath.GetLength() - m_strPatientPath.ReverseFind('\\') - 1);
	GetDlgItem(IDC_EDIT_CURRENT_PATIENT)->SetWindowText(m_strPatientName);
}

void CRaywattLabDlg::initOCTViewLayout() {
	const int crossSectionSize = 620;
	RECT rect;
	GetDlgItem(IDC_PICT_OCT_IMAGE)->GetWindowRect(&rect);
	rect.right = rect.left + crossSectionSize;
	rect.bottom = rect.top + crossSectionSize;
	GetDlgItem(IDC_PICT_OCT_IMAGE)->MoveWindow(&rect);
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

	x = (m_largeMonitorMode) ? x + width + nPaddingLeft : x;
	m_scopeView.MoveWindow(x, y, width, heightScope);
	y = y + heightScope + nPaddingBetweenScopeViews;
	m_scopeViewFFT.MoveWindow(x, y, width, heightScope);

	m_scopeView.ShowWindow((m_largeMonitorMode) ? SW_SHOW : SW_HIDE);
	m_scopeViewFFT.ShowWindow((m_largeMonitorMode) ? SW_SHOW : SW_HIDE);
}
void CRaywattLabDlg::updateBrightnessContrast(CLabImaging* pImaging) {
	const double rangeB[] = { 0.0f, 100.0f };
	const double rangeC[] = { 0.5f, 3.0f };
	const double rangeSliderB[] = { m_sliderBrightness.GetRangeMin(), m_sliderBrightness.GetRangeMax() };
	const double rangeSliderC[] = { m_sliderContrast.GetRangeMin(), m_sliderContrast.GetRangeMax() };
	const int posB = m_sliderBrightness.GetPos();
	const int posC = m_sliderContrast.GetPos();

	double brightness = ((double)posB / rangeSliderB[1]) * (rangeB[1] - rangeB[0]) + rangeB[0];
	double contrast = ((double)posC / rangeSliderC[1]) * (rangeC[1] - rangeC[0]) + rangeC[0];

	pImaging->SetBrightnessContrast(brightness, contrast);

	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("BRIGHTNESS"), posB);
	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("CONTRAST"), posC);

	CString strBuffer = _T("");
	strBuffer.Format(_T("%d"), posB);
	GetDlgItem(IDC_EDIT_BRIGHTNESS)->SetWindowText(strBuffer);
	strBuffer.Format(_T("%d"), posC);
	GetDlgItem(IDC_EDIT_CONTRAST)->SetWindowText(strBuffer);
}
void CRaywattLabDlg::updateLevel(CLabImaging* pImaging) {
	const int low = m_sliderLowLevel.GetPos();
	const int high = m_sliderHighLevel.GetPos();

	pImaging->SetLevel(low, high);

	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("LOWLEVEL"), low);
	AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("HIGHLEVEL"), high);

	CString strBuffer = _T("");
	strBuffer.Format(_T("%d"), low);
	GetDlgItem(IDC_EDIT_LOWLEVEL)->SetWindowText(strBuffer);
	strBuffer.Format(_T("%d"), high);
	GetDlgItem(IDC_EDIT_HIGHLEVEL)->SetWindowText(strBuffer);
}
CString CRaywattLabDlg::generateFileName(CString strPath, CString strExtension, CString strPrefix) {
	CTime currentTime = CTime::GetCurrentTime();
	CString strFilePath = _T("");

	if (!strPrefix.IsEmpty()) {
		//strFilePath.Format(_T("%s\\%s_%s%s"), strPath, strPrefix, currentTime.Format("%m%d_%H%M%S"), strExtension);
		strFilePath.Format(_T("%s\\%s%s"), strPath, strPrefix, strExtension);
	}
	else {
		strFilePath.Format(_T("%s\\%s%s"), strPath, currentTime.Format("%m%d_%H%M%S"), strExtension);
	}
	return strFilePath;
}
CString CRaywattLabDlg::getLoadedFilePath() {
	CString strDataPath = _T("");
	CString strDataFile = _T("");
	int nSelected = m_listPatientData.GetCurSel();
	m_listPatientData.GetText(nSelected, strDataFile);
	strDataPath.Format(_T("%s/%s"), m_strPatientPath, strDataFile);

	return strDataPath;
}
CString CRaywattLabDlg::splitFileName(CString strFilePath) {
	return strFilePath.Right(strFilePath.GetLength() - strFilePath.ReverseFind('\\') - 1);
}
CLabImaging* CRaywattLabDlg::createImaging(IImaging::Setting imaging) {
	CLabImaging* pImaging = new CLabImaging(imaging, this);

	CCalibration* calibration = new CCalibration(imaging.nAScan, imaging.nFFTLength);
	calibration->Initialize(m_strCurCalibration);
	USHORT* background = readBackground(BACKGROUND_FILEPATH, imaging);

	pImaging->Initialize(calibration, background);
	pImaging->SetColor(m_chkImageHotColor);

	int subtract = ((CButton*)GetDlgItem(IDC_CHECK_BACKGROUND_SUBTRACT))->GetCheck();
	pImaging->SetBackgroundSubtract(subtract);
	
	updateBrightnessContrast(pImaging);
	updateLevel(pImaging);
	
	return pImaging;
}
USHORT* CRaywattLabDlg::readBackground(const char* strBackgroundFile, IImaging::Setting setting) {
	if (strBackgroundFile == nullptr) return nullptr;

	FILE* fp = fopen(strBackgroundFile, "rb");
	if (fp == nullptr) return nullptr;

	USHORT* pBackground = new USHORT[setting.nBufferSize];
	fread(pBackground, sizeof(USHORT), setting.nBufferSize, fp);

	fclose(fp);

	return pBackground;
}
IImaging::Setting CRaywattLabDlg::initReader(tstring strFilePath, CDataReader* pReader)
{
	CConfiguration& config = CConfiguration::GetInstance();
	IImaging::Setting setting = config.imaging;

	if (pReader != nullptr) {
		OCTHeader header = pReader->ReadHeader(strFilePath);
		if (header.type != OCTHeader::Type::Unknown)
		{
			setting.Set(header.width, header.height);
		}
		pReader->Initialize(strFilePath, setting.nBufferSize);
	}

	return setting;
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
void CRaywattLabDlg::drawGuideLine(cv::Mat image) {
	CConfiguration& config = CConfiguration::GetInstance();

	const cv::Scalar lineColor = cv::Scalar(0xff, 0xff, 0xff);
	const int lineThickness = 1;
	const int centerX = image.cols / 2;
	const int centerY = image.rows / 2;
	const int markerSize = 10;
	const double umPerPixel = config.measurement.fAxialResolutionScale * 2;	// fft signal scale -> circle image scale

	// horizontal line
	cv::line(image, cv::Point(0, centerY), cv::Point(image.cols - 1, centerY), lineColor, lineThickness);

	// distance marking
	int markerFrom = centerY - markerSize / 2;
	int markerTo = centerY + markerSize / 2;
	cv::line(image, cv::Point(centerX, markerFrom), cv::Point(centerX, markerTo), lineColor, lineThickness);
	for (int dist = 1; dist <= 9; dist += 2) {	// 1, 3, 5, 7, 9mm
		int actualDist = dist * 1000 / umPerPixel;
		cv::line(image,
			cv::Point(centerX - actualDist / 2, markerFrom),
			cv::Point(centerX - actualDist / 2, markerTo), lineColor, lineThickness * 2);
		cv::line(image,
			cv::Point(centerX + actualDist / 2, markerFrom),
			cv::Point(centerX + actualDist / 2, markerTo), lineColor, lineThickness * 2);
	}
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
#if 0
	CThread *pThread = pDlg->m_pThreadCalibration;
	CZaberController* pLinearStage = pDlg->m_pRotaryJunction;

	long long from, step, count = 0;

	CString strValue = _T("");
	pDlg->GetDlgItem(IDC_EDIT_CALIBRATION_FROM)->GetWindowText(strValue);
	from = _ttoi64(strValue);
	pDlg->GetDlgItem(IDC_EDIT_CALIBRATION_STEP)->GetWindowText(strValue);
	step = _ttoi64(strValue);
	pDlg->GetDlgItem(IDC_EDIT_CALIBRATION_COUNT)->GetWindowText(strValue);
	count = _ttoi64(strValue);

	pLinearStage->MoveMicrometer(from);
	while (pDlg->m_pThreadCalibration->isRun && !pLinearStage->IsMoving()) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	for (int frame = 0; frame < count && pDlg->m_pThreadCalibration->isRun; frame++) {
		long long position = from + (frame * step);
		pLinearStage->MoveMicrometer(position);
		while (pDlg->m_pThreadCalibration->isRun && !pLinearStage->IsMoving()) {
			Sleep(DELAY_FOR_STOP_THREAD);
		}

		pDlg->PostMessage(WM_SAVE_CALIBRATION_FRAME, frame);
		CUtility::SuspendThread(pDlg->m_pThreadCalibration);
	}

#endif
	pDlg->PostMessage(WM_SAVE_CALIBRATION_DONE);
	while (pDlg->m_pThreadCalibration->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}
	return NOERROR;
}
UINT CRaywattLabDlg::threadPullback(LPVOID param) {
	CRaywattLabDlg* pDlg = (CRaywattLabDlg*)param;
	CConfiguration& config = CConfiguration::GetInstance();
#if 0
	CZaberController* pPullbackStage = pDlg->m_pRotaryJunction;

	if (pPullbackStage->IsOpen()) {
		pPullbackStage->SetSpeed(config.stepMotor.pullbackSpeed);

		// Start Recording OCT
		pDlg->m_pDataWriter->StartRecording();

		// Pullback Linear Stage
		pPullbackStage->MoveRelative(config.stepMotor.pullbackDistance * -1);
		while (pDlg->m_pThreadPullback->isRun) {
			if (pPullbackStage->IsMoving()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}

		// Stop Recording OCT
		pDlg->m_pDataWriter->StopRecording();

		CString strPrefix = _T("");
		strPrefix.Format(_T("%dalines_%drpm_%dmm_%dmms"), config.imaging.nBScan, config.bldcMotor.velocityPullback, config.stepMotor.pullbackDistance, config.stepMotor.pullbackSpeed);
		CString strFileName = pDlg->generateFileName(pDlg->m_strPatientPath, _T(".bin"), strPrefix);

		CDataWriter* pDataManager = pDlg->m_pDataWriter;
		const int nNumOfSamples = pDataManager->GetNumOfSamples();

		pDataManager->StartSave(strFileName.GetBuffer());
		for (int nFrame = 0; nFrame < nNumOfSamples && pDlg->m_pThreadPullback->isRun; nFrame++) {
			pDataManager->WriteFrame(nFrame);
		}
		pDataManager->StopSave();
	}
#endif
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
	ON_MESSAGE(WM_PROCESS_CROSSSECTION, &CRaywattLabDlg::OnMsgProcessOCTDone)
	ON_MESSAGE(WM_SAVE_CALIBRATION_FRAME, &CRaywattLabDlg::OnMsgSaveCalibrationFrame)
	ON_MESSAGE(WM_SAVE_CALIBRATION_DONE, &CRaywattLabDlg::OnMsgSaveCalibrationDone)
	ON_MESSAGE(WM_PULLBACK_DONE, &CRaywattLabDlg::OnMsgPullbackDone)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_DATA_FOLDER, &CRaywattLabDlg::OnBnClickedButtonOpenDataFolder)
	ON_BN_CLICKED(IDC_BUTTON_LOAD_SELECTED_DATA, &CRaywattLabDlg::OnBnClickedButtonLoadSelectedData)
	ON_BN_CLICKED(IDC_BUTTON_PLAY_LOADED_DATA, &CRaywattLabDlg::OnBnClickedButtonPlayLoadedData)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_VIDEO, &CRaywattLabDlg::OnBnClickedButtonSaveVideo)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_TIF, &CRaywattLabDlg::OnBnClickedButtonSaveTif)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_PNG, &CRaywattLabDlg::OnBnClickedButtonSavePng)
	ON_BN_CLICKED(IDC_RADIO_IMAGE_CIRCLE, &CRaywattLabDlg::OnBnClickedRadioImageCircle)
	ON_BN_CLICKED(IDC_RADIO_IMAGE_RECTANGLE, &CRaywattLabDlg::OnBnClickedRadioImageRectangle)
	ON_BN_CLICKED(IDC_RADIO_COLOR_BLACK, &CRaywattLabDlg::OnBnClickedRadioColorBlack)
	ON_BN_CLICKED(IDC_RADIO_COLOR_WHITE, &CRaywattLabDlg::OnBnClickedRadioColorWhite)
	ON_BN_CLICKED(IDC_CHECK_HOT_COLOR, &CRaywattLabDlg::OnBnClickedCheckHotColor)
	ON_BN_CLICKED(IDC_CHECK_SHOW_GUIDE, &CRaywattLabDlg::OnBnClickedCheckShowGuide)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_BRIGHTNESS, &CRaywattLabDlg::OnNMCustomdrawSliderBrightness)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_CONTRAST, &CRaywattLabDlg::OnNMCustomdrawSliderContrast)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_LOWLEVEL, &CRaywattLabDlg::OnNMCustomdrawSliderLowlevel)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_HIGHLEVEL, &CRaywattLabDlg::OnNMCustomdrawSliderHighlevel)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_DATA, &CRaywattLabDlg::OnBnClickedButtonSaveData)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_ROTARY_JUNCTION, &CRaywattLabDlg::OnBnClickedButtonOpenRotaryJunction)
	ON_BN_CLICKED(IDC_BUTTON_ADMIN_INITIALIZE, &CRaywattLabDlg::OnBnClickedButtonAdminInitialize)
	ON_BN_CLICKED(IDC_BUTTON_SAVE_CALIBRATION, &CRaywattLabDlg::OnBnClickedButtonSaveCalibration)
	ON_BN_CLICKED(IDC_BUTTON_NEXT_CALIB, &CRaywattLabDlg::OnBnClickedButtonChangeCalibration)
	ON_BN_CLICKED(IDC_CHECK_BACKGROUND_SUBTRACT, &CRaywattLabDlg::OnBnClickedCheckBackgroundSubtract)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_CALIB_FOLDER, &CRaywattLabDlg::OnBnClickedButtonOpenCalibFolder)
	ON_BN_CLICKED(IDC_BUTTON_MEASURE, &CRaywattLabDlg::OnBnClickedButtonMeasure)
	ON_BN_CLICKED(IDC_CHECK_INIT_MOTOR, &CRaywattLabDlg::OnBnClickedCheckInitMotor)
	ON_BN_CLICKED(IDC_CHECK_INIT_STAGE, &CRaywattLabDlg::OnBnClickedCheckInitStage)
	ON_BN_CLICKED(IDC_BUTTON_PULLBACK, &CRaywattLabDlg::OnBnClickedButtonPullback)
	ON_BN_CLICKED(IDC_BUTTON_RESTART_ACQUISITION, &CRaywattLabDlg::OnBnClickedButtonRestartAcquisition)
	ON_BN_CLICKED(IDC_BUTTON_START_ACQUISITION, &CRaywattLabDlg::OnBnClickedButtonStartAcquisition)
	ON_BN_CLICKED(IDC_BUTTON_SHOW_SCOPE, &CRaywattLabDlg::OnBnClickedButtonShowScope)
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_SLIDER_FRAME, &CRaywattLabDlg::OnNMCustomdrawSliderFrame)
END_MESSAGE_MAP()

LRESULT CRaywattLabDlg::OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam) {
	CLabImaging* pImaging = (m_isRealtime) ? m_pImagingRealtime : m_pImagingSimulate;
	cv::Mat image = (m_radioImageShape == 0) ? pImaging->GetCircleImage().clone() : pImaging->GetRectangleImage().clone();
	Ipp16u* scopeData = pImaging->GetScopeData();
	Ipp16u* scopeFFTData = pImaging->GetScopeFFTData();
	CString strFrameRate = _T("");
	
	if (m_isRealtime && m_pAcqDevice != nullptr) {
		strFrameRate.Format(_T("%.2lf"), m_pAcqDevice->GetFPS());
	}
	else if (!m_isRealtime && m_btnPlayData.pushed) {
		int nFrameInfo = lParam;	// 0 if real time frame
		int nCurFrame = (nFrameInfo >> 16) & 0xFFFF;
		int nTotalFrame = (nFrameInfo & 0xFFFF);

		CString strFrameNum = _T("");
		strFrameNum.Format(_T("%04d / %04d"), nCurFrame + 1, nTotalFrame);
		GetDlgItem(IDC_STATIC_FRAME_NUM)->SetWindowText(strFrameNum);

		m_sliderFrame.SetPos(nCurFrame);
	}

	CConfiguration& config = CConfiguration::GetInstance();
	const int nScopeLength = config.imaging.nAScan;
	const int nOutputLength = config.imaging.nOutputLength;

	if (m_chkShowGuide && m_radioImageShape == 0) {
		drawGuideLine(image);
	}

	drawToPictureBox(m_pictOCTImage, image.cols, image.rows, (char*)image.data);
	GetDlgItem(IDC_EDIT_FRAME_RATE)->SetWindowText(strFrameRate);

	m_scopeView.SetChannelBuffer(0, scopeData, nScopeLength);
	//m_scopeView.SetChannelBuffer(1, scopeData + nScopeLength, nScopeLength);
	m_scopeViewFFT.SetChannelBuffer(0, scopeFFTData, nOutputLength);
	//m_scopeViewFFT.SetChannelBuffer(1, scopeFFTData + nOutputLength, nOutputLength);

	if (m_pDataWriter->IsRecording()) {
		Ipp16u* pFFTBuffer = new Ipp16u[nOutputLength];
		memcpy(pFFTBuffer, scopeFFTData, sizeof(Ipp16u) * nOutputLength);
		m_vFFTData.push_back(pFFTBuffer);
	}

	return NOERROR;
}

LRESULT CRaywattLabDlg::OnMsgSaveCalibrationFrame(WPARAM wParam, LPARAM lParam) {
	int frame = (int)wParam;

	// To-Do : how to save frame? call DataWriter::Push directly?
	CConfiguration& config = CConfiguration::GetInstance();
	int nFrameSize = config.acquisition.nAScan * config.acquisition.nBScan * sizeof(unsigned short);

	memcpy(m_pFrameBuffer, m_pImagingRealtime->GetFringesBuffer(), nFrameSize);

	CString strFilePath = _T("");
	strFilePath.Format(_T("%s_%d.bin"), m_strCalibrationPrefix, frame);
	HANDLE hFile = CreateFile(
		strFilePath, GENERIC_WRITE,
		FILE_SHARE_READ, nullptr, CREATE_ALWAYS,
		FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
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

	//AllocConsole();
	//freopen("CONOUT$", "w", stdout);

	// TODO: Add extra initialization here
	CConfiguration& config = CConfiguration::GetInstance();
	if (!config.IsInit()) {
		config.Initialize(_T(".\\raywattLab.ini"));
	}

	setLogger(_T(".\\"));

	CLookUpTable& lut = CLookUpTable::GetInstance();
	int result = lut.Load("LUT_abbott.csv");

	m_largeMonitorMode = false;
	m_showScope = false;

	RECT rect;
	GetDesktopWindow()->GetWindowRect(&rect);
	printf("%d %d %d %d\n", rect.left, rect.top, rect.right, rect.bottom);

	if ((rect.right - rect.left) > 1280) m_largeMonitorMode = true;

	::GetWindowRect(this->m_hWnd, &rect);
	if (m_largeMonitorMode) {
		GetDlgItem(IDC_BUTTON_SHOW_SCOPE)->EnableWindow(FALSE);
		::MoveWindow(this->m_hWnd, rect.left, rect.top, 1550, 1024, TRUE);
	}
	else {
		GetDlgItem(IDC_BUTTON_SHOW_SCOPE)->EnableWindow(TRUE);
		::MoveWindow(this->m_hWnd, rect.left, rect.top, 1024, 1024, TRUE);
	}

	initToggleButton(m_btnLoadData, IDC_BUTTON_LOAD_SELECTED_DATA, _T("Load"), _T("Unload"));
	initToggleButton(m_btnPlayData, IDC_BUTTON_PLAY_LOADED_DATA, _T("Play"), _T("Pause"));
	initToggleButton(m_btnSaveData, IDC_BUTTON_SAVE_DATA, _T("Save Data"), _T("Done"));
	initToggleButton(m_btnOpenRotaryJunction, IDC_BUTTON_OPEN_ROTARY_JUNCTION, _T("Setting"), _T("Close"));

	m_strPatientPath = AfxGetApp()->GetProfileString(_T("RECENT_SETTING"), _T("PATIENT_PATH"), _T(""));
	updatePatientDataList();

	initOCTViewLayout();

	int nScopeLength = config.imaging.nAScan;
	m_scopeView.Create(this, 0);
	m_scopeView.AddChannel(_T("Data 1"), nScopeLength);
	m_scopeView.AddChannel(_T("Data 2"), nScopeLength);

	int nOutputLength = config.imaging.nOutputLength;
	m_scopeViewFFT.Create(this, 0);
	m_scopeViewFFT.AddChannel(_T("FFT Data 1"), nOutputLength);
	m_scopeViewFFT.AddChannel(_T("FFT Data 2"), nOutputLength);

	initScopeViewLayout();

	m_radioImageShape = 0;
	m_radioImageColor = 0;
	m_chkImageHotColor = TRUE;
	m_chkShowGuide = FALSE;
	m_chkInitMotor = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("INIT_MOTOR"), FALSE);
	m_chkInitStage = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("INIT_STAGE"), FALSE);
	
	UpdateData(FALSE);

	int brightness = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("BRIGHTNESS"), 0);
	int contrast = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("CONTRAST"), 0);

	m_sliderBrightness.SetRange(0, 100);
	m_sliderBrightness.SetPos(brightness);

	m_sliderContrast.SetRange(0, 100);
	m_sliderContrast.SetPos(contrast);

	int lowLevel = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("LOWLEVEL"), 50);
	int highLevel = AfxGetApp()->GetProfileInt(_T("RECENT_SETTING"), _T("HIGHLEVEL"), 51);

	m_sliderLowLevel.SetRange(50, 299);
	m_sliderLowLevel.SetPos(lowLevel);

	m_sliderHighLevel.SetRange(51, 300);
	m_sliderHighLevel.SetPos(highLevel);

	int goodClockStart = 0;
	int goodClockEnd = config.imaging.nAScan;

	m_pImagingRealtime = createImaging(config.imaging);
	m_pImagingRealtime->SetGoodClockRange(goodClockStart, goodClockEnd);
	m_pImagingRealtime->Start();

	m_pImagingSimulate = createImaging(config.imaging);
	m_pImagingSimulate->SetGoodClockRange(goodClockStart, goodClockEnd);
	m_pImagingSimulate->Start();

	int nBufferSize = config.acquisition.nAScan * config.acquisition.nBScan;

	m_pDataWriter = new CDataWriter();
	m_pDataWriter->Initialize(nBufferSize * sizeof(unsigned short));

	m_pDataReader = new CDataReader();

	m_pRJController = new CRJController();
	m_pLaserModule = new CLaserModule();

	m_pFrameBuffer = new char[nBufferSize * sizeof(unsigned short)];

	updateBrightnessContrast(m_pImagingRealtime);
	updateBrightnessContrast(m_pImagingSimulate);
	updateLevel(m_pImagingRealtime);
	updateLevel(m_pImagingSimulate);

	m_dlgRotaryJunction.Create(IDD_ROTARY_JUNCTION_DIALOG);

	GetDlgItem(IDC_EDIT_PREFIX)->SetWindowText(_T("1"));

	m_strCalibPath = AfxGetApp()->GetProfileString(_T("RECENT_SETTING"), _T("CALIB_PATH"), _T(""));
	GetDlgItem(IDC_EDIT_CALIB_PATH)->SetWindowText(m_strCalibPath);

	wchar_t strBuffer[MAX_PATH];
	wsprintf(strBuffer, L"%d", config.acquisition.nBScan);
	GetDlgItem(IDC_EDIT_BSCAN)->SetWindowText(strBuffer);

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

	m_pRJController->StopMotor();
	m_pRJController->SwitchOff();
	m_pRJController->Disconnect();

	m_pRJController->Disconnect();
	delete m_pRJController;

	m_pLaserModule->Disconnect();
	delete m_pLaserModule;
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
	if (m_bInitialized)
	{
		finalizeDevices();
		GetDlgItem(IDC_BUTTON_ADMIN_INITIALIZE)->SetWindowText(_T("Initialize"));
		GetDlgItem(IDC_BUTTON_START_ACQUISITION)->EnableWindow(FALSE);
	}
	else {
		int nNumDevices = CLaserController::GetInstance()->GetNumDevices();
		if (nNumDevices <= 0)
		{
			AfxMessageBox(_T("[FAILED] Cannot find Laser. Please restart computer"));
			return;
		}

		CLaserController::GetInstance()->LaserOnOff(true);
		int result = initializeDevices();
		CLaserController::GetInstance()->LaserOnOff(false);

		AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("INIT_MOTOR"), m_chkInitMotor);
		AfxGetApp()->WriteProfileInt(_T("RECENT_SETTING"), _T("INIT_STAGE"), m_chkInitStage);

		if (result == NOERROR) {
			GetDlgItem(IDC_BUTTON_ADMIN_INITIALIZE)->SetWindowText(_T("Finalize"));
			GetDlgItem(IDC_BUTTON_START_ACQUISITION)->EnableWindow(TRUE);
			m_bInitialized = true;
		}
		else {
			GetDlgItem(IDC_BUTTON_START_ACQUISITION)->EnableWindow(FALSE);
			AfxMessageBox(_T("[FAILED] Please check the device connection"));
		}
	}
}


void CRaywattLabDlg::OnBnClickedButtonStartAcquisition()
{
	if (m_bStartAcquisition)
	{
		m_pAcqDevice->StopAcquisition();
		CLaserController::GetInstance()->LaserOnOff(false);

		GetDlgItem(IDC_BUTTON_START_ACQUISITION)->SetWindowText(_T("Start Acq."));
		GetDlgItem(IDC_BUTTON_ADMIN_INITIALIZE)->EnableWindow(TRUE);
		GetDlgItem(IDC_BUTTON_SAVE_DATA)->EnableWindow(FALSE);
		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(FALSE);
		GetDlgItem(IDC_BUTTON_RESTART_ACQUISITION)->EnableWindow(FALSE);
	}
	else {
		CLaserController::GetInstance()->LaserOnOff(true);
		m_pAcqDevice->StartAcquisition();

		GetDlgItem(IDC_BUTTON_START_ACQUISITION)->SetWindowText(_T("Stop Acq."));
		GetDlgItem(IDC_BUTTON_ADMIN_INITIALIZE)->EnableWindow(FALSE);
		GetDlgItem(IDC_BUTTON_SAVE_DATA)->EnableWindow(TRUE);
		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(TRUE);
		GetDlgItem(IDC_BUTTON_RESTART_ACQUISITION)->EnableWindow(TRUE);
	}
	m_bStartAcquisition = !m_bStartAcquisition;
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

	if (dataLoaded) {
		result = m_pSimDevice->StopAcquisition();
		m_isRealtime = true;

		GetDlgItem(IDC_STATIC_FRAME_NUM)->SetWindowText(_T("0000 / 0000"));
		m_sliderFrame.SetPos(0);

		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(TRUE);
	}
	else {
		m_isRealtime = false;

		CString strFilePath = _T("");
		CString strFileName = _T("");
		int nSelected = m_listPatientData.GetCurSel();
		m_listPatientData.GetText(nSelected, strFileName);
		strFilePath.Format(_T("%s/%s"), m_strPatientPath, strFileName);

		IImaging::Setting setting = initReader(strFilePath.GetBuffer(), m_pDataReader);
		if (m_pImagingSimulate != nullptr) {
			m_pImagingSimulate->Stop();
			delete m_pImagingSimulate;
		}
		m_pImagingSimulate = createImaging(setting);
		m_pImagingSimulate->Start();

		if (m_pSimDevice == nullptr) {
			m_pSimDevice = new CSimulateDevice(m_pDataReader);
		}
		m_pSimDevice->SetImaging(m_pImagingSimulate);

		result = m_pSimDevice->InitDevice();
		((CSimulateDevice*)m_pSimDevice)->SetPause(!dataPlayed);
		result = m_pSimDevice->StartAcquisition();

		m_sliderFrame.SetRange(0, m_pDataReader->GetNumOfSamples() - 1);

		CString strFrameNum = _T("");
		strFrameNum.Format(_T("0001 / %04d"), m_pDataReader->GetNumOfSamples());
		GetDlgItem(IDC_STATIC_FRAME_NUM)->SetWindowText(strFrameNum);

		GetDlgItem(IDC_BUTTON_SAVE_CALIBRATION)->EnableWindow(FALSE);
	}

	if (result == NOERROR) {
		toggleButton(this, m_btnLoadData);
		dataLoaded = m_btnLoadData.pushed;

		GetDlgItem(IDC_BUTTON_PLAY_LOADED_DATA)->EnableWindow(dataLoaded);

		if (dataPlayed) {
			toggleButton(this, m_btnPlayData);
		}

		GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->EnableWindow(dataLoaded);
		GetDlgItem(IDC_BUTTON_SAVE_TIF)->EnableWindow(dataLoaded);
		GetDlgItem(IDC_BUTTON_SAVE_PNG)->EnableWindow(dataLoaded);
		GetDlgItem(IDC_BUTTON_RESTART_ACQUISITION)->EnableWindow(dataLoaded || m_bStartAcquisition);
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
	}
}


void CRaywattLabDlg::OnBnClickedButtonSaveData()
{
	int result = NOERROR;
	bool dataSaving = m_btnSaveData.pushed;
	bool dataPlayed = m_btnPlayData.pushed;

	CConfiguration& config = CConfiguration::GetInstance();
	const int nOutputLength = config.imaging.nOutputLength;

	if (dataSaving) {
		CString strPrefix = _T("");
		GetDlgItem(IDC_EDIT_PREFIX)->GetWindowText(strPrefix);
		CString strFileName = generateFileName(m_strPatientPath, _T(".bin"), strPrefix);
		m_pDataWriter->StopRecording();
		GetDlgItem(IDC_BUTTON_SAVE_DATA)->SetWindowText(_T("Saving"));

		m_pRJController->StopMotor();

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
			measurement.CalculateAxialResolution(pFFTData, config.imaging.nOutputLength, config.measurement, nPeakValue,  nPeakIndex, nLineWidth);
			if (nPeakValue > nMaxPeak) {
				nMaxPeak = nPeakValue;
				nMaxIndex = nPeakIndex;
				nMaxWidth = nLineWidth;
				measurement.CalculateNoisePower(pFFTData, config.imaging.nOutputLength, config.measurement, nPeakIndex, nNoisePower);
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
		IImaging::Setting setting = m_pImagingRealtime->GetSetting();
		int nBufferSize = setting.nAScan * setting.nBScan;
		m_pDataWriter->Initialize(nBufferSize * sizeof(unsigned short));
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
	CConfiguration& config = CConfiguration::GetInstance();

	GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->SetWindowText(_T("Saving"));

	CString strDataPath = getLoadedFilePath();
	CString strAviPath = strDataPath;
	strAviPath.Replace(_T(".bin"), _T(".avi"));

	CLabImaging* pImaging = createImaging(config.imaging);

	CDataReader* pReader = new CDataReader();
	initReader(strDataPath.GetBuffer(), pReader);

	CVideoWriter videoWriter;
	bool isCircle = (m_radioImageShape == 0);
	int width = (isCircle) ? config.imaging.nCircleSize : config.imaging.nBScan;
	int height = (isCircle) ? config.imaging.nCircleSize : config.imaging.nOutputLength;
	videoWriter.StartRecording(strAviPath, width, height);
	for (int i = 0; i < pReader->GetNumOfSamples(); i++) {
		pImaging->Process(pReader->GetSample(pReader->GetNumOfSamples() - i - 1));
		pImaging->PostProcess(pImaging->GetProcessedImage());
		videoWriter.PushToBuffer(((isCircle) ? pImaging->GetCircleImage() : pImaging->GetRectangleImage()));
	}
	videoWriter.StopRecording();

	CString strMessage = _T("");
	CString strFileName = splitFileName(strAviPath);
	strMessage.Format(_T("%s saved."), strFileName);
	AfxMessageBox(strMessage);
	GetDlgItem(IDC_BUTTON_SAVE_VIDEO)->SetWindowText(_T("AVI"));

	delete pReader;
	delete pImaging;
}


void CRaywattLabDlg::OnBnClickedButtonSaveTif()
{
	if (!m_btnLoadData.pushed || m_btnPlayData.pushed) return;

	GetDlgItem(IDC_BUTTON_SAVE_TIF)->SetWindowText(_T("Saving"));

	CConfiguration& config = CConfiguration::GetInstance();

	CString strDataPath = getLoadedFilePath();
	CString strTifPath = strDataPath;
	strTifPath.Replace(_T(".bin"), _T(".tif"));

	CLabImaging* pImaging = createImaging(config.imaging);

	CDataReader* pReader = new CDataReader();
	initReader(strDataPath.GetBuffer(), pReader);

	CTIFFWriter tiffWriter(strTifPath);
	bool isCircle = (m_radioImageShape == 0);
	for (int i = 0; i < pReader->GetNumOfSamples(); i++) {
		pImaging->Process(pReader->GetSample(i));
		pImaging->PostProcess(pImaging->GetProcessedImage());

		tiffWriter.SaveFrame(((isCircle) ? pImaging->GetCircleImage() : pImaging->GetRectangleImage()));
	}

	CString strMessage = _T("");
	CString strFileName = splitFileName(strTifPath);
	strMessage.Format(_T("%s saved."), strFileName);
	AfxMessageBox(strMessage);
	GetDlgItem(IDC_BUTTON_SAVE_TIF)->SetWindowText(_T("TIF"));

	delete pReader;
	delete pImaging;
}


void CRaywattLabDlg::OnBnClickedButtonSavePng()
{
	if (!m_btnLoadData.pushed || m_btnPlayData.pushed) return;

	GetDlgItem(IDC_BUTTON_SAVE_PNG)->SetWindowText(_T("Saving"));

	CConfiguration& config = CConfiguration::GetInstance();

	CString strDataPath = getLoadedFilePath();

	CString strPngDirectoryW = strDataPath.Left(strDataPath.GetLength() - 4);
	_tmkdir(strPngDirectoryW.GetBuffer());

	CLabImaging* pImaging = createImaging(config.imaging);

	CDataReader* pReader = new CDataReader();
	initReader(strDataPath.GetBuffer(), pReader);

	bool isCircle = (m_radioImageShape == 0);
	for (int i = 0; i < pReader->GetNumOfSamples(); i++) {
		pImaging->Process(pReader->GetSample(i));
		pImaging->PostProcess(pImaging->GetProcessedImage());

		CStringA strPngDirectory(strPngDirectoryW);
		char strPngName[MAX_PATH];
		sprintf(strPngName, "%s\\%03d.png", strPngDirectory.GetBuffer(), i);
		cv::imwrite(strPngName, ((isCircle) ? pImaging->GetCircleImage() : pImaging->GetRectangleImage()));
	}

	CString strMessage = _T("");
	strMessage.Format(_T("%d frames saved."), pReader->GetNumOfSamples());
	AfxMessageBox(strMessage);
	GetDlgItem(IDC_BUTTON_SAVE_PNG)->SetWindowText(_T("PNG"));

	delete pReader;
	delete pImaging;
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

void CRaywattLabDlg::OnBnClickedCheckShowGuide()
{
	UpdateData(TRUE);
	if (m_pImagingRealtime != nullptr) m_pImagingRealtime->ShowCalibGuide(m_chkShowGuide);
	if (m_pImagingSimulate != nullptr) m_pImagingSimulate->ShowCalibGuide(m_chkShowGuide);
}



void CRaywattLabDlg::OnNMCustomdrawSliderBrightness(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);
	updateBrightnessContrast(m_pImagingRealtime);
	updateBrightnessContrast(m_pImagingSimulate);
	*pResult = 0;
}


void CRaywattLabDlg::OnNMCustomdrawSliderContrast(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);
	updateBrightnessContrast(m_pImagingRealtime);
	updateBrightnessContrast(m_pImagingSimulate);
	*pResult = 0;
}

void CRaywattLabDlg::OnNMCustomdrawSliderLowlevel(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);
	updateLevel(m_pImagingRealtime);
	updateLevel(m_pImagingSimulate);
	*pResult = 0;
}


void CRaywattLabDlg::OnNMCustomdrawSliderHighlevel(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);
	updateLevel(m_pImagingRealtime);
	updateLevel(m_pImagingSimulate);
	*pResult = 0;
}


void CRaywattLabDlg::OnNMCustomdrawSliderFrame(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	UpdateData(TRUE);

	if (m_pSimDevice != nullptr && !m_btnPlayData.pushed) {
		((CSimulateDevice*)m_pSimDevice)->SetFrame(m_sliderFrame.GetPos());

		CString strFrameNum = _T("");
		strFrameNum.Format(_T("%04d / %04d"), m_sliderFrame.GetPos() + 1, m_pDataReader->GetNumOfSamples());
		GetDlgItem(IDC_STATIC_FRAME_NUM)->SetWindowText(strFrameNum);
	}

	*pResult = 0;
}



void CRaywattLabDlg::OnBnClickedButtonOpenRotaryJunction()
{
	bool dlgVisible = m_dlgRotaryJunction.IsWindowVisible();

	if (dlgVisible) {
		//m_dlgRotaryJunction.SetStepMotor(m_pRotaryJunction, m_pLaserModule);
		m_dlgRotaryJunction.ShowWindow(SW_HIDE);
	}
	else {
		m_dlgRotaryJunction.ShowWindow(SW_SHOW);
	}
	toggleButton(this, m_btnOpenRotaryJunction);
}


void CRaywattLabDlg::OnBnClickedButtonSaveCalibration()
{
	if (m_pAcqDevice == nullptr || !m_pAcqDevice->IsInit() || !m_pRJController->IsConnected()) {
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

	m_strCalibrationPrefix = _T("");
	m_strCalibrationPrefix.Format(_T("%s\\pos"), m_strPatientPath);
	CUtility::StartThread(threadSaveCalibration, m_pThreadCalibration, this);
}

void CRaywattLabDlg::OnBnClickedButtonChangeCalibration()
{
	if (m_vCalibList.empty()) return;

	CConfiguration& config = CConfiguration::GetInstance();

	CString strCurFile = m_vCalibList.at(m_nCurCalibIndex);
	CCalibration* calibration = new CCalibration(config.imaging.nAScan, config.imaging.nFFTLength);
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
	CConfiguration& config = CConfiguration::GetInstance();

	COCTMeasurement measurement;
	USHORT* pFFTData = pImaging->GetScopeFFTData();

	USHORT nPeakValue, nNoisePower;
	int nPeakIndex, nLineWidth;

	measurement.CalculateAxialResolution(pFFTData, config.imaging.nOutputLength, config.measurement, nPeakValue, nPeakIndex, nLineWidth);
	measurement.CalculateNoisePower(pFFTData, config.imaging.nOutputLength, config.measurement, nPeakIndex, nNoisePower);

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

void CRaywattLabDlg::OnBnClickedButtonRestartAcquisition()
{
	CString strBuffer = _T("");
	GetDlgItem(IDC_EDIT_BSCAN)->GetWindowText(strBuffer);
	int nBScan = _ttoi64(strBuffer);

	if (nBScan <= 0) return;

	if (m_btnLoadData.pushed)
	{
		CConfiguration& config = CConfiguration::GetInstance();
		config.imaging.Set(config.imaging.nAScan, nBScan);
		
		int result = m_pSimDevice->StopAcquisition();
		m_pImagingSimulate->Stop();
		delete m_pImagingSimulate;

		m_pImagingSimulate = createImaging(config.imaging);
		m_pImagingSimulate->Start();

		if (m_btnPlayData.pushed) {
			toggleButton(this, m_btnPlayData);
		}

		CString strFilePath = _T("");
		CString strFileName = _T("");
		int nSelected = m_listPatientData.GetCurSel();
		m_listPatientData.GetText(nSelected, strFileName);
		strFilePath.Format(_T("%s/%s"), m_strPatientPath, strFileName);

		initReader(strFilePath.GetBuffer(), m_pDataReader);
		m_pSimDevice->SetImaging(m_pImagingSimulate);

		result = m_pSimDevice->InitDevice();
		((CSimulateDevice*)m_pSimDevice)->SetPause(true);
		result = m_pSimDevice->StartAcquisition();

		return;
	}

	if (m_bStartAcquisition) {
		IImaging::Setting imaging = m_pImagingRealtime->GetSetting();
		imaging.Set(imaging.nAScan, nBScan);

		// stop acquisition & imaging
		m_pAcqDevice->StopAcquisition();
		m_pImagingRealtime->Stop();
		delete m_pImagingRealtime;

		// re-allocate imaging
		m_pImagingRealtime = createImaging(imaging);
		m_pImagingRealtime->Start();

		CATSDevice::Setting acquire = ((CATSDevice*)m_pAcqDevice)->GetSetting();
		acquire.nAScan = imaging.nAScan;
		acquire.nBScan = imaging.nBScan;
		((CATSDevice*)m_pAcqDevice)->SetSetting(acquire);
		m_pAcqDevice->SetImaging(m_pImagingRealtime);
		m_pAcqDevice->StartAcquisition();

		CConfiguration& config = CConfiguration::GetInstance();
		config.imaging = imaging;
		config.acquisition = acquire;
	}
}

void CRaywattLabDlg::OnBnClickedButtonShowScope()
{
	m_showScope = !m_showScope;

	m_pictOCTImage.ShowWindow((m_showScope ? SW_HIDE : SW_SHOW));
	m_scopeView.ShowWindow((m_showScope ? SW_SHOW : SW_HIDE));
	m_scopeViewFFT.ShowWindow((m_showScope ? SW_SHOW : SW_HIDE));

	GetDlgItem(IDC_BUTTON_SHOW_SCOPE)->SetWindowText((m_showScope ? L"Show Image" : L"Show Scope"));
}
