#include "Config.h"
#include "OCTSystem.h"
#include "Utility.h"
#include "Configuration.h"
#include "OCTImaging.h"
#include "LabImaging.h"
#include "DataWriter.h"
#include "CutViewManager.h"
#include "VolumeGenerator.h"
#include "ATSDevice.h"
#include "SimulateDevice.h"
#include "LaserController.h"
#include "MotorController.h"
#include "ZaberController.h"
#include "ArduinoController.h"
#include "RayLearning.h"
#include "ImagingSession.h"

/*
* COCTSystem
*/
COCTSystem::COCTSystem() {
	m_callback = nullptr;
	m_cbCrossSection = nullptr;
	m_cbLongitude = nullptr;
	m_cbObjectDetection = nullptr;

	m_pThreadService = nullptr;
	m_pThreadSaveRaw = nullptr;
	m_pThreadGenerateVolume = nullptr;
	m_pThreadRotaryJunction = nullptr;

	m_pImagingRealtime = nullptr;
	m_pImagingPullback = nullptr;
	m_pImagingLiveView = nullptr;

	m_pDataWriter = nullptr;

	m_pVolume = nullptr;

	m_pAcqDevice = nullptr;	

	m_curSession = SESSION_UNKNOWN;
	for (int i = 0; i < MAX_SESSION_NUM; i++) {
		m_reviewSession[i] = nullptr;
	}
	m_openedSession = nullptr;

	for (int i = 0; i < STEP_MOTOR_NUM; i++) {
		m_pStepMotor[i] = new CArduinoController();
	}

	m_prevState = RayScannerState::Initial;
	m_curState = RayScannerState::Initial;
	m_cathState = CatheterState::Unloaded;

	//Property
	m_fBrightness = 0.0f;
	m_fContrast = 0.5f;
	m_fDegree = 90;
}

/*
* ~COCTSystem
*/
COCTSystem::~COCTSystem() {
	Stop();
}

void COCTSystem::SetLogger(TCHAR* logRootPath) {

	time_t timer = time(nullptr);
	tm t;
	errno_t err = localtime_s(&t, &timer);

	char rootPath[MAX_PATH];
	WideCharToMultiByte(CP_ACP, 0, logRootPath, MAX_PATH, rootPath, MAX_PATH, nullptr, nullptr);

	char logFile[_MAX_PATH];
	sprintf(logFile, "%s\\core_%d-%02d-%02d.log", rootPath, (t.tm_year + 1900), (t.tm_mon + 1), t.tm_mday);
	printf("plog::init - %s\n", logFile);

#ifdef DEBUG
	plog::init(plog::debug, logFile);
#else
	plog::init(plog::info, logFile);
#endif
}

/*
* Start
*/
RayError COCTSystem::Start() {
	if (m_pThreadService != nullptr) return RayError::SystemRunning;

	CConfiguration& config = CConfiguration::GetInstance();
	if (!config.IsInit()) {
		config.Initialize(_T(".\\raycore.ini"));
	}

	m_fBrightness = config.imaging.brightness;
	m_fContrast = config.imaging.contrast;

	SetLogger(config.logRootPath);

	CUtility::StartThread(threadService, m_pThreadService, this);

	IImaging::Setting settingPullback = config.imaging;
	settingPullback.Set(settingPullback.nAScan, config.imaging.nBScan); //  config.acquisition.nLaserSpeed / (config.bldcMotor.velocityPullback / 60));
	m_pImagingPullback = CImagingSession::CreateColorImaging(this, settingPullback, nullptr, ImagingType::Default);
	m_pImagingPullback->SetSession(SESSION_REALTIME);
	m_pImagingPullback->Start();

	IImaging::Setting settingLiveView = config.imaging;
	settingLiveView.Set(settingLiveView.nAScan, 4000); // config.imaging.nBScan); // ceil((double)config.acquisition.nLaserSpeed / ((double)config.bldcMotor.velocityLiveView / 60.f)));
	m_pImagingLiveView = CImagingSession::CreateColorImaging(this, settingLiveView, nullptr, ImagingType::Default);
	m_pImagingLiveView->SetSession(SESSION_REALTIME);
	m_pImagingLiveView->Start();

	m_pAcqDevice = new CATSDevice(config.acquisition);

	m_pVolume = new CVolumeGenerator();
	m_pVolume->Initialize(config.imaging.nCircleSize, config.imaging.nCircleSize, config.volume.size, config.volume.size);

	CLaserController* pLaser = CLaserController::GetInstance();
	pLaser->LaserOnOff(true);

	return RayError::OK;
}

/*
* Stop
*/
RayError COCTSystem::Stop() {
	CUtility::StopThread(m_pThreadService);
	CUtility::StopThread(m_pThreadSaveRaw);
	CUtility::StopThread(m_pThreadGenerateVolume);
	CUtility::StopThread(m_pThreadRotaryJunction);

	closeAllSessions();
	if (m_openedSession != nullptr) {
		delete m_openedSession;
		m_openedSession = nullptr;
	}

	if (m_pAcqDevice != nullptr) {
		m_pAcqDevice->StopAcquisition();
		delete m_pAcqDevice;
		m_pAcqDevice = nullptr;
	}

	m_pImagingRealtime = nullptr;
	if (m_pImagingPullback != nullptr) {
		m_pImagingPullback->Stop();
		delete m_pImagingPullback;
		m_pImagingPullback = nullptr;
	}
	if (m_pImagingLiveView != nullptr) {
		m_pImagingLiveView->Stop();
		delete m_pImagingLiveView;
		m_pImagingLiveView = nullptr;
	}
	if (m_pDataWriter != nullptr) {
		delete m_pDataWriter;
		m_pDataWriter = nullptr;
	}
	if (m_pVolume != nullptr) {
		delete m_pVolume;
		m_pVolume = nullptr;
	}

	CMotorController* pMotor = CMotorController::GetInstance();
	pMotor->StopMotor();
	pMotor->SwitchOff();
	pMotor->Disconnect();

	for (int i = 0; i < STEP_MOTOR_NUM; i++) {
		m_pStepMotor[i]->Close();
		delete m_pStepMotor[i];
		m_pStepMotor[i] = nullptr;
	}

	return RayError::OK;
}

