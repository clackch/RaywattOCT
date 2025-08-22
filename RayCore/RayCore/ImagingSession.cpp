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
#include <string>

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

CImagingSession* CImagingSession::CreateSession(CMessageService* pMsg, int nSession, const char* strFilePath, double imageResolution) {
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
	setting.distPerPixel = (imageResolution * 1000.f / 2.f); // imageResolution: 1024x1024 circle 기준 (mm per pixel)
	PLOGI.printf("%s opened - %d x %d (%d frames)", strFilePath, setting.nAScan, setting.nBScan, nNumOfSamples);

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

void CImagingSession::StopThreadForRestart()
{
	CUtility::StopThread(m_pThreadUpdateCutView);
	CUtility::StopThread(m_pThreadObjectDetection);
	CUtility::StopThread(m_pThreadVolumeGeneration);
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
		cv::Mat imgZOffset;
		m_pImaging->ApplyZOffset(it->second, imgZOffset, GetZOffset(nFrame));
		m_pImaging->PostProcess(imgZOffset);
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

	std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
	if (it != m_mapImage.end())
	{
		it->second = imgResult;
	}
	else {
		m_mapImage.insert(std::make_pair(nFrame, imgResult));
	}

	cv::Mat imgZOffset;
	m_pImaging->ApplyZOffset(imgResult, imgZOffset, GetZOffset(nFrame));
	m_pImaging->PostProcess(imgZOffset);

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

	cv::Mat imgZOffset, imgCircle;
	for (int nFrame = 0; nFrame < m_pCutView->GetNumOfSamples(); nFrame++)
	{
		std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
		if (it != m_mapImage.end())
		{
			m_pImaging->ApplyZOffset(it->second, imgZOffset, GetZOffset(nFrame));
			m_pImaging->CircularizeImage(imgZOffset, imgCircle);
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

bool CImagingSession::LoadZOffset(const char* strDataFilePath) {
	std::string strPath(strDataFilePath);
	std::string strZOffsetFilePath = strPath.substr(0, strPath.size() - 3).append("cal");

	int nNumOfSamples = (m_pDataManager == nullptr) ? 0 : m_pDataManager->GetNumOfSamples();
	m_vZOffset.clear();

	FILE* fp = fopen(strZOffsetFilePath.c_str(), "r");
	if (fp) {
		PLOGI.printf("ZOffset file loaded: %s", strZOffsetFilePath.c_str());
		for (int i = 0; i < nNumOfSamples; i++) {
			int offset = 0;
			fscanf(fp, "%d,", &offset);
			//PLOGI.printf("%d", offset);

			m_vZOffset.push_back(offset);
		}
		fclose(fp);
		PLOGI.printf("ZOffset file loaded: %s done.", strZOffsetFilePath.c_str());

		return true;
	}

	return true;
}

int CImagingSession::GetZOffset(int nFrame) {
	if (m_pDataManager == nullptr) return 0;
	if (m_vZOffset.size() != m_pDataManager->GetNumOfSamples()) return GetZOffset();

	return m_vZOffset.at(nFrame);

}

 void* CImagingSession::GetGuidewireRadius(int nFrame) {
	if (m_vGuidewireRadius.size() <= nFrame) return 0;

	auto& vRadius = m_vGuidewireRadius.at(nFrame);

	return static_cast<void*>(vRadius.data());
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
	pSession->m_mapImageWithoutCompensation.clear();
	const int nNumOfSamples = pDataManager->GetNumOfSamples();
	PLOGI.printf("Session #%d process oct imaging - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadImaging->isRun; nFrame++)
	{
		char* pBuffer = pDataManager->GetSample(nFrame);
		pImaging->Process(pBuffer);
		cv::Mat imgResult = pImaging->GetProcessedImage().clone();
		pSession->m_mapImage.insert(std::make_pair(nFrame, imgResult));
		cv::Mat imgResultWithoutCompensation = pImaging->GetWithoutCompensationImage().clone();
		pSession->m_mapImageWithoutCompensation.insert(std::make_pair(nFrame, imgResultWithoutCompensation));
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
	cv::Mat imgCircle, imgZOffset;

	PLOGI.printf("Session #%d update cutview - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadUpdateCutView->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImage.find(nFrame);
		if (it == pSession->m_mapImage.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}

		pImaging->ApplyZOffset(it->second, imgZOffset, pSession->GetZOffset(nFrame));
		pImaging->CircularizeImage(imgZOffset, imgCircle);
		pCutView->AddRecord(imgCircle, nFrame);

		pSession->m_pMsg->postMessage(WM_PROCESS_CUTVIEW, nSession, nFrame);
	}
	delete pImaging;

	PLOGI.printf("Session #%d update cutview done.", pSession->m_nSession);
	pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::GenerateCutView, pSession->m_nSession);

	return NOERROR;
}

// ===================== 분류 로직 =====================
inline AreaResult classify_by_area_only(const std::vector<cv::Point>& contour, const cv::Size& frameSize, const AreaParams& P = AreaParams{}){
	AreaResult R;
	R.center = cv::Point2f(frameSize.width / 2.f, frameSize.height / 2.f);
	R.Rfov = 0.5 * std::min(frameSize.width, frameSize.height);
	R.fovArea = CV_PI * R.Rfov * R.Rfov;

	if (contour.size() < 3 || R.fovArea <= 0.0) {
		R.decision = AreaDecision::Invalid;
		return R;
	}

	R.contourArea = std::abs(cv::contourArea(contour));
	R.areaFrac = (R.fovArea > 1e-9) ? (R.contourArea / R.fovArea) : 0.0;

	// 순서: 작은 면적(=sheath) 우선, 다음 큰 면적
	if (R.areaFrac <= std::max(0.0, P.sheathAreaFracMax)) {
		R.decision = AreaDecision::ExcludeSheath;
	}
	else if (R.areaFrac >= std::min(1.0, P.areaFracMax)) {
		R.decision = AreaDecision::ExcludeTooLarge;
	}
	else {
		R.decision = AreaDecision::Accept;
	}
	return R;
}

// 면적 비율(f) ↔ 반지름: π r^2 = f * π Rfov^2  ⇒  r = Rfov * sqrt(f)
static inline int radius_from_frac(double Rfov, double frac) {
	frac = std::max(0.0, std::min(1.0, frac));
	return (int)std::round(Rfov * std::sqrt(frac));
}

// ===================== 시각화 =====================
inline void draw_area_thresholds_and_contour(cv::Mat& imgBgr, const std::vector<cv::Point>& contour, const AreaParams& P, const AreaResult& R){
	if (imgBgr.empty()) return;
	if (imgBgr.channels() == 1) cv::cvtColor(imgBgr, imgBgr, cv::COLOR_GRAY2BGR);

	// 1) FOV 원(회색 점선 느낌)
	cv::circle(imgBgr, R.center, (int)std::round(R.Rfov), cv::Scalar(160, 160, 160), 1, cv::LINE_AA);

	// 2) 임계 원들
	const int rSheath = radius_from_frac(R.Rfov, P.sheathAreaFracMax);
	const int rMax = radius_from_frac(R.Rfov, P.areaFracMax);

	if (rSheath > 0)
		cv::circle(imgBgr, R.center, rSheath, cv::Scalar(255, 0, 0), 2, cv::LINE_AA); // 작은 면적 한계

	if (rMax > 0)
		cv::circle(imgBgr, R.center, rMax, cv::Scalar(255, 0, 0), 2, cv::LINE_AA); // 큰 면적 한계

	// 3) 컨투어 (분류별 색)
	cv::Scalar col =
		(R.decision == AreaDecision::ExcludeSheath) ? cv::Scalar(0, 255, 255) :   // 노랑-초록
		(R.decision == AreaDecision::ExcludeTooLarge) ? cv::Scalar(0, 0, 255) :   // 빨강
		(R.decision == AreaDecision::Accept) ? cv::Scalar(0, 255, 0) :   // 초록
		cv::Scalar(255, 255, 255);  // 흰색(Invalid)

	if (!contour.empty()) {
		std::vector<std::vector<cv::Point>> cs{ contour };
		cv::drawContours(imgBgr, cs, 0, col, 2, cv::LINE_AA);
	}
}

int CImagingSession::IsLumenNormal(cv::Mat image, std::vector<cv::Point> contour, double lumenThresholdMin, double lumenThresholdMax, bool showLumenGuide){
	AreaParams ap;
	ap.sheathAreaFracMax = lumenThresholdMin;
	ap.areaFracMax = lumenThresholdMax;

	AreaResult result = classify_by_area_only(contour, image.size(), ap);
	
	if(showLumenGuide)
		draw_area_thresholds_and_contour(image, contour, ap, result);

	if (result.decision == AreaDecision::Accept)
		return 1;
	else
		return 0;
}

std::vector<cv::Point> CImagingSession::GetValidLumenContour(const cv::Mat& imageResultWithoutCompensation, int imgSize, const cv::Mat& centerMask, const cv::Ptr<cv::CLAHE>& clahe, IRayLearning* learning, COCTImaging* pImaging){
	cv::Mat enhancedImage;
	clahe->apply(imageResultWithoutCompensation, enhancedImage);
	pImaging->CircularizeImage(enhancedImage, enhancedImage);
	cv::cvtColor(enhancedImage, enhancedImage, cv::COLOR_GRAY2BGR);

	cv::Mat contourImage = learning->FindLumen(enhancedImage);

	std::vector<std::vector<cv::Point>> vContours;
	cv::findContours(contourImage, vContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

	std::vector<cv::Point> validContour;

	if (vContours.size() > 0) {
		cv::Mat andResult;
		cv::Mat xorResult;

		// find contour which contains center point
		int idx = 0;
		for (int i = 0; i < (int)vContours.size(); i++) {
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

		if (!validContour.empty()) {
			// removal of the outer part of the circle (OCT cross-section)
			cv::Mat mask1 = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
			cv::Point center(imgSize / 2, imgSize / 2);
			cv::circle(mask1, center, imgSize / 2, cv::Scalar(255), cv::FILLED);

			cv::Mat mask2 = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
			cv::drawContours(mask2, vContours, idx, cv::Scalar(255), cv::FILLED);

			cv::Mat andResult2, xorResult2;
			cv::bitwise_and(mask2, mask1, andResult2);
			cv::bitwise_xor(mask2, andResult2, xorResult2);
			bool isCompletelyContained = cv::countNonZero(xorResult2) == 0;

			vContours.clear();
			if (!isCompletelyContained) {
				std::vector<std::vector<cv::Point>> vCircle;
				cv::findContours(andResult2, vCircle, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

				if (vCircle.size() > 0) {
					// find contour which contains center point
					for (int i = 0; i < (int)vCircle.size(); i++) {
						cv::Mat curContour = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
						cv::drawContours(curContour, vCircle, i, cv::Scalar(255), cv::FILLED);

						cv::bitwise_and(centerMask, curContour, andResult);
						cv::bitwise_xor(centerMask, andResult, xorResult);

						if (cv::countNonZero(xorResult) == 0) {
							validContour = vCircle[i];
							break;
						}
					}
				}
			}
		}
	}

	return validContour;
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
	std::vector<std::vector<float>>& vGuidewireRadius = pSession->m_vGuidewireRadius;
	const int nNumOfSamples = pDataManager->GetNumOfSamples();
	cv::Mat circleImage, imgZOffset, enhancedImage;

	int imgSize = 1024;
	cv::Point center(imgSize / 2, imgSize / 2);
	
	//empty lumen
	std::vector<cv::Point> vEmptyLumen;

	//center point mask
	cv::Mat centerMask = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
	cv::circle(centerMask, center, 1, cv::Scalar(255), cv::FILLED);

	cv::Ptr<cv::CLAHE> clahe = cv::createCLAHE(3.5, cv::Size(4, 4));
	
	CLookUpTable& lut = CLookUpTable::GetInstance();

	PLOGI.printf("Session #%d lumen detection start - %d frames", pSession->m_nSession, nNumOfSamples);
	vLumen.clear();
	vSidebranch.clear();
	vStent.clear();
	vGuidewire.clear();
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadObjectDetection->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImageWithoutCompensation.find(nFrame);
		if (it == pSession->m_mapImageWithoutCompensation.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}

		pImaging->ApplyZOffset(it->second, imgZOffset, pSession->GetZOffset(nFrame));
		pImaging->CircularizeImage(imgZOffset, circleImage);
		cv::cvtColor(circleImage, circleImage, cv::COLOR_GRAY2BGR);

		//lumen
		std::vector<cv::Point> validContour = pSession->GetValidLumenContour(imgZOffset, imgSize, centerMask, clahe, learning, pImaging);

		std::vector<std::vector<cv::Point>> vContours;
		if (!validContour.empty()) {
			vContours.push_back(validContour);
			pImaging->SetLumenContourOffset(validContour);
		}
		else {
			vContours.clear();
			vContours.push_back(vEmptyLumen);
			pImaging->SetLumenContourOffset(vEmptyLumen);
		}

		//Test
		if (false) {//!validContour.empty()) {
			pSession->IsLumenNormal(circleImage, validContour, 0.01, 0.30, true);
			cv::imwrite(cv::format("./test/%06d.png", nFrame), circleImage);
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

		//pImaging->EraseStentOutLier(mStent);

		vStent.push_back(mStent);

		//guidewire
		std::vector<cv::Rect2f> vGuidewires = learning->FindGuidewire();

		std::vector<cv::Point> centerPoints;
		std::vector<float> Radius;
		cv::Mat mGuidewire(vGuidewires.size(), 1, CV_32SC2);

		/*cv::Mat mask3 = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
		for (int i = 0; i < vGuidewires.size(); i++) {
			cv::rectangle(mask3, vGuidewires[i], cv::Scalar(255), -1);
		}
		std::vector<std::vector<cv::Point>> realContours;
		cv::Mat imgCheck = circleImage.clone();
		cv::findContours(mask3, realContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);
		cv::drawContours(imgCheck, realContours, -1, cv::Scalar(0, 255, 0), 2);

		string check = "GW Center Image" + std::to_string(nFrame) + ".png";
		cv::imwrite(check, imgCheck);*/

		pImaging->GetGuideWireCenterPoint(circleImage, vGuidewires, centerPoints, Radius);

		if (centerPoints.size() > 0)
		{
			for (size_t row = 0; row < vGuidewires.size(); row++) {
				mGuidewire.at<cv::Point>(row, 0) = cv::Point(centerPoints[row].x, centerPoints[row].y);
				if (Radius[row] < 0) continue;
				cv::circle(circleImage, centerPoints[row], static_cast<int>(Radius[row]), cv::Scalar(0, 255, 0), 2);
			}
			vGuidewire.push_back(mGuidewire);
			vGuidewireRadius.push_back(Radius);
		}
		else {
			vGuidewire.push_back(mGuidewire);
			vGuidewireRadius.push_back(std::vector<float>(1));
		}

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
	cv::Mat imgCircle, imgResize, imgZOffset;

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
		
		pImaging->ApplyZOffset(it->second, imgZOffset, pSession->GetZOffset(nFrame));
		pImaging->CircularizeImage(imgZOffset, imgCircle);

		// remove sheath
		int nSheathPos = config.measurement.nSheathPosition + 15;
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