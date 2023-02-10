#include "Config.h"
#include "OCTSystem.h"
#include "Utility.h"
#include "Configuration.h"
#include "OCTImaging.h"
#include "DataWriter.h"
#include "CutViewManager.h"
#include "VolumeGenerator.h"
#include "ATSDevice.h"
#include "SimulateDevice.h"
#include "LaserController.h"
#include "MotorController.h"
#include "ZaberController.h"
#include "RayLearning.h"
#include "ImagingSession.h"

/*
* COCTSystem
*/
COCTSystem::COCTSystem() {
	m_callback = nullptr;
	m_cbCrossSection = nullptr;
	m_cbLongitude = nullptr;

	m_pThreadService = nullptr;
	m_pThreadSaveRaw = nullptr;
	m_pThreadGenerateVolume = nullptr;
	m_pThreadLumenDetection = nullptr;
	m_pThreadRotaryJunction = nullptr;

	m_pImagingRealtime = nullptr;

	m_pDataWriter = nullptr;

	m_pVolume = nullptr;

	m_pAcqDevice = nullptr;	
	m_pLearning = nullptr;

	for (int i = 0; i < MAX_SESSION_NUM; i++) {
		m_reviewSession[i] = nullptr;
	}
	m_openedSession = nullptr;

	m_prevState = RayScannerState::Initial;
	m_curState = RayScannerState::Initial;
	m_cathState = CatheterState::Unloaded;

	//Property
	m_fBrightness = 0.0f;
	m_fContrast = 0.5f;
	m_fDegree = 90;
	m_fLowLevel = 108.f;
	m_fHighLevel = 109.f;
}

/*
* ~COCTSystem
*/
COCTSystem::~COCTSystem() {
	Stop();
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

	CUtility::StartThread(threadService, m_pThreadService, this);

	m_pImagingRealtime = CImagingSession::CreateColorImaging(this);
	m_pImagingRealtime->SetSession(SESSION_REVIEW);
	m_pImagingRealtime->Start();

	m_pVolume = new CVolumeGenerator();
	m_pVolume->Initialize(config.nCircleSize, config.nCircleSize, config.volume.size, config.volume.size);

	m_pAcqDevice = new CATSDevice();
	m_pAcqDevice->SetImaging(m_pImagingRealtime);

	return RayError::OK;
}

/*
* Stop
*/
RayError COCTSystem::Stop() {
	CUtility::StopThread(m_pThreadService);
	CUtility::StopThread(m_pThreadSaveRaw);
	CUtility::StopThread(m_pThreadGenerateVolume);
	CUtility::StopThread(m_pThreadLumenDetection);
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
	if (m_pImagingRealtime != nullptr) {
		m_pImagingRealtime->Stop();
		delete m_pImagingRealtime;
		m_pImagingRealtime = nullptr;
	}
	if (m_pDataWriter != nullptr) {
		delete m_pDataWriter;
		m_pDataWriter = nullptr;
	}
	if (m_pVolume != nullptr) {
		delete m_pVolume;
		m_pVolume = nullptr;
	}
	if (m_pLearning != nullptr) {
		delete m_pLearning;
		m_pLearning = nullptr;
	}

	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	pMotor->StopMotor();
	pMotor->SwitchOff();
	pMotor->Disconnect();

	pLinearStage->Close();

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
	// disconnectAcqDevice();
	// disconnectRotaryJunction();

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
		CZaberController* pDelayLine = CZaberController::GetInstance(ZABER_TYPE_DELAYLINE);

		if (pDelayLine->IsOpen() == false) return RayError::DeviceNotConnected;

		pDelayLine->RotateRelative((forward ? DELAYLINE_FORWARD_POSITION : DELAYLINE_BACKWARD_POSITION));

		return RayError::OK;
	}
}

