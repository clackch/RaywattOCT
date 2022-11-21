#include "Config.h"
#include "OCTSystem.h"
#include "Utility.h"
#include "Configuration.h"
#include "OCTImaging.h"
#include "DataWriter.h"
#include "DataReader.h"
#include "CutViewManager.h"
#include "VolumeGenerator.h"
#include "ATSDevice.h"
#include "SimulateDevice.h"
#include "LaserController.h"
#include "MotorController.h"
#include "ZaberController.h"

/*
* COCTSystem
*/
COCTSystem::COCTSystem() {
	m_callback = nullptr;
	m_cbCrossSection = nullptr;
	m_cbLongitude = nullptr;
	m_pThreadService = nullptr;
	m_pThreadInitialize = nullptr;
	m_pThreadAutoCalibration = nullptr;
	m_pThreadHoming = nullptr;
	m_pThreadPullbackScan = nullptr;
	m_pThreadSaveRaw = nullptr;
	m_pThreadUpdateCutView = nullptr;
	m_pThreadGenerateVolume = nullptr;
	m_pThreadLoadCatheter = nullptr;
	m_pThreadUnloadCatheter = nullptr;

	m_pImagingRealtime = nullptr;
	m_pImagingSimulate = nullptr;

	m_pSimulationData = nullptr;

	m_pCutView = nullptr;
	m_pVolume = nullptr;

	m_pAcqDevice = nullptr;	
	m_pSimDevice = nullptr;

	m_showCalibGuide = false;

	m_prevState = RayScannerState::None;
	m_curState = RayScannerState::None;

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

	m_pImagingRealtime = createColorImaging(this);
	m_pImagingRealtime->SetBackgroundColor(m_backgroundColor);
	m_pImagingRealtime->Start();

	m_pImagingSimulate = createColorImaging(this);
	m_pImagingSimulate->SetBackgroundColor(m_backgroundColor);
	m_pImagingSimulate->Start();

	m_pCutView = new CCutViewManager();
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
	CUtility::StopThread(m_pThreadInitialize);
	CUtility::StopThread(m_pThreadAutoCalibration);
	CUtility::StopThread(m_pThreadHoming);
	CUtility::StopThread(m_pThreadPullbackScan);
	CUtility::StopThread(m_pThreadSaveRaw);
	CUtility::StopThread(m_pThreadUpdateCutView);
	CUtility::StopThread(m_pThreadGenerateVolume);
	CUtility::StopThread(m_pThreadLoadCatheter);
	CUtility::StopThread(m_pThreadUnloadCatheter);

	terminateSimulation();

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
	if (m_pImagingSimulate != nullptr) {
		m_pImagingSimulate->Stop();
		delete m_pImagingSimulate;
		m_pImagingSimulate = nullptr;
	}
	if (m_pSimulationData != nullptr) {
		delete m_pSimulationData;
		m_pSimulationData = nullptr;
	}
	if (m_pCutView != nullptr) {
		delete m_pCutView;
		m_pCutView = nullptr;
	}
	if (m_pVolume != nullptr) {
		delete m_pVolume;
		m_pVolume = nullptr;
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
* ConnectDevices
*/
RayError COCTSystem::ConnectDevices() {
	int result = NOERROR;

	if (m_curState == RayScannerState::None) {
		result |= connectAcqDevice();
		result |= connectRotaryJunction();

		return (result == NOERROR) ? RayError::OK : RayError::DeviceNotConnected;
	}

	return RayError::WrongOCTScannerState;
}

/*
* Initialize
*/
RayError COCTSystem::Initialize() {
	if (m_curState == RayScannerState::None) {
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Initializing);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* Finalize
*/
RayError COCTSystem::Finalize() {
	if (m_curState == RayScannerState::LiveView || m_curState == RayScannerState::Review) {
		finalizeAcqDevice();
		finalizeRotaryJunction();

		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::None);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* AutoCalibration
*/
RayError COCTSystem::AutoCalibration() {
	if (m_curState == RayScannerState::LiveView) {
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::AutoCalibration);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* ManualCalibration
*/
RayError COCTSystem::ManualCalibration(bool forward) {
	if (m_curState == RayScannerState::LiveView) {
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
	if (m_curState == RayScannerState::LiveView || m_curState == RayScannerState::AutoCalibration || m_curState == RayScannerState::Review) {
		m_pImagingRealtime->ShowCalibGuide(show);
		m_pImagingSimulate->ShowCalibGuide(show);
		m_showCalibGuide = show;

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
}

/*
* PreparePullback
*/
RayError COCTSystem::PreparePullback() {
	if (m_curState == RayScannerState::LiveView) {
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Homing);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* PullbackScan
*/
RayError COCTSystem::PullbackScan(char *strFilePath) {
	if (m_curState == RayScannerState::Ready) {
		m_strFilePath = CUtility::StringToWstring(strFilePath);

		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Scanning);

		return RayError::OK;
		
	}

	return RayError::WrongOCTScannerState;
}

/*
* LoadCatheter
*/
RayError COCTSystem::LoadCatheter() {

	if (m_curState == RayScannerState::Ready) {
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::LoadCatheter);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* UnloadCatheter
*/
RayError COCTSystem::UnloadCatheter() {

	if (m_curState == RayScannerState::Review) {
		CUtility::StartThread(threadUnloadCatheter, m_pThreadUnloadCatheter, this);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* StartReview
*/
RayError COCTSystem::StartReview(char* strFilePath) {
	if (m_curState == RayScannerState::None) {
		CDataReader* pReader = new CDataReader();
		int nNumOfSamples = pReader->Initialize(CUtility::StringToWstring(strFilePath));

		if (nNumOfSamples <= 0) return RayError::WrongFilePath;

		prepareSimulation(pReader);
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);
	
		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* EndReview
*/
RayError COCTSystem::EndReview()
{
	if (m_curState == RayScannerState::Review) {
		m_pSimDevice->StopAcquisition();

		if (m_prevState == RayScannerState::Scanning) {
			postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::LiveView);
		}
		else if (m_prevState == RayScannerState::None) {
			postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::None);		
		}
		else {
			return RayError::WrongOCTScannerState;
		}

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* MotorOnOff
*/
RayError COCTSystem::MotorOnOff(bool mode)
{
	if (m_curState == RayScannerState::LiveView || m_curState == RayScannerState::AutoCalibration) {
		setMotorOnOff(mode);

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
}

/*
* PlayPause
*/
RayError COCTSystem::PlayPause()
{
	if (m_curState == RayScannerState::Review) {
		bool isPaused = ((CSimulateDevice*)m_pSimDevice)->IsPaused();
		((CSimulateDevice*)m_pSimDevice)->SetPause(!isPaused);

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
}

/*
* PrevFrame
*/
RayError COCTSystem::PrevFrame()
{
	if (m_curState == RayScannerState::Review) {
		bool isPaused = ((CSimulateDevice*)m_pSimDevice)->IsPaused();
		if (!isPaused)
			return RayError::NotPausedState;

		((CSimulateDevice*)m_pSimDevice)->PrevFrame();

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
}

/*
* NextFrame
*/
RayError COCTSystem::NextFrame()
{
	if (m_curState == RayScannerState::Review) {
		bool isPaused = ((CSimulateDevice*)m_pSimDevice)->IsPaused();
		if (!isPaused)
			return RayError::NotPausedState;

		((CSimulateDevice*)m_pSimDevice)->NextFrame();

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
}

/*
* NextFrame
*/
RayError COCTSystem::MoveToFrame(int nFrame) {
	if (m_curState == RayScannerState::Review) {
		bool isPaused = ((CSimulateDevice*)m_pSimDevice)->IsPaused();
		if (!isPaused)
			return RayError::NotPausedState;

		((CSimulateDevice*)m_pSimDevice)->SetFrame(nFrame);

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
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
* GetBrightness
*/
double COCTSystem::GetBrightness() {
	return m_fBrightness;
}

/*
* SetBrightness
*/
RayError COCTSystem::SetBrightness(double value) {
	m_fBrightness = value;

	m_pImagingRealtime->SetBrightnessContrast(m_fBrightness, m_fContrast);
	m_pImagingSimulate->SetBrightnessContrast(m_fBrightness, m_fContrast);
	
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
	m_fContrast = value;

	m_pImagingRealtime->SetBrightnessContrast(m_fBrightness, m_fContrast);
	m_pImagingSimulate->SetBrightnessContrast(m_fBrightness, m_fContrast);

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
	m_fDegree = value;
	if (m_pCutView != nullptr) {
		m_pCutView->GenerateCutView(m_fDegree);
	}

	return RayError::OK;
}

/*
* GetDegree
*/
UINT COCTSystem::GetBackgroundColor() {
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
* GetDegree
*/
RayError COCTSystem::SetBackgroundColor(UINT value) {
	cv::Scalar color;
	color[0] = 0xff & value;
	color[1] = 0xff & (value >> 8);
	color[2] = 0xff & (value >> 16);

	m_backgroundColor = color;

	m_pImagingRealtime->SetBackgroundColor(m_backgroundColor);
	m_pImagingSimulate->SetBackgroundColor(m_backgroundColor);

	return RayError::OK;
}

/*
* GetVolumeDepth
*/
UINT COCTSystem::GetVolumeDepth() {
	if (m_pSimulationData == nullptr) return 0;

	return m_pSimulationData->GetNumOfSamples();
}

/*
* GetVolumeDepth
*/
double COCTSystem::GetLowLevel() {
	return m_fLowLevel;
}
/*
* SetLowLevel
*/
RayError COCTSystem::SetLowLevel(double value) {
	m_fLowLevel = value;

	m_pImagingRealtime->SetLevel(m_fLowLevel, m_fHighLevel);
	m_pImagingSimulate->SetLevel(m_fLowLevel, m_fHighLevel);

	return RayError::OK;
}
/*
* GetHighLevel
*/
double COCTSystem::GetHighLevel() {
	return m_fHighLevel;
}
/*
* SetHighLevel
*/
RayError COCTSystem::SetHighLevel(double value) {
	m_fHighLevel = value;

	m_pImagingRealtime->SetLevel(m_fLowLevel, m_fHighLevel);
	m_pImagingSimulate->SetLevel(m_fLowLevel, m_fHighLevel);

	return RayError::OK;
}

/*
* GetVolumeData
*/
void *COCTSystem::GetVolumeData() {
	if (m_pVolume == nullptr) return nullptr;

	return m_pVolume->GetVolumeData();
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
	if (m_pSimDevice == nullptr) return true;	// default state is paused

	return ((CSimulateDevice*)m_pSimDevice)->IsPaused();
}

/*
* threadService
*/
UINT COCTSystem::threadService(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CThread* pThread = pSystem->m_pThreadService;

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
		case WM_NOTIFY_SAVE_DONE:
		{
			pSystem->OnMsgNotifySaveDone(wParam, lParam);
			break;
		}
		case WM_NOTIFY_CUTVIEW_DONE:
		{
			pSystem->OnMsgNotifyCutViewDone(wParam, lParam);
			break;
		}
		case WM_NOTIFY_VOLUME_DONE:
		{
			pSystem->OnMsgNotifyVolumeDone(wParam, lParam);
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
		default:
			break;
		}
	
		Sleep(5);
	}

	return (UINT)RayError::OK;
}

/*
* threadInitialize
*/
UINT COCTSystem::threadInitialize(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	int nResult = NOERROR;

	nResult = pSystem->initializeAcqDevice();
	if (nResult != NOERROR) {
		//TODO : logging
		printf("Failed to connect acquisition device");
	}

	nResult = pSystem->initializeRotaryJunction();
	if (nResult != NOERROR) {
		//TODO : logging
		printf("Failed to connect rotary junction devices");
	}

	if (nResult == NOERROR) {
		pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::LiveView);
	}
	else {
		pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::None);

		RayError errorCode = (pSystem->checkConnection()) ? RayError::InitializeFailed : RayError::DeviceNotConnected;
		pSystem->postMessage(WM_NOTIFY_ERROR_OCCURED, (WPARAM)errorCode);
	}

	while (pSystem->m_pThreadInitialize->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadAutoCalibration
*/
UINT COCTSystem::threadAutoCalibration(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;

	Sleep(5000);

	pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::LiveView);

	while (pSystem->m_pThreadAutoCalibration->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadHoming
*/
UINT COCTSystem::threadHoming(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CZaberController* pZaberCtrl = (CZaberController*)CZaberController::GetInstance(ZABER_TYPE_PULLBACK);
	CConfiguration& config = CConfiguration::GetInstance();

	if (pZaberCtrl->IsOpen()) {
		pZaberCtrl->SetSpeed(config.zaber.pullbackSpeed);
		pZaberCtrl->Move(config.catheter.position);
		while (pSystem->m_pThreadHoming->isRun) {
			if (pZaberCtrl->GetZaberStatus()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}

	pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Ready);

	while (pSystem->m_pThreadHoming->isRun) {
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
	pSystem->setMotorOnOff(true);
	Sleep(config.motor.settleDown);

	pZaber->SetSpeed(config.zaber.pullbackSpeed);

	// 2. Start Recording OCT
	pDataWriter->StartRecording();

	// 3. Pullback Linear Stage
	if (pZaber->IsOpen()) {
		pZaber->MoveRelative(config.zaber.pullbackDistance * -1);
		while (pSystem->m_pThreadPullbackScan->isRun) {
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
	pSystem->setMotorOnOff(false);

	pSystem->prepareSimulation(pDataWriter);

	pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Review);

	while (pSystem->m_pThreadPullbackScan->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadSaveRaw
*/
UINT COCTSystem::threadSaveRaw(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	tstring strSaveFilePath = pSystem->m_strFilePath;
	CDataWriter* pDataWriter = (CDataWriter *)pSystem->m_pSimulationData;
	const int nNumOfSamples = pDataWriter->GetNumOfSamples();

	int nFrame = 0;
	pSystem->postMessage(WM_UPDATE_SAVE_RAW, 0, nNumOfSamples);

	pDataWriter->StartSave(strSaveFilePath);
	for (nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadSaveRaw->isRun; nFrame++) {
		pDataWriter->WriteFrame(nFrame);

		pSystem->postMessage(WM_UPDATE_SAVE_RAW, nFrame + 1, nNumOfSamples);
	}
	pDataWriter->StopSave();

	pSystem->postMessage(WM_NOTIFY_SAVE_DONE);

	while (pSystem->m_pThreadSaveRaw->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadUpdateCutView
*/
UINT COCTSystem::threadUpdateCutView(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	IDataManager* pDataManager = pSystem->m_pSimulationData;

	CCutViewManager* pCutView = pSystem->m_pCutView;
	double fDegree = pSystem->m_fDegree;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging
	COCTImaging* pImaging = pSystem->createColorImaging(NULL);

	pCutView->Initialize(nNumOfSamples, pSystem->m_backgroundColor);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadUpdateCutView->isRun; nFrame++) {
		unsigned short* pBuffer = pDataManager->GetSample(nFrame);

		pCutView->AddRecord(pBuffer, pImaging, nFrame);
		pCutView->GenerateCutView(nFrame, fDegree);
		pSystem->updateCutView(nFrame);
	}
	delete pImaging;

	if (pSystem->m_pThreadUpdateCutView->isRun) {
		pSystem->postMessage(WM_NOTIFY_CUTVIEW_DONE);
	}

	// wait for StopThread
	while (pSystem->m_pThreadUpdateCutView->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadGenerateVolume
*/
UINT COCTSystem::threadGenerateVolume(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	IDataManager* pDataManager = pSystem->m_pSimulationData;

	CVolumeGenerator* pVolume = pSystem->m_pVolume;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging
	COCTImaging* pImaging = pSystem->createColorImaging(NULL);

	for (int nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadGenerateVolume->isRun; nFrame++) {
		unsigned short* pBuffer = pDataManager->GetSample(nFrame);

		pVolume->AddRecord(pBuffer, pImaging, nFrame);
	}
	delete pImaging;

	if (pSystem->m_pThreadGenerateVolume->isRun) {
		pSystem->postMessage(WM_NOTIFY_VOLUME_DONE);
	}

	// wait for StopThread
	while (pSystem->m_pThreadGenerateVolume->isRun) {
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
		while (pSystem->m_pThreadLoadCatheter->isRun) {
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

	// 4. Motor OFF
	pMotor->StopMotor();

	// 5. Wait
	Sleep(config.catheter.waitingTime);

	// 6. Homing
	pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Homing);

	while (pSystem->m_pThreadLoadCatheter->isRun) {
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

	// Set Linear Stage Position
	if (pZaber->IsOpen()) {
		pZaber->SetSpeed(config.zaber.pullbackSpeed);
		pZaber->Move(config.catheter.position);
		while (pSystem->m_pThreadPullbackScan->isRun) {
			if (pZaber->GetZaberStatus()) {
				break;
			}
			else {
				Sleep(DELAY_FOR_STOP_THREAD);
			}
		}
	}

	// wait for StopThread
	while (pSystem->m_pThreadUnloadCatheter->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* createColorImaging
*/
COCTImaging* COCTSystem::createColorImaging(CMessageService* msg) {
	COCTImaging* pImaging = new COCTImaging(msg);

	pImaging->Initialize(_T("CALIBRATION.DAT"));
	pImaging->SetColor(true);
	pImaging->SetBrightnessContrast(m_fBrightness, m_fContrast);

	return pImaging;
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
* initializeAcqDevice
*/
int COCTSystem::connectAcqDevice() {
	if (m_pAcqDevice->IsInit()) return NOERROR;

	return m_pAcqDevice->InitDevice();
}

/*
* initializeAcqDevice
*/
int COCTSystem::initializeAcqDevice() {
	if (m_pAcqDevice->IsInit()) {
		m_pAcqDevice->StopAcquisition();
		m_pAcqDevice->CleanUp();
	}

	int result = connectAcqDevice();
	if (result == NOERROR) {
		m_pAcqDevice->StartAcquisition();
		CLaserController::GetInstance()->LaserOnOff(true);
	}

	return result;
}

/*
* finalizeAcqDevice
*/
int COCTSystem::finalizeAcqDevice() {
	if (m_pAcqDevice->IsInit()) {
		m_pAcqDevice->StopAcquisition();
		m_pAcqDevice->CleanUp();
	}

	CLaserController::GetInstance()->LaserOnOff(false);

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
	}

	return (result) ? NOERROR : E_FAIL;
}

/*
* initializeRotaryJunction
*/
int COCTSystem::initializeRotaryJunction() {
	CMotorController* pMotor = CMotorController::GetInstance();

	bool result = pMotor->SwitchOn();

	return (result) ? NOERROR : E_FAIL;
}

/*
* finalizeRotaryJunction
*/
int COCTSystem::finalizeRotaryJunction() {
	CMotorController* pMotor = CMotorController::GetInstance();

	bool result = pMotor->SwitchOff();

	return (result) ? NOERROR : E_FAIL;
}

/*
* OnMsgProcessOCTDone
*/
LRESULT COCTSystem::OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam) {
	cv::Mat image;
	int nCurFrame = wParam;
	int nTotalFrame = lParam;
	int nFrameInfo = (nCurFrame << 16) | (nTotalFrame);
	bool isRealTime = (nFrameInfo == 0);

	if (m_curState == RayScannerState::Review) {
		if (isRealTime) return NOERROR;

		image = m_pImagingSimulate->GetCircleImage();
		m_nOffsetNavigation = nCurFrame;

		if (m_pThreadUpdateCutView == NULL) {
			updateCutView(nTotalFrame);
		}
	}
	else {
		image = m_pImagingRealtime->GetCircleImage();
	}

	if (m_cbCrossSection != nullptr) m_cbCrossSection(image.data, image.cols, image.rows, image.channels(), nFrameInfo);

	return NOERROR;
}

/*
* setMotorOnOff
*/
void COCTSystem::setMotorOnOff(bool on) {
	CMotorController* pMotorCtrl = CMotorController::GetInstance();
	CConfiguration& config = CConfiguration::GetInstance();

	if (on) {
		pMotorCtrl->PerfomRun(config.motor.velocity);
	}
	else {
		pMotorCtrl->StopMotor();
	}
}

/*
* updateCutView
*/
void COCTSystem::updateCutView(int drawSamples) {
	cv::Mat imgCutView = m_pCutView->GetCutViewROI(512);
	cv::Mat imgDisplay = imgCutView.clone();
	cv::Mat imgEdit, imgMask;
	cv::Mat imgResize;
	cv::Size sizeInterpolation = cv::Size(imgCutView.cols * CUTVIEW_INTERPOLATION_SCALE, imgCutView.rows);
	cv::Rect rectMask;

	int nCurFrame = drawSamples;
	int nTotalFrame = m_pCutView->GetNumOfSamples();
	int nFrameInfo = (nCurFrame << 16) | (nTotalFrame);	

	if (sizeInterpolation.width % 4 != 0) {
		sizeInterpolation.width -= (sizeInterpolation.width % 4);
	}

	cv::convertScaleAbs(imgCutView, imgEdit, m_fContrast, m_fBrightness);

	imgMask = cv::Mat(imgCutView.rows, imgCutView.cols, CV_8UC1);
	rectMask = cv::Rect(0, 0, drawSamples, imgMask.rows);
	memset(imgMask.data, 0x00, imgMask.cols * imgMask.rows);
	imgMask(rectMask) = 0x01;
	cv::copyTo(imgEdit, imgDisplay, imgMask);
	cv::resize(imgDisplay, imgResize, sizeInterpolation);

	if (m_cbLongitude != nullptr) m_cbLongitude(imgResize.data, imgResize.cols, imgResize.rows, imgResize.channels(), nFrameInfo);
}

/*
* prepareSimulation
*/
void COCTSystem::prepareSimulation(IDataManager* pDataManager) {
	terminateSimulation();

	m_pSimulationData = pDataManager;

	m_pSimDevice = new CSimulateDevice(m_pSimulationData);
	m_pSimDevice->SetImaging(m_pImagingSimulate);
	m_pSimDevice->InitDevice();
}

/*
* terminateSimulation
*/
void COCTSystem::terminateSimulation() {
	if (m_pSimDevice != nullptr) {
		m_pSimDevice->StopAcquisition();
		delete m_pSimDevice;
		m_pSimDevice = nullptr;
	}
	if (m_pSimulationData != nullptr) {
		delete m_pSimulationData;
		m_pSimulationData = nullptr;
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
	case RayScannerState::None:
		CUtility::StopThread(m_pThreadInitialize);
		CUtility::StopThread(m_pThreadUpdateCutView);
		CUtility::StopThread(m_pThreadGenerateVolume);
		break;
	case RayScannerState::Initializing:
		CUtility::StartThread(threadInitialize, m_pThreadInitialize, this);
		break;
	case RayScannerState::LiveView:
		CUtility::StopThread(m_pThreadInitialize);
		CUtility::StopThread(m_pThreadUpdateCutView);
		CUtility::StopThread(m_pThreadGenerateVolume);
		CUtility::StopThread(m_pThreadAutoCalibration);
		break;
	case RayScannerState::AutoCalibration:
		CUtility::StartThread(threadAutoCalibration, m_pThreadAutoCalibration, this);
		break;
	case RayScannerState::Homing:
		CUtility::StopThread(m_pThreadLoadCatheter);
		CUtility::StopThread(m_pThreadUnloadCatheter);
		CUtility::StartThread(threadHoming, m_pThreadHoming, this);
		break;
	case RayScannerState::Ready:
		CUtility::StopThread(m_pThreadHoming);
		break;
	case RayScannerState::LoadCatheter:
		CUtility::StartThread(threadLoadCatheter, m_pThreadLoadCatheter, this);
		break;
	case RayScannerState::Scanning:
		CUtility::StartThread(threadPullbackScan, m_pThreadPullbackScan, this);
		break;
	case RayScannerState::Review:
		CUtility::StopThread(m_pThreadPullbackScan);
		
		if (m_prevState == RayScannerState::Scanning) {
			CUtility::StartThread(threadSaveRaw, m_pThreadSaveRaw, this);
		}

		CUtility::StartThread(threadUpdateCutView, m_pThreadUpdateCutView, this);
		CUtility::StartThread(threadGenerateVolume, m_pThreadGenerateVolume, this);

		m_pSimDevice->StartAcquisition();
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
* OnMsgNotifySaveDone
*/
LRESULT COCTSystem::OnMsgNotifySaveDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadSaveRaw);

	return NOERROR;
}

/*
* OnMsgUpdateCutViewDone
*/
LRESULT COCTSystem::OnMsgNotifyCutViewDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadUpdateCutView);

	return NOERROR;
}

/*
* OnMsgNotifyVolumeDone
*/
LRESULT COCTSystem::OnMsgNotifyVolumeDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadGenerateVolume);

	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::WorkDone, (int)RayWorkItem::GenerateVolume);

	return NOERROR;
}

/*
* OnMsgUpdateCutViewDone
*/
LRESULT COCTSystem::OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam) {
	if (m_callback != nullptr) m_callback((int)RayCallbackRequest::Error, wParam);

	return NOERROR;
}