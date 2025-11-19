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
#include "RJController.h"
#include "IRayLearning.h"
#include "ImagingSession.h"
#include "LaserModule.h"
#include "LookUpTable.h"
#include "PullbackLengthManager.h"
#include <fstream>

/*
* COCTSystem
*/
COCTSystem::COCTSystem() {

	m_callback = nullptr;
	m_cbCrossSection = nullptr;
	m_cbLongitude = nullptr;
	m_cbObjectDetection = nullptr;	

	m_isTestMode = false;

	InitializeCriticalSection(&m_csSession);

	CConfiguration& config = CConfiguration::GetInstance();
	if (!config.IsInit()) {
		config.Initialize(_T(".\\raycore.ini"));
	}

	catheterRFID = config.catheter.catheterRFID;

	SetLogger(config.logRootPath);
	if (config.laserModule.autoCalibrationForSeverance != 0)
		PLOGI.printf("auto Calibration is set for Severance");
	else
		PLOGI.printf("auto Calibration is set for General");
}

/*
* ~COCTSystem
*/
COCTSystem::~COCTSystem() {
	if(m_bInit)
		Stop();
	DeleteCriticalSection(&m_csSession);
}

void COCTSystem::SetLogger(TCHAR* logRootPath) {

	time_t timer = time(nullptr);
	tm t;
	errno_t err = localtime_s(&t, &timer);

	if (err != 0) {
		PLOGI.printf("localtime_s failed with error code: %d", err);
	}

	char rootPath[MAX_PATH] = "";
	WideCharToMultiByte(CP_ACP, 0, logRootPath, MAX_PATH, rootPath, MAX_PATH, nullptr, nullptr);

	char logFile[_MAX_PATH] = "";
	errno_t rc = sprintf_s(        
		logFile,                   
		sizeof(logFile),           
		"%s\\core_%04d-%02d-%02d.log",
		rootPath,
		t.tm_year + 1900,
		t.tm_mon + 1,
		t.tm_mday
	);
	if (rc < 0) {
		PLOGI.printf("Fail to create log file: %d\n", rc);
	}

	printf("plog::init - %s\n", logFile);

#ifdef DEBUG
	plog::init(plog::debug, logFile);
#else
	plog::init(plog::info, logFile);
#endif
}

/*
* Init
*/
RayError COCTSystem::Init() {

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

	m_pRJController = new CRJController();
	m_pRJController->SetMessage(this);
	m_pLaserModule = new CLaserModule();

	m_prevState = RayScannerState::Initial;
	m_curState = RayScannerState::Initial;
	m_cathState = CatheterState::Unloaded;

	//Property
	m_fBrightness = 0.0f;
	m_fContrast = 0.5f;
	m_fDegree = 90;

	m_bInit = true;

	return RayError::OK;
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

	CUtility::StartThread(threadService, m_pThreadService, this);

	IImaging::Setting settingPullback = config.imaging;
	settingPullback.Set(settingPullback.nAScan, floor((double)config.acquisition.nLaserSpeed / ((double)config.bldcMotor.velocityPullback / 60.f)));
	PLOGI.printf("Pullback setting: LaserSpeed=%ld, Velocity=%ldrpm, NumOfAlines=%ld", config.acquisition.nLaserSpeed, config.bldcMotor.velocityPullback, settingPullback.nBScan);
	m_pImagingPullback = CImagingSession::CreateColorImaging(this, settingPullback, nullptr, ImagingType::Default);
	if (!m_pImagingPullback) {
		PLOGI.printf("Failed to create imaging pullback");
		return RayError::WrongSession;
	}
	m_pImagingPullback->SetSession(SESSION_REALTIME);
	m_pImagingPullback->Start();

	IImaging::Setting settingLiveView = config.imaging;
	settingLiveView.Set(settingLiveView.nAScan, floor((double)config.acquisition.nLaserSpeed / ((double)config.bldcMotor.velocityLiveView / 60.f)));
	PLOGI.printf("Pullback setting: LaserSpeed=%ld, Velocity=%ldrpm, NumOfAlines=%ld", config.acquisition.nLaserSpeed, config.bldcMotor.velocityLiveView, settingLiveView.nBScan);
	m_pImagingLiveView = CImagingSession::CreateColorImaging(this, settingLiveView, nullptr, ImagingType::Default);
	if (!m_pImagingLiveView) {
		PLOGI.printf("Failed to create Imaging LiveView");
		return RayError::WrongSession;
	}
	m_pImagingLiveView->SetSession(SESSION_REALTIME);
	m_pImagingLiveView->Start();

	loadAutoCalibPatch();

	m_pAcqDevice = new CATSDevice(config.acquisition);
	m_pRJController->SetCatheterUsage(config.catheter.catheterUsage);

	return RayError::OK;
}

/*
* Stop
*/
RayError COCTSystem::Stop() {
	PLOGI.printf("Stop threads");
	CUtility::StopThread(m_pThreadService);
	CUtility::StopThread(m_pThreadSaveRaw);

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

	PLOGI.printf("Finalize Motor");
	if (m_pRJController != nullptr) {
		delete m_pRJController;
		m_pRJController = nullptr;
	}

	PLOGI.printf("Finalize LaserModule");
	if (m_pLaserModule->IsConnected()) {
		m_pLaserModule->Disconnect();
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

		// Connect to COM Interface first time
		CLaserController* pLaser = CLaserController::GetInstance();

		if (pLaser == nullptr) {
			PLOGI.printf("Laser is not initialized");
			return RayError::DeviceNotConnected;
		}

		pLaser->LaserOnOff(true);

		result |= connectAcqDevice();
		PLOGI.printf("connect DAQ - %s", ((result == NOERROR) ? "Succeed" : "Failed"));

		pLaser->LaserOnOff(false);

		result |= connectRotaryJunction();
		PLOGI.printf("connect Rotary Junction and Laser Module - %s", ((result == NOERROR) ? "Succeed" : "Failed"));

		if (m_isTestMode) {
			if (m_pRJController->IsConnected() == false)
			{
				//pMotor = new CMotorControllerStub();
				PLOGI.printf("Use MotorControllerStub instaed of MotorController");
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
		if (m_pRJController->GetState() != eRJState::Loaded) return RayError::RotaryJunctionError;

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
		if (m_pLaserModule->IsConnected() == false) return RayError::DeviceNotConnected;
		if (m_pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) return RayError::DeviceBusy;
		if (m_pRJController->GetState() == eRJState::Error) return RayError::RotaryJunctionError;

		m_pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_DEFAULT);
		m_pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, (forward ? DELAYLINE_FORWARD_POSITION : DELAYLINE_BACKWARD_POSITION));

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

		CConfiguration& config = CConfiguration::GetInstance();

		laserOnOff(true);
		restartAcqDevice(m_pImagingPullback);

		m_pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_PULLBACK);
		m_pRJController->PerformRun(config.bldcMotor.velocityPullback);
		m_pRJController->Current(eStepMotorIndex::Pullback, 0);
		m_pRJController->Current(eStepMotorIndex::Hub, 0);
		m_pRJController->Set(eStepMotorIndex::Both, m_pRJController->ConvertMMtoStep(config.stepMotor.pullbackSpeed));

		return RayError::OK;
	}
	return RayError::WrongState;
}
/*
* PullbackScan
*/
RayError COCTSystem::PullbackScan(char *strFilePath) {
	if (m_curState == RayScannerState::Default) {
		if (m_pRJController->GetState() == eRJState::Error) return RayError::RotaryJunctionError;
		m_strFilePath = CUtility::StringToWstring(strFilePath);

		postPriorMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Scanning);

		return RayError::OK;
		
	}

	return RayError::WrongState;
}

/*
* LoadCatheter
*/
RayError COCTSystem::LoadCatheter() {
	if (m_curState == RayScannerState::Default || m_curState == RayScannerState::Review) {
		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;
		if (controlRotaryJunction(eRJState::Loading) == NOERROR) {
			return RayError::OK;
		}
		else {
			if (m_isTestMode) {
				postMessage(WM_UPDATE_RJ_STATE, (WPARAM) eRJState::Loading);
			}
			return RayError::DeviceNotConnected;
		}
	}

	return RayError::WrongState;
}

/*
* UnloadCatheter
*/
RayError COCTSystem::UnloadCatheter() {
	if (m_curState == RayScannerState::Default || m_curState == RayScannerState::Review) {
		if (m_pThreadRotaryJunction != nullptr) return RayError::DeviceBusy;

		if (controlRotaryJunction(eRJState::Unloading) == NOERROR) {
			return RayError::OK;
		}
		else {
			return RayError::DeviceNotConnected;
		}

		return RayError::OK;
	}

	return RayError::WrongState;
}