/*
* RegisterCallback
*/
RayError COCTSystem::RegisterCallback(FunctionPtr cb) {
	m_callback = cb;

	return RayError::OK;
}

/*
* UnregisterCallback
*/
RayError COCTSystem::UnregisterCallback() {
	m_callback = nullptr;

	return RayError::OK;
}

/*
* ConnectDevices
*/
RayError COCTSystem::ConnectDevices() {
	int result = NOERROR;

	if (m_curState == RayScannerState::Initial) {
		result |= connectAcqDevice();
		result |= connectRotaryJunction();

		if (result == NOERROR) {
			postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Default);
			return RayError::OK;
		}
		else {
			return RayError::DeviceNotConnected;
		}
	}

	return RayError::WrongState;
}

/*
* DisconnectDevices
*/
RayError COCTSystem::DisconnectDevices() {
	int result = NOERROR;

	// To-Do: stop all threads
	disconnectAcqDevice();
	disconnectRotaryJunction();

	postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Initial);

	return RayError::OK;
}


/*
* AutoCalibration
*/
RayError COCTSystem::AutoCalibration() {
	if (m_curState == RayScannerState::Default || m_curState == RayScannerState::Review) {
		//To-Do: check Catheter

		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		CUtility::StartThread(threadAutoCalibration, m_pThreadRotaryJunction, this);

		return RayError::OK;
	}

	return RayError::WrongState;
}

/*
* ManualCalibration
*/
RayError COCTSystem::ManualCalibration(bool forward) {
	if (m_curState == RayScannerState::Default) {
		if (m_pStepMotor[STEP_MOTOR_DELAYLINE]->IsOpen() == false) return RayError::DeviceNotConnected;

		m_pStepMotor[STEP_MOTOR_DELAYLINE]->MoveRelative((forward ? DELAYLINE_FORWARD_POSITION : DELAYLINE_BACKWARD_POSITION));

		return RayError::OK;
	}
}

/*
* ShowCalibrationGuide
*/
RayError COCTSystem::ShowCalibrationGuide(bool show) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	m_pImagingPullback->ShowCalibGuide(show);
	m_pImagingLiveView->ShowCalibGuide(show);

	return RayError::OK;
}

/*
* ReadyPullback
*/
RayError COCTSystem::ReadyPullback()
{
	if (m_curState == RayScannerState::Default) {
		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		CLaserController* pLaser = CLaserController::GetInstance();
		CMotorController* pMotorCtrl = CMotorController::GetInstance();
		CConfiguration& config = CConfiguration::GetInstance();

		restartAcqDevice(m_pImagingPullback);

		//pLaser->LaserOnOff(true);
		pMotorCtrl->PerformRun(config.bldcMotor.velocityPullback);

		return RayError::OK;
	}
	return RayError::WrongState;
}
/*
* PullbackScan
*/
RayError COCTSystem::PullbackScan(char *strFilePath) {
	if (m_curState == RayScannerState::Default) {
		m_strFilePath = CUtility::StringToWstring(strFilePath);

		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Scanning);

		return RayError::OK;
		
	}

	return RayError::WrongState;
}

/*
* LoadCatheter
*/
RayError COCTSystem::LoadCatheter() {
	if (m_curState == RayScannerState::Default || m_curState == RayScannerState::Review) {
		//To-Do: check catheter

		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		CUtility::StartThread(threadLoadCatheter, m_pThreadRotaryJunction, this);

		return RayError::OK;
	}

	return RayError::WrongState;
}

/*
* UnloadCatheter
*/
RayError COCTSystem::UnloadCatheter() {
	if (m_curState == RayScannerState::Default || m_curState == RayScannerState::Review) {

		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		CUtility::StartThread(threadUnloadCatheter, m_pThreadRotaryJunction, this);

		return RayError::OK;
	}

	return RayError::WrongState;
}

/*
* StartReview
* return N (>0) when current state & argument is right.
* return Error Code (<0) when something is wrong.
*/
int COCTSystem::StartReview(char* strFilePath) {
	if (m_curState == RayScannerState::Initial || m_curState == RayScannerState::Default) {
		CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_REVIEW, strFilePath);
		if (pSession == nullptr) {
			PLOGE.printf("InvalidArgument : %s", strFilePath);
			return (int)RayError::InvalidArgument;
		}

		postPriorMessage(WM_START_REVIEW_SESSION, SESSION_REVIEW, (LPARAM)pSession);
		postPriorMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);
	
		return pSession->GetDataManager()->GetNumOfSamples();
	}
	PLOGE.printf("WrongState : %d", m_curState);

	return (int)RayError::WrongState;
}

/*
* StartCompare
*/
RayError COCTSystem::StartCompare(char* strFilePath) {
	if (m_curState != RayScannerState::Review) return RayError::WrongState;

	CImagingSession* pSession = CImagingSession::CreateSession(this, SESSION_COMPARE, strFilePath);
	if (pSession == nullptr) {
		return RayError::InvalidArgument;
	}

	if (m_reviewSession[SESSION_COMPARE] != nullptr) {
		m_reviewSession[SESSION_COMPARE]->Stop();
	}

	postPriorMessage(WM_START_REVIEW_SESSION, SESSION_COMPARE, (LPARAM)pSession);

	return RayError::OK;
}

