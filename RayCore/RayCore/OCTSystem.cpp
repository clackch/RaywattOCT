#include "Config.h"
#include "OCTSystem.h"
#include "Utility.h"
#include "Configuration.h"
#include "OCTImaging.h"
#include "LabImaging.h"
#include "DataWriter.h"
#include "CutViewManager.h"
#include "ATSDevice.h"
#include "SimulateDevice.h"
#include "LaserController.h"
#include "MotorController.h"
#include "ZaberController.h"
#include "ArduinoController.h"
#include "IRayLearning.h"
#include "ImagingSession.h"
#include "LaserModule.h"
#include "LookUpTable.h"

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
	InitializeCriticalSection(&m_csSession);

	m_pPullbackMotor = new CArduinoController();
	m_pLaserModule = new CLaserModule();

	m_prevState = RayScannerState::Initial;
	m_curState = RayScannerState::Initial;
	m_cathState = CatheterState::Unloaded;

	//Property
	m_fBrightness = 0.0f;
	m_fContrast = 0.5f;
	m_fDegree = 90;
	m_isTestMode = false;
}

/*
* ~COCTSystem
*/
COCTSystem::~COCTSystem() {
	Stop();
	DeleteCriticalSection(&m_csSession);
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
	settingPullback.Set(settingPullback.nAScan, floor((double)config.acquisition.nLaserSpeed / ((double)config.bldcMotor.velocityPullback / 60.f)));
	PLOGI.printf("Pullback setting: LaserSpeed=%ld, Velocity=%ldrpm, NumOfAlines=%ld", config.acquisition.nLaserSpeed, config.bldcMotor.velocityPullback, settingPullback.nBScan);
	m_pImagingPullback = CImagingSession::CreateColorImaging(this, settingPullback, nullptr, ImagingType::Default);
	m_pImagingPullback->SetSession(SESSION_REALTIME);
	m_pImagingPullback->Start();

	IImaging::Setting settingLiveView = config.imaging;
	settingLiveView.Set(settingLiveView.nAScan, floor((double)config.acquisition.nLaserSpeed / ((double)config.bldcMotor.velocityLiveView / 60.f)));
	PLOGI.printf("Pullback setting: LaserSpeed=%ld, Velocity=%ldrpm, NumOfAlines=%ld", config.acquisition.nLaserSpeed, config.bldcMotor.velocityLiveView, settingLiveView.nBScan);
	m_pImagingLiveView = CImagingSession::CreateColorImaging(this, settingLiveView, nullptr, ImagingType::Default);
	m_pImagingLiveView->SetSession(SESSION_REALTIME);
	m_pImagingLiveView->Start();

	m_pAcqDevice = new CATSDevice(config.acquisition);

	return RayError::OK;
}

