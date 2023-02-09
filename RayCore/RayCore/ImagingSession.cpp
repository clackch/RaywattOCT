#include "ImagingSession.h"
#include "Utility.h"
#include "MessageService.h"
#include "OCTImaging.h"
#include "SimulateDevice.h"
#include "DataReader.h"
#include "Configuration.h"
#include "CutViewManager.h"

CImagingSession::CImagingSession(CMessageService* pMsg, int nSession, bool deleteData) :
	m_pMsg(pMsg),
	m_nSession(nSession),
	m_deleteData(deleteData)
{
	m_pImaging = nullptr;
	m_pSimDevice = nullptr;
	m_pDataManager = nullptr;

	m_pThreadUpdateCutView = nullptr;
	m_pCutView = nullptr;
	m_backgroundColor = cv::Scalar(0, 0, 0);
}
CImagingSession::~CImagingSession() {
	Stop();
	if (m_pImaging != nullptr) delete m_pImaging;
	if (m_pSimDevice != nullptr) delete m_pSimDevice;
	if (m_deleteData && m_pDataManager != nullptr) delete m_pDataManager;
	if (m_pThreadUpdateCutView != nullptr) delete m_pThreadUpdateCutView;
	if (m_pCutView != nullptr) delete m_pCutView;
}

CImagingSession* CImagingSession::CreateSession(CMessageService* pMsg, int nSession, IDataManager* pWriter) {
	if (pMsg == nullptr || pWriter == nullptr) return nullptr;

	return createSession(pMsg, nSession, pWriter, false);
}

CImagingSession* CImagingSession::CreateSession(CMessageService* pMsg, int nSession, const char* strFilePath) {
	CDataReader* pReader = new CDataReader();
	int nNumOfSamples = pReader->Initialize(CUtility::StringToWstring(strFilePath));

	if (pMsg == nullptr || nNumOfSamples <= 0) {
		delete pReader;
		return nullptr;
	}

	return createSession(pMsg, nSession, pReader, true);
}

COCTImaging* CImagingSession::CreateColorImaging(CMessageService* msg) {
	COCTImaging* pImaging = new COCTImaging(msg);
	CConfiguration& config = CConfiguration::GetInstance();

	pImaging->Initialize(_T("CALIBRATION.DAT"));
	pImaging->SetColor(true);
	pImaging->SetBrightnessContrast(config.imaging.brightness, config.imaging.contrast);

	return pImaging;
}

void CImagingSession::EnableCutView(cv::Scalar backgroundColor) {
	CUtility::StopThread(m_pThreadUpdateCutView);
	if (m_pCutView != nullptr) delete m_pCutView;

	m_pCutView = new CCutViewManager();
	m_backgroundColor = backgroundColor;
}

int CImagingSession::Start() {
	if (m_pSimDevice == nullptr || m_pImaging == nullptr) return -1;

	if (m_pCutView != nullptr) {
		CUtility::StartThread(threadUpdateCutView, m_pThreadUpdateCutView, this);
	}

	m_pImaging->Start();
	return m_pSimDevice->StartAcquisition();
}

int CImagingSession::Stop() {
	if (m_pImaging != nullptr) m_pImaging->Stop();
	if (m_pSimDevice != nullptr) m_pSimDevice->StopAcquisition();
	if (m_pThreadUpdateCutView != nullptr) CUtility::StopThread(m_pThreadUpdateCutView);

	return NOERROR;
}

bool CImagingSession::IsPaused() {
	if (m_pSimDevice == nullptr) return true;
	return m_pSimDevice->IsPaused();
}
void CImagingSession::SetPause(bool pause) {
	if (m_pSimDevice != nullptr) m_pSimDevice->SetPause(pause);
}
void CImagingSession::PrevFrame() {
	if (m_pSimDevice != nullptr) m_pSimDevice->PrevFrame();
}
void CImagingSession::NextFrame() {
	if (m_pSimDevice != nullptr) m_pSimDevice->NextFrame();
}
void CImagingSession::MoveToFrame(int nFrame) {
	if (m_pSimDevice != nullptr) m_pSimDevice->SetFrame(nFrame);
}
UINT CImagingSession::GetImageWidth() {
	if (m_pImaging != nullptr) return m_pImaging->GetImageWidth();
	return 0;
}
UINT CImagingSession::GetImageHeight() {
	if (m_pImaging != nullptr) return m_pImaging->GetImageHeight();
	return 0;
}
UINT CImagingSession::GetImageChannels() {
	if (m_pImaging != nullptr) return m_pImaging->GetImageChannels();
	return 0;
}
UINT CImagingSession::GetImageDepth() {
	if (m_pDataManager != nullptr) return m_pDataManager->GetNumOfSamples();
	return 0;
}
void* CImagingSession::GetImageData(int nFrame) {
	if (m_pImaging == nullptr || m_pDataManager == nullptr) return nullptr;
	if (nFrame < 0 || nFrame >= m_pDataManager->GetNumOfSamples()) return nullptr;

	unsigned short* pBuffer = m_pDataManager->GetSample(nFrame);
	m_pImaging->Process(pBuffer);
	
	if (m_pCutView != nullptr) {
		m_pCutView->AddRecord(m_pImaging->GetCircleImage(), nFrame);
	}

	return m_pImaging->GetCircleImage().data;
}
UINT CImagingSession::GetCutViewWidth() {
	if (m_pCutView == nullptr) return 0;

	return m_pCutView->GetLongitudeSize().width;
}
UINT CImagingSession::GetCutViewHeight() {
	if (m_pCutView == nullptr) return 0;

	return m_pCutView->GetLongitudeSize().height;
}
UINT CImagingSession::GetCutViewChannels() {
	if (m_pCutView == nullptr) return 0;

	return m_pCutView->GetCutView().channels();
}

CImagingSession* CImagingSession::createSession(CMessageService* pMsg, int nSession, IDataManager* pData, bool deleteData) {
	CImagingSession* pSession = new CImagingSession(pMsg, nSession, deleteData);

	pSession->m_pDataManager = pData;
	pSession->m_pImaging = CreateColorImaging(pMsg);
	pSession->m_pImaging->SetSession(nSession);
	pSession->m_pSimDevice = new CSimulateDevice(pData);
	pSession->m_pSimDevice->InitDevice();
	pSession->m_pSimDevice->SetImaging(pSession->m_pImaging);

	return pSession;
}
UINT CImagingSession::threadUpdateCutView(LPVOID param) {
	CImagingSession* pSession = (CImagingSession*)param;
	IDataManager* pDataManager = pSession->m_pDataManager;
	int nSession = pSession->m_nSession;

	CCutViewManager* pCutView = pSession->m_pCutView;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging (without message)
	COCTImaging* pImaging = CreateColorImaging(nullptr);

	pCutView->Initialize(nNumOfSamples, pSession->m_backgroundColor, CUTVIEW_INTERPOLATION_SCALE);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadUpdateCutView->isRun; nFrame++) {
		unsigned short* pBuffer = pDataManager->GetSample(nFrame);
		pImaging->Process(pBuffer);

		pCutView->AddRecord(pImaging->GetCircleImage(), nFrame);
		pSession->m_pMsg->postMessage(WM_PROCESS_CUTVIEW, nSession, nFrame + 1);
	}
	delete pImaging;

	// wait for StopThread
	while (pSession->m_pThreadUpdateCutView->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}