/*
* EndReview
*/
RayError COCTSystem::EndReview()
{
	if (m_curState == RayScannerState::Review) {
		postPriorMessage(WM_IGNORE_MESSAGES);
		stopAllSessions();
		postMessage(WM_STOP_IGNORE_MESSAGES);

		switch (m_prevState) {
		case RayScannerState::Initial:
		case RayScannerState::Default:
			postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)m_prevState);
			break;
		case RayScannerState::Scanning:
			postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Default);
			break;
		default:
			break;
		}

		return RayError::OK;
	}

	return RayError::WrongState;
}

/*
* StartLiveView
*/
RayError COCTSystem::StartLiveView()
{
	if (m_curState == RayScannerState::Default) {
		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		CLaserController* pLaser = CLaserController::GetInstance();
		CMotorController* pMotorCtrl = CMotorController::GetInstance();
		CConfiguration& config = CConfiguration::GetInstance();

		restartAcqDevice(m_pImagingLiveView);

		//pLaser->LaserOnOff(true);
		pMotorCtrl->PerformRun(config.bldcMotor.velocityLiveView);

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* StopLiveView
*/
RayError COCTSystem::StopLiveView()
{
	if (m_curState == RayScannerState::Default) {
		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		CLaserController* pLaser = CLaserController::GetInstance();
		CMotorController* pMotorCtrl = CMotorController::GetInstance();

		//pLaser->LaserOnOff(false);
		pMotorCtrl->StopMotor();

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* SetSession
*/
RayError COCTSystem::SetSession(int session) 
{
	if (session <= SessionType::SESSION_UNKNOWN || session >= SessionType::MAX_SESSION_NUM) return RayError::WrongSession;
	if (m_reviewSession[session] == nullptr)
	{
		PLOGI.printf("Session #%d is null", session);
		return RayError::WrongSession;
	}

	m_curSession = (SessionType) session;

	return RayError::OK;
}

/*
* RegisterImageCallback
*/
RayError COCTSystem::RegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude) {
	m_cbCrossSection = cbCrossSection;
	m_cbLongitude = cbLongitude;

	return RayError::OK;
}

/*
* UnregisterImageCallback
*/
RayError COCTSystem::UnregisterImageCallback() {
	m_cbCrossSection = nullptr;
	m_cbLongitude = nullptr;

	return RayError::OK;
}

/*
* RegisterDetectionCallback
*/
RayError COCTSystem::RegisterDetectionCallback(FunctionObjPtr cbObjectDetection) {
	m_cbObjectDetection = cbObjectDetection;

	return RayError::OK;
}

/*
* UnregisterDetectionCallback
*/
RayError COCTSystem::UnregisterDetectionCallback() {
	m_cbObjectDetection = nullptr;

	return RayError::OK;
}

/*
* GetVolumeData
*/
void* COCTSystem::GetVolumeData() {
	if (m_pVolume == nullptr) return nullptr;

	return m_pVolume->GetVolumeData();
}

/*
* StartLumenDetection
*/
RayError COCTSystem::StartLumenDetection() {
	if (m_curState == RayScannerState::Review)
	{
		if (m_reviewSession[SESSION_REVIEW] == nullptr) return RayError::WrongSession;
		m_reviewSession[SESSION_REVIEW]->StartObjectDetection();
		PLOGI.printf("Start object detection manually.");
	}

	return RayError::WrongState;
}

/*
* OpenImage
*/
RayError COCTSystem::OpenImage(char* strFilePath) {
	CloseImage();

	CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_UNKNOWN, strFilePath);
	if (pSession == nullptr) {
		return RayError::InvalidArgument;
	}

	m_openedSession = pSession;
	m_openedSession->InitCutView(cv::Scalar(0x00, 0x00, 0x00));

	return RayError::OK;
}

/*
* CloseImage
*/
RayError COCTSystem::CloseImage() {
	if (m_openedSession != nullptr) {
		delete m_openedSession;
		m_openedSession = nullptr;
	}

	return RayError::OK;
}

/*
* GetImageData
*/
void* COCTSystem::GetImageData(int nFrame) {
	if (m_openedSession != nullptr) return m_openedSession->GetImageData(nFrame);
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) {
			PLOGI.printf("Session #%d is not started.", m_curSession);
			return nullptr;
		}
		if (m_reviewSession[m_curSession]->IsProcessed(nFrame)) {
			cv::Mat image = m_reviewSession[m_curSession]->PostProcess(nFrame);
			return image.data;
		}
	}

	return nullptr;
}

/*
* GetLongitudeData
*/
void* COCTSystem::GetLongitudeData(double fDegree) {
	if (m_openedSession == nullptr) return nullptr;

	CCutViewManager* pCutView = m_openedSession->GetCutView();
	if (pCutView == nullptr) return nullptr;

	CConfiguration& config = CConfiguration::GetInstance();

	m_openedSession->AddFramesIntoCutView();
	pCutView->GenerateCutView(fDegree);
	cv::Mat imgLongitude = pCutView->DrawLongitudeImage(pCutView->GetNumOfSamples(), config.imaging.brightness, config.imaging.contrast);

	return imgLongitude.data;
}

/*
* GetLumenContour
*/
void* COCTSystem::GetLumenContour(int nFrame) {
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return nullptr;

	return m_reviewSession[SESSION_REVIEW]->GetLumenContour(nFrame);
}

