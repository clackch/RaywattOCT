#include "ImagingSession.h"
#include "Utility.h"
#include "MessageService.h"
#include "OCTImaging.h"
#include "LabImaging.h"
#include "TIFFImaging.h"
#include "Calibration.h"
#include "SimulateDevice.h"
#include "DataReader.h"
#include "TIFFReader.h"
#include "Configuration.h"
#include "CutViewManager.h"
#include "RayLearning.h"

CImagingSession::CImagingSession(CMessageService* pMsg, int nSession, bool deleteData) :
	m_pMsg(pMsg),
	m_nSession(nSession),
	m_deleteData(deleteData),
	m_detectLumen(false)
{
	m_imagingType = ImagingType::Default;
	m_pImaging = nullptr;
	m_pSimDevice = nullptr;
	m_pDataManager = nullptr;

	m_pThreadUpdateCutView = nullptr;
	m_pCutView = nullptr;
}
CImagingSession::~CImagingSession() {
	Stop();
	if (m_pImaging != nullptr) delete m_pImaging;
	if (m_pSimDevice != nullptr) delete m_pSimDevice;
	if (m_deleteData && m_pDataManager != nullptr) delete m_pDataManager;
	if (m_pThreadUpdateCutView != nullptr) delete m_pThreadUpdateCutView;
	if (m_pCutView != nullptr) delete m_pCutView;
}

CImagingSession* CImagingSession::CreateSession(CMessageService* pMsg, int nSession, IImaging::Setting setting, IDataManager* pWriter) {
	if (pMsg == nullptr || pWriter == nullptr) return nullptr;

	return createSession(pMsg, setting, nSession,  pWriter, true, ImagingType::Default);
}

CImagingSession* CImagingSession::CreateSession(CMessageService* pMsg, int nSession, const char* strFilePath) {
	CConfiguration& config = CConfiguration::GetInstance();
	IImaging::Setting setting = config.imaging;
	int nHeaderSize = 0;

	if (pMsg == nullptr) return nullptr;

	CDataReader* pReader = nullptr;
	int nNumOfSamples = 0;
	ImagingType type = ImagingType::Default;

	std::string ext = CUtility::GetFileExtension(strFilePath);
	if (ext.compare(FILE_EXTENSION_OCT) == 0)
	{
		pReader = new CDataReader();
		OCTHeader header = pReader->ReadHeader(CUtility::StringToWstring(strFilePath));
		setting.Set(header.width, header.height);
		nNumOfSamples = pReader->Initialize(CUtility::StringToWstring(strFilePath), setting.nBufferSize);
		PLOGI.printf("%s opened - %d x %d (%d frames)", strFilePath, header.width, header.height, nNumOfSamples);
	}
	else if (ext.compare(FILE_EXTENSION_RAW) == 0)
	{
		setting.Set(config.acquisition.nAScan, config.acquisition.nBScan);

		pReader = new CDataReader();
		nNumOfSamples = pReader->Initialize(CUtility::StringToWstring(strFilePath), setting.nBufferSize);
	}
	else if (ext.compare(FILE_EXTENSION_TIF) == 0)
	{
		pReader = new CTIFFReader();
		nNumOfSamples = ((CTIFFReader*)pReader)->Initialize(strFilePath);
		type = ImagingType::TIFFImaging;
		
		int nWidth, nHeight;
		((CTIFFReader*)pReader)->GetImageSize(nWidth, nHeight);
		setting.Set(nWidth, nHeight);
	}

	if (nNumOfSamples <= 0) {
		if(pReader != nullptr) delete pReader;

		return nullptr;
	}

	return createSession(pMsg, setting, nSession, pReader, true, type);
}

COCTImaging* CImagingSession::CreateColorImaging(CMessageService* msg, IImaging::Setting setting, IDataManager* pData, ImagingType type) {
	CConfiguration& config = CConfiguration::GetInstance();
	COCTImaging* pImaging = nullptr;

	CCalibration* calibration = new CCalibration(setting.nAScan, setting.nFFTLength);
	if (pData != nullptr && pData->GetExtraData(OCTHeader::ExtraData::Dispersion) != nullptr)
	{
		calibration->Initialize((char*)pData->GetExtraData(OCTHeader::ExtraData::Dispersion));
	}
	else 
	{
		calibration->Initialize(_T("CALIBRATION.DAT"));
	}

	USHORT* background = nullptr;
	if (pData != nullptr && pData->GetExtraData(OCTHeader::ExtraData::Background) != nullptr) 
	{
		background = new USHORT[setting.nBufferSize];
		memcpy(background, pData->GetExtraData(OCTHeader::ExtraData::Background), sizeof(USHORT) * setting.nBufferSize);
	}
	else 
	{
		background = readBackground("BACKGROUND.bin", setting);
	}

	switch (type)
	{
	case ImagingType::OCTImaging:
		pImaging = new COCTImaging(setting, msg);
		pImaging->Initialize(calibration);
		break;
	case ImagingType::LabImaging:
		pImaging = new CLabImaging(setting, msg);
		((CLabImaging *)pImaging)->Initialize(calibration, background);
		((CLabImaging *)pImaging)->SetBackgroundSubtract(true);
		break;
	case ImagingType::TIFFImaging:
		pImaging = new CTIFFImaging(setting, msg);
		((CTIFFImaging*)pImaging)->Initialize();
		break;
	default:
		return nullptr;
	}

	pImaging->SetColor(true);
	pImaging->SetMeasurementSetting(config.measurement);

	return pImaging;
}

