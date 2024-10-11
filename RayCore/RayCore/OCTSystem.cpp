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
		result |= connectRotaryJunction();
		PLOGI.printf("connect Rotary Junction and Laser Module - %s", ((result == NOERROR) ? "Succeed" : "Failed"));

		// Connect to COM Interface first time
		CLaserController* pLaser = CLaserController::GetInstance();
		pLaser->LaserOnOff(true);

		result |= connectAcqDevice();
		PLOGI.printf("connect DAQ - %s", ((result == NOERROR) ? "Succeed" : "Failed"));

		pLaser->LaserOnOff(false);

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
		if (m_pLaserModule->IsConnected() == false) return RayError::DeviceNotConnected;
		if (m_pLaserModule->IsMoving(eStepMotorIndex::DelayLine)) return RayError::DeviceBusy;

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
					cv::Size axes(45, 45);
					cv::Scalar color(0, 0, 0);
					cv::ellipse(imgOCT, center, axes, 0, 0, 360, color, -1/*»ö»ó Ã¤¿ì±â = -1*/);
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
	return (config.measurement.fAxialResolutionScale / 1000.f) * 2;	// Convert polar scale to cartesian scale (mm)
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
	
	if (value == 0.0) {
		config.measurement.fSheathRadius = config.measurement.fSheathRadiusOnePointSix;		
	}
	else {
		config.measurement.fSheathRadius = config.measurement.fSheathRadiusTwoPointSix;
	}
	config.measurement.nSheathPosition = config.measurement.fSheathRadius * 1000.f / config.measurement.fAxialResolutionScale;

	return RayError::OK;
}

/*
* GetImageThreshold
*/
double COCTSystem::GetImageThreshold()
{
	return m_fImageThreshold;
}

/*
* SetImageThreshold
*/
RayError COCTSystem::SetImageThreshold(double value)
{
	m_fImageThreshold = value;

	return RayError::OK;
}

/*
* GetImageRoi
*/
double COCTSystem::GetImageRoi()
{
	return m_fImageRoi;
}