/*
* GetNumOfLumenContourPoints
*/
int COCTSystem::GetNumOfLumenContourPoints(int nFrame) {
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return 0;

	return m_reviewSession[SESSION_REVIEW]->GetNumOfLumenContourPoints(nFrame);
}


/*
* GetBrightness
*/
double COCTSystem::GetBrightness() {
	const double rangeB[] = { 0.0f, 100.0f };

	double percentage = (m_fBrightness - rangeB[0]) / (rangeB[1] - rangeB[0]) * 100.f;
	return percentage;
}

/*
* SetBrightness
*/
RayError COCTSystem::SetBrightness(double value) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	const double rangeB[] = { 0.0f, 100.0f };

	value = (value < 0) ? 0 : (value > 100) ? 100 : value;
	double brightness = (value / 100.f) * (rangeB[1] - rangeB[0]) + rangeB[0];

	m_fBrightness = brightness;
	setBrightnessContrastAllSessions();
	
	return RayError::OK;
}

/*
* GetContrast
*/
double COCTSystem::GetContrast() {
	const double rangeC[] = { 0.5f, 3.0f };

	double percentage = (m_fContrast - rangeC[0]) / (rangeC[1] - rangeC[0]) * 100.f;
	return percentage;
}

/*
* SetContrast
*/
RayError COCTSystem::SetContrast(double value) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	const double rangeC[] = { 0.5f, 3.0f };

	value = (value < 0) ? 0 : (value > 100) ? 100 : value;
	double contrast = (value / 100.f) * (rangeC[1] - rangeC[0]) + rangeC[0];

	m_fContrast = contrast;
	setBrightnessContrastAllSessions();

	return RayError::OK;
}

/*
* GetDegree
*/
double COCTSystem::GetDegree() {
	return m_fDegree;
}

/*
* SetDegree
*/
RayError COCTSystem::SetDegree(double value) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	m_fDegree = value;

	if (m_curSession != SESSION_UNKNOWN && m_reviewSession[m_curSession] != nullptr) {
		CCutViewManager *pCutView = m_reviewSession[m_curSession]->GetCutView();
		if (pCutView != nullptr) {
			int nFrames = pCutView->GetNumOfGeneratedSamples();
			if (nFrames > 0) {
				int nCurFrame = nFrames - 1;
				this->postMessage(WM_PROCESS_CUTVIEW, m_curSession, nCurFrame);
			}
		}
	}

	return RayError::OK;
}

/*
* GetLongitudeBackgroundColor
*/
UINT COCTSystem::GetLongitudeBackgroundColor() {
	UINT nValue = 0x00;

	UINT b = m_backgroundColor[0];
	UINT g = m_backgroundColor[1];
	UINT r = m_backgroundColor[2];

	nValue = (b & 0xff);
	nValue |= ((g & 0xff) << 8);
	nValue |= ((r & 0xff) << 16);

	return nValue;
}

/*
* SetLongitudeBackgroundColor
*/
RayError COCTSystem::SetLongitudeBackgroundColor(UINT value) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	cv::Scalar color;
	color[0] = 0xff & value;
	color[1] = 0xff & (value >> 8);
	color[2] = 0xff & (value >> 16);

	m_backgroundColor = color;

	return RayError::OK;
}

/*
* GetVolumeDepth
*/
UINT COCTSystem::GetVolumeDepth() {
	if (m_pDataWriter == nullptr) return 0;

	return m_pDataWriter->GetNumOfSamples();
}

/*
* GetMotorOnOff
*/
bool COCTSystem::GetMotorOnOff()
{
	return CMotorController::GetInstance()->IsRun();
}

/*
* GetImageWidth
*/
UINT COCTSystem::GetImageWidth() 
{
	if (m_openedSession != nullptr) return m_openedSession->GetImageWidth();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetImageWidth();
	}
}

/*
* GetImageHeight
*/
UINT COCTSystem::GetImageHeight() 
{
	if (m_openedSession != nullptr) return m_openedSession->GetImageHeight();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetImageHeight();
	}
}

/*
* GetImageChannels
*/
UINT COCTSystem::GetImageChannels() 
{
	if (m_openedSession != nullptr) return m_openedSession->GetImageChannels();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetImageChannels();
	}
}

/*
* GetImageDepth
*/
UINT COCTSystem::GetImageDepth()
{
	if (m_openedSession != nullptr) return m_openedSession->GetImageDepth();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetImageDepth();
	}
}

/*
* GetLongitudeImageWidth
*/
UINT COCTSystem::GetLongitudeImageWidth()
{
	if (m_openedSession != nullptr) return m_openedSession->GetCutViewWidth();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetCutViewWidth();
	}
}

/*
* GetLongitudeImageHeight
*/
UINT COCTSystem::GetLongitudeImageHeight()
{
	if (m_openedSession != nullptr) return m_openedSession->GetCutViewHeight();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetCutViewHeight();
	}
}

/*
* GetLongitudeImageChannels
*/
UINT COCTSystem::GetLongitudeImageChannels()
{
	if (m_openedSession != nullptr) return m_openedSession->GetCutViewChannels();
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) return 0;
		return m_reviewSession[m_curSession]->GetCutViewChannels();
	}
}