/*
* StartReview
* return N (>0) when current state & argument is right.
* return Error Code (<0) when something is wrong.
*/
int COCTSystem::StartReview(char* strFilePath, double imageResolution, double zOffset) {
	if (m_curState == RayScannerState::Initial || m_curState == RayScannerState::Default) {
		if (imageResolution == 0.0f) return (int)RayError::InvalidArgument;

		CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_REVIEW, strFilePath, imageResolution);
		if (pSession == nullptr) {
			PLOGE.printf("InvalidArgument : %s", strFilePath);
			return (int)RayError::InvalidArgument;
		}
		pSession->SetAutoCalibPatch(m_autoCalibPatch);

		std::string strPath(strFilePath);
		std::string strZOffsetFilePath = strPath.substr(0, strPath.size() - 3).append("zOffset");
		if (!pSession->LoadZOffset(strZOffsetFilePath)) {
			//pSession->CalculateZOffset(pSession->GetDataManager()->GetNumOfSamples(), m_autoCalibPatch, strZOffsetFilePath);
		}
		pSession->SetZOffset((int)zOffset);

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
RayError COCTSystem::StartCompare(char* strFilePath, double imageResolution, double zOffset) {
	if (m_curState != RayScannerState::Review) return RayError::WrongState;

	CImagingSession* pSession = CImagingSession::CreateSession(this, SESSION_COMPARE, strFilePath, imageResolution);
	if (pSession == nullptr) {
		return RayError::InvalidArgument;
	}
	
	std::string strPath(strFilePath);
	std::string strZOffsetFilePath = strPath.substr(0, strPath.size() - 3).append("zOffset");
	if (!pSession->LoadZOffset(strZOffsetFilePath)) {
		//pSession->CalculateZOffset(pSession->GetDataManager()->GetNumOfSamples(), m_autoCalibPatch, strZOffsetFilePath);
	}

	pSession->SetZOffset((int)zOffset);

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

RayError COCTSystem::EndCompare()
{
	if (m_reviewSession[SESSION_COMPARE] != nullptr) {
		m_reviewSession[SESSION_COMPARE]->Stop();
	}

	return RayError::OK;
}

RayError COCTSystem::RestartReview()
{
	if (m_curState == RayScannerState::Review) {

		m_reviewSession[SESSION_REVIEW]->StopThreadForRestart();
		m_reviewSession[SESSION_REVIEW]->StartCutViewUpdate(m_backgroundColor);
		m_reviewSession[SESSION_REVIEW]->StartVolumeGeneration();
		m_reviewSession[SESSION_REVIEW]->StartObjectDetection();

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
		if (m_pRJController->GetState() == eRJState::Error) return RayError::RotaryJunctionError;
		m_pImagingLiveView->Start();

		CConfiguration& config = CConfiguration::GetInstance();

		m_pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_LIVEVIEW);
		m_pRJController->PerformRun(config.bldcMotor.velocityLiveView);

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
		if (m_pRJController->GetState() == eRJState::Error) return RayError::RotaryJunctionError;
		m_pImagingLiveView->Stop();
		Sleep(500);

		laserOnOff(false);
		m_pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_STANDBY_OFF);
		m_pRJController->StopMotor();

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

RayError COCTSystem::RJCleanModeOnOff(bool isOn)
{
	PLOGI.printf("RJCleanModeOnOff : %d", isOn);	

	if (m_pRJController->IsConnected()) {
		if (isOn) {
			if (m_pRJController->GetState() == eRJState::Disconnected) {
				controlRotaryJunction(eRJState::Cleaning);
			}
			else {
				return RayError::WrongState;
			}
		}
		else {
			if (m_pRJController->GetState() == eRJState::Cleaning) {
				postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::CleanRotaryJunction);
			}
			else {
				return RayError::WrongState;
			}
		}
	}
	else {
		if (isOn) {
			CUtility::StartThread(threadCleanRotaryJunction, m_pThreadRotaryJunction, this);
		}
		else {
			postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::CleanRotaryJunction);
		}
	}

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

			CConfiguration& config = CConfiguration::GetInstance();
			int nDiameter = config.volume.size;
			int nFrames = m_reviewSession[SESSION_REVIEW]->GetImageDepth();

			// remove lumen area from volume data
			if (pLumenContours != nullptr)
			{
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
			else { // remove center sheath only to get Stent with volume data
				for (int i = 0; i < nFrames; i++) {
					int nOffset = (nDiameter * nDiameter) * i;
					cv::Mat imgOCT = cv::Mat(nDiameter, nDiameter, CV_8UC1, ((char*)pVolumeData) + nOffset);

					cv::Point center(nDiameter/2, nDiameter/2);
					cv::Size axes(30, 30);
					cv::Scalar color(0, 0, 0);
					cv::ellipse(imgOCT, center, axes, 0, 0, 360, color, -1);
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

RayError COCTSystem::SetConfigPath(char* strPath) {
	CConfiguration& config = CConfiguration::GetInstance();

	if (strPath == nullptr || !CUtility::IsExist(strPath, false)) {
		return RayError::InvalidArgument;
	}
	config.SetPath(CUtility::StringToWstring(strPath));

	return RayError::OK;
}

/*
* OpenImage
*/
RayError COCTSystem::OpenImage(char* strFilePath, double imageResolution, double zOffset) {
	PLOGI.printf("OpenImage");
	CloseImage();

	CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_UNKNOWN, strFilePath, imageResolution);
	if (pSession == nullptr) {
		return RayError::InvalidArgument;
	}
	std::string strPath(strFilePath);
	std::string strZOffsetFilePath = strPath.substr(0, strPath.size() - 3).append("zOffset");
	if (!pSession->LoadZOffset(strZOffsetFilePath)) {
		PLOGI.printf("ZOffset file not found: %s", strZOffsetFilePath.c_str());
		// app의 fileCopyDialog에서만 OpenImage를 호출하는데, 이미 저장된 zOffset 파일이 있다고 가정 중.
		//pSession->CalculateZOffset(pSession->GetDataManager()->GetNumOfSamples(), m_autoCalibPatch);
	}
	pSession->SetZOffset((int)zOffset);

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
	// m_openedSession을 정해주는 게 OpenImage 함수 밖에 없는데, ZOffset 파일이 있다고 가정하고 진행 중.
	if (m_openedSession != nullptr) {
		return m_openedSession->GetImageData(nFrame);
	}
	else {
		if (m_curSession == SESSION_UNKNOWN || m_reviewSession[m_curSession] == nullptr) {
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
* GetNumOfSidebranchContourSize
*/
int COCTSystem::GetNumOfSidebranchContourSize(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return 0;

	return m_reviewSession[SESSION_REVIEW]->GetNumOfSidebranchContourSize(nFrame);
}

/*
* GetSidebranchContour
*/
void* COCTSystem::GetSidebranchContour(int nFrame, int nSb){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return nullptr;

	return m_reviewSession[SESSION_REVIEW]->GetSidebranchContour(nFrame, nSb);
}

/*
* GetNumOfSidebranchContourPoints
*/
int COCTSystem::GetNumOfSidebranchContourPoints(int nFrame, int nSb){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return 0;

	return m_reviewSession[SESSION_REVIEW]->GetNumOfSidebranchContourPoints(nFrame, nSb);
}
/*
* GetStentPoints
*/
void* COCTSystem::GetStentPoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return nullptr;

	return m_reviewSession[SESSION_REVIEW]->GetStentPoints(nFrame);
}
/*
* GetNumOfStentPoints
*/
int COCTSystem::GetNumOfStentPoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return 0;

	return m_reviewSession[SESSION_REVIEW]->GetNumOfStentPoints(nFrame);
}
/*
* GetGuidewirePoints
*/
void* COCTSystem::GetGuidewirePoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return nullptr;

	return m_reviewSession[SESSION_REVIEW]->GetGuidewirePoints(nFrame);
}
/*
* GetNumOfGuidewirePoints
*/
int COCTSystem::GetNumOfGuidewirePoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return 0;

	return m_reviewSession[SESSION_REVIEW]->GetNumOfGuidewirePoints(nFrame);
}

/*
* GetNumOfGuidewirePoints
*/

void* COCTSystem::GetGuidewireRadius(int nFrame) {
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return nullptr;

	return m_reviewSession[SESSION_REVIEW]->GetGuidewireRadius(nFrame);
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
* GetColormap
*/
double COCTSystem::GetColormap()
{
	return m_fColormap;
}

/*
* SetColormap
*/
RayError COCTSystem::SetColormap(double value)
{
	// Read LUT from File
	CLookUpTable& lut = CLookUpTable::GetInstance();

	if (value == 3 /*enhancedLUT.csv*/) {
		lut.Load("LUT_enhanced.csv", true);
		lut.SetEnhancedLUT(!(lut.GetEnhancedLUT()));
		return RayError::OK;
	}
	lut.SetCurrentColormap((int)value);
	m_fColormap = value;
	PLOGI.printf("read LUT :%d %s", (int)value, (m_fColormap >= 0) ? "Succeed" : "Failed");

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
	return m_pRJController->IsRun();
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
* GetImageResolution
*/
double COCTSystem::GetImageResolution()
{
	CConfiguration& config = CConfiguration::GetInstance();
	return (config.measurement.GetAxialResolutionScale() / 1000.f) * 2;	// Convert polar scale to cartesian scale (mm)
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

RayError COCTSystem::SetSheathDiameter(double value)
{
	CConfiguration& config = CConfiguration::GetInstance();

	PLOGI.printf("Set catheter size as %.1f (%d)", (value <= 2.0 ? 1.7f : 2.6f), m_bFirstLoad);
		
	if (value <= 2.0) {
		config.measurement.fSheathRadius = config.measurement.fSheathRadiusOnePointSeven;
		config.measurement.fSheathThickness = config.measurement.fSheathThicknessOnePointSeven;
		autoCalibrationFranch = 60;
	}
	else {
		config.measurement.fSheathRadius = config.measurement.fSheathRadiusTwoPointSix;
		config.measurement.fSheathThickness = config.measurement.fSheathThicknessTwoPointSix;
		autoCalibrationFranch = 0;
	}
	config.measurement.nSheathPosition = config.measurement.GetSheathRadius() * 1000.f / config.measurement.GetAxialResolutionScale();
	config.measurement.nSheathThickness = config.measurement.GetSheathThickness() * 1000.f / config.measurement.GetAxialResolutionScale();

	m_pImagingPullback->SetMeasurementSetting(config.measurement);
	m_pImagingLiveView->SetMeasurementSetting(config.measurement);

	if (value <= 2.0) {
		m_pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_MAX);
		m_pLaserModule->Current(eStepMotorIndex::DelayLine, DELAY_LINE_UPPER_END_POSITION);

		Sleep(500);

		PLOGI.printf("Homing start =========================================");
		// m_pLaserModule Move 0 OR sensor #1 이동
		m_pLaserModule->Move(eStepMotorIndex::DelayLine, 0, false, static_cast<char>(0x03));

		Sleep(500);

		while (m_pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) {
			Sleep(50);
		}
		m_pLaserModule->PrintPhotoSensor();

		// m_pLaserModule Current 0
		m_pLaserModule->Current(eStepMotorIndex::DelayLine, 0);

		Sleep(500);

		PLOGI.printf("Move to %d =========================================", config.laserModule.delayPositionOnePointSeven);
		m_pLaserModule->Move(eStepMotorIndex::DelayLine, config.laserModule.delayPositionOnePointSeven, false, static_cast<char>(0x02));

		Sleep(500);

		while (m_pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) {
			Sleep(50);
		}
		m_pLaserModule->PrintPhotoSensor();

		m_pLaserModule->Current(eStepMotorIndex::DelayLine, config.laserModule.delayPositionOnePointSeven);
	}

	if (!m_bFirstLoad) return RayError::OK;
	m_bFirstLoad = false;

	return RayError::OK;
}

/*
* GetImageCompensation
*/
bool COCTSystem::GetImageCompensation()
{
	return m_bImageCompensation;
}

/*
* SetImageCompensation
*/
RayError COCTSystem::SetImageCompensation(bool value)
{
	m_bImageCompensation = value;

	COCTImaging::SetImageCompensation(m_bImageCompensation);

	return RayError::OK;
}

/*
* SetImageCompensationControlWindow
*/
RayError COCTSystem::SetImageCompensationControlWindow(bool value)
{
	m_bImageCompensationControlWindow = value;

	CConfiguration& config = CConfiguration::GetInstance();

	COCTImaging::SetImageCompensationControlWindow(m_bImageCompensationControlWindow, config.imaging);

	m_bImageCompensationControlWindow = 0;

	return RayError::OK;
}

/*
* GetFieldOfView
*/
double COCTSystem::GetFieldOfView()
{
	return m_fFieldOfView;
}

/*
* SetFieldOfView
*/
RayError COCTSystem::SetFieldOfView(double value)
{
	m_fFieldOfView = value;

	return RayError::OK;
}

/*
* SetZOffset
*/
RayError COCTSystem::SetZOffset(double value)
{
	if (m_reviewSession[SESSION_REVIEW] != nullptr)
	{
		if (m_reviewSession[SESSION_REVIEW]->GetImaging() != nullptr)
		{
			m_reviewSession[SESSION_REVIEW]->SetZOffset((int)value);
			PLOGI.printf("Set Z Offset (%d)", (int)value);
		}
	}

	return RayError::OK;
}

/*
* GetAutoPullback
*/
double COCTSystem::GetAutoPullback()
{
	return m_bAutoPullbackOnOff;
}

/*
* SetAutoPullback
*/
RayError COCTSystem::SetAutoPullback(double value)
{
	m_bAutoPullbackOnOff = (bool)value;

	return RayError::OK;
}

/*
* GetLumenThresholdMin
*/
double COCTSystem::GetLumenThresholdMin()
{
	return m_fLumenThresholdMin;
}

/*
* SetLumenThresholdMin
*/
RayError COCTSystem::SetLumenThresholdMin(double value)
{
	m_fLumenThresholdMin = value;

	return RayError::OK;
}

/*
* GetLumenThresholdMax
*/
double COCTSystem::GetLumenThresholdMax()
{
	return m_fLumenThresholdMax;
}

/*
* SetLumenThresholdMax
*/
RayError COCTSystem::SetLumenThresholdMax(double value)
{
	m_fLumenThresholdMax = value;

	return RayError::OK;
}

/*
* GetShowLumenGuide
*/
double COCTSystem::GetShowLumenGuide()
{
	return m_bShowLumenGuide;
}

/*
* SetShowLumenGuide
*/
RayError COCTSystem::SetShowLumenGuide(double value)
{
	m_bShowLumenGuide = (bool)value;

	return RayError::OK;
}

/*
* GetLumenSnrThreshold
*/
double COCTSystem::GetLumenSnrThreshold()
{
	return m_fLumenSrnThreshold;
}

/*
* SetLumenSnrThreshold
*/
RayError COCTSystem::SetLumenSnrThreshold(double value)
{
	m_fLumenSrnThreshold = value;

	return RayError::OK;
}

/*
* SetRefractiveIndex
*/
RayError COCTSystem::SetRefractiveIndex(double value)
{
	PLOGI.printf("Set Refractive Index (%f)", value);

	CConfiguration& config = CConfiguration::GetInstance();

	config.measurement.fRefractiveIndex = value;

	config.imaging.distPerPixel = config.measurement.GetAxialResolutionScale();
	m_pImagingPullback->SetDistPerPixel(config.imaging.distPerPixel);
	m_pImagingLiveView->SetDistPerPixel(config.imaging.distPerPixel);

	return RayError::OK;
}

/*
* threadService
*/
UINT COCTSystem::threadService(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CThread* pThread = pSystem->m_pThreadService;
	bool ignoreMsg = false;

	PLOGI.printf("Service Start");

	// Initialize
	IRayLearning* learning = IRayLearning::GetInstance();
	learning->Initialize(true);

	// LUT Load
	CLookUpTable& lut = CLookUpTable::GetInstance();
	lut.Load("LUT_green.csv");
	lut.Load("LUT_gray.csv");
	lut.Load("LUT_orange.csv");

#ifdef DEBUG
	cv::Mat imgSample = cv::imread(".\\oct_sample.png");

	//lumen
	cv::Mat testLumen = learning->FindLumen(imgSample);
	if (!testLumen.empty()) {
		cv::imshow("lumen", testLumen);
		cv::waitKey();
	}
	//sidebranch
	cv::Mat testSb = learning->FindSidebranch();
	if (!testSb.empty()) {
		cv::imshow("side_branch", testSb);
		cv::waitKey();
	}
	//stent
	std::vector<cv::Rect2f> testStent = learning->FindStent(imgSample);
	if (!testStent.empty()) {
		cv::Mat mask = imgSample.clone();

		for (auto& rect : testStent) {
			cv::Point point(rect.x + rect.width / 2, rect.y + rect.height / 2);
			cv::circle(mask, point, 1, cv::Scalar(255, 255, 255), 3);
		}

		if (testStent.size() > 0) {
			cv::imshow("stent", mask);
			cv::waitKey();
		}
	}
	//guidewire
	std::vector<cv::Rect2f> testGw = learning->FindGuidewire();
	if (!testGw.empty()) {
		cv::Mat mask = imgSample.clone();

		for (auto& rect : testGw) {
			cv::Point point(rect.x + rect.width / 2, rect.y + rect.height / 2);
			cv::circle(mask, point, 1, cv::Scalar(255, 255, 255), 3);
		}

		if (testGw.size() > 0) {
			cv::imshow("guide_wire", mask);
			cv::waitKey();
		}
	}

	PLOGI.printf("sample lumen detection done.");
#endif	

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
		case WM_UPDATE_RJ_STATE:
		{
			pSystem->OnMsgUpdateRJState(wParam, lParam);
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
	PLOGI.printf("threadSaveRaw");

	COCTSystem* pSystem = (COCTSystem*)param;
	tstring strSaveFilePath = pSystem->m_strFilePath;
	CImagingSession* pSession = pSystem->m_reviewSession[SESSION_REALTIME];
	PullbackLengthManager* pDataWriter = (PullbackLengthManager*)pSession->GetDataManager();
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

	PLOGI.printf("Save Start (%d frames)", nNumOfSamples);

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

	while (pSystem->m_pThreadSaveRaw->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("[DONE]threadSaveRaw");

	return NOERROR;
}

UINT COCTSystem::threadInitializeRotaryJunction(LPVOID param) {
	PLOGI.printf("threadInitializeRotaryJunction");

	COCTSystem* pSystem = (COCTSystem*)param;
	CRJController* pRJController = pSystem->m_pRJController;
	CConfiguration& config = CConfiguration::GetInstance();
	pRJController->SetModeOfOperation(MOTOR_DATA_MODE_VELOCITY);
	pRJController->SwitchOff();
	pRJController->SwitchOn();
	pRJController->SetManualMode(config.catheter.manualLoad);
	pRJController->Set(eStepMotorIndex::Both, STEP_MOTOR_SPEED_DEFAULT);

	while (pSystem->m_pThreadRotaryJunction->isRun && !pRJController->InitialStatusReceived()) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("photoSensor %d %d %d %d %d %d", pRJController->GetPhotoSensorOnOff(0), pRJController->GetPhotoSensorOnOff(1), pRJController->GetPhotoSensorOnOff(2)
		, pRJController->GetPhotoSensorOnOff(3), pRJController->GetPhotoSensorOnOff(4), pRJController->GetPhotoSensorOnOff(5));
	// SM (Hub) > Sensor #1
	if (pSystem->m_pThreadRotaryJunction->isRun && !pRJController->GetPhotoSensorOnOff(0)) {
		pRJController->Current(eStepMotorIndex::Hub, pRJController->ConvertMMtoStep(PULLBACK_MAX_DISTANCE));
		pRJController->Move(eStepMotorIndex::Hub, HUB_MOTOR_POS_INITIAL, false, 0x1 /* photo-sensor #1 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	pRJController->Current(eStepMotorIndex::Hub, HUB_MOTOR_POS_INITIAL);

	PLOGI.printf("photoSensor %d %d %d %d %d %d", pRJController->GetPhotoSensorOnOff(0), pRJController->GetPhotoSensorOnOff(1), pRJController->GetPhotoSensorOnOff(2)
		, pRJController->GetPhotoSensorOnOff(3), pRJController->GetPhotoSensorOnOff(4), pRJController->GetPhotoSensorOnOff(5));
	// SM (Pullback) > Sensor #4
	if (pSystem->m_pThreadRotaryJunction->isRun && (pRJController->GetPhotoSensorOnOff(1) || !pRJController->GetPhotoSensorOnOff(3))) {
		pRJController->Current(eStepMotorIndex::Pullback, 0);
		pRJController->Move(eStepMotorIndex::Pullback, pRJController->ConvertMMtoStep(PULLBACK_MAX_DISTANCE), false, 0x28 /* photo-sensor #4, #6 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}

	PLOGI.printf("photoSensor %d %d %d %d %d %d", pRJController->GetPhotoSensorOnOff(0), pRJController->GetPhotoSensorOnOff(1), pRJController->GetPhotoSensorOnOff(2)
		, pRJController->GetPhotoSensorOnOff(3), pRJController->GetPhotoSensorOnOff(4), pRJController->GetPhotoSensorOnOff(5));
	// SM (Pullback) > Sensor #2 > Sensor #4
	if (pSystem->m_pThreadRotaryJunction->isRun && pRJController->GetPhotoSensorOnOff(5)) {
		pRJController->Current(eStepMotorIndex::Pullback, pRJController->ConvertMMtoStep(PULLBACK_MAX_DISTANCE));
		pRJController->Move(eStepMotorIndex::Pullback, 0, false, 0x2 /* photo-sensor #2 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);

		pRJController->Current(eStepMotorIndex::Pullback, 0);
		pRJController->Move(eStepMotorIndex::Pullback, pRJController->ConvertMMtoStep(PULLBACK_MAX_DISTANCE), false, 0x8 /* photo-sensor #4 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	pRJController->Current(eStepMotorIndex::Pullback, config.stepMotor.unLoadDistance);

	if (pRJController->GetState() == eRJState::Initializing) {
		pRJController->UpdateState(eRJState::Disconnected);
	}

	pRJController->DisableStepMotors();

	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::InitializeRotaryJunction);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("[DONE]threadInitializeRotaryJunction");

	return NOERROR;
}

/*
* threadAutoCalibration
*/
UINT COCTSystem::threadAutoCalibration(LPVOID param) {
	PLOGI.printf("threadAutoCalibration");

	COCTSystem* pSystem = (COCTSystem*)param;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;
	CConfiguration& config = CConfiguration::GetInstance();
	int nTargetPos = 0;
	RayError autoCalibError = RayError::OK;

	if (pLaserModule != nullptr && pLaserModule->IsConnected())
	{
		// 0. Speed Up
		pLaserModule->Set(eStepMotorIndex::Both, CM_SM_SPEED_AUTO);
		pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_AUTO_1ST);

		// 1. First Scan to Find a Valid Range
		pSystem->m_cathState = CatheterState::FindingSheath;													// OCTSyestm mode setting
		pSystem->m_pImagingLiveView->SetAutoCalibrationMathod(AutoCalibrationMathod::FindingMinMagnitude);		// OCTImaging mode setting

		int startPosition = pLaserModule->GetPosition(eStepMotorIndex::DelayLine);
		pSystem->m_vCalibrationInfo.clear();
		nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, 5000);
		pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);
		std::vector<std::pair<int, int>> info = pSystem->m_vCalibrationInfo;

		// 2. Find Target Position Laxly
		std::vector<int> gradient(info.size() - 1);
		int minVal = INT_MAX, minIndex = 0, Loc = startPosition;
		int errorValThreshold = 10;		// 1차 확인에서 값이 너무 작게 나오는 경우를 걸러내기 위한 임계값

		// 2-1. Calculate gradient, Find minVal, maxVal
		for (int i = 1; i < info.size(); i++)
		{
			gradient[i - 1] = info[i].first - info[i - 1].first;
			if (info[i - 1].first < errorValThreshold) {
				if (i < 2)
					gradient[i - 1] = -1;
				else
					gradient[i - 1] = info[i].first - info[i - 2].first;
			}else if(info[i].first < errorValThreshold) {
				gradient[i - 1] = -1;
			}
			else if (info[i].first < minVal)
			{
				minVal = info[i].first;
				Loc = info[i].second;
				minIndex = i;
			}
		}

		// 2-2. Find a target position based on Gradient
		if (config.laserModule.autoCalibrationForSeverance == 0) {
			std::vector<std::pair<int, int>> minList; // pair<motor loc, index>
			for (int i = 0; i < gradient.size() - 1; i++)
			{
				if (gradient[i] < 0 && gradient[i + 1] >= 0 && gradient[i] != -1 && gradient[i + 1] != -1) // local min
				{
					if (minVal * 1.3 < info[i + 1].first) // 최솟값의 130% 이상인 값은 제외
						continue;
					minList.push_back({ info[i + 1].second, i + 1 });
					//PLOGI.printf("local min found. Loc : %d, Value : %d", info[i + 1].second, info[i + 1].first);
				}
			}
			std::sort(minList.begin(), minList.end(), [](const std::pair<int, int>& a, const std::pair<int, int>& b) {
				return a.first < b.first; // motor loc 기준 오름차순 정렬
				});

			std::vector<std::pair<int, int>> maxList; // pair<distLoc, index>
			std::pair<int, int> contingencyMax = { INT_MIN, 0 }; // pair<gradient difference, index>
			int minMaxDistRange = 1500; // local max가 local min과 너무 멀리 떨어져 있는 경우를 배제하기 위한 임계값
			for (int i = 1; i < gradient.size() - 1; i++)
			{
				int distLoc = abs(info[i + 1].second - Loc); // minLoc과의 거리
				if (gradient[i] >= 0 && gradient[i + 1] < 0 && gradient[i] != -1 && gradient[i + 1] != -1) // local max
				{
					maxList.push_back({ distLoc, i + 1 });
					//PLOGI.printf("local max found. Loc : %d, Value : %d", info[i + 1].second, info[i + 1].first);
				}
				if (abs(info[i + 1].second - Loc) > minMaxDistRange ||
					(gradient[i] > 20000000 && gradient[i + 1] > 20000000)) continue;
				if (gradient[i] - gradient[i + 1] > contingencyMax.first && gradient[i] > 0) // local max 후보군 중 가장 급격한 변화가 있는 위치 저장
					contingencyMax = { gradient[i] - gradient[i + 1], i + 1 };
			}
			if (contingencyMax.second > 0/*maxList.empty()*/) {
				maxList.push_back({ abs(info[contingencyMax.second].second - Loc), contingencyMax.second });
				//PLOGI.printf("local max contingency selected. Loc : %d, Value : %d", info[contingencyMax.second].second, info[contingencyMax.second].first);
			}
			std::sort(maxList.begin(), maxList.end(), [](const std::pair<int, int>& a, const std::pair<int, int>& b) {
				return a.first < b.first; // minLoc과의 거리 기준 오름차순 정렬
				});

			if (minList.empty()) {
				nTargetPos = startPosition;
				//PLOGI.printf("Calibration is failed.");
				autoCalibError = RayError::AutoCalibError;
			}
			else {
				for(int i = 0; i < minList.size(); i++)
				{
					if (abs(info[minList[i].second].second - Loc) < minMaxDistRange)
					{
						Loc = info[minList[i].second].second;
						//PLOGI.printf("Local min selected. Loc : %d, Value : %d", info[minList[i].second].second, info[minList[i].second].first);
						break;
					}
				}

				bool maxFound = false;
				int gradientThreshold = 100000000; // local max가 확실히 원하는 위치에 있는 경우를 위한 임계값
				for(int i = 0; i < maxList.size(); i++)
				{
					if(gradient[ maxList[i].second - 1 ] - gradient[maxList[i].second] > gradientThreshold)
					{
						Loc = info[maxList[i].second].second;
						maxFound = true;
						//PLOGI.printf("Local max selected. Loc : %d, Value : %d __1", info[maxList[i].second].second, info[maxList[i].second].first);
						break;
					}
				}

				if (!maxFound) {
					for (int i = 0; i < maxList.size(); i++)
					{
						if (info[maxList[i].second].second < Loc) continue;
						if (abs(info[maxList[i].second].second - Loc) < minMaxDistRange)
						{
							Loc = info[maxList[i].second].second;
							maxFound = true;
							//PLOGI.printf("Local max selected. Loc : %d, Value : %d__2", info[maxList[i].second].second, info[maxList[i].second].first);
							if (maxList.size() == 1)
								Loc -= 200; // 약간 더 안쪽으로 보정
							break;
						}
					}
				}
				if (!maxFound)
				{
					for (int i = 0; i < maxList.size(); i++)
					{
						if (abs(info[maxList[i].second].second - Loc) < minMaxDistRange)
						{
							Loc = info[maxList[i].second].second;
							maxFound = true;
							//PLOGI.printf("Local max selected. Loc : %d, Value : %d__3", info[maxList[i].second].second, info[maxList[i].second].first);
							break;
						}
					}
				}

				int valDist = 10000000;	// 조건에 맞는 local max가 없는 경우, local min 좌우의 값 차이가 유효할 정도로 큰지 확인하기 위한 임계값
				int adjustVal = 200;	// 조건에 맞는 local max가 없는 경우, local min에서 local max로 이동하기 위한 보정값
				if (!maxFound) {
					//PLOGI.printf("Cannot find Local max");
					int nowIndex = minList[0].second;
					if (abs(info[nowIndex - 2].first - info[nowIndex + 2].first) < valDist) {
						if (nowIndex - 2 >= 0 && nowIndex + 1 < gradient.size() &&
							abs(gradient[nowIndex - 2] - gradient[nowIndex - 1]) < abs(gradient[nowIndex] - gradient[nowIndex + 1])) {
							Loc += adjustVal;
							//PLOGI.printf("2 steps away frames are inValid, so we are using gradients: plus");
						}
						else {
							Loc -= adjustVal;
							//PLOGI.printf("2 steps away frames are inValid, so we are using gradients: minus");
						}
					}
					else {
						if (info[nowIndex - 2].first > info[nowIndex + 2].first) {
							Loc += adjustVal;
							//PLOGI.printf("2 steps away frames are valid, so we are using values: plus");
						}
						else {
							Loc -= adjustVal;
							//PLOGI.printf("2 steps away frames are valid, so we are using values: minus");
						}
					}
				}
			}
		}
		else {
			// for severance
			int maxLaplacian = INT_MIN;
			int maxIndex = 1; double avgGradient = 0.0;
			for (int i = 2; i < gradient.size(); i++) {
				if (gradient[i] == -1 || gradient[i - 1] == -1)
					continue;
				avgGradient += abs(gradient[i]);
				if (maxLaplacian < gradient[i - 1] - gradient[i]) {
					maxLaplacian = gradient[i - 1] - gradient[i];
					maxIndex = i;
				}
			}
			avgGradient /= (double)(gradient.size() - 2);
			//PLOGI.printf("avgGradient : %f, maxGradient : %d, diff : %d", avgGradient, abs(gradient[maxIndex]), abs(info[maxIndex].second - Loc));

			int maxGradientCheck = abs(gradient[maxIndex - 1]);
			if (maxGradientCheck < abs(gradient[maxIndex]))
				maxGradientCheck = abs(gradient[maxIndex]);
			/* 세브란스 장비와 일반 장비 둘 다 동일하게 적용하기 위해 사용했던 로직. 현재는 분기 처리로 대체
			if (abs(info[maxIndex].second - Loc) < 600 //±2 frame 정도의 step 차이
				|| abs(info[maxIndex].second - Loc) < 1500 && abs(gradient[maxIndex - 1]) > avgGradient * 3)
				Loc = info[maxIndex].second;
			else {
				// min 위치와 max 위치가 너무 멀리 떨어져 있는 경우 올바르지 않은 위치로 간주
				int valDist = 10000000; // min 좌우의 값 차이가 유효할 정도로 큰지 확인하기 위한 임계값
				int adjustVal = 200; // min loc에서 올바른 위치 이동하기 위한 보정값
				int nowIndex = minIndex;
				if (abs(info[nowIndex - 2].first - info[nowIndex + 2].first) < valDist) {
					if (nowIndex - 2 >= 0 && nowIndex + 1 < gradient.size() &&
						abs(gradient[nowIndex - 2] - gradient[nowIndex - 1]) > abs(gradient[nowIndex] - gradient[nowIndex + 1]))
						Loc += adjustVal;
					else
						Loc -= adjustVal;
				}
				else {
					if (info[nowIndex - 2].first > info[nowIndex + 2].first) {
						Loc += adjustVal;
					}
					else
						Loc -= adjustVal;
				}
			}
			*/
			Loc = info[maxIndex].second;
		}
		PLOGI.printf("first calibration. checkPosition : %d, checkValue : %d", Loc, minVal);

		// 3. Find a Perfect Sheath Position
		if(autoCalibError != RayError::AutoCalibError){
			// 3-1. Move to the target position found in 1st step
			int nJumpStep = 3200;			// 1차 탐색에서 정한 위치로부터, 2차 탐색을 위해 이동할 거리(step)
			int nSearchRange = 600;			// 2차 탐색 범위 
			int Loc2nd = Loc - nJumpStep;
			pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_MAX);
			pLaserModule->Move(eStepMotorIndex::DelayLine, Loc2nd);
			pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);

			// 3-2. Second Scan to Find Sheath Position
			pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_AUTO_2ND);
			pSystem->m_pImagingLiveView->SetAutoCalibrationMathod(AutoCalibrationMathod::FindingSheath);
			pSystem->m_vCalibrationInfo.clear();

			nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, nSearchRange);
			pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);
			pSystem->m_pImagingLiveView->SetAutoCalibrationMathod(AutoCalibrationMathod::Disable);

			// 3-3. Calculate Slope with Linear Regression
			int sumIndex = 0, sumRow = 0, sumMult = 0; double sumIndexSq = 0.0;
			int validCount = pSystem->m_vCalibrationInfo.size(), distanceThreshold = 60, rowThreshold = 398;
			for (int i = 0; i < pSystem->m_vCalibrationInfo.size(); i++) {
				if(i > 0 && pSystem->m_vCalibrationInfo.at(i).first - pSystem->m_vCalibrationInfo.at(i - 1).first > distanceThreshold
					|| pSystem->m_vCalibrationInfo.at(i).first > rowThreshold) {
					validCount--;
					continue;
				}
				sumIndex += i;
				sumRow += pSystem->m_vCalibrationInfo.at(i).first;
				sumMult += i * pSystem->m_vCalibrationInfo.at(i).first;
				sumIndexSq += (double)(i * i);
			}
			double numerator = (double)(validCount * sumMult) - (double)(sumIndex * sumRow);
			double denominator = (double)(validCount * sumIndexSq) - (double)(sumIndex * sumIndex);
			double slope = (denominator == 0.0) ? 0 : numerator / denominator;
			slope = (slope > 10) ? 4 : slope;
			//PLOGI.printf("sheath slope : %f", slope);

			// 3-4. Adjust sheath position with slope information
			int expectedRow = pSystem->m_vCalibrationInfo.at(0).first;
			int errorThresholdPlus = 15, errorThresholdMinus = 5; // 2차 탐색의 step별 row 이동 범위 threshold
			int minusMove = 35; // 외경을 내경으로 판단한 경우 보정값
			
			// plus direction check
			for(auto& val : pSystem->m_vCalibrationInfo)
			{
				int rowMoving = val.first - expectedRow;
				if (rowMoving > errorThresholdPlus) {
					if(slope == 0)
						val.first -= minusMove;
					else
						val.first = expectedRow + (int)slope;
				}
				expectedRow = val.first;
				//PLOGI.printf("find sheath at the first row %d", expectedRow);
			}
			// minus direction check
			for (int i = pSystem->m_vCalibrationInfo.size() - 1; i >= 0; i--) {
				int rowMoving = pSystem->m_vCalibrationInfo.at(i).first - expectedRow;
				if (rowMoving > errorThresholdMinus) {
					if(slope == 0)
						pSystem->m_vCalibrationInfo.at(i).first -= minusMove;
					else
						pSystem->m_vCalibrationInfo.at(i).first = expectedRow - (int)slope;
				}
				expectedRow = pSystem->m_vCalibrationInfo.at(i).first;
				//PLOGI.printf("find sheath at the second row %d", expectedRow);
			}
			
			// 3-5. Find the closest frame where the sheath position is near idealRow(180)
			int minDiff = INT_MAX;
			int closestIdx = pSystem->m_vCalibrationInfo.size() - 1;	// 내경이 row 180 위치에 가장 가까운 프레임 Index
			int idealRow = 180;		// 2차 진행 시에 내경이 위치해야 한다고 가정하는 이상적인 row 위치

			for (int i = pSystem->m_vCalibrationInfo.size() - 1; i >= 0; i--)
			{
				int nowRow = pSystem->m_vCalibrationInfo.at(i).first;
				if (abs(nowRow - idealRow) < minDiff)
				{
					closestIdx = i;
					minDiff = abs(nowRow - idealRow);
				}
				if(nowRow < idealRow)	// 내경이 이상적인 위치보다 더 이상 깊은 위치에 있는 경우는 탐색 종료
					break;
			}
			// 3-6. Adjust target position
			Loc2nd = pSystem->m_vCalibrationInfo.at(closestIdx).second;
			Loc2nd += (idealRow - pSystem->m_vCalibrationInfo.at(closestIdx).first) * 3; // 보정값 적용
			PLOGI.printf("second calibration. ZOffset Position : %d, row : %d, diff : %d", Loc2nd, pSystem->m_vCalibrationInfo.at(closestIdx).first, minDiff);

			int adjustMotorStep = 480; // 내경에서 외경까지의 거리 180 step + reflection 배제를 위해 움직였던 거리 300 step
			nTargetPos = Loc2nd - adjustMotorStep;
			//PLOGI.printf("Target Position : %d", nTargetPos);

			if (minDiff > 50) // 내경 위치가 너무 이상적인 위치에서 멀리 떨어져 있는 경우 보정 실패로 간주
				autoCalibError = RayError::AutoCalibError;
		}
		else {
			nTargetPos = startPosition;
		}

		// 4. Move to calibrated position
		pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_MAX);
		pLaserModule->Move(eStepMotorIndex::DelayLine, nTargetPos);
		pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);

		// 5. Final Check for Sheath Position
		pSystem->m_pImagingLiveView->SetAutoCalibrationMathod(AutoCalibrationMathod::CheckSheathPixelNum);
		pSystem->m_cathState = CatheterState::CheckSheath;
		pSystem->m_vCalibrationInfo.clear();
		while (pSystem->m_vCalibrationInfo.size() < 3) {
			Sleep(50);
		}
		auto const& sheathInfo = pSystem->m_vCalibrationInfo;
		int validSheathCount = 0, needAdjustCount = 0, idealRow = 25;
		float avgDiff = 0.0f;
		for(auto const& val : sheathInfo)
		{
			PLOGI.printf("sheath check - row : %d", val.first);
			int diffIdeal = val.first - idealRow;
			if (abs(diffIdeal) < 20) {
				validSheathCount++;
				if (diffIdeal > 5) {
					needAdjustCount++;
					avgDiff += diffIdeal;
				}
				else if (diffIdeal < -5) {
					needAdjustCount--;
					avgDiff += diffIdeal;
				}
				//PLOGI.printf("sheath check - true");
			}
		}
		if (validSheathCount > sheathInfo.size() / 2) {
			if (abs(needAdjustCount) > sheathInfo.size() / 2) {
				avgDiff /= abs(needAdjustCount);
				int adjustDir = (needAdjustCount > 0) ? -1 : 1;
				int adjustStep = avgDiff * adjustDir * 3; 
				PLOGI.printf("sheath adjustment - dir : %d, step : %d", needAdjustCount, adjustStep);
				nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, adjustStep);
				pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);
			}
			nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, -(pSystem->autoCalibrationFranch * 1.5));
			pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);
			autoCalibError = RayError::OK;
		}
		else
			autoCalibError = RayError::AutoCalibError;

		pSystem->m_pImagingLiveView->SetAutoCalibrationMathod(AutoCalibrationMathod::Disable);
		
