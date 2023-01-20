#include "ImagingSession.h"
#include "Utility.h"
#include "MessageService.h"
#include "OCTImaging.h"
#include "SimulateDevice.h"
#include "DataReader.h"
#include "Configuration.h"

CImagingSession::CImagingSession(CMessageService* pMsg, int nSession, bool deleteData) :
	m_pMsg(pMsg),
	m_nSession(nSession),
	m_deleteData(deleteData)
{
	m_pImaging = nullptr;
	m_pSimDevice = nullptr;
	m_pDataManager = nullptr;
}
CImagingSession::~CImagingSession() {
	Stop();
	if (m_pImaging != nullptr) delete m_pImaging;
	if (m_pSimDevice != nullptr) delete m_pSimDevice;
	if (m_deleteData && m_pDataManager != nullptr) delete m_pDataManager;
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

void CImagingSession::EnableWorkItem(RayWorkItem item, bool enable) {
	switch (item) {
	case RayWorkItem::UpdateCutView:
		break;
	case RayWorkItem::GenerateVolume:
		break;
	default:
		break;
	}
}
int CImagingSession::Start() {
	if (m_pSimDevice == nullptr || m_pImaging == nullptr) return -1;

	m_pImaging->Start();
	return m_pSimDevice->StartAcquisition();
}

int CImagingSession::Stop() {
	if(m_pImaging != nullptr) m_pImaging->Stop();
	if(m_pSimDevice != nullptr) m_pSimDevice->StopAcquisition();

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

CImagingSession* CImagingSession::createSession(CMessageService* pMsg, int nSession, IDataManager* pData, bool deleteData) {
	CImagingSession* pSession = new CImagingSession(pMsg, nSession, deleteData);

	pSession->m_pDataManager = pData;
	pSession->m_pImaging = CreateColorImaging(pMsg);
	pSession->m_pSimDevice = new CSimulateDevice(pData);
	pSession->m_pSimDevice->InitDevice();
	pSession->m_pSimDevice->SetImaging(pSession->m_pImaging);

	return pSession;
}
COCTImaging* CImagingSession::CreateColorImaging(CMessageService* msg) {
	COCTImaging* pImaging = new COCTImaging(msg);
	CConfiguration& config = CConfiguration::GetInstance();

	pImaging->Initialize(_T("CALIBRATION.DAT"));
	pImaging->SetColor(true);
	pImaging->SetBrightnessContrast(config.imaging.brightness, config.imaging.contrast);

	return pImaging;
}