/*
* Stop
*/
RayError COCTSystem::Stop() {
	PLOGI.printf("Stop threads");
	CUtility::StopThread(m_pThreadService);
	CUtility::StopThread(m_pThreadSaveRaw);
	CUtility::StopThread(m_pThreadRotaryJunction);

	PLOGI.printf("Close All Sessions");
	closeAllSessions();
	if (m_openedSession != nullptr) {
		delete m_openedSession;
		m_openedSession = nullptr;
	}

	PLOGI.printf("Stop Acquisition");
	if (m_pAcqDevice != nullptr) {
		m_pAcqDevice->StopAcquisition();
		delete m_pAcqDevice;
		m_pAcqDevice = nullptr;
	}

	PLOGI.printf("Stop Imaging");
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

	PLOGI.printf("Laser Off");
	CLaserController* pLaser = CLaserController::GetInstance();
	pLaser->LaserOnOff(false);

	PLOGI.printf("Stop Motor");
	CMotorController* pMotor = CMotorController::GetInstance();
	pMotor->StopMotor();
	pMotor->SwitchOff();
	pMotor->Disconnect();

	PLOGI.printf("Close COM Ports");
	if (m_pPullbackMotor->IsOpen()) {
		m_pPullbackMotor->SetSpeed(StepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		m_pPullbackMotor->MoveAbsolute(StepMotorIndex::Pullback, PULLBACK_MOTOR_POS_INITIAL);
		m_pPullbackMotor->Close();
	}
	delete m_pPullbackMotor;
	m_pPullbackMotor = nullptr;

	if (m_pLaserModule->IsOpen()) {
		m_pLaserModule->SetVLD(0);
		m_pLaserModule->SetVOA(0);
		m_pLaserModule->Close();
	}
	delete m_pLaserModule;
	m_pLaserModule = nullptr;

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
		PLOGI.printf("connect DAQ - %s", ((result == NOERROR) ? "Succeed" : "Failed"));
		result |= connectRotaryJunction();
		PLOGI.printf("connect Rotary Junction - %s", ((result == NOERROR) ? "Succeed" : "Failed"));

		// Connect to COM Interface first time asynchronous
		CLaserController* pLaser = CLaserController::GetInstance();
		pLaser->LaserOnOff(false);

		if (m_isTestMode) {
			CMotorController *pMotor = CMotorController::GetInstance();
			if (pMotor->IsConnected() == false)
			{
				delete pMotor;
				CMotorControllerStub* pMotorStub = new CMotorControllerStub();
				pMotorStub->EnableStub();
			}

			result = NOERROR;
		}

		if (result == NOERROR) {
			postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Default);
			return RayError::OK;
		}
		else {
			return RayError::DeviceNotConnected;
		}
	}
	PLOGI.printf("WrongState - %d", m_curState);

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
		if (m_pLaserModule->IsOpen() == false) return RayError::DeviceNotConnected;
		if (m_pLaserModule->IsMoving(MotorIndex::DelayLine)) return RayError::DeviceBusy;

		m_pLaserModule->MoveRelative(MotorIndex::DelayLine, (forward ? DELAYLINE_FORWARD_POSITION : DELAYLINE_BACKWARD_POSITION));

		return RayError::OK;
	}

	return RayError::WrongState;
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

		CMotorController* pMotorCtrl = CMotorController::GetInstance();
		CConfiguration& config = CConfiguration::GetInstance();

		laserOnOff(true);
		restartAcqDevice(m_pImagingPullback);

		pMotorCtrl->PerformRun(config.bldcMotor.velocityPullback);
		m_pPullbackMotor->SetSpeed(StepMotorIndex::Both, config.stepMotor.pullbackSpeed);

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

		CMotorController* pMotorCtrl = CMotorController::GetInstance();
		CConfiguration& config = CConfiguration::GetInstance();

		pMotorCtrl->PerformRun(config.bldcMotor.velocityLiveView);

		laserOnOff(true);
		restartAcqDevice(m_pImagingLiveView);

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

		CMotorController* pMotorCtrl = CMotorController::GetInstance();

		laserOnOff(false);
		pMotorCtrl->StopMotor();

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* LaserOnOff
*/
RayError COCTSystem::LaserOnOff(bool isOn)
{
	laserOnOff(isOn);

	return RayError::OK;
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
void* COCTSystem::GetVolumeData(void* pLumenContours) {
	if (m_curState == RayScannerState::Review)
	{
		if (m_reviewSession[SESSION_REVIEW] != nullptr) {
			void* pVolumeData = m_reviewSession[SESSION_REVIEW]->GetVolumeData();

			// remove lumen area from volume data
			if (pLumenContours != nullptr)
			{
				CConfiguration& config = CConfiguration::GetInstance();
				int nDiameter = config.volume.size;
				int nFrames = m_reviewSession[SESSION_REVIEW]->GetImageDepth();

				for (int i = 0; i < nFrames; i++) {
					int nOffset = (nDiameter * nDiameter) * i;
					cv::Mat imgOCT = cv::Mat(nDiameter, nDiameter, CV_8UC1, ((char*)pVolumeData) + nOffset);
					cv::Mat imgLumen = cv::Mat(nDiameter, nDiameter, CV_8UC1, ((char *)pLumenContours) + nOffset);

					cv::Mat imgMask;
					cv::bitwise_not(imgLumen, imgMask);

					cv::Mat imgOrigin = imgOCT.clone();
					memset(imgOCT.data, 0x00, (nDiameter * nDiameter));
					cv::copyTo(imgOrigin, imgOCT, imgMask);
				}
			}

			return pVolumeData;
		}
	}
	return nullptr;
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
	
	redrawCutView();

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

	// Read LUT from File
	CLookUpTable& lut = CLookUpTable::GetInstance();
	int result = lut.Load("LUT.csv");
	PLOGI.printf("read LUT : %s", (result > 0) ? "Succeed" : "Failed");

	// Initialize (first prediction)
	cv::Mat imgSample = cv::imread(".\\oct_sample.png");
	IRayLearning* learning = IRayLearning::GetInstance();
	learning->Initialize(true);
	learning->FindLumen(imgSample);

	PLOGI.printf("sample lumen detection done.");

	if (pSystem->m_callback != nullptr)
	{
		pSystem->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::StartService);
	}

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

		//pSystem->postMessage(WM_UPDATE_SAVE_RAW, nFrame + 1, nNumOfSamples);
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
* threadAutoCalibration
*/
UINT COCTSystem::threadAutoCalibration(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;
	int nTargetPos = 0;

	if (pLaserModule != nullptr && pLaserModule->IsOpen())
	{
		// 1. Start Finding Sheath
		pSystem->m_vCalibrationInfo.clear();
		pSystem->m_cathState = CatheterState::FindingSheath;
		
		// 1-1. Move Delay-line & Find Sheath
		nTargetPos = pLaserModule->MoveRelative(MotorIndex::DelayLine, DELAYLINE_BACKWARD_POSITION * 50);
		do {
			Sleep(10);
			if (!pLaserModule->IsMoving(MotorIndex::DelayLine)) pLaserModule->MoveAbsolute(MotorIndex::DelayLine, nTargetPos);
		} while (pLaserModule->GetPosition(MotorIndex::DelayLine) != nTargetPos);

		nTargetPos = pLaserModule->MoveRelative(MotorIndex::DelayLine, DELAYLINE_FORWARD_POSITION * 100);
		do {
			Sleep(10);
			if (!pLaserModule->IsMoving(MotorIndex::DelayLine)) pLaserModule->MoveAbsolute(MotorIndex::DelayLine, nTargetPos);
		} while (pLaserModule->GetPosition(MotorIndex::DelayLine) != nTargetPos);

		// 1-2. Find Z-Offset Position
		const int nSheathPosition = CConfiguration::GetInstance().measurement.nSheathPosition;
		int nMinDiff = INT_MAX;
		int nZOffset = 0;
		for (int i = 0; i < pSystem->m_vCalibrationInfo.size(); i++) {
			int nDiff = abs(nSheathPosition - pSystem->m_vCalibrationInfo.at(i).first);
			if (nMinDiff > nDiff) {
				nMinDiff = nDiff;
				nZOffset = pSystem->m_vCalibrationInfo.at(i).second;
			}
		}

		// 1-3. Move to calibrated position
		nTargetPos = nZOffset;
		pLaserModule->MoveAbsolute(MotorIndex::DelayLine, nZOffset);
		do {
			Sleep(10);
			if (!pLaserModule->IsMoving(MotorIndex::DelayLine)) pLaserModule->MoveAbsolute(MotorIndex::DelayLine, nTargetPos);
		} while (pLaserModule->GetPosition(MotorIndex::DelayLine) != nTargetPos);

		// 2. Start Finding Peak
		pSystem->m_vCalibrationInfo.clear();
		pSystem->m_cathState = CatheterState::FindingPeak;

		// 2-1. Move Polarization-control & Find Peak
		nTargetPos = pLaserModule->MoveAbsolute(MotorIndex::Polarization, 0);
		do {
			Sleep(10);
			if (!pLaserModule->IsMoving(MotorIndex::Polarization)) pLaserModule->MoveAbsolute(MotorIndex::Polarization, nTargetPos);
		} while (pLaserModule->GetPosition(MotorIndex::Polarization) != nTargetPos);

		nTargetPos = pLaserModule->MoveRelative(MotorIndex::Polarization, 3240);
		do {
			Sleep(10);
			if (!pLaserModule->IsMoving(MotorIndex::Polarization)) pLaserModule->MoveAbsolute(MotorIndex::Polarization, nTargetPos);
		} while (pLaserModule->GetPosition(MotorIndex::Polarization) != nTargetPos);

		// 2-2. Find Max Peak
		int nMaxPeak = INT_MIN;
		int nMaxPeakPos = 0;
		for (int i = 0; i < pSystem->m_vCalibrationInfo.size(); i++) {
			if (nMaxPeak < pSystem->m_vCalibrationInfo.at(i).first) {
				nMaxPeak = pSystem->m_vCalibrationInfo.at(i).first;
				nMaxPeakPos = pSystem->m_vCalibrationInfo.at(i).second;
			}
		}

		// 2-3. Move to calibrated position
		nTargetPos = nMaxPeakPos;
		pLaserModule->MoveAbsolute(MotorIndex::DelayLine, nTargetPos);
		do {
			Sleep(10);
			if (!pLaserModule->IsMoving(MotorIndex::DelayLine)) pLaserModule->MoveAbsolute(MotorIndex::DelayLine, nTargetPos);
		} while (pLaserModule->GetPosition(MotorIndex::DelayLine) != nTargetPos);
	}
	
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
	CArduinoController* pPullbackMotor = pSystem->m_pPullbackMotor;
	IImaging::Setting settingPullback = pSystem->m_pImagingPullback->GetSetting();
	int pullbackTime = ((double)config.stepMotor.pullbackDistance / (double)config.stepMotor.pullbackSpeed) * 1000;

	PLOGI.printf("Pullback start - %dmm, %dmm/s - %dsec", config.stepMotor.pullbackDistance, config.stepMotor.pullbackSpeed, pullbackTime);

	// 1. Start Recording OCT
	CDataWriter* pDataWriter = new CDataWriter();
	pDataWriter->Initialize(settingPullback.nBufferSize * sizeof(USHORT));
	pDataWriter->AddExtraData(OCTHeader::ExtraData::Dispersion, pSystem->m_pImagingPullback->GetCalibrationData(), settingPullback.nAScan * 2 * sizeof(int));
	if (ImagingType::Default == ImagingType::LabImaging)
	{
		pDataWriter->AddExtraData(OCTHeader::ExtraData::Background, 
			((CLabImaging*)pSystem->m_pImagingPullback)->GetBackground(), settingPullback.nBufferSize * sizeof(USHORT));
	}
	pDataWriter->StartRecording();
	pSystem->m_pAcqDevice->SetWriter(pDataWriter);

	// 2. Pullback Linear Stage
	if (pPullbackMotor->IsOpen()) {		
		pPullbackMotor->MoveAbsolute(StepMotorIndex::Both, config.stepMotor.pullbackDistance);
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
		Sleep(pullbackTime);
	}

	// 3. Stop Recording OCT
	pDataWriter->StopRecording();
	pSystem->m_pAcqDevice->SetWriter(nullptr);

	// 4. Motor OFF
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
	CArduinoController* pPullbackMotor = pSystem->m_pPullbackMotor;

	pSystem->postMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterLoading);

	// 1. Rotate BLDC Motor
	pMotor->PerformRun(config.bldcMotor.velocityLoad);

	// 2. Move Step-Motor (Pullback)
	if (pPullbackMotor->IsOpen()) {
		pPullbackMotor->SetSpeed(StepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pPullbackMotor->MoveAbsolute(StepMotorIndex::Pullback, PULLBACK_MOTOR_POS_LOAD);

		pPullbackMotor->SetSpeed(StepMotorIndex::Pullback, STEP_MOTOR_SPEED_LOAD);
		pPullbackMotor->MoveAbsolute(StepMotorIndex::Pullback, 0);
		pPullbackMotor->MoveAbsolute(StepMotorIndex::Pullback, DISTANCE_BETWEEN_MOTORS);
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime());
	}

	// 3. Stop BLDC Motor
	pMotor->StopMotor();

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
	CArduinoController* pPullbackMotor = pSystem->m_pPullbackMotor;

	pSystem->postPriorMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterUnloading);

	if (pPullbackMotor->IsOpen()) {
		pPullbackMotor->SetSpeed(StepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pPullbackMotor->MoveAbsolute(StepMotorIndex::Pullback, PULLBACK_MOTOR_POS_INITIAL);
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime() / 2);
	}

	pSystem->postPriorMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Unloaded);

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

	PLOGI.printf("Catheter Validation");

	pSystem->laserOnOff(true);
	pSystem->restartAcqDevice(pSystem->m_pImagingLiveView);
	pMotor->PerformRun(config.bldcMotor.velocityLiveView);

	// To-Do: determine image verification
	bool verified = true;

	pMotor->StopMotor();
	pSystem->laserOnOff(false);

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
	result &= m_pPullbackMotor->IsOpen();
	result &= m_pLaserModule->IsOpen();

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
	if (m_pAcqDevice == nullptr) return NOERROR;

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

	bool result = true;

	if (!m_pPullbackMotor->IsOpen()) {
		m_pPullbackMotor->Open(config.stepMotor.port);
		Sleep(DELAY_BETWEEN_COMMAND);
		m_pPullbackMotor->SetCurrent(StepMotorIndex::Pullback, PULLBACK_MOTOR_POS_INITIAL);
		Sleep(DELAY_BETWEEN_COMMAND);
		m_pPullbackMotor->SetCurrent(StepMotorIndex::Hub, HUB_MOTOR_POS_INITIAL);
	}

	if (!m_pLaserModule->IsOpen()) {
		result &= m_pLaserModule->Open(config.laserModule.port);
		if (result) {
			m_pLaserModule->SetVLD(0);
			Sleep(500);
			m_pLaserModule->SetVOA(config.laserModule.voaValue);
			Sleep(500);
			m_pLaserModule->MoveAbsolute(MotorIndex::DelayLine, config.laserModule.delayPosition);
			while (m_pLaserModule->IsMoving(MotorIndex::DelayLine)) {
				Sleep(10);
			}
			Sleep(500);
			m_pLaserModule->MoveAbsolute(MotorIndex::Polarization, config.laserModule.polarPosition);
		}
		else
		{
			PLOGE.printf("Failed to connect to laser module");
		}
	}

	if (!pMotor->IsConnected()) {
		result &= pMotor->Connect(config.bldcMotor.port);
		result &= pMotor->SetModeOfOperation(MOTOR_DATA_MODE_VELOCITY);
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

	m_pPullbackMotor->Close();
	m_pLaserModule->Close();

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

		switch (m_cathState)
		{
		case CatheterState::FindingSheath:
		{
			int nSheathPosition = m_pImagingRealtime->GetSheathPosition();
			int nDelayLinePos = m_pLaserModule->GetPosition(MotorIndex::DelayLine);
			m_vCalibrationInfo.push_back(std::make_pair(nSheathPosition, nDelayLinePos));
			PLOGI.printf("FindingSheath - %d, %d", nSheathPosition, nDelayLinePos);
		}
			break;
		case CatheterState::FindingPeak:
		{
			COCTMeasurement measurement;
			CConfiguration& config = CConfiguration::GetInstance();

			USHORT nPeakValue;
			int nPeakIndex, nLineWidth;
			measurement.CalculateAxialResolution(((CLabImaging *)m_pImagingRealtime)->GetScopeFFTData(), config.imaging.nOutputLength, config.measurement, nPeakValue, nPeakIndex, nLineWidth);

			int nPolarizationPos = m_pLaserModule->GetPosition(MotorIndex::Polarization);
			m_vCalibrationInfo.push_back(std::make_pair(nPeakValue, nPolarizationPos));
			PLOGI.printf("FindingPeak - %d, %d", nPeakValue, nPolarizationPos);
		}
			break;
		default:
			break;
		}
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
	EnterCriticalSection(&m_csSession);
	for (int i = 0; i < MAX_SESSION_NUM; i++) {
		if (m_reviewSession[i] != nullptr) {
			delete m_reviewSession[i];
			m_reviewSession[i] = nullptr;
		}
	}
	LeaveCriticalSection(&m_csSession);
	m_curSession = SESSION_UNKNOWN;
}
void COCTSystem::setBrightnessContrastAllSessions() {
	CConfiguration& config = CConfiguration::GetInstance();
	config.imaging.brightness = m_fBrightness;
	config.imaging.contrast = m_fContrast;

	m_pImagingPullback->SetBrightnessContrast(m_fBrightness, m_fContrast);
	m_pImagingLiveView->SetBrightnessContrast(m_fBrightness, m_fContrast);

	EnterCriticalSection(&m_csSession);
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
	LeaveCriticalSection(&m_csSession);

	if (m_openedSession != nullptr && m_openedSession->GetImaging() != nullptr)
	{
		m_openedSession->GetImaging()->SetBrightnessContrast(m_fBrightness, m_fContrast);
	}

	redrawCutView();
}