#if 0
		// 2. Start Finding Peak
		pSystem->m_vCalibrationInfo.clear();
		pSystem->m_cathState = CatheterState::FindingPeak;

		// 2-1. Move Polarization-control & Find Peak
		nTargetPos = pLaserModule->Move(eStepMotorIndex::Polarization, 0);
		pSystem->waitForStepMotors(eStepMotorIndex::Polarization, pSystem->m_pThreadRotaryJunction->isRun);

		nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::Polarization, 3240);
		pSystem->waitForStepMotors(eStepMotorIndex::Polarization, pSystem->m_pThreadRotaryJunction->isRun);

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
		pLaserModule->Move(eStepMotorIndex::Polarization, nTargetPos);
		pSystem->waitForStepMotors(eStepMotorIndex::Polarization, pSystem->m_pThreadRotaryJunction->isRun);
#endif
		// 6. Default Speed
		pLaserModule->Set(eStepMotorIndex::Both, CM_SM_SPEED_DEFAULT);
	}

	pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Calibrated);
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::AutoCalibration);
	if (autoCalibError == RayError::AutoCalibError)
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::AutoCalibError);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("[DONE]threadAutoCalibration");

	return NOERROR;
}

/*
* threadPullbackScan
*/
UINT COCTSystem::threadPullbackScan(LPVOID param) {
	PLOGI.printf("threadPullbackScan");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;
	IImaging::Setting settingPullback = pSystem->m_pImagingPullback->GetSetting();
	int pullbackTime = ((double)config.stepMotor.pullbackDistance / (double)config.stepMotor.pullbackSpeed) * 1000;

	pullbackTime = (pullbackTime <= 0) ? config.stepMotor.noPullbackTime * 1000 : pullbackTime;
	PLOGI.printf("Pullback start - %dmm, %dmm/s - %dmsec", config.stepMotor.pullbackDistance, config.stepMotor.pullbackSpeed, pullbackTime);

	int pullbackType = pSystem->GetPullbackType(config.stepMotor.pullbackDistance, config.stepMotor.pullbackSpeed);
	PLOGI.printf("PullbackType = %d", pullbackType);

	// 1. Start Recording OCT
	CDataWriter* pDataWriter = new PullbackLengthManager();
	pDataWriter->Initialize(settingPullback.nBufferSize * sizeof(USHORT));
	size_t numAScans = static_cast<size_t>(settingPullback.nAScan);
	if (numAScans > std::numeric_limits<size_t>::max() / (2 * sizeof(int)))
	{
		PLOGI.printf("nAScan is too large.");
		delete pDataWriter;
		return ERROR;;
	}
	pDataWriter->AddExtraData(OCTHeader::ExtraData::Dispersion, pSystem->m_pImagingPullback->GetCalibrationData(), numAScans * 2 * sizeof(int));
	if (ImagingType::Default == ImagingType::LabImaging)
	{
		pDataWriter->AddExtraData(OCTHeader::ExtraData::Background, 
			((CLabImaging*)pSystem->m_pImagingPullback)->GetBackground(), settingPullback.nBufferSize * sizeof(USHORT));
	}
	pDataWriter->StartRecording();
	pSystem->m_pAcqDevice->SetWriter(pDataWriter);

	// 2. Pullback Linear Stage
	if (pRJController->IsConnected() && config.stepMotor.pullbackDistance > 0) {
		pRJController->changeSMProfileToPullback();
		pRJController->Move(eStepMotorIndex::Both, pRJController->ConvertMMtoStep(config.stepMotor.pullbackDistance), false);

		auto now = std::chrono::system_clock::now();
		auto duration = now.time_since_epoch();
		double seconds_since_epoch = std::chrono::duration_cast<std::chrono::seconds>(duration).count() +
			std::chrono::duration_cast<std::chrono::microseconds>(duration).count() / 1'000'000.0;
		pSystem->SetPullbackStartTime(seconds_since_epoch);

		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	else {
		Sleep(pullbackTime);
	}

	// 3. Stop Recording OCT
	pDataWriter->StopRecording();
	PLOGI.printf("Stop Acquisition");
	pSystem->m_pAcqDevice->StopAcquisition();
	PLOGI.printf("Set Writer null");
	pSystem->m_pAcqDevice->SetWriter(nullptr);
	PLOGI.printf("Before StopMotor");
	pSystem->postPriorMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::Recording);

	// 4. Motor OFF
	Sleep(500);
	pRJController->StopMotor();

	// 5. Homing

	BYTE* uidRFID = new BYTE[CUSTOM_UID_LENGTH + HARDWARE_UID_LENGTH];
	if(config.catheter.catheterRFID)
		pRJController->IncreaseRFIDUsage(pRJController->GetRFIDUID(uidRFID), uidRFID);

	pRJController->changeSMProfileToLoadUnload();
	Sleep(2000);
	int bldcHomingSpeed = config.bldcMotor.velocityLiveView / 2;
	pRJController->PerformRun(bldcHomingSpeed);
	pRJController->Set(eStepMotorIndex::Both, config.stepMotor.homingSpeed);
	pRJController->Move(eStepMotorIndex::Both, 0);
	pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	pRJController->StopMotor();
	pRJController->Current(eStepMotorIndex::Pullback, DISTANCE_BETWEEN_MOTORS);
	pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_STANDBY_OFF);
	PLOGI.printf("Pullback done.");

	if (auto* mgr = dynamic_cast<PullbackLengthManager*>(pDataWriter)) {
		PLOGI.printf("GetNumOfSamples() = %d", mgr->GetNumOfSamples());
		mgr->SetSMProfile(config.stepMotor.SMPullbackProfile);
		mgr->CutPullbackLength(pullbackType, config.bldcMotor.velocityPullback);
	}

	CImagingSession* pSession = CImagingSession::CreateSession(pSystem, SESSION_REVIEW, settingPullback, pDataWriter);

	if (pSession == nullptr) {
		return ERROR;
	}
	pSystem->postPriorMessage(WM_START_REVIEW_SESSION, SESSION_REVIEW, (LPARAM)pSession);
	pSystem->postPriorMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);
	pSystem->postPriorMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::Pullback);

	pSession->StartObjectDetection();

	// In case of Homing failed
	if (pRJController->GetPhotoSensorOnOff(0) == false) {
		PLOGI.printf("Pullback Homing failed.");
		pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_ERROR);
		pRJController->UpdateState(eRJState::Error);
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::HomingFailed);
	}

	PLOGI.printf("pRJController->GetCatheterUsage() = %d", pRJController->GetCatheterUsage());
	if(config.catheter.catheterRFID && pRJController->GetRFIDCountCurrentState()>= pRJController->GetCatheterUsage()){
		pRJController->UpdateState(eRJState::Error);
	}
    
	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("[DONE]threadPullbackScan");

	return NOERROR;
}