/*
* ShowCalibrationGuide
*/
RayError COCTSystem::ShowCalibrationGuide(bool show) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	m_pImagingRealtime->ShowCalibGuide(show);

	return RayError::OK;
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
*/
RayError COCTSystem::StartReview(char* strFilePath) {
	if (m_curState == RayScannerState::Initial || m_curState == RayScannerState::Default) {
		CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_REVIEW, strFilePath);
		if (pSession == nullptr) {
			return RayError::InvalidArgument;
		}

		postMessage(WM_START_REVIEW_SESSION, SESSION_REVIEW, (LPARAM)pSession);
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);
	
		return RayError::OK;
	}

	return RayError::WrongState;
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

	postMessage(WM_START_REVIEW_SESSION, SESSION_COMPARE, (LPARAM)pSession);

	return RayError::OK;
}

/*
* EndReview
*/
RayError COCTSystem::EndReview()
{
	if (m_curState == RayScannerState::Review) {
		stopAllSessions();

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

		pLaser->LaserOnOff(true);
		pMotorCtrl->PerfomRun(config.motor.velocityLiveView);

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

		pLaser->LaserOnOff(false);
		pMotorCtrl->StopMotor();

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* PlayPause
*/
RayError COCTSystem::PlayPause()
{
	if (m_curState == RayScannerState::Review) {
		bool isPaused = GetIsPaused();
		m_reviewSession[SESSION_REVIEW]->SetPause(!isPaused);

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* PrevFrame
*/
RayError COCTSystem::PrevFrame()
{
	if (m_curState == RayScannerState::Review) {
		if (!GetIsPaused())
			return RayError::NotPaused;

		m_reviewSession[SESSION_REVIEW]->PrevFrame();

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* NextFrame
*/
RayError COCTSystem::NextFrame()
{
	if (m_curState == RayScannerState::Review) {
		if (!GetIsPaused())
			return RayError::NotPaused;

		m_reviewSession[SESSION_REVIEW]->NextFrame();

		return RayError::OK;
	}
	return RayError::WrongState;
}

/*
* NextFrame
*/
RayError COCTSystem::MoveToFrame(int nFrame) {
	if (m_curState == RayScannerState::Review) {
		if (!GetIsPaused())
			return RayError::NotPaused;

		m_reviewSession[SESSION_REVIEW]->MoveToFrame(nFrame);

		return RayError::OK;
	}
	return RayError::WrongState;
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
* GetVolumeData
*/
void* COCTSystem::GetVolumeData() {
	if (m_pVolume == nullptr) return nullptr;

	return m_pVolume->GetVolumeData();
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

	pSession->EnableCutView(cv::Scalar(0x00, 0x00, 0x00));
	m_openedSession = pSession;

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
	if (m_openedSession == nullptr) return nullptr;

	return m_openedSession->GetImageData(nFrame);
}

/*
* GetLongitudeData
*/
void* COCTSystem::GetLongitudeData(double fDegree) {
	if (m_openedSession == nullptr) return nullptr;

	CCutViewManager* pCutView = m_openedSession->GetCutView();
	if (pCutView == nullptr) return nullptr;

	pCutView->GenerateCutView(fDegree);
	cv::Mat imgLongitude = pCutView->DrawLongitudeImage(pCutView->GetNumOfSamples());

	return imgLongitude.data;
}


/*
* GetBrightness
*/
double COCTSystem::GetBrightness() {
	return m_fBrightness;
}

/*
* SetBrightness
*/
RayError COCTSystem::SetBrightness(double value) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	m_fBrightness = value;

	m_pImagingRealtime->SetBrightnessContrast(m_fBrightness, m_fContrast);
	
	return RayError::OK;
}

/*
* GetContrast
*/
double COCTSystem::GetContrast() {
	return m_fContrast;
}

/*
* SetContrast
*/
RayError COCTSystem::SetContrast(double value) {
	if (m_pThreadService == nullptr) return RayError::SystemNotRunning;

	m_fContrast = value;

	m_pImagingRealtime->SetBrightnessContrast(m_fBrightness, m_fContrast);

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

	if (m_reviewSession[SESSION_REVIEW] != nullptr) {
		CCutViewManager *pCutView = m_reviewSession[SESSION_REVIEW]->GetCutView();
		if (pCutView != nullptr) {
			int nFrames = pCutView->GetNumOfGeneratedSamples();
			if (nFrames > 0) {
				this->postMessage(WM_PROCESS_CUTVIEW, SESSION_REVIEW, nFrames);
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
* GetIsPaused
*/
bool COCTSystem::GetIsPaused()
{
	if (m_reviewSession[SESSION_REVIEW] == nullptr) return true;	// default state is paused

	return m_reviewSession[SESSION_REVIEW]->IsPaused();
}

/*
* GetImageWidth
*/
UINT COCTSystem::GetImageWidth() 
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetImageWidth();
}

/*
* GetImageHeight
*/
UINT COCTSystem::GetImageHeight() 
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetImageHeight();
}

/*
* GetImageChannels
*/
UINT COCTSystem::GetImageChannels() 
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetImageChannels();
}

/*
* GetImageDepth
*/
UINT COCTSystem::GetImageDepth()
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetImageDepth();
}

/*
* GetLongitudeImageWidth
*/
UINT COCTSystem::GetLongitudeImageWidth()
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetCutViewWidth();
}

/*
* GetLongitudeImageHeight
*/
UINT COCTSystem::GetLongitudeImageHeight()
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetCutViewHeight();
}

/*
* GetLongitudeImageChannels
*/
UINT COCTSystem::GetLongitudeImageChannels()
{
	if (m_openedSession == nullptr) return 0;
	return m_openedSession->GetCutViewChannels();
}

/*
* threadService
*/
UINT COCTSystem::threadService(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CThread* pThread = pSystem->m_pThreadService;

	// Connect to COM Interface first time asynchronous
	CLaserController::GetInstance();

	// Initialize (first prediction)
	cv::Mat imgSample = cv::imread(".\\oct_sample.png");
	pSystem->m_pLearning = new CRayLearning();
	pSystem->m_pLearning->Initialize(true);
	pSystem->m_pLearning->FindLumen(imgSample);

	while (pThread->isRun) {
		std::tuple<int, WPARAM, LPARAM> popMsgThread = pSystem->popMessage();
		int popMsg = std::get<0>(popMsgThread);
		WPARAM wParam = std::get<1>(popMsgThread);
		LPARAM lParam = std::get<2>(popMsgThread);

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
		case WM_PROCESS_OCT_DONE:
		{
			pSystem->OnMsgProcessOCTDone(wParam, lParam);
			break;
		}
		case WM_PROCESS_CUTVIEW:
		{
			pSystem->OnMsgProcessCutView(wParam, lParam);
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
	CDataWriter* pDataWriter = (CDataWriter*)pSystem->m_reviewSession[SESSION_REVIEW]->GetDataManager();
	const int nNumOfSamples = pDataWriter->GetNumOfSamples();

	int nFrame = 0;
	pSystem->postMessage(WM_UPDATE_SAVE_RAW, 0, nNumOfSamples);

	pDataWriter->StartSave(strSaveFilePath);
	for (nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadSaveRaw->isRun; nFrame++) {
		pDataWriter->WriteFrame(nFrame);

		pSystem->postMessage(WM_UPDATE_SAVE_RAW, nFrame + 1, nNumOfSamples);
	}
	pDataWriter->StopSave();

	pSystem->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::SaveRawData);

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
	IDataManager* pDataManager = pSystem->m_reviewSession[SESSION_REVIEW]->GetDataManager();

	CVolumeGenerator* pVolume = pSystem->m_pVolume;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging
	COCTImaging* pImaging = CImagingSession::CreateColorImaging(nullptr);

	for (int nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadGenerateVolume->isRun; nFrame++) {
		unsigned short* pBuffer = pDataManager->GetSample(nFrame);

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
* threadLumenDetection
*/
UINT COCTSystem::threadLumenDetection(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	IDataManager* pDataManager = pSystem->m_reviewSession[SESSION_REVIEW]->GetDataManager();
	std::vector<std::vector<std::vector<cv::Point>>>& vLumen = pSystem->m_vLumen;

	CRayLearning* pLearning = pSystem->m_pLearning;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging
	COCTImaging* pImaging = CImagingSession::CreateColorImaging(nullptr);

	vLumen.clear();
	for (int nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadLumenDetection->isRun; nFrame++) {
		unsigned short* pBuffer = pDataManager->GetSample(nFrame);

		pImaging->Process(pBuffer);
		vLumen.push_back(pLearning->FindLumen(pImaging->GetCircleImage()));
	}
	delete pImaging;

	pSystem->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::LumenDetection);

	// wait for StopThread
	while (pSystem->m_pThreadLumenDetection->isRun) {
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
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	CDataWriter *pDataWriter = new CDataWriter();
	pDataWriter->Initialize(config.nBufferSize * sizeof(unsigned short));
	pSystem->m_pAcqDevice->SetWriter(pDataWriter);

	// 1. Motor ON
	pMotor->PerfomRun(config.motor.velocityPullback);
	Sleep(config.motor.settleDown);

	pZaber->SetSpeed(config.zaber.pullbackSpeed);

	// 2. Start Recording OCT
	pDataWriter->StartRecording();

	// 3. Pullback Linear Stage
	if (pZaber->IsOpen()) {
		pZaber->MoveRelative(config.zaber.pullbackDistance * -1);
		while (pSystem->m_pThreadRotaryJunction->isRun) {
			if (pZaber->GetZaberStatus()) {
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

	// 5. Motor OFF
	pMotor->StopMotor();

	CImagingSession* pSession = CImagingSession::CreateSession(pSystem, SESSION_REVIEW, pDataWriter);
	pSystem->postMessage(WM_START_REVIEW_SESSION, SESSION_REVIEW, (LPARAM)pSession);
	pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::Pullback);

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
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	// 1. Motor ON
	int nVelocity = config.catheter.velocity;
	pMotor->PerfomRun(nVelocity);

	// 2. Set Linear Stage Position
	if (pZaber->IsOpen()) {
		pZaber->SetSpeed(config.catheter.speed);
		pZaber->Move(config.catheter.position);
		while (pSystem->m_pThreadRotaryJunction->isRun) {
			if (pZaber->GetZaberStatus()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}

	// 3. Wait
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
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	// Set Linear Stage Position to Zero
	if (pZaber->IsOpen()) {
		pZaber->SetSpeed(config.catheter.speed);
		pZaber->Move(0);
		while (pSystem->m_pThreadRotaryJunction->isRun) {
			if (pZaber->GetZaberStatus()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}

	pSystem->postMessage(WM_UPDATE_CATHETER_STATE, (WPARAM)CatheterState::Unloaded);
	pSystem->postMessage(WM_NOTIFY_DEVICE_WORK_DONE, (WPARAM)RayWorkItem::UnloadCatheter);

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

	pSystem->startAcqDevice();
	pLaser->LaserOnOff(true);
	pMotor->PerfomRun(config.motor.velocityLiveView);

	// To-Do: determine image verification
	bool verified = true;

	pMotor->StopMotor();
	pLaser->LaserOnOff(false);

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
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);
	
	result &= m_pAcqDevice->IsInit();
	result &= pLinearStage->IsOpen();
	result &= pMotor->IsConnected();

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
* connectRotaryJunction
*/
int COCTSystem::connectRotaryJunction() {
	CConfiguration& config = CConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	bool result = true;
	
	if (!pLinearStage->IsOpen()) {
		result &= pLinearStage->Open(config.zaber.pullback);
	}

	if (!pMotor->IsConnected()) {
		result &= pMotor->Connect();
		result &= pMotor->SwitchOn();
	}

	return (result) ? NOERROR : E_FAIL;
}

/*
* disconnectRotaryJunction
*/
int COCTSystem::disconnectRotaryJunction() {
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	bool result = true;
	
	if (pMotor->IsConnected()) {
		result &= pMotor->SwitchOff();
	}

	if (pLinearStage->IsOpen()) {
		pLinearStage->Close();
	}

	return (result) ? NOERROR : E_FAIL;
}

/*
* OnMsgProcessOCTDone
*/
LRESULT COCTSystem::OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam) {
	cv::Mat image;
	int nSession = wParam;
	int nFrameInfo = lParam;	// 0 if real time frame
	int nCurFrame = (nFrameInfo >> 16) & 0xFFFF;
	int nTotalFrame = nFrameInfo & 0xFFFF;
	bool isRealTime = (nFrameInfo == 0);

	if (m_curState == RayScannerState::Review) {
		if (isRealTime) return NOERROR;

		image = m_reviewSession[nSession]->GetImaging()->GetCircleImage();
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
	int nDrawSamples = lParam;

	CCutViewManager* pCutView = m_reviewSession[nSession]->GetCutView();

	pCutView->GenerateCutView(m_fDegree);
	cv::Mat imgCutView = pCutView->DrawLongitudeImage(nDrawSamples);

	int nCurFrame = nDrawSamples + 1;
	int nTotalFrame = pCutView->GetNumOfSamples();
	int nFrameInfo = (nCurFrame << 16) | (nTotalFrame);

	if (m_cbLongitude != nullptr) m_cbLongitude(nSession, imgCutView.data, imgCutView.cols, imgCutView.rows, imgCutView.channels(), nFrameInfo);

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
}
/*
* OnMsgUpdateScannerState
*/
LRESULT COCTSystem::OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam) {
	CConfiguration& config = CConfiguration::GetInstance();
	m_prevState = m_curState;
	m_curState = (RayScannerState)wParam;

	if(m_callback != nullptr) m_callback((int)RayCallbackRequest::State, (int)m_curState);

	switch (m_curState) {
	case RayScannerState::Initial:
		CUtility::StopThread(m_pThreadGenerateVolume);
		CUtility::StopThread(m_pThreadLumenDetection);
		closeAllSessions();
		// To-Do: unload catheter
		break;
	case RayScannerState::Default:
		CUtility::StopThread(m_pThreadGenerateVolume);
		CUtility::StopThread(m_pThreadLumenDetection);
		closeAllSessions();
		break;
	case RayScannerState::Scanning:
		CUtility::StartThread(threadPullbackScan, m_pThreadRotaryJunction, this);
		break;
	case RayScannerState::Review:		
		if (m_prevState == RayScannerState::Scanning) {
			CUtility::StartThread(threadSaveRaw, m_pThreadSaveRaw, this);
		}

		CUtility::StartThread(threadGenerateVolume, m_pThreadGenerateVolume, this);
		CUtility::StartThread(threadLumenDetection, m_pThreadLumenDetection, this);
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
	if (m_callback != nullptr) m_callback((int) RayCallbackRequest::Progress, nFrameInfo);

	return NOERROR;
}

/*
* OnMsgUpdateCatheterState
*/
LRESULT COCTSystem::OnMsgUpdateCatheterState(WPARAM wParam, LPARAM lParam) {
	CConfiguration& config = CConfiguration::GetInstance();
	m_cathState = (CatheterState)wParam;

	CUtility::StopThread(m_pThreadRotaryJunction);

	switch (m_cathState) {
	case CatheterState::Unloaded:
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Default);
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
		m_reviewSession[nSession]->Stop();
		delete m_reviewSession[nSession];
		m_reviewSession[nSession] = nullptr;
	}

	if (nSession == SESSION_REVIEW) {
		pSession->EnableCutView(m_backgroundColor);
	}

	m_reviewSession[nSession] = pSession;
	m_reviewSession[nSession]->Start();

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
		CUtility::StopThread(m_pThreadLumenDetection);
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