void COCTSystem::redrawCutView() {
	if (m_curSession != SESSION_UNKNOWN && m_reviewSession[m_curSession] != nullptr) {
		CCutViewManager* pCutView = m_reviewSession[m_curSession]->GetCutView();
		if (pCutView != nullptr) {
			int nFrames = pCutView->GetNumOfGeneratedSamples();
			if (nFrames > 0) {
				int nCurFrame = nFrames - 1;
				this->postPriorMessage(WM_PROCESS_CUTVIEW, m_curSession, nCurFrame);
			}
		}
	}
}
void COCTSystem::laserOnOff(bool isOn) {
	CConfiguration& config = CConfiguration::GetInstance();
	CLaserController* pLaser = CLaserController::GetInstance();

	pLaser->LaserOnOff(isOn);
	
	if (m_pLaserModule != nullptr && m_pLaserModule->IsOpen()) {
		int vldPower = (isOn) ? config.laserModule.vldValue : 0;
		m_pLaserModule->SetVLD(vldPower);
	}
}
/*
* OnMsgUpdateScannerState
*/
LRESULT COCTSystem::OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam) {
	m_prevState = m_curState;
	m_curState = (RayScannerState)wParam;

	if(m_callback != nullptr) m_callback((int)RayCallbackRequest::State, (int)m_curState, lParam);

	PLOGI.printf("ScannerState - %d > %d", m_prevState, m_curState);
	switch (m_curState) {
	case RayScannerState::Initial:
		closeAllSessions();
		// To-Do: unload catheter
		break;
	case RayScannerState::Default:
		closeAllSessions();
		break;
	case RayScannerState::Scanning:
		CUtility::StartThread(threadPullbackScan, m_pThreadRotaryJunction, this);
		break;
	case RayScannerState::Review:		
		if (m_prevState == RayScannerState::Scanning) {
			CUtility::StartThread(threadSaveRaw, m_pThreadSaveRaw, this);
		}
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

	if (m_callback != nullptr) m_callback((int) RayCallbackRequest::ProgressSave, nFrame, nTotalFrame);

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
		PLOGI.printf("Catheter - Unloaded.");
		postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::UnloadCatheter);
		break;
	case CatheterState::Loaded:
		PLOGI.printf("Catheter - Loaded.");
		CUtility::StartThread(threadValidateCatheter, m_pThreadRotaryJunction, this);
		break;
	case CatheterState::Enable:
		PLOGI.printf("Catheter - Enable.");
		break;
	case CatheterState::Calibrated:
		PLOGI.printf("Catheter - Calibrated.");
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
		pSession->StartVolumeGeneration();
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
	case RayWorkItem::StartService:
	case RayWorkItem::OCTImaging:
	case RayWorkItem::GenerateCutView:
	case RayWorkItem::DetectLumen:
	case RayWorkItem::GenerateVolume:
		break;
	default:
		return NOERROR;
	}

	if (m_callback != nullptr) {
		m_callback((int)RayCallbackRequest::WorkDone, (int)workItem, lParam);
	}

	return NOERROR;
}

/*
* OnMsgNotifyEventOccured
*/
LRESULT COCTSystem::OnMsgNotifyEventOccured(WPARAM wParam, LPARAM lParam) {
	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::Event, wParam, lParam);

	return NOERROR;
}

/*
* OnMsgDeviceWorkDone
*/
LRESULT COCTSystem::OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadRotaryJunction);

	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::WorkDone, wParam, lParam);

	return NOERROR;
}

/*
* OnMsgNotifyErrorOccured
*/
LRESULT COCTSystem::OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam) {
	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::Error, wParam, lParam);

	return NOERROR;
}