/*
* threadService
*/
UINT COCTSystem::threadService(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CThread* pThread = pSystem->m_pThreadService;
	bool ignoreMsg = false;

	PLOGI.printf("Service Start");

	// Connect to COM Interface first time asynchronous
	CLaserController* pLaser = CLaserController::GetInstance();
	//pLaser->LaserOnOff(true);

	// Initialize (first prediction)
	cv::Mat imgSample = cv::imread(".\\oct_sample.png");
	CRayLearning& learning = CRayLearning::GetInstance();
	learning.Initialize(true);
	learning.FindLumen(imgSample);

	PLOGI.printf("sample lumen detection done.");
	printf("[threadService] start!\n");
	while (pThread->isRun) {
		std::tuple<int, WPARAM, LPARAM> popMsgThread = pSystem->popMessage();
		int popMsg = std::get<0>(popMsgThread);
		WPARAM wParam = std::get<1>(popMsgThread);
		LPARAM lParam = std::get<2>(popMsgThread);

		if (ignoreMsg)
		{
			if (popMsg == WM_STOP_IGNORE_MESSAGES) ignoreMsg = false;
			continue;
		}

		switch(popMsg) {
		case WM_UPDATE_SCANNER_STATE :
		{
			pSystem->OnMsgUpdateScannerState(wParam, lParam);
			break;
		}
		case WM_UPDATE_SAVE_RAW :
		{
			pSystem->OnMsgUpdateSaveRaw(wParam, lParam);
			break;
		}
		case WM_NOTIFY_PROCESS_DONE:
		{
			pSystem->OnMsgNotifyProcessDone(wParam, lParam);
			break;
		}
		case WM_NOTIFY_EVENT_OCCURED:
		{
			pSystem->OnMsgNotifyEventOccured(wParam, lParam);
			break;
		}
		case WM_NOTIFY_DEVICE_WORK_DONE:
		{
			pSystem->OnMsgDeviceWorkDone(wParam, lParam);
			break;
		}
		case WM_NOTIFY_ERROR_OCCURED:
		{
			pSystem->OnMsgNotifyErrorOccured(wParam, lParam);
			break;
		}
		case WM_PROCESS_CROSSSECTION:
		{
			pSystem->OnMsgProcessCrossSection(wParam, lParam);
			break;
		}
		case WM_PROCESS_CUTVIEW:
		{
			pSystem->OnMsgProcessCutView(wParam, lParam);
			break;
		}
		case WM_PROCESS_DETECTION:
		{
			pSystem->OnMsgProcessDetection(wParam, lParam);
			break;
		}
		case WM_UPDATE_CATHETER_STATE:
		{
			pSystem->OnMsgUpdateCatheterState(wParam, lParam);
			break;
		}
		case WM_START_REVIEW_SESSION:
		{
			pSystem->OnMsgStartReviewSession(wParam, lParam);
			break;
		}
		case WM_IGNORE_MESSAGES:
		{
			ignoreMsg = true;
			break;
		}
		default:
			break;
		}
	
		Sleep(5);
	}

	pLaser->LaserOnOff(false);

	return (UINT)RayError::OK;
}

