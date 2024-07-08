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
#include "IRayLearning.h"
#include "LookUpTable.h"

CImagingSession::CImagingSession(CMessageService* pMsg, int nSession, bool deleteData) :
	m_pMsg(pMsg),
	m_nSession(nSession),
	m_deleteData(deleteData)
{
	m_imagingType = ImagingType::Default;
	m_pImaging = nullptr;
	m_pDataManager = nullptr;
	m_pThreadImaging = nullptr;

	m_pThreadUpdateCutView = nullptr;
	m_pThreadObjectDetection = nullptr;
	m_pThreadVolumeGeneration = nullptr;
	m_pCutView = nullptr;

	m_pVolumeData = nullptr;
}
CImagingSession::~CImagingSession() {
	Stop();
	if (m_pImaging != nullptr) delete m_pImaging;
	if (m_deleteData && m_pDataManager != nullptr) delete m_pDataManager;
	if (m_pThreadUpdateCutView != nullptr) delete m_pThreadUpdateCutView;
	if (m_pThreadObjectDetection != nullptr) delete m_pThreadObjectDetection;
	if (m_pThreadVolumeGeneration != nullptr) delete m_pThreadVolumeGeneration;
	if (m_pCutView != nullptr) delete m_pCutView;
	if (m_pVolumeData != nullptr) delete m_pVolumeData;
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
		PLOGI.printf("Read dispersion from .oct file.");
		calibration->Initialize((char*)pData->GetExtraData(OCTHeader::ExtraData::Dispersion));
	}
	else 
	{
		PLOGI.printf("Read dispersion from .dat file.");
		calibration->Initialize(_T("CALIBRATION.DAT"));
	}

	USHORT* background = nullptr;
	if (pData != nullptr && pData->GetExtraData(OCTHeader::ExtraData::Background) != nullptr) 
	{
		PLOGI.printf("Read background from .oct file.");
		background = new USHORT[setting.nBufferSize];
		memcpy(background, pData->GetExtraData(OCTHeader::ExtraData::Background), sizeof(USHORT) * setting.nBufferSize);
	}
	else 
	{
		PLOGI.printf("Read background from .dat file.");
		background = readBackground("BACKGROUND.bin", setting);
	}

	PLOGI.printf("Create Imaging - %d x %d (type: %d)", setting.nAScan, setting.nBScan, type);

	switch (type)
	{
	case ImagingType::OCTImaging:
		pImaging = new COCTImaging(setting, msg);
		pImaging->Initialize(calibration);
		break;
	case ImagingType::LabImaging:
		pImaging = new CLabImaging(setting, msg);
		((CLabImaging *)pImaging)->Initialize(calibration, background);
		((CLabImaging *)pImaging)->SetBackgroundSubtract(false);
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

RayError CImagingSession::Start() {
	if (m_pImaging == nullptr) return RayError::InvalidFunctionCall;

	CUtility::StopThread(m_pThreadImaging);
	CUtility::StartThread(threadImaging, m_pThreadImaging, this);

	return RayError::OK;
}

RayError CImagingSession::Stop() {
	CUtility::StopThread(m_pThreadUpdateCutView);
	CUtility::StopThread(m_pThreadObjectDetection);
	CUtility::StopThread(m_pThreadVolumeGeneration);
	CUtility::StopThread(m_pThreadImaging);

	return RayError::OK;
}

void CImagingSession::StartCutViewUpdate(cv::Scalar backgroundColor) {
	if (m_pThreadUpdateCutView != nullptr) return;

	InitCutView(backgroundColor);
	CUtility::StartThread(threadUpdateCutView, m_pThreadUpdateCutView, this);
}
void CImagingSession::StartObjectDetection() {
	if (m_pThreadObjectDetection != nullptr) return;
	CUtility::StartThread(threadDetectObject, m_pThreadObjectDetection, this);
}
void CImagingSession::StartVolumeGeneration() {
	if (m_pThreadVolumeGeneration != nullptr) return;
	CUtility::StartThread(threadGenerateVolume, m_pThreadVolumeGeneration, this);
}

bool CImagingSession::IsProcessed(int nFrame) {
	std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
	return (it != m_mapImage.end());
}
cv::Mat CImagingSession::PostProcess(int nFrame) {
	std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
	if (it != m_mapImage.end()) {
		m_pImaging->PostProcess(it->second);
	}
	return m_pImaging->GetCircleImage();
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
	cv::Mat imgResult = m_pImaging->GetProcessedImage().clone();

	m_pImaging->PostProcess(imgResult);

	std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
	if (it != m_mapImage.end())
	{
		it->second = imgResult;
	}
	else {
		m_mapImage.insert(std::make_pair(nFrame, imgResult));
	}

	return m_pImaging->GetCircleImage().data;
}
void CImagingSession::InitCutView(cv::Scalar backgroundColor) {
	if (m_pCutView != nullptr) delete m_pCutView;

	m_pCutView = new CCutViewManager();
	m_pCutView->Initialize(m_pDataManager->GetNumOfSamples(), backgroundColor);
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
void CImagingSession::AddFramesIntoCutView() {
	if (m_pCutView == nullptr) return;

	cv::Mat imgCircle;
	for (int nFrame = 0; nFrame < m_pCutView->GetNumOfSamples(); nFrame++)
	{
		std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
		if (it != m_mapImage.end())
		{
			m_pImaging->CircularizeImage(it->second, imgCircle);
			m_pCutView->AddRecord(imgCircle, nFrame);
		}
	}
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

int CImagingSession::GetNumOfSidebranchContourSize(int nFrame){
	if (m_vSidebranch.size() <= nFrame) return 0;

	return m_vSidebranch.at(nFrame).size();
}

void* CImagingSession::GetSidebranchContour(int nFrame, int nSb){
	if (m_vSidebranch.size() <= nFrame) return nullptr;
	if (m_vSidebranch.at(nFrame).size() <= 0) return nullptr;

	cv::Mat matContour = m_vSidebranch.at(nFrame).at(nSb);
	return matContour.ptr();
}

int CImagingSession::GetNumOfSidebranchContourPoints(int nFrame, int nSb){
	if (m_vSidebranch.size() <= nFrame) return 0;
	if (m_vSidebranch.at(nFrame).size() <= 0) return 0;

	cv::Mat matContour = m_vSidebranch.at(nFrame).at(nSb);
	return matContour.cols * matContour.rows;
}

void* CImagingSession::GetStentPoints(int nFrame){
	if (m_vStent.size() <= nFrame) return nullptr;

	cv::Mat mat = m_vStent.at(nFrame);
	return mat.ptr();
}

int CImagingSession::GetNumOfStentPoints(int nFrame){
	if (m_vStent.size() <= nFrame) return 0;

	cv::Mat mat = m_vStent.at(nFrame);
	return mat.cols * mat.rows;
}

void* CImagingSession::GetGuidewirePoints(int nFrame){
	if (m_vGuidewire.size() <= nFrame) return nullptr;

	cv::Mat mat = m_vGuidewire.at(nFrame);
	return mat.ptr();
}

int CImagingSession::GetNumOfGuidewirePoints(int nFrame){
	if (m_vGuidewire.size() <= nFrame) return 0;

	cv::Mat mat = m_vGuidewire.at(nFrame);
	return mat.cols * mat.rows;
}

CImagingSession* CImagingSession::createSession(CMessageService* pMsg, IImaging::Setting setting, int nSession, IDataManager* pData, bool deleteData, ImagingType type) {
	CImagingSession* pSession = new CImagingSession(pMsg, nSession, deleteData);

	pSession->m_imagingType = type;
	pSession->m_pDataManager = pData;
	pSession->m_pImaging = CreateColorImaging(pMsg, setting, pData, type);
	pSession->m_pImaging->SetSession(nSession);

	return pSession;
}
UINT CImagingSession::threadImaging(LPVOID param) {
	CImagingSession* pSession = (CImagingSession*)param;
	IDataManager* pDataManager = pSession->m_pDataManager;
	COCTImaging* pImaging = pSession->m_pImaging;
	CMessageService* pMsg = pSession->m_pMsg;

	pSession->m_mapImage.clear();
	const int nNumOfSamples = pDataManager->GetNumOfSamples();
	PLOGI.printf("Session #%d process oct imaging - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadImaging->isRun; nFrame++)
	{
		char* pBuffer = pDataManager->GetSample(nFrame);
		pImaging->Process(pBuffer);
		cv::Mat imgResult = pImaging->GetProcessedImage().clone();
		pSession->m_mapImage.insert(std::make_pair(nFrame, imgResult));
		pSession->m_mapSheathPosition.insert(std::make_pair(nFrame, pImaging->GetFoundSheathPosition()));
	}
	PLOGI.printf("Session #%d process oct imaging done.", pSession->m_nSession);
	pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::OCTImaging, pSession->m_nSession);
	
	return NOERROR;
}
UINT CImagingSession::threadUpdateCutView(LPVOID param) {
	CImagingSession* pSession = (CImagingSession*)param;
	IDataManager* pDataManager = pSession->m_pDataManager;
	int nSession = pSession->m_nSession;
	IImaging::Setting setting = pSession->m_pImaging->GetSetting();

	// apply brightness / contrast when DrawLongitudeImage is called.
	setting.brightness = 0.f;
	setting.contrast = 1.0f;

	// prepare imaging (without message)
	COCTImaging* pImaging = CreateColorImaging(nullptr, setting, pDataManager, pSession->GetImagingType());

	CCutViewManager* pCutView = pSession->m_pCutView;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();
	cv::Mat imgCircle;

	PLOGI.printf("Session #%d update cutview - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadUpdateCutView->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImage.find(nFrame);
		if (it == pSession->m_mapImage.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}
		pImaging->CircularizeImage(it->second, imgCircle);
		pCutView->AddRecord(imgCircle, nFrame);

		pSession->m_pMsg->postMessage(WM_PROCESS_CUTVIEW, nSession, nFrame);
	}
	delete pImaging;

	PLOGI.printf("Session #%d update cutview done.", pSession->m_nSession);
	pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::GenerateCutView, pSession->m_nSession);

	return NOERROR;
}
UINT CImagingSession::threadDetectObject(LPVOID param) {
	PLOGI.printf("threadDetectObject start\n");
	CImagingSession* pSession = (CImagingSession*)param;
	IDataManager* pDataManager = pSession->m_pDataManager;
	int nSession = pSession->m_nSession;

	// prepare imaging (without message)
	COCTImaging* pImaging = CreateColorImaging(nullptr, pSession->m_pImaging->GetSetting(), pDataManager, pSession->GetImagingType());

	IRayLearning* learning = IRayLearning::GetInstance();
	std::vector<std::vector<cv::Mat>>& vLumen = pSession->m_vLumen;
	std::vector<std::vector<cv::Mat>>& vSidebranch = pSession->m_vSidebranch;
	std::vector<cv::Mat>& vStent = pSession->m_vStent;
	std::vector<cv::Mat>& vGuidewire = pSession->m_vGuidewire;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();

	int imgSize = 1024;
	cv::Point center(imgSize / 2, imgSize / 2);
	
	//empty lumen
	std::vector<cv::Point> vEmptyLumen;

	//center point mask
	cv::Mat centerMask = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
	cv::circle(centerMask, center, 1, cv::Scalar(255), cv::FILLED);
	
	CLookUpTable& lut = CLookUpTable::GetInstance();

	PLOGI.printf("Session #%d lumen detection start - %d frames", pSession->m_nSession, nNumOfSamples);
	vLumen.clear();
	vSidebranch.clear();
	vStent.clear();
	vGuidewire.clear();
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadObjectDetection->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImage.find(nFrame);
		if (it == pSession->m_mapImage.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}
		pImaging->PostProcess(it->second);

		cv::Mat circleImage;
		pImaging->CircularizeImage(it->second, circleImage);
		cv::cvtColor(circleImage, circleImage, cv::COLOR_GRAY2BGR);
		//lut.Apply(circleImage, 3/*ML LUT*/);

		//lumen
		cv::Mat contourImage = learning->FindLumen(circleImage);		
		std::vector<std::vector<cv::Point>> vContours;
		cv::findContours(contourImage, vContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

		if (vContours.size() == 0) {
			vContours.clear();
			vContours.push_back(vEmptyLumen);
			pImaging->SetLumenContourOffset(vEmptyLumen);
		}
		else {
			std::vector<cv::Point> validContour;
			cv::Mat andResult;
			cv::Mat xorResult;

			//find contour which contains center point
			int idx = -1;
			for (int i = 0; i < vContours.size(); i++) {
				cv::Mat curContour = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
				cv::drawContours(curContour, vContours, i, cv::Scalar(255), cv::FILLED);

				cv::bitwise_and(centerMask, curContour, andResult);
				cv::bitwise_xor(centerMask, andResult, xorResult);

				if (cv::countNonZero(xorResult) == 0) {
					validContour = vContours[i];
					idx = i;
					break;
				}
			}

			if (idx != -1) {
				//removal of the outer part of the circle(OCT cross-section)
				cv::Mat mask1 = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
				cv::Point center(imgSize / 2, imgSize / 2);
				cv::circle(mask1, center, imgSize / 2, cv::Scalar(255), cv::FILLED);

				cv::Mat mask2 = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
				cv::drawContours(mask2, vContours, idx, cv::Scalar(255), cv::FILLED);

				cv::bitwise_and(mask2, mask1, andResult);
				cv::bitwise_xor(mask2, andResult, xorResult);
				bool isCompletelyContained = cv::countNonZero(xorResult) == 0;

				vContours.clear();
				if (isCompletelyContained) {
					vContours.push_back(validContour);
					pImaging->SetLumenContourOffset(validContour);
				}
				else {
					std::vector<std::vector<cv::Point>> vCircle;
					cv::findContours(andResult, vCircle, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

					if (vCircle.size() == 0) {
						vContours.push_back(vEmptyLumen);
						pImaging->SetLumenContourOffset(vEmptyLumen);
					}
					else {
						//find contour which contains center point
						idx = -1;
						for (int i = 0; i < vCircle.size(); i++) {
							cv::Mat curContour = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
							cv::drawContours(curContour, vCircle, i, cv::Scalar(255), cv::FILLED);

							cv::bitwise_and(centerMask, curContour, andResult);
							cv::bitwise_xor(centerMask, andResult, xorResult);

							if (cv::countNonZero(xorResult) == 0) {
								validContour = vCircle[i];
								idx = i;
								break;
							}
						}

						if (idx != -1) {
							vContours.push_back(validContour);
							pImaging->SetLumenContourOffset(validContour);
						}
						else {
							vContours.push_back(vEmptyLumen);
							pImaging->SetLumenContourOffset(vEmptyLumen);
						}
					}
				}
			}
			else {
				vContours.clear();
				vContours.push_back(vEmptyLumen);
				pImaging->SetLumenContourOffset(vEmptyLumen);
			}
		}

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

		//sidebranch
		cv::Mat contourSb = learning->FindSidebranch();
		std::vector<std::vector<cv::Point>> vSbContours;
		cv::findContours(contourSb, vSbContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

		std::vector<cv::Mat> vSidebranchs;
		for (int i = 0; i < vSbContours.size(); i++) {
			std::vector<cv::Point> contour = vSbContours.at(i);
			cv::Mat matContour(contour.size(), 1, CV_32SC2);
			for (size_t row = 0; row < contour.size(); row++) {
				matContour.at<cv::Point>(row, 0) = contour[row];
			}
			vSidebranchs.push_back(matContour);
		}
		vSidebranch.push_back(vSidebranchs);

		//stent
		std::vector<cv::Rect2f> vStents = learning->FindStent(circleImage);
		cv::Mat mStent(vStents.size(), 1, CV_32SC2);
		for (size_t row = 0; row < vStents.size(); row++) {
			mStent.at<cv::Point>(row, 0) = cv::Point(vStents[row].x + vStents[row].width / 2, vStents[row].y + vStents[row].height / 2);
		}
		
		pImaging->EraseStentOutLier(mStent);
		
		vStent.push_back(mStent);

		//guidewire
		std::vector<cv::Rect2f> vGuidewires = learning->FindGuidewire();
		cv::Mat mGuidewire(vGuidewires.size(), 1, CV_32SC2);
		for (size_t row = 0; row < vGuidewires.size(); row++) {
			//TODO - Rect 영역 내에서 GW 테두리 분석해서 중점 찾는 로직 필요
			mGuidewire.at<cv::Point>(row, 0) = cv::Point(vGuidewires[row].x + vGuidewires[row].width / 2, vGuidewires[row].y + vGuidewires[row].height / 2);
		}
		vGuidewire.push_back(mGuidewire);

		pSession->m_pMsg->postMessage(WM_PROCESS_DETECTION, nSession, nFrame);
	}
	delete pImaging;

	PLOGI.printf("Session #%d lumen detection done.", pSession->m_nSession);
	pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::DetectLumen, pSession->m_nSession);

	PLOGI.printf("threadDetectObject end\n");
	return NOERROR;
}
UINT CImagingSession::threadGenerateVolume(LPVOID param) {
	CImagingSession* pSession = (CImagingSession*)param;
	IDataManager* pDataManager = pSession->m_pDataManager;
	int nSession = pSession->m_nSession;

	// prepare imaging (without message)
	COCTImaging* pImaging = CreateColorImaging(nullptr, pSession->m_pImaging->GetSetting(), pDataManager, pSession->GetImagingType());

	CConfiguration& config = CConfiguration::GetInstance();
	const int nNumOfSamples = pDataManager->GetNumOfSamples();
	const int nDiameter = config.volume.size;
	const int nImageSize = nDiameter * nDiameter;
	cv::Mat imgCircle, imgResize;

	if (pSession->m_pVolumeData != nullptr)
	{
		delete[] pSession->m_pVolumeData;
	}
	pSession->m_pVolumeData = new char[nImageSize * nNumOfSamples];

	PLOGI.printf("Session #%d volume generation start - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadVolumeGeneration->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImage.find(nFrame);
		if (it == pSession->m_mapImage.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}
		
		cv::Mat imgRect = it->second.clone();
		pImaging->CircularizeImage(imgRect, imgCircle);

		// remove sheath
		int nSheathPos = config.measurement.nSheathPosition + 15;
		//int nSheathPos = pSession->m_mapSheathPosition.find(nFrame)->second;
		cv::circle(imgCircle, cv::Point(imgCircle.cols / 2, imgCircle.rows / 2), nSheathPos / 2, cv::Scalar(0, 0, 0), -1);

		cv::resize(imgCircle, imgResize, cv::Size(nDiameter, nDiameter));
		memcpy(pSession->m_pVolumeData + nImageSize * nFrame, imgResize.data, nImageSize);
	}
	delete pImaging;

	PLOGI.printf("Session #%d volume generation done.", pSession->m_nSession);
	pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::GenerateVolume, pSession->m_nSession);

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