/*
* SetImageRoi
*/
RayError COCTSystem::SetImageRoi(double value)
{
	m_fImageRoi = value;

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
	lut.Load("LUT_abbott.csv");
	lut.Load("LUT_enhanced.csv");
	//lut.Load("LUT_ML.csv");

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
	PLOGI.printf("Save done.\n");

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

	if (pLaserModule != nullptr && pLaserModule->IsConnected())
	{
		// 0. Speed Up
		pLaserModule->Set(eStepMotorIndex::Both, CALIBRATION_MODULE_SM_DEFAULT_SPEED * 5);

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
		int nZOffset = 0;
		for (int i = 0; i < pSystem->m_vCalibrationInfo.size(); i++) {
			int nDiff = abs(nSheathPosition - pSystem->m_vCalibrationInfo.at(i).first);
			if (nMinDiff > nDiff) {
				nMinDiff = nDiff;
				nZOffset = pSystem->m_vCalibrationInfo.at(i).second;
			}
		}
		PLOGI.printf("Calibrated zOffset: %d", nZOffset);

		// 1-3. Move to calibrated position
		nTargetPos = nZOffset;
		pLaserModule->Move(eStepMotorIndex::DelayLine, nZOffset);
		pSystem->waitForStepMotors(eStepMotorIndex::DelayLine, pSystem->m_pThreadRotaryJunction->isRun);

#if 1
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
		// 3. Default Speed
		pLaserModule->Set(eStepMotorIndex::Both, CALIBRATION_MODULE_SM_DEFAULT_SPEED);
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
	CRJController* pRJController = pSystem->m_pRJController;
	IImaging::Setting settingPullback = pSystem->m_pImagingPullback->GetSetting();
	int pullbackTime = ((double)config.stepMotor.pullbackDistance / (double)config.stepMotor.pullbackSpeed) * 1000;

	pullbackTime = (pullbackTime <= 0) ? 3000 : pullbackTime;
	PLOGI.printf("Pullback start - %dmm, %dmm/s - %dmsec", config.stepMotor.pullbackDistance, config.stepMotor.pullbackSpeed, pullbackTime);

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
	if (pRJController->IsConnected() && config.stepMotor.pullbackDistance > 0) {
		pRJController->Move(eStepMotorIndex::Both, pRJController->ConvertMMtoStep(config.stepMotor.pullbackDistance), false);
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
	Sleep(2000);
	pRJController->Set(eStepMotorIndex::Both, STEP_MOTOR_SPEED_DEFAULT);
	pRJController->Move(eStepMotorIndex::Both, 0);
	pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	pRJController->Current(eStepMotorIndex::Pullback, DISTANCE_BETWEEN_MOTORS);
	pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_STANDBY_OFF);

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
	CRJController* pRJController = pSystem->m_pRJController;

	pSystem->postMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterLoading);

	if (pRJController->IsConnected()) {
		pRJController->Current(eStepMotorIndex::Pullback, PULLBACK_MOTOR_POS_INITIAL);

		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pRJController->Move(eStepMotorIndex::Pullback, 9000);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);

		pRJController->PerformRun(config.bldcMotor.velocityLoad);

		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_LOAD);
		pRJController->Move(eStepMotorIndex::Pullback, PULLBACK_MOTOR_POS_LOAD);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);

		pRJController->StopMotor();

		pRJController->Set(eStepMotorIndex::Pullback, 30000);
		pRJController->Move(eStepMotorIndex::Pullback, 1400);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);

		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_LOAD);
		pRJController->Move(eStepMotorIndex::Pullback, DISTANCE_BETWEEN_MOTORS);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime());
	}

	// To-Do: Check Catheter Connection
	bool loaded = true;
	if (loaded) {
		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Loaded);
	}
	else {
		pRJController->StopMotor();
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
	CRJController* pRJController = pSystem->m_pRJController;
	CLaserModule* pLaserModule = pSystem->m_pLaserModule;

	PLOGI.printf("Unload catheter");

	pSystem->postPriorMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterUnloading);

	if (pLaserModule != nullptr && pLaserModule->IsConnected()) {
		pLaserModule->SetVLD(0);
	}

	if (pRJController->IsConnected()) {
		if (pRJController->IsRun()) {
			pRJController->StopMotor();
			Sleep(1000);
		}
		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pRJController->Move(eStepMotorIndex::Pullback, 20000, false, 0x08 /* photo-sensor #4 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
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

	PLOGI.printf("Unload catheter done.");

	return NOERROR;
}

/*
* threadValidateCatheter
*/
UINT COCTSystem::threadValidateCatheter(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;

	PLOGI.printf("Catheter Validation");
	Sleep(1500);
#if 0
	pSystem->m_pRJController->DisplayLCD(eLCDImage::LCD_IMAGE_STANDBY_ON);
	pSystem->laserOnOff(true);
	pSystem->restartAcqDevice(pSystem->m_pImagingLiveView);
	pRJController->PerformRun(config.bldcMotor.velocityLiveView);

	// To-Do: determine image verification
	Sleep(2000);
	bool verified = true;

	pRJController->StopMotor();
	pSystem->laserOnOff(false);
#endif
	if (true) {
		if (config.catheter.manualLoad) {
			PLOGI.printf("m_pRJController->UpdateState - WaitManualLoad");
			pSystem->m_pRJController->UpdateState(eRJState::WaitManualLoad);
		}
		else {
			PLOGI.printf("m_pRJController->UpdateState - Loaded");
			pSystem->m_pRJController->UpdateState(eRJState::Loaded);
		}
		PLOGI.printf("postMessage - CatheterState::Enable");
		pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Enable);
	}
	else {
		pSystem->m_pRJController->UpdateState(eRJState::Error);
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)RayError::CatheterNotValid);
	}

	PLOGI.printf("postMessage - RayWorkItem::ValidateCatheter");
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::ValidateCatheter);

	PLOGI.printf("wait for StopThread");
	while (pSystem->m_pThreadRotaryJunction->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}
	PLOGI.printf("threadValidateCatheter Done");

	return NOERROR;
}