/*
* threadSaveRaw
*/
UINT COCTSystem::threadSaveRaw(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	tstring strSaveFilePath = pSystem->m_strFilePath;
	CImagingSession* pSession = pSystem->m_reviewSession[SESSION_REALTIME];
	CDataWriter* pDataWriter = (CDataWriter*)pSession->GetDataManager();
	COCTImaging* pImaging = pSession->GetImaging();
	ImagingType type = pSession->GetImagingType();
	const int nNumOfSamples = pDataWriter->GetNumOfSamples();

	int nFrame = 0;
	pSystem->postMessage(WM_UPDATE_SAVE_RAW, 0, nNumOfSamples);

	IImaging::Setting settingPullback = pSystem->m_pImagingPullback->GetSetting();
	UCHAR extraData = (UCHAR)OCTHeader::ExtraData::Dispersion;
	if (type == ImagingType::LabImaging)
	{
		extraData |= (UCHAR)OCTHeader::ExtraData::Background;
	}

	pDataWriter->StartSave(strSaveFilePath);
	pDataWriter->WriteHeader(OCTHeader::Type::TimeSignal, OCTHeader::DataType::UShort, OCTHeader::Channels::Single, settingPullback.nAScan, settingPullback.nBScan, extraData);
	pDataWriter->WriteExtraData(pImaging->GetCalibrationData(), settingPullback.nAScan * 2 * sizeof(int));
	if (type == ImagingType::LabImaging)
	{
		USHORT* pBackgroundData = ((CLabImaging*)pImaging)->GetBackground();
		pDataWriter->WriteExtraData(pBackgroundData, settingPullback.nBufferSize * sizeof(USHORT));
	}

	for (nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadSaveRaw->isRun; nFrame++) {
		pDataWriter->WriteFrame(nFrame);

		pSystem->postMessage(WM_UPDATE_SAVE_RAW, nFrame + 1, nNumOfSamples);
	}
	pDataWriter->WriteEOF();
	pDataWriter->StopSave();

	pSystem->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::SaveRawData);
	printf("[threadSaveRaw] done.\n");

	while (pSystem->m_pThreadSaveRaw->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadGenerateVolume
*/
UINT COCTSystem::threadGenerateVolume(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CImagingSession* pSession = pSystem->m_reviewSession[SESSION_REVIEW];
	IDataManager* pDataManager = pSession->GetDataManager();
	ImagingType imagingType = pSession->GetImagingType();

	CVolumeGenerator* pVolume = pSystem->m_pVolume;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging
	COCTImaging* pImaging = CImagingSession::CreateColorImaging(nullptr, pSession->GetImaging()->GetSetting(), pDataManager, imagingType);

	for (int nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadGenerateVolume->isRun; nFrame++) {
		char* pBuffer = pDataManager->GetSample(nFrame);

		pVolume->AddRecord(pBuffer, pImaging, nFrame);
	}
	delete pImaging;

	pSystem->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::GenerateVolume);

	// wait for StopThread
	while (pSystem->m_pThreadGenerateVolume->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadAutoCalibration
*/
UINT COCTSystem::threadAutoCalibration(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;

	//To-Do: implement auto calibration
	Sleep(5000);

	pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Calibrated);
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::AutoCalibration);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadPullbackScan
*/
UINT COCTSystem::threadPullbackScan(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CStepMotorController* pPullbackMotor = pSystem->m_pStepMotor[STEP_MOTOR_PULLBACK];
	IImaging::Setting settingPullback = pSystem->m_pImagingPullback->GetSetting();

	PLOGI.printf("Pullback start.");
	CDataWriter* pDataWriter = new CDataWriter();
	pDataWriter->Initialize(settingPullback.nBufferSize * sizeof(USHORT));
	pDataWriter->AddExtraData(OCTHeader::ExtraData::Dispersion, pSystem->m_pImagingPullback->GetCalibrationData(), settingPullback.nAScan * 2 * sizeof(int));
	if (ImagingType::Default == ImagingType::LabImaging)
	{
		pDataWriter->AddExtraData(OCTHeader::ExtraData::Background, 
			((CLabImaging*)pSystem->m_pImagingPullback)->GetBackground(), settingPullback.nBufferSize * sizeof(USHORT));
	}

	pSystem->m_pAcqDevice->SetWriter(pDataWriter);
	pSystem->restartAcqDevice(pSystem->m_pImagingPullback);
	pPullbackMotor->SetSpeed(config.stepMotor.pullbackSpeed);

	// 1. Motor ON
	pMotor->PerformRun(config.bldcMotor.velocityPullback);

	// 2. Start Recording OCT
	pDataWriter->StartRecording();

	// 3. Pullback Linear Stage
	if (pPullbackMotor->IsOpen()) {
		int nPullbackPosition = config.stepMotor.pullbackStart + config.stepMotor.pullbackDistance;
		pPullbackMotor->MoveAbsolute(nPullbackPosition);
		while (pSystem->m_pThreadRotaryJunction->isRun) {
			if (pPullbackMotor->IsMoving()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}
	else {
		Sleep(3000);
	}

	// 4. Stop Recording OCT
	pDataWriter->StopRecording();
	pSystem->m_pAcqDevice->SetWriter(nullptr);

	// 5. Motor OFF
	Sleep(500);
	pMotor->StopMotor();

	PLOGI.printf("Pullback done.");
	CImagingSession* pSession = CImagingSession::CreateSession(pSystem, SESSION_REVIEW, settingPullback, pDataWriter);
	pSystem->postPriorMessage(WM_START_REVIEW_SESSION, SESSION_REVIEW, (LPARAM)pSession);
	pSystem->postPriorMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);
	pSystem->postPriorMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::Pullback);

	pSession->StartObjectDetection();

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadLoadCatheter
*/
UINT COCTSystem::threadLoadCatheter(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CStepMotorController* pPullbackMotor = pSystem->m_pStepMotor[STEP_MOTOR_PULLBACK];

	pSystem->postMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterLoading);

	// 1. Set Linear Stage Position
	if (pPullbackMotor->IsOpen()) {
		pPullbackMotor->SetSpeed(config.stepMotor.pullbackSpeed);
		pPullbackMotor->MoveAbsolute(config.stepMotor.pullbackStart);
		while (pSystem->m_pThreadRotaryJunction->isRun) {
			if (pPullbackMotor->IsMoving()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}

	// 2. Wait
	Sleep(config.catheter.rotationTime);

	// To-Do: Check Catheter Connection
	bool loaded = true;
	if (loaded) {
		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Loaded);
	}
	else {
		pMotor->StopMotor();
		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Unloaded);
	}
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::LoadCatheter);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadUnloadCatheter
*/
UINT COCTSystem::threadUnloadCatheter(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CStepMotorController* pPullbackMotor = pSystem->m_pStepMotor[STEP_MOTOR_PULLBACK];

	// 1. Motor ON
	int nVelocity = config.bldcMotor.velocityHoming;
	pMotor->PerformRun(nVelocity);

	// 2. Set Linear Stage Position to Zero
	if (pPullbackMotor->IsOpen()) {
		pPullbackMotor->SetSpeed(config.stepMotor.pullbackSpeed);
		pPullbackMotor->MoveAbsolute(config.stepMotor.pullbackStart);
		while (pSystem->m_pThreadRotaryJunction->isRun) {
			if (pPullbackMotor->IsMoving()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}

	// 3. Motor Off
	pMotor->StopMotor();

	pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Unloaded);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadValidateCatheter
*/
UINT COCTSystem::threadValidateCatheter(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CLaserController* pLaser = CLaserController::GetInstance();

	pSystem->restartAcqDevice(pSystem->m_pImagingLiveView);
	//pLaser->LaserOnOff(true);
	pMotor->PerformRun(config.bldcMotor.velocityLiveView);

	// To-Do: determine image verification
	bool verified = true;

	pMotor->StopMotor();
	//pLaser->LaserOnOff(false);

	if (verified) {
		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Enable);
	}
	else {
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::CatheterNotValid);
	}

	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::ValidateCatheter);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* createColorImaging
*/
bool COCTSystem::checkConnection() {
	bool result = true;
	CMotorController* pMotor = CMotorController::GetInstance();
	
	result &= m_pAcqDevice->IsInit();
	result &= pMotor->IsConnected();
	for (int i = 0; i < STEP_MOTOR_NUM; i++) {
		result &= m_pStepMotor[i]->IsOpen();
	}

	return result;
}

/*
* connectAcqDevice
*/
int COCTSystem::connectAcqDevice() {
	if (m_pAcqDevice->IsInit()) return NOERROR;

	return m_pAcqDevice->InitDevice();
}

/*
* disconnectAcqDevice
*/
int COCTSystem::disconnectAcqDevice() {
	stopAcqDevice();

	if (m_pAcqDevice->IsInit()) {
		m_pAcqDevice->CleanUp();
	}

	return NOERROR;
}

/*
* startAcqDevice
*/
int COCTSystem::startAcqDevice() {
	if (!m_pAcqDevice->IsInit()) {
		return E_FAIL;
	}

	int result = m_pAcqDevice->StartAcquisition();

	return result;
}

/*
* stopAcqDevice
*/
int COCTSystem::stopAcqDevice() {
	m_pAcqDevice->StopAcquisition();

	return NOERROR;
}

/*
* restartAcqDevice
*/
int COCTSystem::restartAcqDevice(COCTImaging* pImaging) {
	stopAcqDevice();

	m_pImagingRealtime = pImaging;

	IImaging::Setting imaging = pImaging->GetSetting();
	CATSDevice::Setting acquire = ((CATSDevice *)m_pAcqDevice)->GetSetting();
	acquire.nAScan = imaging.nAScan;
	acquire.nBScan = imaging.nBScan;
	((CATSDevice*)m_pAcqDevice)->SetSetting(acquire);
	m_pAcqDevice->SetImaging(pImaging);

	return startAcqDevice();
}

/*
* connectRotaryJunction
*/
int COCTSystem::connectRotaryJunction() {
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CStepMotorController* pPullbackMotor = m_pStepMotor[STEP_MOTOR_PULLBACK];
	CStepMotorController* pDelayLine = m_pStepMotor[STEP_MOTOR_DELAYLINE];

	bool result = true;

	if (!pPullbackMotor->IsOpen()) {
		pPullbackMotor->Open(config.stepMotor.pullback);
		pPullbackMotor->SetCurrent(config.stepMotor.pullbackStart);
	}

	if (!pDelayLine->IsOpen()) {
		pDelayLine->Open(config.stepMotor.delayline);
	}

	if (!pMotor->IsConnected()) {
		result &= pMotor->Connect();
		result &= pMotor->SwitchOff();
		result &= pMotor->SwitchOn();
	}

	return (result) ? NOERROR : E_FAIL;
}

/*
* disconnectRotaryJunction
*/
int COCTSystem::disconnectRotaryJunction() {
	CMotorController* pMotor = CMotorController::GetInstance();

	bool result = true;
	
	if (pMotor->IsConnected()) {
		result &= pMotor->SwitchOff();
	}

	for (int i = 0; i < STEP_MOTOR_NUM; i++)
	{
		if (m_pStepMotor[i]->IsOpen())
		{
			m_pStepMotor[i]->Close();
		}
	}

	return (result) ? NOERROR : E_FAIL;
}

/*
* OnMsgProcessCrossSection
*/
LRESULT COCTSystem::OnMsgProcessCrossSection(WPARAM wParam, LPARAM lParam) {
	cv::Mat image;
	int nSession = wParam;
	int nFrameInfo = lParam;	// 0 if real time frame
	bool isRealTime = (nFrameInfo == 0);

	if (m_curState == RayScannerState::Review) {
		if (isRealTime) return NOERROR;
		int nCurFrame = (nFrameInfo >> 16) & 0xFFFF;
		int nTotalFrame = (nFrameInfo & 0xFFFF);
		
		image = m_reviewSession[nSession]->PostProcess(nCurFrame);
	}
	else {
		if (isRealTime == false) return NOERROR;

		image = m_pImagingRealtime->GetCircleImage();
	}

	if (m_cbCrossSection != nullptr) m_cbCrossSection(nSession, image.data, image.cols, image.rows, image.channels(), nFrameInfo);

	return NOERROR;
}

/*
* OnMsgProcessCutView
*/
LRESULT COCTSystem::OnMsgProcessCutView(WPARAM wParam, LPARAM lParam) {
	int nSession = wParam;
	int nDrawSamples = lParam + 1;

	CConfiguration& config = CConfiguration::GetInstance();
	CCutViewManager* pCutView = m_reviewSession[nSession]->GetCutView();

	pCutView->GenerateCutView(m_fDegree);
	cv::Mat imgCutView = pCutView->DrawLongitudeImage(nDrawSamples, config.imaging.brightness, config.imaging.contrast);

	int nTotalFrame = pCutView->GetNumOfSamples();
	int nFrameInfo = (nDrawSamples << 16) | (nTotalFrame);

	if (m_cbLongitude != nullptr) m_cbLongitude(nSession, imgCutView.data, imgCutView.cols, imgCutView.rows, imgCutView.channels(), nFrameInfo);

	return NOERROR;
}
/*
* OnMsgProcessDetection
*/
LRESULT COCTSystem::OnMsgProcessDetection(WPARAM wParam, LPARAM lParam) {
	UINT nSession = wParam;
	UINT nFrame = lParam;

	if (m_cbObjectDetection != nullptr) m_cbObjectDetection(nFrame);

	return NOERROR;
}
void COCTSystem::stopAllSessions() {
	for (int i = 0; i < MAX_SESSION_NUM; i++) {
		if (m_reviewSession[i] != nullptr) {
			m_reviewSession[i]->Stop();
		}
	}
}
void COCTSystem::closeAllSessions() {
	for (int i = 0; i < MAX_SESSION_NUM; i++) {
		if (m_reviewSession[i] != nullptr) {
			delete m_reviewSession[i];
			m_reviewSession[i] = nullptr;
		}
	}
	m_curSession = SESSION_UNKNOWN;
}
void COCTSystem::setBrightnessContrastAllSessions() {
	CConfiguration& config = CConfiguration::GetInstance();
	config.imaging.brightness = m_fBrightness;
	config.imaging.contrast = m_fContrast;

	m_pImagingPullback->SetBrightnessContrast(m_fBrightness, m_fContrast);
	m_pImagingLiveView->SetBrightnessContrast(m_fBrightness, m_fContrast);

	for (int session = 0; session < SessionType::MAX_SESSION_NUM; session++)
	{
		if (m_reviewSession[session] != nullptr)
		{
			if (m_reviewSession[session]->GetImaging() != nullptr)
			{
				m_reviewSession[session]->GetImaging()->SetBrightnessContrast(m_fBrightness, m_fContrast);
			}
		}
	}

	if (m_openedSession != nullptr && m_openedSession->GetImaging() != nullptr)
	{
		m_openedSession->GetImaging()->SetBrightnessContrast(m_fBrightness, m_fContrast);
	}
}
/*
* OnMsgUpdateScannerState
*/
LRESULT COCTSystem::OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam) {
	m_prevState = m_curState;
	m_curState = (RayScannerState)wParam;

	if(m_callback != nullptr) m_callback((int)RayCallbackRequest::State, (int)m_curState);

	PLOGI.printf("%d > %d", m_prevState, m_curState);
	switch (m_curState) {
	case RayScannerState::Initial:
		CUtility::StopThread(m_pThreadGenerateVolume);
		closeAllSessions();
		// To-Do: unload catheter
		break;
	case RayScannerState::Default:
		CUtility::StopThread(m_pThreadGenerateVolume);
		closeAllSessions();
		break;
	case RayScannerState::Scanning:
		CUtility::StartThread(threadPullbackScan, m_pThreadRotaryJunction, this);
		break;
	case RayScannerState::Review:		
		if (m_prevState == RayScannerState::Scanning) {
			CUtility::StartThread(threadSaveRaw, m_pThreadSaveRaw, this);
		}

		// To-Do: Change to OnDemand ver.
		// CUtility::StartThread(threadGenerateVolume, m_pThreadGenerateVolume, this);
		break;
	default:
		break;
	}

	return NOERROR;
}