void CImagingSession::EnableCutView(cv::Scalar backgroundColor) {
	CUtility::StopThread(m_pThreadUpdateCutView);
	if (m_pCutView != nullptr) delete m_pCutView;

	m_pCutView = new CCutViewManager();
	m_pCutView->Initialize(m_pDataManager->GetNumOfSamples(), backgroundColor);
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

	char* pBuffer = m_pDataManager->GetSample(nFrame);
	m_pImaging->Process(pBuffer);
	
	if (m_pCutView != nullptr) {
		m_pCutView->AddRecord(m_pImaging->GetCircleImage(), nFrame);
	}

	return m_pImaging->GetCircleImage().data;
}
UINT CImagingSession::GetCutViewWidth() {
	if (m_pCutView == nullptr) return 0;

	return m_pCutView->GetCutView().cols;
}
UINT CImagingSession::GetCutViewHeight() {
	if (m_pCutView == nullptr) return 0;

	return m_pCutView->GetCutView().rows;
}
UINT CImagingSession::GetCutViewChannels() {
	if (m_pCutView == nullptr) return 0;

	return m_pCutView->GetCutView().channels();
}
void* CImagingSession::GetLumenContour(int nFrame) {
	if (m_vLumen.size() <= nFrame) return nullptr;
	if (m_vLumen.at(nFrame).size() <= 0) return nullptr;

	cv::Mat matContour = m_vLumen.at(nFrame).at(0);
	return matContour.ptr();
}
int CImagingSession::GetNumOfLumenContourPoints(int nFrame) {
	if (m_vLumen.size() <= nFrame) return 0;
	if (m_vLumen.at(nFrame).size() <= 0) return 0;

	cv::Mat matContour = m_vLumen.at(nFrame).at(0);
	return matContour.cols * matContour.rows;
}


CImagingSession* CImagingSession::createSession(CMessageService* pMsg, IImaging::Setting setting, int nSession, IDataManager* pData, bool deleteData, ImagingType type) {
	CImagingSession* pSession = new CImagingSession(pMsg, nSession, deleteData);

	pSession->m_imagingType = type;
	pSession->m_pDataManager = pData;
	pSession->m_pImaging = CreateColorImaging(pMsg, setting, pData, type);
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
	CRayLearning& learning = CRayLearning::GetInstance();
	std::vector<std::vector<cv::Mat>>& vLumen = pSession->m_vLumen;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	// prepare imaging (without message)
	COCTImaging* pImaging = CreateColorImaging(nullptr, pSession->m_pImaging->GetSetting(), pDataManager, pSession->GetImagingType());

	PLOGI.printf("update cutview & lumen detection start - %d frames", nNumOfSamples);
	printf("update cutview & lumen detection start - %d frames\n", nNumOfSamples);
	vLumen.clear();
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadUpdateCutView->isRun; nFrame++) {
		char* pBuffer = pDataManager->GetSample(nFrame);
		pImaging->Process(pBuffer);

		pCutView->AddRecord(pImaging->GetCircleImage(), nFrame);

		if (pSession->m_detectLumen) {
			std::vector<std::vector<cv::Point>> vContours = learning.FindLumen(pImaging->GetCircleImage());
			std::vector<cv::Mat> vLumens;
			for (int i = 0; i < vContours.size(); i++) {
				std::vector<cv::Point> contour = vContours.at(i);
				cv::Mat matContour(contour.size(), 1, CV_32SC2);
				for (size_t row = 0; row < contour.size(); row++) {
					matContour.at<cv::Point>(row, 0) = contour[row];
				}
				vLumens.push_back(matContour);
			}
			vLumen.push_back(vLumens);
		}

		pSession->m_pMsg->postMessage(WM_PROCESS_CUTVIEW, nSession, nFrame);
	}
	delete pImaging;

	PLOGI.printf("update cutview & lumen detection done.");
	if (pSession->m_detectLumen) pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::LumenDetection);
	// wait for StopThread
	while (pSession->m_pThreadUpdateCutView->isRun) {
		Sleep(DELAY_FOR_STOP_THREAD);
	}

	return NOERROR;
}
USHORT* CImagingSession::readBackground(const char* strBackgroundFile, IImaging::Setting setting) {
	if (strBackgroundFile == nullptr) return nullptr;
	
	FILE* fp = fopen(strBackgroundFile, "rb");
	if (fp == nullptr) return nullptr;

	USHORT* pBackground = new USHORT[setting.nBufferSize];
	fread(pBackground, sizeof(USHORT), setting.nBufferSize, fp);

	fclose(fp);

	return pBackground;
}