/*
* threadLoadCatheter
*/
UINT COCTSystem::threadLoadCatheter(LPVOID param) {
	PLOGI.printf("threadLoadCatheter");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;

	std::vector<std::vector<std::string>> loadCommands = pSystem->readLoadSequence();

	pSystem->postMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterLoading);

	if (pRJController->IsConnected()) {
		pRJController->changeSMProfileToLoadUnload();
		pRJController->Current(eStepMotorIndex::Pullback, config.stepMotor.unLoadDistance);
		PLOGI.printf("unLoadDistance = %d", config.stepMotor.unLoadDistance);

		for (const auto& commands : loadCommands) {
			if (commands.size() != 3) {
				PLOGI.printf("Invalid command format.");
				continue;
			}
			if (pSystem->m_pThreadRotaryJunction->isRun == false) break;

			std::string command = commands[0];
			std::transform(command.begin(), command.end(), command.begin(), ::toupper);

			if (command == "SM") {
				int position = std::stoi(commands[1]);
				int speed = std::stoi(commands[2]);

				pRJController->Set(eStepMotorIndex::Pullback, speed);
				pRJController->Move(eStepMotorIndex::Pullback, position);

				pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
			}
			else if (command == "BLDC") {
				int velocity = std::stoi(commands[1]);
				int delay = std::stoi(commands[2]);
				delay = std::max(0, delay); // 음수 방지

				pRJController->PerformRun(velocity);
				Sleep(delay);
			}
		}
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime());
	}

	if (pLaserModule != nullptr && pLaserModule->IsConnected()) {
		pLaserModule->SetVLD(config.laserModule.vldValue);
	}

	if (pSystem->m_pThreadRotaryJunction->isRun) {
		pSystem->m_bFirstLoad = true;
		pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::LoadCatheter);
		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Loading);
	}
	else {
		PLOGI.printf("CatheterNotValid");
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::CatheterNotValid);
	}

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("[DONE]threadLoadCatheter");

	return NOERROR;
}