/*
* OnMsgUpdateSaveRaw
*/
LRESULT COCTSystem::OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam) {
	UINT nFrame = wParam;
	UINT nTotalFrame = lParam;

	int nFrameInfo = (nFrame << 16) | (nTotalFrame);
	if (m_callback != nullptr) m_callback((int) RayCallbackRequest::ProgressSave, nFrameInfo);

	return NOERROR;
}

/*
* OnMsgUpdateCatheterState
*/
LRESULT COCTSystem::OnMsgUpdateCatheterState(WPARAM wParam, LPARAM lParam) {
	m_cathState = (CatheterState)wParam;

	CUtility::StopThread(m_pThreadRotaryJunction);

	switch (m_cathState) {
	case CatheterState::Unloaded:
		postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::UnloadCatheter);
		break;
	case CatheterState::Loaded:
		CUtility::StartThread(threadValidateCatheter, m_pThreadRotaryJunction, this);
		break;
	case CatheterState::Enable:
		break;
	case CatheterState::Calibrated:
		break;
	default:
		break;
	}

	return NOERROR;
}

/*
* OnMsgStartReviewSession
*/
LRESULT COCTSystem::OnMsgStartReviewSession(WPARAM wParam, LPARAM lParam) {
	int nSession = wParam;
	CImagingSession* pSession = (CImagingSession*)lParam;

	if (m_reviewSession[nSession] != nullptr) {
		PLOGI.printf("Stop session #%d", nSession);
		m_reviewSession[nSession]->Stop();
		delete m_reviewSession[nSession];
		m_reviewSession[nSession] = nullptr;
	}

	PLOGI.printf("Start session #%d", nSession);
	m_reviewSession[nSession] = pSession;
	m_reviewSession[nSession]->Start();
	m_curSession = (SessionType) nSession;

	if (nSession == SESSION_REVIEW) {
		pSession->StartCutViewUpdate(m_backgroundColor);
	}

	return NOERROR;
}