/*
* threadValidateCatheter
*/
UINT COCTSystem::threadManualLoadCatheter(LPVOID param)
{
	COCTSystem* pSystem = (COCTSystem*)param;
	CConfiguration& config = CConfiguration::GetInstance();
	CRJController* pRJController = pSystem->m_pRJController;

	pSystem->postMessage(WM_NOTIFY_EVENT_OCCURED, (WPARAM)RayEvent::CatheterLoading);

	if (pRJController->IsConnected()) {
		pRJController->Current(eStepMotorIndex::Pullback, PULLBACK_MOTOR_POS_INITIAL);
		pRJController->Set(eStepMotorIndex::Pullback, STEP_MOTOR_SPEED_DEFAULT);
		pRJController->Move(eStepMotorIndex::Pullback, 0, false, 0x02 /* photo-sensor #2 */);
		pSystem->waitForStepMotors(pSystem->m_pThreadRotaryJunction->isRun);
	}
	else if (pSystem->m_isTestMode)
	{
		Sleep(config.GetLoadCatheterTime());
	}

	pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Loaded);
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::LoadCatheter);

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

	if (!m_pRJController->IsConnected()) {
		result &= m_pRJController->Connect(config.bldcMotor.port);
		result &= m_pRJController->SetModeOfOperation(MOTOR_DATA_MODE_VELOCITY);
		result &= m_pRJController->SwitchOff();
		result &= m_pRJController->SwitchOn();
		result &= m_pRJController->Current(eStepMotorIndex::Pullback, PULLBACK_MOTOR_POS_INITIAL);
		result &= m_pRJController->Current(eStepMotorIndex::Hub, HUB_MOTOR_POS_INITIAL);

		if (!result) PLOGI.printf("Failed to connect to Rotary Junction");
		m_pRJController->SetManualMode(config.catheter.manualLoad);
	}

	if (!m_pLaserModule->IsConnected()) {
		result = m_pLaserModule->Connect(config.laserModule.port);
		if (result) {
			m_pLaserModule->Set(eStepMotorIndex::Both, CALIBRATION_MODULE_SM_DEFAULT_SPEED);
			m_pLaserModule->SetVLD(0);
			Sleep(500);
			m_pLaserModule->SetVOA(config.laserModule.voaValue);
#ifdef DELAY_LINE_HOMING_WORKS
			Sleep(500);
			m_pLaserModule->Move(MotorIndex::DelayLine, config.laserModule.delayPosition);
			while (m_pLaserModule->IsMoving(MotorIndex::DelayLine)) {
				Sleep(10);
			}
			Sleep(500);
			m_pLaserModule->Move(MotorIndex::Polarization, config.laserModule.polarPosition);
#endif
		}
		else
		{
			PLOGE.printf("Failed to connect to laser module");
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
		//m_pLaserModule->Home(-100000, 10000);
		m_pLaserModule->SetVLD(0);
		m_pLaserModule->SetVOA(0);
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
	double intensity = 0.0;

	if (m_curState == RayScannerState::Review) {
		if (isRealTime) return NOERROR;
		int nCurFrame = (nFrameInfo >> 16) & 0xFFFF;
		int nTotalFrame = (nFrameInfo & 0xFFFF);
		
		image = m_reviewSession[nSession]->PostProcess(nCurFrame);
	}
	else {
		if (isRealTime == false) return NOERROR;

		image = m_pImagingRealtime->GetCircleImage();

		//calculate intensity - m_fImageThreshold/m_fImageRoi
		calculateIntensity(image);
		for (int i = 0; i < 4; i++) {
			intensity += m_fCurrentIntensity[i];
		}
		intensity /= 4.f;

		switch (m_cathState)
		{
		case CatheterState::FindingSheath:
		{
			int nSheathPosition = m_pImagingRealtime->GetSheathPosition();
			int nDelayLinePos = m_pLaserModule->GetPosition(eStepMotorIndex::DelayLine);
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

			int nPolarizationPos = m_pLaserModule->GetPosition(eStepMotorIndex::Polarization);
			m_vCalibrationInfo.push_back(std::make_pair(nPeakValue, nPolarizationPos));
			PLOGI.printf("FindingPeak - %d, %d", nPeakValue, nPolarizationPos);
		}
			break;
		default:
			break;
		}
	}

	if (m_cbCrossSection != nullptr) m_cbCrossSection(nSession, image.data, image.cols, image.rows, image.channels(), nFrameInfo, intensity);

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
}
bool COCTSystem::waitForStepMotors(bool& runFlag) {
	if (!m_pRJController->IsConnected()) return false;

	Sleep(100);
	while (m_pRJController->IsMoving() && runFlag) {
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
void COCTSystem::calculateIntensity(cv::Mat image) {
	CConfiguration& config = CConfiguration::GetInstance();
	double sheathRadius = config.measurement.fSheathRadius * 2;
	double resolution = (config.measurement.fAxialResolutionScale / 1000.f) * 2;
	double radius = sheathRadius / resolution;	// sheath radius as pixel scale

	cv::Mat imgGray, imgRoi;
	cv::cvtColor(image, imgGray, cv::COLOR_BGR2GRAY);

	// find circle (sheath)
	std::vector<cv::Vec3f> circles;
	circles.push_back(cv::Vec3f(imgGray.cols / 2, imgGray.rows / 2, radius));
	//cv::HoughCircles(imgGray, circles, cv::HOUGH_GRADIENT, 2, imgGray.rows / 4, 200, 100, 10, 50);	

	// make ROI
	cv::Mat imgMask = cv::Mat::zeros(imgGray.rows, imgGray.cols, CV_8UC1);
	cv::Point center;
	int roiSize = 0;
	if (circles.size() > 0)
	{
		cv::Vec3f c = circles[0];
		center.x = c[0];
		center.y = c[1];
		radius = c[2];
		roiSize = radius * m_fImageRoi;

		circle(imgMask, center, roiSize, cv::Scalar(255, 255, 255), -1);
		circle(imgMask, center, radius, cv::Scalar(0, 0, 0), -1);
	}
	cv::copyTo(imgGray, imgRoi, imgMask);

	// divide quadrants & calculate intensity
	cv::Rect quadrants[4];
	quadrants[0].x = center.x - roiSize;
	quadrants[0].y = center.y - roiSize;
	quadrants[1].x = center.x;
	quadrants[1].y = center.y - roiSize;
	quadrants[2].x = center.x - roiSize;
	quadrants[2].y = center.y;
	quadrants[3].x = center.x;
	quadrants[3].y = center.y;

	for (int i = 0; i < 4; i++) {
		quadrants[i].width = roiSize;
		quadrants[i].height = roiSize;

		cv::Mat quad = imgRoi(quadrants[i]);
		m_fCurrentIntensity[i] = cv::mean(quad).val[0];
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
		m_pRJController->StartControl();
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
* OnMsgUpdateRJState
*/
LRESULT COCTSystem::OnMsgUpdateRJState(WPARAM wParam, LPARAM lParam) {
	eRJState state = (eRJState)wParam;

	PLOGI.printf("RJState: %d", state);
	switch (state) {
	case eRJState::Disconnected:
		break;
	case eRJState::Connected:
		break;
	case eRJState::Validating:
	{
		BYTE RFIDInfo[MAX_PATH];
		UINT nRFIDLength = m_pRJController->GetRFIDInfo(RFIDInfo);

#if ENABLE_RFID
		if (nRFIDLength != 0) 
#endif
		{
			// To-Do: Validation
			bool isValid = true;
			
			if (isValid) {
				m_pRJController->UpdateState(eRJState::Loading);
			}
			else {
				m_pRJController->UpdateState(eRJState::Error);
			}
		}
		break;
	}
	case eRJState::Loading:
	{
		CConfiguration &config = CConfiguration::GetInstance();
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
		if (m_pLaserModule != nullptr && m_pLaserModule->IsConnected()) {
			CConfiguration& config = CConfiguration::GetInstance();
			m_pLaserModule->SetVLD(config.laserModule.vldValue);
		}
		break;
	case eRJState::Unloading:
	{
		CUtility::StartThread(threadUnloadCatheter, m_pThreadRotaryJunction, this);
		break;
	}
	case eRJState::Unloaded:
		break;
	case eRJState::Error:
		laserOnOff(false);
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