/*
* threadUnloadCatheter
*/
UINT COCTSystem::threadUnloadCatheter(LPVOID param) {
	PLOGI.printf("threadUnloadCatheter");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;	

	pSystem->postPriorMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterUnloading);

	if (pLaserModule != nullptr && pLaserModule->IsConnected()) {
		pLaserModule->SetVLD(0);
	}

	if (pRJController->IsConnected()) {
		if (pRJController->IsRun()) {
			pRJController->StopMotor();
			Sleep(2000);
		}
		pRJController->changeSMProfileToLoadUnload();

		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pRJController->Move(eStepMotorIndex::Pullback, 20000, false, 0x08 /* photo-sensor #4 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);

		// in case of homing failed
		if (!pRJController->GetPhotoSensorOnOff(0))
		{
			PLOGI.printf("photoSensor %d %d %d %d %d %d", pRJController->GetPhotoSensorOnOff(0), pRJController->GetPhotoSensorOnOff(1), pRJController->GetPhotoSensorOnOff(2)
				, pRJController->GetPhotoSensorOnOff(3), pRJController->GetPhotoSensorOnOff(4), pRJController->GetPhotoSensorOnOff(5));
			// SM (Hub) > Sensor #1
			if (pSystem->m_pThreadRotaryJunction->isRun && !pRJController->GetPhotoSensorOnOff(0)) {
				pRJController->Current(eStepMotorIndex::Hub, pRJController->ConvertMMtoStep(PULLBACK_MAX_DISTANCE));
				pRJController->Move(eStepMotorIndex::Hub, HUB_MOTOR_POS_INITIAL, false, 0x1 /* photo-sensor #1 */);
				pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun, true);
			}
			
			pRJController->Current(eStepMotorIndex::Hub, HUB_MOTOR_POS_INITIAL);
		}
		PLOGI.printf("photoSensor %d %d %d %d %d %d", pRJController->GetPhotoSensorOnOff(0), pRJController->GetPhotoSensorOnOff(1), pRJController->GetPhotoSensorOnOff(2)
				, pRJController->GetPhotoSensorOnOff(3), pRJController->GetPhotoSensorOnOff(4), pRJController->GetPhotoSensorOnOff(5));
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime() / 2);
	}

	pSystem->postPriorMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Unloaded);
	pSystem->m_pRJController->UpdateState(eRJState::Unloaded);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	pRJController->DisableStepMotors();

	PLOGI.printf("[DONE]threadUnloadCatheter");

	return NOERROR;
}