/*
* OnMsgNotifyProcessDone
*/
LRESULT COCTSystem::OnMsgNotifyProcessDone(WPARAM wParam, LPARAM lParam) {
	RayWorkItem workItem = (RayWorkItem)wParam;

	switch (workItem) {
	case RayWorkItem::SaveRawData:
		CUtility::StopThread(m_pThreadSaveRaw);
		break;
	case RayWorkItem::GenerateVolume:
		CUtility::StopThread(m_pThreadGenerateVolume);
		break;
	case RayWorkItem::LumenDetection:
		break;
	default:
		return NOERROR;
	}

	if (m_callback != nullptr) {
		m_callback((int)RayCallbackRequest::WorkDone, (int)workItem);
	}

	return NOERROR;
}

/*
* OnMsgNotifyEventOccured
*/
LRESULT COCTSystem::OnMsgNotifyEventOccured(WPARAM wParam, LPARAM lParam) {
	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::Event, wParam);

	return NOERROR;
}

/*
* OnMsgDeviceWorkDone
*/
LRESULT COCTSystem::OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadRotaryJunction);

	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::WorkDone, (int)wParam);

	return NOERROR;
}

/*
* OnMsgNotifyErrorOccured
*/
LRESULT COCTSystem::OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam) {
	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::Error, wParam);

	return NOERROR;
}