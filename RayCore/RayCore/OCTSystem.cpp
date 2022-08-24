#include "pch.h"
#include "OCTSystem.h"
#include "Utility.h"
#include "MoriaConfiguration.h"
#include "MoriaImaging.h"
#include "DataWriter.h"
#include "DataReader.h"
#include "CutViewManager.h"
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
	m_pThreadHoming = nullptr;
	m_pThreadPullbackScan = nullptr;
	m_pThreadSaveRaw = nullptr;
	m_pThreadUpdateCutView = nullptr;
	m_pThreadLoadCatheter = nullptr;
	m_pThreadUnloadCatheter = nullptr;

	m_curState = RayScannerState::None;
	m_bMotorOnOff = false;

	//Property
	m_fBrightness = 0.0f;
	m_fContrast = 0.5f;
	m_fDegree = 90;

	CUtility::StartThread(threadService, m_pThreadService, this);

	//Initialize
	initialize();
}

/*
* ~COCTSystem
*/
COCTSystem::~COCTSystem() {
	CUtility::StopThread(m_pThreadService);
}

/*
* RegisterCallback
*/
RayError COCTSystem::RegisterCallback(FunctionPtr cb) {

	m_callback = cb;

	return RayError::OK;
}

/*
* Initialize
*/
RayError COCTSystem::Initialize() {

	if (m_curState == RayScannerState::None || m_curState == RayScannerState::IntitializeFailed) {
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Initializing);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* PullbackScan
*/
RayError COCTSystem::PullbackScan(char *strFilePath) {
	if (m_curState == RayScannerState::Ready) {

#ifdef TEST_VALUE_FILE_PATH
		m_strFilePath = TEST_VALUE_FILE_PATH;
#else
		m_strFilePath = CUtility::StringToWstring(strFilePath);
#endif

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

	if (m_curState == RayScannerState::Review || m_curState == RayScannerState::SaveDone) {
		CUtility::StartThread(threadUnloadCatheter, m_pThreadUnloadCatheter, this);

		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* EndReview
*/
RayError COCTSystem::EndReview()
{
	if (m_curState == RayScannerState::SaveDone) {
		postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Homing);
		return RayError::OK;
	}

	return RayError::WrongOCTScannerState;
}

/*
* MotorOnOff
*/
RayError COCTSystem::MotorOnOff(bool mode)
{
	if (m_curState >= RayScannerState::Ready) {
		m_bMotorOnOff = mode;
		CMotorController* pMotorCtrl = CMotorController::GetInstance();
		CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();

		if (m_bMotorOnOff) {
			pMotorCtrl->StopMotor();
		}
		else {
			pMotorCtrl->PerfomRun(pConfig.motor.velocity);
		}

		return RayError::OK;
	}
	return RayError::WrongOCTScannerState;
}

/*
* PlayPause
*/
RayError COCTSystem::PlayPause()
{
	if (m_curState >= RayScannerState::Review) {
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
	if (m_curState >= RayScannerState::Review) {
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
	if (m_curState >= RayScannerState::Review) {
		bool isPaused = ((CSimulateDevice*)m_pSimDevice)->IsPaused();
		if (!isPaused)
			return RayError::NotPausedState;

		((CSimulateDevice*)m_pSimDevice)->NextFrame();

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
* GetMotorOnOff
*/
bool COCTSystem::GetMotorOnOff()
{
	return m_bMotorOnOff;
}

/*
* GetIsPaused
*/
bool COCTSystem::GetIsPaused()
{
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
		case WM_UPDATE_CUTVIEW_DONE :
		{
			pSystem->OnMsgUpdateCutViewDone(wParam, lParam);
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
		pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::Homing);
	}
	else {
		pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::IntitializeFailed);
	}

	while (pSystem->m_pThreadInitialize->isRun) {
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
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();

#ifdef TEST_VALUE_FILE_PATH
	Sleep(3000);
#endif
	if (pZaberCtrl->IsOpen()) {
		pZaberCtrl->SetSpeed(pConfig.zaber.pullbackSpeed);
		pZaberCtrl->Move(pConfig.zaber.pullbackDistance);
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
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	// 1. Motor ON
	if (pMotor->IsRun() == false) {
		//pSystem->PostMessage(WM_COMMAND, IDC_BUTTON_MOTOR_ONOFF, 0);
	}
	Sleep(pConfig.motor.settleDown);

#ifndef TEST_VALUE_FILE_PATH
	// 2. Start Recording OCT
	pSystem->m_pDataWriter->StartRecording();
#endif

	// 3. Pullback Linear Stage
	if (pZaber->IsOpen()) {
		pZaber->Pull(pConfig.zaber.pullbackSpeed, pConfig.zaber.pullbackDistance);
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

	// 4. Motor OFF
	//pSystem->PostMessage(WM_COMMAND, IDC_BUTTON_MOTOR_ONOFF, 0);

#ifndef TEST_VALUE_FILE_PATH
	// 5. Stop Recording OCT
	pSystem->m_pDataWriter->StopRecording();
#else
	if (pSystem->m_pDataReader != NULL) {
		delete pSystem->m_pDataReader;
		pSystem->m_pDataReader = NULL;
	}
#endif

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
	CDataWriter* pDataManager = pSystem->m_pDataWriter;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	int nFrame = 0;
	pSystem->postMessage(WM_UPDATE_SAVE_RAW, 0, nNumOfSamples);

	pDataManager->StartSave(strSaveFilePath);
	for (nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadSaveRaw->isRun; nFrame++) {
		pDataManager->WriteFrame(nFrame);

		pSystem->postMessage(WM_UPDATE_SAVE_RAW, nFrame + 1, nNumOfSamples);
	}
	pDataManager->StopSave();

	pSystem->postMessage(WM_UPDATE_SCANNER_STATE, (WPARAM)RayScannerState::SaveDone);

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
#ifdef TEST_VALUE_FILE_PATH
	IDataManager* pDataManager = pSystem->m_pDataReader;
#else
	IDataManager* pDataManager = pSystem->m_pDataWriter;
#endif
	CCutViewManager* pCutView = pSystem->m_pCutView;
	double fDegree = pSystem->m_fDegree;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging
	CMoriaImaging* pImaging = pSystem->createColorImaging(NULL);

	pCutView->Initialize(nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSystem->m_pThreadUpdateCutView->isRun; nFrame++) {
		unsigned short* pBuffer = pDataManager->GetSample(nFrame);

		pCutView->AddRecord(pBuffer, pImaging, nFrame);
		pCutView->GenerateCutView(nFrame, fDegree);
		pSystem->updateCutView(nFrame);
	}
	delete pImaging;

	if (pSystem->m_pThreadUpdateCutView->isRun) {
		pSystem->postMessage(WM_UPDATE_CUTVIEW_DONE);
	}

	// wait for StopThread
	while (pSystem->m_pThreadUpdateCutView->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}

/*
* threadLoadCatheter
*/
UINT COCTSystem::threadLoadCatheter(LPVOID param) {
	COCTSystem* pSystem = (COCTSystem*)param;
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	// 1. Motor ON
	if (pMotor->IsRun() == false) {
		int nVelocity = pConfig.catheter.velocity;
		pMotor->PerfomRun(nVelocity);
	}

	// 2. Set Linear Stage Position
	if (pZaber->IsOpen()) {
		pZaber->SetSpeed(pConfig.catheter.speed);
		pZaber->Move(pConfig.catheter.position);
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
	Sleep(pConfig.catheter.rotationTime);

	// 4. Motor OFF
	pMotor->StopMotor();

	// 5. Wait
	Sleep(pConfig.catheter.waitingTime);

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
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	CZaberController* pZaber = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);

	// Set Linear Stage Position
	if (pZaber->IsOpen()) {
		pZaber->SetSpeed(pConfig.zaber.pullbackSpeed);
		pZaber->Move(pConfig.catheter.position);
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
* initialize
*/
void COCTSystem::initialize() {

	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	if (!pConfig.IsInit()) {
		pConfig.Initialize();
	}

	m_pImagingRealtime = createColorImaging(this);
	m_pImagingRealtime->Start();

	m_pImagingSimulate = createColorImaging(this);
	m_pImagingSimulate->Start();

	m_pDataWriter = new CDataWriter();
	m_pDataWriter->Initialize(pConfig.nBufferSize * sizeof(unsigned short));

	m_pCutView = new CCutViewManager();

	m_pAcqDevice = new CATSDevice();
	m_pAcqDevice->SetImaging(m_pImagingRealtime);
	m_pAcqDevice->SetWriter(m_pDataWriter);

	m_pSimDevice = new CSimulateDevice(m_pDataWriter);
	m_pSimDevice->SetImaging(m_pImagingSimulate);

}

/*
* createColorImaging
*/
CMoriaImaging* COCTSystem::createColorImaging(CMessageService* msg) {
	CMoriaImaging* pImaging = new CMoriaImaging(msg);

	pImaging->Initialize();
	pImaging->SetColor(true);
	pImaging->SetBrightnessContrast(m_fBrightness, m_fContrast);

	return pImaging;
}

/*
* initializeAcqDevice
*/
int COCTSystem::initializeAcqDevice() {
	if (m_pAcqDevice->IsInit()) {
		m_pAcqDevice->StopAcquisition();
		m_pAcqDevice->CleanUp();
	}

	int result = m_pAcqDevice->InitDevice();
	if (result == NOERROR) {
		m_pAcqDevice->StartAcquisition();
		CLaserController::GetInstance()->LaserOnOff(true);
	}

#ifdef TEST_VALUE_FILE_PATH
	return NOERROR;
#else
	return result;
#endif
}

/*
* initializeRotaryJunction
*/
int COCTSystem::initializeRotaryJunction() {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	CMotorController* pMotor = CMotorController::GetInstance();
	CZaberController* pLinearStage = CZaberController::GetInstance(ZABER_TYPE_PULLBACK);
	CZaberController* pInterferometer = CZaberController::GetInstance(ZABER_TYPE_INTERFEROMETER);

	bool result = true;
	result &= pLinearStage->Open(pConfig.zaber.pullback);
	result &= pInterferometer->Open(pConfig.zaber.interferometer);

	result &= pMotor->Connect();
	result &= pMotor->SwitchOn();

#ifdef TEST_VALUE_FILE_PATH
	return NOERROR;
#else
	return (result) ? NOERROR : E_FAIL;
#endif
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

	if (m_curState >= RayScannerState::Review) {
		if (isRealTime) return NOERROR;

		image = m_pImagingSimulate->GetCircleImage();
		m_pCutView->DrawCutViewGuideLine(image, m_fDegree);
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
* updateCutView
*/
void COCTSystem::updateCutView(int drawSamples) {
	cv::Mat imgCutView = m_pCutView->GetCutViewROI(512);
	cv::Mat imgResize;
	cv::Size sizeInterpolation = cv::Size(imgCutView.cols * CUTVIEW_INTERPOLATION_SCALE, imgCutView.rows);

	if (sizeInterpolation.width % 4 != 0) {
		sizeInterpolation.width -= (sizeInterpolation.width % 4);
	}
	cv::resize(imgCutView, imgResize, sizeInterpolation);

	if (m_nOffsetNavigation < drawSamples) {
		int offset = m_nOffsetNavigation * CUTVIEW_INTERPOLATION_SCALE;
		cv::Point ptStart, ptEnd;

		ptStart.x = offset;
		ptStart.y = 0;
		ptEnd.x = offset;
		ptEnd.y = imgResize.rows;
		cv::line(imgResize, ptStart, ptEnd, cv::Scalar(0xF5, 0xA5, 0x42), 2);
	}

	if (m_cbLongitude != nullptr) m_cbLongitude(imgResize.data, imgResize.cols, imgResize.rows, imgResize.channels(), 0);
}

/*
* OnMsgUpdateScannerState
*/
LRESULT COCTSystem::OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam) {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	m_curState = (RayScannerState)wParam;

	m_callback((int)RayCallbackRequest::State, (int)m_curState);

	switch (m_curState) {
	case RayScannerState::IntitializeFailed:
		CUtility::StopThread(m_pThreadInitialize);
		break;
	case RayScannerState::Initializing:
		CUtility::StartThread(threadInitialize, m_pThreadInitialize, this);
		break;
	case RayScannerState::Homing:
		m_pSimDevice->StopAcquisition();
		CUtility::StopThread(m_pThreadInitialize);
		CUtility::StopThread(m_pThreadUpdateCutView);
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
#ifdef TEST_VALUE_FILE_PATH
		delete m_pSimDevice;
		{
			m_pDataReader = new CDataReader();
			m_pDataReader->Initialize(m_strFilePath);
			m_pSimDevice = new CSimulateDevice(m_pDataReader);
			m_pSimDevice->SetImaging(m_pImagingSimulate);
		}
		m_curState = RayScannerState::SaveDone;
#else
		CUtility::StartThread(threadSaveRaw, m_pThreadSaveRaw, this);
#endif
		CUtility::StartThread(threadUpdateCutView, m_pThreadUpdateCutView, this);

		m_pSimDevice->InitDevice();
		m_pSimDevice->StartAcquisition();
		break;
	case RayScannerState::SaveDone:
		CUtility::StopThread(m_pThreadSaveRaw);
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
* OnMsgUpdateCutViewDone
*/
LRESULT COCTSystem::OnMsgUpdateCutViewDone(WPARAM wParam, LPARAM lParam) {
	CUtility::StopThread(m_pThreadUpdateCutView);

	return NOERROR;
}