void COCTSystem::autoCalibrationInit(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;
	int nTargetPos = 0;

	if (pLaserModule != nullptr && pLaserModule->IsConnected())
	{
		// 0. Speed Up
		pLaserModule->Set(eStepMotorIndex::Both, CM_SM_SPEED_AUTO);
		pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_AUTO);

		// 1. Start Finding Sheath
		pSystem->m_vCalibrationInfo.clear();
		pSystem->m_cathState = CatheterState::FindingSheath;

		// 1-1. Move Delay-line & Find Sheath
		nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, -1000);
		pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);

		nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, 2000);
		pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);

		// 1-2. Find Z-Offset Position
		const int nSheathPosition = CConfiguration::GetInstance().measurement.nSheathPosition;
		int nMinDiff = INT_MAX;
		int nZOffset = CConfiguration::GetInstance().laserModule.delayPosition;
		for (int i = 0; i < pSystem->m_vCalibrationInfo.size(); i++) {
			int nDiff = abs(nSheathPosition - pSystem->m_vCalibrationInfo.at(i).first);
			if (nMinDiff > nDiff) {
				nMinDiff = nDiff;
				nZOffset = pSystem->m_vCalibrationInfo.at(i).second;
				PLOGI.printf("nDiff: %d, Calibrated zOffset: %d", nDiff, nZOffset);
			}
		}

		Sleep(300);

		// 1-3. Move to calibrated position
		nTargetPos = nZOffset;
		pLaserModule->Move(eStepMotorIndex::DelayLine, nZOffset);
		pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);

		// 2. Start Finding Peak
		pSystem->m_vCalibrationInfo.clear();
		pSystem->m_cathState = CatheterState::FindingPeak;

		// 2-1. Move Polarization-control & Find Peak
		nTargetPos = pLaserModule->Move(eStepMotorIndex::Polarization, 0);
		pSystem->waitForStepMotors(eStepMotorIndex::Polarization, pSystem->m_pThreadRotaryJunction->isRun);

		nTargetPos = pLaserModule->MoveRelative(eStepMotorIndex::Polarization, 3240);
		pSystem->waitForStepMotors(eStepMotorIndex::Polarization, pSystem->m_pThreadRotaryJunction->isRun);

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
		pLaserModule->Move(eStepMotorIndex::Polarization, nTargetPos);
		pSystem->waitForStepMotors(eStepMotorIndex::Polarization, pSystem->m_pThreadRotaryJunction->isRun);
	}
}

void COCTSystem::loadAutoCalibPatch() {
	std::string patchPath = "res/matching/autoCalibPatch.tif";
	cv::Mat patch = cv::imread(patchPath, cv::IMREAD_GRAYSCALE);
	if(patch.empty()) {
		PLOGI.printf("Failed to load auto calibration patch image.");
		return;
	}
	else {
		if (patch.type() == CV_8UC1)
			patch.convertTo(m_autoCalibPatch, CV_32F, 1.0 / 255.0);
		else if (patch.type() == CV_32F)
			m_autoCalibPatch = patch;
		else {
			PLOGI.printf("Invalid auto calibration patch image format.");
			return;
		}
	}
}

/*
* threadValidateCatheter
*/
UINT COCTSystem::threadValidateCatheter(LPVOID param) {
	PLOGI.printf("threadValidateCatheter");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;
	
	bool verified = false;

	Sleep(2000);

	if (config.catheter.catheterValidationOnOff && pSystem->m_curState == RayScannerState::Default) {
		pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_STANDBY_ON);
		pSystem->m_pImagingLiveView->Stop();
		pSystem->m_pImagingLiveView->Start();
		pSystem->laserOnOff(true);
		pSystem->restartAcqDevice(pSystem->m_pImagingLiveView);
		pRJController->PerformRun(config.bldcMotor.velocityLiveView);

		Sleep(1000);

		//determine image verification
		for (int i = 0; i < 10; i++) {
			cv::Mat image = pSystem->m_pImagingLiveView->GetWithoutCompensationImage();

			cv::Mat sobel_x, sobel_y;
			cv::Sobel(image, sobel_x, CV_64F, 1, 0, 3, 1, 0, cv::BORDER_CONSTANT);

			cv::Mat sobel_vis;
			cv::convertScaleAbs(sobel_x, sobel_vis);

			cv::Scalar mean, stddev;
			cv::meanStdDev(sobel_vis, mean, stddev);
			verified = stddev[0] >= 30.0;
			PLOGI.printf("verified value = %.2f", stddev[0]);
			if (verified) break;
		}

		if (verified && config.catheter.catheterAutoCalibrationOnOff) {
			pSystem->autoCalibrationInit(param);
		}

		pRJController->StopMotor();
		pSystem->laserOnOff(false);
	}
	else {
		verified = true;
	}

	PLOGI.printf("postMessage - RayWorkItem::ValidateCatheter");
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::ValidateCatheter);

	if (verified) {
		if (config.catheter.manualLoad) {
			PLOGI.printf("m_pRJController->UpdateState - WaitManualLoad");
			pRJController->UpdateState(eRJState::WaitManualLoad);
		}
		else {
			PLOGI.printf("m_pRJController->UpdateState - Loaded");
			pRJController->UpdateState(eRJState::Loaded);
		}
		PLOGI.printf("postMessage - CatheterState::Enable");

		RFIDProtocol::SRFIDState rfidState;
		RFIDProtocol::getCurRFIDData(&rfidState);
		PLOGI.printf("pRJController->GetRFIDState().aStep = %d", rfidState.aStep);

		pLaserModule->ReadPosition();
		int position = pLaserModule->GetPosition(eStepMotorIndex::DelayLine);

		pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_MAX);
		pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, config.laserModule.delayPosition - position);

		Sleep(100);

		while (pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) {
			Sleep(50);
		}

		pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_MAX);
		pLaserModule->MoveRelative(eStepMotorIndex::DelayLine, rfidState.aStep);

		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Enable);
	}
	else {
		if (pLaserModule != nullptr && pLaserModule->IsConnected()) {
			pLaserModule->SetVLD(0);
		}
		pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_ERROR);
		pRJController->UpdateState(eRJState::Error);
		PLOGI.printf("CatheterNotValid");
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::CatheterNotValid);
	}

	PLOGI.printf("wait for StopThread");
	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}
	
	PLOGI.printf("[DONE]threadValidateCatheter");

	return NOERROR;
}

/*
* threadValidateCatheter
*/
UINT COCTSystem::threadManualLoadCatheter(LPVOID param){
	PLOGI.printf("threadManualLoadCatheter");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;

	pSystem->postMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterLoading);

	if (pRJController->IsConnected()) {
		pRJController->Current(eStepMotorIndex::Pullback, config.stepMotor.unLoadDistance);
		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pRJController->Move(eStepMotorIndex::Pullback, 0, false, 0x02 /* photo-sensor #2 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime());
	}

	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::LoadCatheter);
	pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Loading);

	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	PLOGI.printf("[DONE]threadManualLoadCatheter");

	return NOERROR;
}

/*
* threadCleanRotaryJunction
*/
UINT COCTSystem::threadCleanRotaryJunction(LPVOID param){
	PLOGI.printf("threadCleanRotaryJunction");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;

	if (pRJController->IsConnected()) {
		pRJController->Current(eStepMotorIndex::Pullback, config.stepMotor.unLoadDistance);
		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pRJController->Move(eStepMotorIndex::Pullback, 0, false, 0x02 /* photo-sensor #2 */);

		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	else {
		Sleep(3000);
	}

	PLOGI.printf("Clean rotary junction - wait for stop cleaning");
	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	if (pRJController->IsConnected()) {
		pRJController->StopStepMotors();
		pRJController->Move(eStepMotorIndex::Pullback, 20000, false, 0x08 /* photo-sensor #4 */);

		bool isHoming = true;
		pSystem->waitForStepMotors(isHoming);
	}
	else {
		Sleep(5000);
	}

	pSystem->controlRotaryJunction(eRJState::Disconnected);

	PLOGI.printf("[DONE]threadCleanRotaryJunction");

	return NOERROR;
}

/*
* RFIDValidating
*/
UINT COCTSystem::threadRFIDValidation(LPVOID param) {
	PLOGI.printf("threadRFIDValidation");

	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;
	RFID_ValidType isValid = RFID_ValidType::WAITING;
	while (isValid == WAITING) {
		isValid = pRJController->isValidRFID();
		Sleep(100);
		if (!pSystem->m_pThreadRotaryJunction->isRun) {
			return NOERROR;
		}
	}
	if (isValid == RFID_ValidType::VALID)
	{
		PLOGI.printf("validation true");
		pRJController->UpdateState(eRJState::Loading);
	}
	else if (isValid == RFID_ValidType::INVALID) {
		PLOGI.printf("validation false");
		pRJController->UpdateState(eRJState::RFIDError);
	}
	
	PLOGI.printf("[DONE]threadRFIDValidation");

	return NOERROR;
}

/*
* createColorImaging
*/
bool COCTSystem::checkConnection() {
	bool result = true;
	
	result &= m_pAcqDevice->IsInit();
	result &= m_pRJController->IsConnected();
	result &= m_pLaserModule->IsConnected();

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
	m_pImagingRealtime->SetPatchImage(m_autoCalibPatch);

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

	bool result = true;

	if (!m_pLaserModule->IsConnected()) {
		result = m_pLaserModule->Connect(config.laserModule.port);
		if (result) {
			m_pLaserModule->Set(eStepMotorIndex::Both, CM_SM_SPEED_DEFAULT);
			m_pLaserModule->SetVLD(0);
			m_pLaserModule->SetVOA(config.laserModule.voaValue);
#ifdef DELAY_LINE_HOMING_WORKS
			m_pLaserModule->Set(eStepMotorIndex::DelayLine, CM_SM_SPEED_MAX);
			m_pLaserModule->Current(eStepMotorIndex::DelayLine, DELAY_LINE_UPPER_END_POSITION);

			Sleep(500);

			PLOGI.printf("Homing start =========================================");
			// m_pLaserModule Move 0 OR sensor #1 이동
			m_pLaserModule->Move(eStepMotorIndex::DelayLine, 0, false, static_cast<char>(0x03));

			Sleep(500);

			while (m_pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) {
				Sleep(50);
			}
			m_pLaserModule->PrintPhotoSensor();

			// m_pLaserModule Current 0
			m_pLaserModule->Current(eStepMotorIndex::DelayLine, 0);

			Sleep(500);

			PLOGI.printf("Move to %d =========================================", config.laserModule.delayPosition);
			m_pLaserModule->Move(eStepMotorIndex::DelayLine, config.laserModule.delayPosition, false, static_cast<char>(0x02));

			Sleep(500);

			while (m_pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) {
				Sleep(50);
			}
			m_pLaserModule->PrintPhotoSensor();

			m_pLaserModule->Current(eStepMotorIndex::DelayLine, config.laserModule.delayPosition);
			m_pLaserModule->Move(eStepMotorIndex::Polarization, config.laserModule.polarPosition);
#endif
		}
		else
		{
			PLOGE.printf("Failed to connect to laser module");
		}
	}

	if (!m_pRJController->IsConnected()) {
		result &= m_pRJController->Connect(config.bldcMotor.port);

		if (result) {
			m_pRJController->StartControl();
			m_pRJController->UpdateState(eRJState::Initializing);
			PLOGI.printf("Success to connect to Rotary Junction");
		}
		else {
			PLOGI.printf("Failed to connect to Rotary Junction");
		}
	}

	return (result) ? NOERROR : E_FAIL;
}

/*
* disconnectRotaryJunction
*/
int COCTSystem::disconnectRotaryJunction() {
	bool result = true;

	PLOGI.printf("Laser Off");
	CLaserController* pLaser = CLaserController::GetInstance();
	if (pLaser == nullptr) {
		PLOGI.printf("pLaser is not initialized");
		return false;
	}

	pLaser->LaserOnOff(false);
	
	if (m_pRJController->IsConnected()) {
		result &= m_pRJController->StopMotor();
		result &= m_pRJController->SwitchOff();
	}

	PLOGI.printf("Catheter State : %d", m_cathState);
	if (m_cathState != CatheterState::Unloaded) {
		CUtility::StartThread(threadUnloadCatheter, m_pThreadRotaryJunction, this);
		while (m_pThreadRotaryJunction != nullptr)
		{
			Sleep(100);
		}
	}

	if (m_pLaserModule->IsConnected()) {
		CConfiguration& config = CConfiguration::GetInstance();
		m_pLaserModule->Move(eStepMotorIndex::Polarization, 0);
		m_pLaserModule->SetVLD(0);
		m_pLaserModule->SetVOA(0);

		Sleep(1000);

		bool run = true;
		waitForStepMotors(eStepMotorIndex::DelayLine, run);
		PLOGI.printf("======================Homing (Terminate)");
		m_pLaserModule->PrintPhotoSensor();
	}

	m_pRJController->Disconnect();
	m_pLaserModule->Disconnect();

	return (result) ? NOERROR : E_FAIL;
}

/*
* controlRotaryJunction
*/
int COCTSystem::controlRotaryJunction(eRJState state) {
	if (m_pRJController == nullptr || !m_pRJController->IsConnected()) return E_FAIL;

	m_pRJController->UpdateState(state);

	return NOERROR;
}

/*
* OnMsgProcessCrossSection
*/
LRESULT COCTSystem::OnMsgProcessCrossSection(WPARAM wParam, LPARAM lParam) {
	cv::Mat image;
	int nSession = wParam;
	int nFrameInfo = lParam;	// 0 if real time frame
	bool isRealTime = (nFrameInfo == 0);
	double isCleared = 0.0;

	if (m_curState == RayScannerState::Review) {
		if (isRealTime) return NOERROR;
		int nCurFrame = (nFrameInfo >> 16) & 0xFFFF;
		int nTotalFrame = (nFrameInfo & 0xFFFF);
		
		image = m_reviewSession[nSession]->PostProcess(nCurFrame);
	}
	else {
		if (isRealTime == false) return NOERROR;
		image = m_pImagingRealtime->GetCircleImage();

		if (m_bAutoPullbackOnOff) {
			//Lumen Detect
			int imgSize = 1024;
			cv::Point center(imgSize / 2, imgSize / 2);
			//center point mask
			cv::Mat centerMask = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
			cv::circle(centerMask, center, 1, cv::Scalar(255), cv::FILLED);
			cv::Ptr<cv::CLAHE> clahe = cv::createCLAHE(3.5, cv::Size(4, 4));
			
			std::vector<cv::Point> validContour = CImagingSession::GetValidLumenContour(m_pImagingRealtime->GetWithoutCompensationImage(), imgSize, centerMask, clahe, m_pImagingRealtime);

			if (!validContour.empty()) {
				isCleared = CImagingSession::IsLumenNormal(image, validContour, m_fLumenThresholdMin, m_fLumenThresholdMax, m_fLumenSrnThreshold, m_bShowLumenGuide);
			}
		}

		switch (m_cathState)
		{
		case CatheterState::FindingSheath:
		{
			int nSheathPosition = m_pImagingRealtime->GetSheathPosition();
			int nDelayLinePos = m_pLaserModule->GetPosition(eStepMotorIndex::DelayLine);
			m_vCalibrationInfo.push_back(std::make_pair(nSheathPosition, nDelayLinePos));
			PLOGI.printf("FindingSheath - %d, %d", nSheathPosition, nDelayLinePos);
			//PLOGI.printf("FindingSheath - %lf, %d", FFTscore, nDelayLinePos);
		}
			break;
		case CatheterState::CheckSheath: 
		{
			int pixelNum = m_pImagingRealtime->GetPixelNum();
			m_vCalibrationInfo.push_back(std::make_pair(pixelNum, 0));
		}
			break;
		case CatheterState::FindingPeak:
		{
			COCTMeasurement measurement;
			CConfiguration& config = CConfiguration::GetInstance();

			USHORT nPeakValue;
			int nPeakIndex, nLineWidth;
			measurement.CalculateAxialResolution(((CLabImaging *)m_pImagingRealtime)->GetScopeFFTData(), config.imaging.nOutputLength, config.measurement, nPeakValue, nPeakIndex, nLineWidth);

			int nPolarizationPos = m_pLaserModule->GetPosition(eStepMotorIndex::Polarization);
			m_vCalibrationInfo.push_back(std::make_pair(nPeakValue, nPolarizationPos));
			//PLOGI.printf("FindingPeak - %d, %d", nPeakValue, nPolarizationPos);
		}
			break;
		default:
			break;
		}
	}

	if (m_cbCrossSection != nullptr) m_cbCrossSection(nSession, image.data, image.cols, image.rows, image.channels(), nFrameInfo, isCleared);

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

	if (m_cbLongitude != nullptr) m_cbLongitude(nSession, imgCutView.data, imgCutView.cols, imgCutView.rows, imgCutView.channels(), nFrameInfo, 0.0);

	return NOERROR;
}
/*
* OnMsgProcessDetection
*/
LRESULT COCTSystem::OnMsgProcessDetection(WPARAM wParam, LPARAM lParam) {
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
	CLaserController* pLaser = CLaserController::GetInstance();

	if (pLaser == nullptr) {
		PLOGI.printf("Laser is not initialized");
		return;
	}

	pLaser->LaserOnOff(isOn);
}
bool COCTSystem::waitForStepMotors(bool& runFlag, bool log) {
	if (!m_pRJController->IsConnected()) return false;

	Sleep(100);
	while (m_pRJController->IsMoving() && runFlag) {
		if (log) {
			PLOGI.printf("photoSensor %d %d %d %d %d %d", m_pRJController->GetPhotoSensorOnOff(0), m_pRJController->GetPhotoSensorOnOff(1), m_pRJController->GetPhotoSensorOnOff(2)
				, m_pRJController->GetPhotoSensorOnOff(3), m_pRJController->GetPhotoSensorOnOff(4), m_pRJController->GetPhotoSensorOnOff(5));
		}
		Sleep(30);
	}

	return m_pRJController->IsMoving();
}
bool COCTSystem::waitForStepMotors(eStepMotorIndex idxMotor, bool& runFlag) {
	if (!m_pLaserModule->IsConnected()) return false;

	Sleep(100);
	while (m_pLaserModule->IsMoving(idxMotor) && runFlag) {
		Sleep(30);
	}

	return m_pLaserModule->IsMoving(idxMotor);
}
std::vector<std::vector<std::string>> COCTSystem::readLoadSequence()
{
	std::ifstream reader("./LoadSequence.txt");
	std::vector<std::vector<std::string>> loadCommands;

	if (reader.is_open()) {
		std::string line;
		while (std::getline(reader, line)) {
			std::vector<std::string> commands;
			std::stringstream ss(line);
			std::string token;

			while (std::getline(ss, token, ',')) {
				commands.push_back(token);
			}

			if (commands.size() == 3) {
				loadCommands.push_back(commands);
			}
		}
		reader.close();
	}
	else {
		std::cerr << "Cannot open LoadSequence.txt" << std::endl;
	}

	return loadCommands;
}

int COCTSystem::GetPullbackType(int pullbackDistance, int pullbackSpeed) {
	if (pullbackDistance <= 61) {
		if (pullbackSpeed <= 21) {
			return 0;
		}
		else if (pullbackSpeed <= 61) {
			return 2;
		}
		else {
			return 4;
		}
	}
	else {
		if (pullbackSpeed <= 41) {
			return 1;
		}
		else {
			return 3;
		}
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
	case CatheterState::Loading:
		PLOGI.printf("Catheter - Loaded.");
		CUtility::StartThread(threadValidateCatheter, m_pThreadRotaryJunction, this);
		break;
	case CatheterState::Enable:
		PLOGI.printf("Catheter - Enable.");
		postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::EnableCatheter);
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
* OnMsgUpdateRJState
*/
LRESULT COCTSystem::OnMsgUpdateRJState(WPARAM wParam, LPARAM lParam) {
	eRJState state = (eRJState)wParam;
	bool bStopThread = (bool)lParam;

	PLOGI.printf("RJState: %s", m_pRJController->GetStateString(state));
	if (bStopThread) CUtility::StopThread(m_pThreadRotaryJunction);

	switch (state) {
	case eRJState::None:
		break;
	case eRJState::Initializing:
		CUtility::StartThread(threadInitializeRotaryJunction, m_pThreadRotaryJunction, this);
		break;
	case eRJState::Disconnected:
		break;
	case eRJState::Cleaning:
		CUtility::StartThread(threadCleanRotaryJunction, m_pThreadRotaryJunction, this);
		break;
	case eRJState::Connected:
		break;
	case eRJState::Validating:
	{
		PLOGI.printf("RFID VALIDATION start");
		if (catheterRFID) {
			RFIDProtocol::initState(false);
			m_pRJController->ReadRFID();
			RFID_ValidType isValid = m_pRJController->isValidRFID();
			CUtility::StopThread(m_pThreadRotaryJunction);
			if (isValid == RFID_ValidType::VALID)
			{
				PLOGI.printf("validation true");
				m_pRJController->UpdateState(eRJState::Loading);
			}
			else if (isValid == RFID_ValidType::INVALID) {
				PLOGI.printf("validation false");
				m_pRJController->UpdateState(eRJState::RFIDError);
			}
			else {
				if (CUtility::StartThread(threadRFIDValidation, m_pThreadRotaryJunction, this)) {
				}
			}
		}
		else {
			RFIDProtocol::initState(false);
			m_pRJController->ReadRFID();
			m_pRJController->UpdateState(eRJState::Loading);
		}
		break;
	}
	case eRJState::Loading:
	{
		CConfiguration& config = CConfiguration::GetInstance();
		CUtility::StopThread(m_pThreadRotaryJunction);
		if (config.catheter.manualLoad) {
			CUtility::StartThread(threadManualLoadCatheter, m_pThreadRotaryJunction, this);
		}
		else {
			CUtility::StartThread(threadLoadCatheter, m_pThreadRotaryJunction, this);
		}
		break;
	}
	case eRJState::WaitManualLoad:
		break;
	case eRJState::Loaded:
		break;
	case eRJState::Unloading:
	{
		CUtility::StartThread(threadUnloadCatheter, m_pThreadRotaryJunction, this);
		break;
	}
	case eRJState::Unloaded:
		break;
	case eRJState::Error:
		if (m_pThreadRotaryJunction != nullptr) m_pThreadRotaryJunction->isRun = false;
		if (m_pLaserModule != nullptr) m_pLaserModule->SetVLD(0);
		laserOnOff(false);

		PLOGI.printf("RotaryJunctionError");
		postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::RotaryJunctionError);
		break;
	case eRJState::RFIDError:
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
	RayWorkItem item = (RayWorkItem)wParam;

	if (item != RayWorkItem::Recording) {
		PLOGI.printf("CUtility::StopThread(m_pThreadRotaryJunction)");
		CUtility::StopThread(m_pThreadRotaryJunction);
	}

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