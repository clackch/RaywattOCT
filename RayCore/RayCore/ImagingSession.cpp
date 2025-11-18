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
#include <limits>
#include <cmath>
#include <exception>

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
		if (header.width == 0 || header.height == 0) {
			PLOGI.printf("The OCT file header is abnormal");
			return nullptr;
		}
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

	// 여기서 열려고 했는데, static이라서 안됨..

	//std::string strPath(strFilePath);
	//std::string strZOffsetFilePath = strPath.substr(0, strPath.size() - 3).append("zOffset");
	//if (LoadZOffset(strZOffsetFilePath)) {
	//	//pSession->CalculateZOffset(pSession->GetDataManager()->GetNumOfSamples(), m_autoCalibPatch, strZOffsetFilePath);
	//}

	return createSession(pMsg, setting, nSession, pReader, true, type);
}

COCTImaging* CImagingSession::CreateColorImaging(CMessageService* msg, IImaging::Setting setting, IDataManager* pData, ImagingType type) {
	CConfiguration& config = CConfiguration::GetInstance();
	COCTImaging* pImaging = nullptr;

	// codesonar suppr C++-resource-leaknew
	CCalibration* calibration = new CCalibration(setting.nAScan, setting.nFFTLength);
	if (pData != nullptr && pData->GetExtraData(OCTHeader::ExtraData::Dispersion) != nullptr)
	{
		PLOGI.printf("Read dispersion from .oct file.");
		calibration->Initialize((char*)pData->GetExtraData(OCTHeader::ExtraData::Dispersion));
	}
	else 
	{
		PLOGI.printf("Read dispersion from .dat file.");
		tstring strCalibPath = config.configPath + _T("\\CALIBRATION.DAT");
		calibration->Initialize(strCalibPath);
	}

	USHORT* background = nullptr;
	if (pData != nullptr && pData->GetExtraData(OCTHeader::ExtraData::Background) != nullptr)
	{
		PLOGI.printf("Read background from .oct file.");
		if (setting.nBufferSize > 1024 * 1024 * 3) {
			PLOGI.printf("BufferSize is too big : %d", setting.nBufferSize);
			return nullptr;
		}
		background = new USHORT[setting.nBufferSize];
		memcpy(background, pData->GetExtraData(OCTHeader::ExtraData::Background), sizeof(USHORT) * setting.nBufferSize);
	}
	else
	{
		PLOGI.printf("Read background from .dat file.");
		tstring strBackgroundPath = config.configPath + _T("\\BACKGROUND.bin");

		char strPath[MAX_PATH + 1] = { 0 };
		int len = WideCharToMultiByte(CP_ACP, 0, strBackgroundPath.c_str(), strBackgroundPath.length(), strPath, MAX_PATH, nullptr, nullptr);
		background = readBackground(strPath, setting);
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
		/* threadImaging에서 이미 ZOffset을 적용했으므로 주석처리
		cv::Mat imgZOffset;
		m_pImaging->ApplyZOffset(it->second, imgZOffset, GetZOffset(nFrame));
		m_pImaging->PostProcess(imgZOffset);*/
		
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
	CConfiguration& config = CConfiguration::GetInstance();

	m_pImaging->Process(pBuffer);
	cv::Mat imgResult = m_pImaging->GetProcessedImage().clone();

	// buffer에서 다시 가져오는거라.. 원본에 ZOffset 적용 안된 상태

	cv::Mat imgZOffset;

	// ZOffset 파일에서 읽어오는 상태면, 계산 안해도 됨..
	//int nowZOffset = CalculateZOffset(imgResult, m_autoCalibPatch);

	//imgZOffset = imgResult.clone();
	if (config.measurement.calPerFrame) {
		m_pImaging->ApplyZOffset(imgResult, imgZOffset, GetZOffset(nFrame) + GetZOffset());
	}
	else {
		m_pImaging->ApplyZOffset(imgResult, imgZOffset, GetZOffset());
	}

	std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
	if (it != m_mapImage.end())
	{
		it->second = imgZOffset;
	}
	else {
		m_mapImage.insert(std::make_pair(nFrame, imgZOffset));
	}
	
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

	cv::Mat /*imgZOffset, */imgCircle;
	for (int nFrame = 0; nFrame < m_pCutView->GetNumOfSamples(); nFrame++)
	{
		std::map<int, cv::Mat>::iterator it = m_mapImage.find(nFrame);
		if (it != m_mapImage.end())
		{
			// threadImaging에서 이미 ZOffset을 적용했으므로 주석처리
			//m_pImaging->ApplyZOffset(it->second, imgZOffset, GetZOffset(nFrame));
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

bool CImagingSession::LoadZOffset(const std::string strDataFilePath) {
	PLOGI.printf("LoadZOffset: start loading from %s", strDataFilePath.c_str());
	int nNumOfSamples = (m_pDataManager == nullptr) ? 0 : m_pDataManager->GetNumOfSamples();
	m_vZOffset.clear();
	m_strDataFilePath = strDataFilePath;

	FILE* fp = fopen(strDataFilePath.c_str(), "r");
	if (fp) {
		PLOGI.printf("ZOffset file loaded: %s", strDataFilePath.c_str());
		for (int i = 0; i < nNumOfSamples; i++) {
			int offset = 0;
			int ret = fscanf(fp, "%d,", &offset);

			if (ret != 1) {
				if (ret == EOF) {
					PLOGI.printf("fscanf failed or reached EOF");
					return false;
				}
			}
			else {
				PLOGI.printf("fscanf: expected 1 item, got %d\n", ret);
			}

			m_vZOffset.push_back(offset);
		}
		fclose(fp);
		PLOGI.printf("ZOffset file loaded: %s done.", strDataFilePath.c_str());

		return true;
	}

	return false;
}

int CImagingSession::GetZOffset(int nFrame) {
	if (m_pDataManager == nullptr || m_vZOffset.size() != m_pDataManager->GetNumOfSamples()) return 0;
	//if (m_vZOffset.size() != m_pDataManager->GetNumOfSamples()) return GetZOffset();

	return m_vZOffset.at(nFrame);

}

int CImagingSession::CalculateZOffset(const cv::Mat image, const cv::Mat autoCalibPatch) {
	if (autoCalibPatch.empty()) return 0;
	auto start = std::chrono::high_resolution_clock::now();

	cv::Mat img = image.clone();
	cv::rotate(img, img, cv::ROTATE_90_COUNTERCLOCKWISE);
	cv::Rect roi(0, 0, img.cols, img.rows/2);
	img = img(roi);
	cv::Rect zeroRegion(0, 0, img.cols, 30);
	img(zeroRegion).setTo(cv::Scalar::all(0));
	//cv::imwrite("CalculateZOffset_origin.tif", img);

	//cv::imwrite("CheckSheathPixels_origin" + std::to_string(i) + ".tif", img);
	if (img.type() == CV_8U)
		img.convertTo(img, CV_32F, 1.0 / 255.0);
	else if (img.type() == CV_32F) {}
	else {
		PLOGI.printf("CalculateZOffset: Unsupported image type");
	}

	cv::Mat result;
	cv::matchTemplate(img, autoCalibPatch, result, cv::TM_CCOEFF_NORMED);

	cv::Mat mask = result != 1.0f;
	double maxVal; cv::Point maxLoc;
	cv::minMaxLoc(result, nullptr, &maxVal, nullptr, &maxLoc, mask);

	int nowRow = 0, idealRow = 25;
	if (maxVal < 0.6) {
		PLOGI.printf("CalculateZOffset: maxRowVal is too small - %lf", maxVal);
		return 0;
	}
	else
	{
		/* section을 나눠 sheath 파악 안정성 추가*/
		int validCount = 0, sectionDivision = 6, height = result.rows, width = result.cols / sectionDivision;
		for (int i = 0; i < sectionDivision; i++)
		{
			cv::Mat section = result(cv::Rect(i * width, 0, width, height));
			cv::Mat sectionMask = section != 1.0f;
			cv::Point sectionMaxLoc;
			cv::minMaxLoc(section, nullptr, &maxVal, nullptr, &sectionMaxLoc, sectionMask);
			PLOGI.printf("CalculateZOffset: x %d, y %d, value %lf", sectionMaxLoc.x, sectionMaxLoc.y, maxVal);
			if (std::abs(sectionMaxLoc.y - maxLoc.y) < 15 && maxVal > 0.6)
			{
				nowRow += sectionMaxLoc.y;
				validCount++;
				PLOGI.printf("Valid");
			}
		}
		if (validCount > 0)
			nowRow /= validCount;
		else
			return 0;
	}
	int nowZOffset = idealRow - nowRow;
	PLOGI.printf("CalculateZOffset: nowRow %d, idealRow %d, zOffset %d", nowRow, idealRow, nowZOffset);
	auto end = std::chrono::high_resolution_clock::now();
	std::chrono::duration<double, std::milli> elapsed = end - start;
	PLOGI.printf("CalculateZOffset: frame done in %f ms.", elapsed.count());
	return nowZOffset;
}

bool CImagingSession::SaveZOffset(const std::string strDataFilePath) {
	PLOGI.printf("SaveZOffset: start saving to %s", strDataFilePath.c_str());
	FILE* fp = fopen(strDataFilePath.c_str(), "w+");
	if (fp) {
		PLOGI.printf("ZOffset file opened: %s", strDataFilePath.c_str());
		for (size_t i = 0; i < m_vZOffset.size(); i++) {
			if (fprintf(fp, "%d,", m_vZOffset[i]) < 0) {
				PLOGE.printf("fprintf failed at index %zu", i);
				fclose(fp);
				return false;
			}
		}
		fclose(fp);
		PLOGI.printf("ZOffset file saved: %s done.", strDataFilePath.c_str());
		return true;
	}
	return false;
}

 void* CImagingSession::GetGuidewireRadius(int nFrame) {
	if (m_vGuidewireRadius.size() <= nFrame) return 0;

	auto& vRadius = m_vGuidewireRadius.at(nFrame);

	return static_cast<void*>(vRadius.data());
 }

CImagingSession* CImagingSession::createSession(CMessageService* pMsg, IImaging::Setting setting, int nSession, IDataManager* pData, bool deleteData, ImagingType type) {
	// codesonar suppr C resource-leak
	CImagingSession* pSession = new CImagingSession(pMsg, nSession, deleteData);

	pSession->m_imagingType = type;
	pSession->m_pDataManager = pData;
	pSession->m_pImaging = CreateColorImaging(pMsg, setting, pData, type);

	if (pSession->m_pImaging == nullptr) {
		PLOGI.printf("ImagingSession is not initialized");
		return nullptr;
	}
	pSession->m_pImaging->SetSession(nSession);

	return pSession;
}
UINT CImagingSession::threadImaging(LPVOID param) {
	CConfiguration& config = CConfiguration::GetInstance();
	CImagingSession* pSession = (CImagingSession*)param;
	IDataManager* pDataManager = pSession->m_pDataManager;
	COCTImaging* pImaging = pSession->m_pImaging;
	CMessageService* pMsg = pSession->m_pMsg;

	pSession->m_mapImage.clear();
	pSession->m_mapImageWithoutCompensation.clear();
	const int nNumOfSamples = pDataManager->GetNumOfSamples();
	PLOGI.printf("Session #%d process oct imaging - %d frames", pSession->m_nSession, nNumOfSamples);
	// zOffset file load
	if( pSession->m_vZOffset.size() != nNumOfSamples) {
		PLOGI.printf("ZOffset size (%d) is different from number of samples (%d). Resetting ZOffset.", pSession->m_vZOffset.size(), nNumOfSamples);
		pSession->m_vZOffset.clear();
	}
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadImaging->isRun; nFrame++)
	{
		char* pBuffer = pDataManager->GetSample(nFrame);
		pImaging->Process(pBuffer);
		cv::Mat imgResult = pImaging->GetProcessedImage().clone();

		int nowOffset = 0;
		// if file load failed, calculate zOffset
		if (config.measurement.calPerFrame) {
			if (pSession->m_vZOffset.size() != nNumOfSamples) {
				nowOffset = pSession->CalculateZOffset(imgResult, pSession->GetAutoCalibPatch());
				pSession->m_vZOffset.push_back(nowOffset);
			}
			else nowOffset = pSession->GetZOffset(nFrame);
		}
		// applyZOffset
		pImaging->ApplyZOffset(imgResult, imgResult, nowOffset + pSession->GetZOffset());

		pSession->m_mapImage.insert(std::make_pair(nFrame, imgResult));
		PLOGI.printf("mapImage inserted: frame %d", pSession->m_mapImage.size());
		cv::Mat imgResultWithoutCompensation = pImaging->GetWithoutCompensationImage().clone();
		pSession->m_mapImageWithoutCompensation.insert(std::make_pair(nFrame, imgResultWithoutCompensation));
		PLOGI.printf("done");
	}

	if (pSession->m_vZOffset.size() == nNumOfSamples)
		pSession->SaveZOffset(pSession->m_strDataFilePath);
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
	cv::Mat imgCircle/*, imgZOffset*/;

	if (pImaging == nullptr) {
		return ERROR;
	}

	PLOGI.printf("Session #%d update cutview - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadUpdateCutView->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImage.find(nFrame);
		if (it == pSession->m_mapImage.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}
		
		// threadImaging에서 이미 ZOffset을 적용했으므로 주석처리
		//pImaging->ApplyZOffset(it->second, imgZOffset, pSession->GetZOffset(nFrame));
		pImaging->CircularizeImage(it->second, imgCircle);
		pCutView->AddRecord(imgCircle, nFrame);

		pSession->m_pMsg->postMessage(WM_PROCESS_CUTVIEW, nSession, nFrame);
	}
	delete pImaging;

	PLOGI.printf("Session #%d update cutview done.", pSession->m_nSession);
	pSession->m_pMsg->postMessage(WM_NOTIFY_PROCESS_DONE, (WPARAM)RayWorkItem::GenerateCutView, pSession->m_nSession);

	return NOERROR;
}

// Lumen Validation
enum class AreaDecision {
	Accept,         // 정상 처리
	ExcludeSheath,  // 너무 작아 sheath로 제외
	ExcludeTooLarge,// 너무 커서 제외
	Invalid,         // 컨투어가 유효하지 않음
	SNRLow
};

struct AreaParams {
	double sheathAreaFracMax = 0.004; // FOV 면적의 0.4% 이하면 sheath로 제외
	double areaFracMax = 0.40;  // FOV 면적의 40% 이상이면 너무 큼 → 제외
	double minEdgeLenStraight = 8.0;   // ← 간선이 이 길이 이상이면 "직선 간선"
};

struct AreaResult {
	AreaDecision decision = AreaDecision::Invalid;
	double contourArea = 0.0;   // px^2
	double fovArea = 0.0;   // px^2 (π Rfov^2)
	double areaFrac = 0.0;   // contourArea / fovArea
	double Rfov = 0.0;   // FOV 반경 (min(W,H)/2)
	cv::Point2f center;         // 이미지 중심 (FOV 중심)
};

struct EdgePrefix {
	int N{};
	std::vector<double> seg;   // seg[i] = |c[i] -> c[i+1]|
	std::vector<double> pref;  // 길이 2N+1, 원형을 2번 펼친 누적합
	double P{};                // 둘레
	explicit EdgePrefix(const std::vector<cv::Point>& c) {
		N = (int)c.size();
		if (N < 2) return;
		seg.resize(N);
		for (int i = 0; i < N; ++i) {
			int j = (i + 1 == N) ? 0 : (i + 1);
			double dx = double(c[j].x) - c[i].x;
			double dy = double(c[j].y) - c[i].y;
			seg[i] = std::hypot(dx, dy);
		}
		P = std::accumulate(seg.begin(), seg.end(), 0.0);
		// 원형을 2배로 펼쳐서 누적합 구성
		double doubleZero = 0.0;
		pref.resize(2 * N + 1, doubleZero);
		for (int k = 0; k < 2 * N; ++k) pref[k + 1] = pref[k] + seg[k % N];
	}

	// 정방향: 정점 i에서 거리 dist 만큼 진행한 끝 정점
	int forwardVertex(int i, double dist) const {
		int startEdge = i; // i에서 시작하는 엣지 인덱스
		double target = pref[startEdge] + dist;
		// [i+1, i+N] 범위에서 target 이상이 되는 첫 엣지 찾기
		int e = int(std::lower_bound(pref.begin() + startEdge + 1,
			pref.begin() + startEdge + N + 1,
			target) - pref.begin());
		return e % N; // 엣지 e의 끝 정점
	}

	// 역방향: 정점 i에서 뒤로 거리 dist 만큼 간 시작 정점
	int backwardVertex(int i, double dist) const {
		int curr = i + N; // 가운데 창으로 이동 (음수 인덱스 회피)
		double target = pref[curr] - dist;
		auto first = pref.begin() + (curr - N);
		auto last = pref.begin() + curr;
		auto it = std::upper_bound(first, last, target);
		int e = int((it - pref.begin()) - 1);
		return (e % N + N) % N;
	}
};

template<typename T>
static inline T clamp_val(T v, T lo, T hi) { return (v < lo) ? lo : (v > hi ? hi : v); }

// --- Cubic Bezier (de Casteljau) ---
static inline cv::Point2f bezier3(const cv::Point2f& P0, const cv::Point2f& P1, const cv::Point2f& P2, const cv::Point2f& P3, float u) {
	cv::Point2f A = P0 + (P1 - P0) * u;
	cv::Point2f B = P1 + (P2 - P1) * u;
	cv::Point2f C = P2 + (P3 - P2) * u;
	cv::Point2f D = A + (B - A) * u;
	cv::Point2f E = B + (C - B) * u;
	return D + (E - D) * u;
}

// 긴 엣지 런 제거 + “멀리 잡은 핸들”로 베지어 연결
std::vector<cv::Point> reconstruct_RemoveLongRuns_WithFarBezier(const std::vector<cv::Point>& c, double minEdgeLen, int innerSamples = 3, double handleMinPx = 10.0, double handleFrac = 0.7) {
	const size_t N = c.size();
	if (N < 4) return c;

	// 0) 엣지 길이 및 누적합 준비 (한 번만)
	EdgePrefix ep(c);
	if (ep.N != static_cast<int>(N) || ep.P <= 0.0 || ep.seg.size() != N) {
		return c;
	}

	// 인덱스 wrap 함수
	auto wrap = [N](size_t k) -> size_t { return N ? (k % N) : 0; };

	// 1) 긴 엣지 끝점 제거 플래그
	std::deque<uint8_t> rm(N, 0); // vector → deque (기능 동일, 오탐 회피)
	for (size_t i = 0; i < N; ++i) {
		size_t j = (i + 1 == N) ? 0 : (i + 1);
		if (ep.seg[i] >= minEdgeLen) { rm[i] = 1; rm[j] = 1; }
	}

	// 2) 첫 유지점 찾기
	size_t start = N;
	for (size_t i = 0; i < N; ++i) { if (!rm[i]) { start = i; break; } }
	if (start == N) return {}; // 다 지워짐

	std::vector<cv::Point> out; out.reserve(N);
	auto push = [&](const cv::Point& p) {
		if (out.empty() || out.back() != p) out.emplace_back(p);
		};

	size_t i = start;
	do {
		if (!rm[i]) {
			push(c[i]);
			const size_t j = wrap(i + 1);
			if (rm[j]) {
				// run 끝 r 찾기 (최대 N회 가드)
				size_t k = j;
				for (size_t guard = 0; guard < N && rm[k]; ++guard) {
					k = wrap(k + 1);
				}
				const size_t r = k;

				// 원본과 동일: r == i면 즉시 종료
				if (r == i) break;

				// 핸들용 먼 점 A, D: prefix + 이진탐색
				cv::Point2f Bp((float)c[i].x, (float)c[i].y);
				cv::Point2f Cp((float)c[r].x, (float)c[r].y);

				double chord = cv::norm(Cp - Bp);
				double target = std::max(handleMinPx, handleFrac * chord);

				int idxA_i = ep.backwardVertex(static_cast<int>(i), target); // i에서 뒤로
				int idxD_i = ep.forwardVertex(static_cast<int>(r), target);  // r에서 앞으로

				if (idxA_i < 0) idxA_i = 0;
				else if (idxA_i >= static_cast<int>(N)) idxA_i = static_cast<int>(N) - 1;
				if (idxD_i < 0) idxD_i = 0;
				else if (idxD_i >= static_cast<int>(N)) idxD_i = static_cast<int>(N) - 1;

				size_t idxA = static_cast<size_t>(idxA_i);
				size_t idxD = static_cast<size_t>(idxD_i);

				cv::Point2f A((float)c[idxA].x, (float)c[idxA].y);
				cv::Point2f D((float)c[idxD].x, (float)c[idxD].y);

				// Catmull-Rom 접선 → Bezier 핸들
				auto fitHandle = [chord](cv::Point2f v) {
					float L = (float)std::max(1e-3, chord);
					float h = cv::norm(v);
					const float lo = 0.15f, hi = 0.80f;
					if (h < lo * L) v *= (lo * L / std::max(h, 1e-6f));
					else if (h > hi * L) v *= (hi * L / h);
					return v;
					};
				cv::Point2f m0 = fitHandle(0.5f * (Cp - A));
				cv::Point2f m1 = fitHandle(0.5f * (D - Bp));

				// Bezier 제어점 P0..P3
				const float hs = 1.f / 3.f;
				cv::Point2f P0 = Bp, P1 = Bp + m0 * hs, P2 = Cp - m1 * hs, P3 = Cp;

				// 내부 샘플 삽입
				int inner = clamp_val(innerSamples, 1, 12);
				for (int s = 1; s <= inner; ++s) {
					float u = float(s) / float(inner + 1);
					cv::Point2f qf = bezier3(P0, P1, P2, P3, u);
					cv::Point qi(cvRound(qf.x), cvRound(qf.y));
					push(qi);
				}

				i = r; // run 건너뜀
				continue;
			}
		}
		i = wrap(i + 1);
	} while (i != start);

	// 시작점과 끝점이 같으면 중복 제거
	if (!out.empty() && out.front() == out.back()) out.pop_back();
	return out;
}

// ===================== 분류 로직 =====================
inline AreaResult classify_by_area_only(const std::vector<cv::Point>& contour, const cv::Size& frameSize, const AreaParams& P = AreaParams{}){
	AreaResult R;
	R.center = cv::Point2f(frameSize.width / 2.f, frameSize.height / 2.f);
	R.Rfov = 0.5 * std::min(frameSize.width, frameSize.height);
	R.fovArea = CV_PI * R.Rfov * R.Rfov;

	if (contour.size() < 3 || R.fovArea <= 0.0) {
		if (R.decision != AreaDecision::Invalid)
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
		std::vector<std::vector<cv::Point>> cs(1);
		cs[0] = contour;
		cv::drawContours(imgBgr, cs, 0, col, 1, cv::LINE_AA);
	}
}

static inline bool finite_pos(double x) { return std::isfinite(x) && x > 0.0; }
static inline int  clamp_nonneg(int v) { return (v < 0) ? 0 : v; }

// ---------- Robust median & MAD ----------
static double median_of(std::vector<float> v) {
	try {
		if (v.empty()) {
			PLOGI.printf("[median_of] empty input");
			return std::numeric_limits<double>::quiet_NaN();
		}
		const size_t n = v.size();
		const size_t mid = n / 2;
		std::nth_element(v.begin(), v.begin() + mid, v.end());
		const double m1 = v[mid];
		if (n % 2 == 1) {
			//PLOGI.printf("[median_of] n=%d med=%.6f", (int)n, m1);
			return m1;
		}
		std::nth_element(v.begin(), v.begin() + mid - 1, v.end());
		const double m0 = v[mid - 1];
		const double med = 0.5 * (m0 + m1);
		return med;
	}
	catch (const std::exception& e) {
		PLOGI.printf("[median_of] exception: %s", e.what());
		return std::numeric_limits<double>::quiet_NaN();
	}
	catch (...) {
		PLOGI.printf("[median_of] unknown exception");
		return std::numeric_limits<double>::quiet_NaN();
	}
}

static double mad_of(const std::vector<float>& v, double med) {
	try {
		if (v.empty() || !std::isfinite(med)) {
			PLOGI.printf("[mad_of] invalid input: n=%d med=%g", (int)v.size(), med);
			return std::numeric_limits<double>::quiet_NaN();
		}
		std::vector<float> dev; dev.reserve(v.size());
		for (float x : v) dev.push_back(std::abs(x - (float)med));
		const double mad = median_of(std::move(dev));
		if (!std::isfinite(mad)) {
			PLOGI.printf("[mad_of] median(|x-med|) is not finite");
			return std::numeric_limits<double>::quiet_NaN();
		}
		const double sigma = 1.4826 * mad; // 정규 일관성 상수
		return sigma;
	}
	catch (const std::exception& e) {
		PLOGI.printf("[mad_of] exception: %s", e.what());
		return std::numeric_limits<double>::quiet_NaN();
	}
	catch (...) {
		PLOGI.printf("[mad_of] unknown exception");
		return std::numeric_limits<double>::quiet_NaN();
	}
}

// ---------- Gray[0..1] ----------
static cv::Mat toGray01(const cv::Mat& img, bool useLog = false) {
	try {
		CV_Assert(!img.empty());
		cv::Mat g;
		if (img.channels() == 3) cv::cvtColor(img, g, cv::COLOR_BGR2GRAY);
		else if (img.channels() == 4) cv::cvtColor(img, g, cv::COLOR_BGRA2GRAY);
		else g = img;

		if (g.depth() == CV_8U)        g.convertTo(g, CV_32F, 1.0 / 255.0);
		else if (g.depth() == CV_16U)  g.convertTo(g, CV_32F, 1.0 / 65535.0);
		else                           g.convertTo(g, CV_32F);

		// 0~1 클램프
		cv::min(g, 1.0f, g); cv::max(g, 0.0f, g);

		if (useLog) {
			cv::Mat logv; cv::log(g + 1e-6f, logv);
			double mn = 0.0, mx = 0.0; cv::minMaxLoc(logv, &mn, &mx);
			if (mx > mn) logv = (logv - (float)mn) / (float)(mx - mn);
			g = logv;
		}
		return g;
	}
	catch (const std::exception& e) {
		PLOGI.printf("[toGray01] exception: %s", e.what());
		return cv::Mat(); // empty
	}
	catch (...) {
		PLOGI.printf("[toGray01] unknown exception");
		return cv::Mat();
	}
}

// ---------- Lumen SNR ----------
static bool ComputeLumenSNR(
	const cv::Mat& image,
	const std::vector<cv::Point>& contour,
	double& out_snr,
	int erosion_px = 3,
	int r1_wall = 5,
	int r2_wall = 12,
	bool useLogIntensity = false,
	int excludeCenterRadius = 55,         // 0이면 미사용
	cv::Point excludeCenter = { -1, -1 }  // (-1,-1)면 이미지 중심
) {
	try {
		out_snr = std::numeric_limits<double>::quiet_NaN();

		if (image.empty()) {
			PLOGI.printf("[SNR] image is empty");
			return false;
		}
		if (contour.size() < 3) {
			PLOGI.printf("[SNR] contour too small: %d", (int)contour.size());
			return false;
		}

		erosion_px = clamp_nonneg(erosion_px);
		r1_wall = clamp_nonneg(r1_wall);
		r2_wall = std::max(clamp_nonneg(r2_wall), r1_wall + 1);
		excludeCenterRadius = clamp_nonneg(excludeCenterRadius);

		const float eps = 1e-6f;
		const int H = image.rows, W = image.cols;

		// 1) Gray 0..1
		cv::Mat gray = toGray01(image, useLogIntensity);
		if (gray.empty() || gray.type() != CV_32F) {
			PLOGI.printf("[SNR] gray invalid: empty=%d type=%d", gray.empty(), gray.type());
			return false;
		}

		// 2) Lumen mask from contour
		cv::Mat lumenMask(gray.size(), CV_8U, cv::Scalar(0));
		std::vector<std::vector<cv::Point>> polys(1);
		polys[0] = contour;
		cv::fillPoly(lumenMask, polys, cv::Scalar(255));
		const int lumen_pix = cv::countNonZero(lumenMask);
		if (lumen_pix < 50) {
			PLOGI.printf("[SNR] lumenMask too small: %d", lumen_pix);
			return false;
		}

		// 3) Erode lumen
		cv::Mat L_eroded;
		if (erosion_px > 0) {
			const cv::Mat k = cv::getStructuringElement(
				cv::MORPH_ELLIPSE, cv::Size(2 * erosion_px + 1, 2 * erosion_px + 1));
			cv::erode(lumenMask, L_eroded, k);
		}
		else {
			L_eroded = lumenMask.clone();
		}
		const int lum_e_pix = cv::countNonZero(L_eroded);
		if (lum_e_pix < 50) {
			PLOGI.printf("[SNR] eroded lumen too small: %d", lum_e_pix);
			return false;
		}

		// 4) Wall ring band (r1 ~ r2)
		cv::Mat d1, d2, W_band;
		const cv::Mat k1 = cv::getStructuringElement(cv::MORPH_ELLIPSE, cv::Size(2 * r1_wall + 1, 2 * r1_wall + 1));
		const cv::Mat k2 = cv::getStructuringElement(cv::MORPH_ELLIPSE, cv::Size(2 * r2_wall + 1, 2 * r2_wall + 1));
		cv::dilate(lumenMask, d1, k1);
		cv::dilate(lumenMask, d2, k2);
		cv::subtract(d2, d1, W_band);
		cv::threshold(W_band, W_band, 0, 255, cv::THRESH_BINARY);
		const int wall_pix = cv::countNonZero(W_band);
		if (wall_pix < 50) {
			PLOGI.printf("[SNR] wall ring too small: %d (r1=%d r2=%d)", wall_pix, r1_wall, r2_wall);
			return false;
		}

		// 5) central exclude
		if (excludeCenterRadius > 0) {
			cv::Point c = excludeCenter;
			if (c.x < 0 || c.y < 0) c = { W / 2, H / 2 };
			cv::Mat excl(gray.size(), CV_8U, cv::Scalar(0));
			cv::circle(excl, c, excludeCenterRadius, cv::Scalar(255), -1, cv::LINE_AA);
			cv::bitwise_and(L_eroded, 255 - excl, L_eroded);
			cv::bitwise_and(W_band, 255 - excl, W_band);
		}

		// 6) Collect samples
		std::vector<float> lumVals; lumVals.reserve(std::max(0, cv::countNonZero(L_eroded)));
		std::vector<float> wallVals; wallVals.reserve(std::max(0, cv::countNonZero(W_band)));

		for (int y = 0; y < H; ++y) {
			const float* gp = gray.ptr<float>(y);
			const uchar* lm = L_eroded.ptr<uchar>(y);
			const uchar* wb = W_band.ptr<uchar>(y);
			for (int x = 0; x < W; ++x) {
				if (lm[x]) lumVals.push_back(gp[x]);
				if (wb[x]) wallVals.push_back(gp[x]);
			}
		}

		// 제외 영역(-1) 필터링은 toGray01에서 이미 0~1로 클램프했으므로 생략 가능
		if ((int)lumVals.size() < 100 || (int)wallVals.size() < 100) {
			PLOGI.printf("[SNR] not enough samples: L=%d W=%d",
				(int)lumVals.size(), (int)wallVals.size());
			return false;
		}

		// 7) μ, σ(=MAD*1.4826) & SNR
		const double mu_l = median_of(lumVals);
		const double sig_l = mad_of(lumVals, mu_l);
		const double mu_w = median_of(wallVals);

		if (!std::isfinite(mu_l) || !std::isfinite(mu_w) || !finite_pos(sig_l)) {
			PLOGI.printf("[SNR] invalid stats: mu_l=%g mu_w=%g sig_l=%g (L=%d W=%d)",
				mu_l, mu_w, sig_l, (int)lumVals.size(), (int)wallVals.size());
			return false;
		}

		const double snr = (mu_w - mu_l) / (sig_l + eps);
		//PLOGI.printf("[SNR] nL=%d nW=%d | mu_l=%.6f sig_l=%.6f mu_w=%.6f | SNR=%.6f", (int)lumVals.size(), (int)wallVals.size(), mu_l, sig_l, mu_w, snr);

		if (!std::isfinite(snr)) {
			PLOGI.printf("[SNR] snr is not finite");
			return false;
		}

		// 비정상 큰 값 방지(드물게 분자 과대/분모 과소 시)
		if (std::abs(snr) > 1e6) {
			PLOGI.printf("[SNR] snr too large: %.6f (clamped)", snr);
			out_snr = (snr > 0 ? 1e6 : -1e6);
			return true;
		}

		out_snr = snr;
		return true;

	}
	catch (const cv::Exception& e) {
		PLOGI.printf("[SNR] OpenCV exception: %s", e.what());
		out_snr = std::numeric_limits<double>::quiet_NaN();
		return false;
	}
	catch (const std::exception& e) {
		PLOGI.printf("[SNR] std::exception: %s", e.what());
		out_snr = std::numeric_limits<double>::quiet_NaN();
		return false;
	}
	catch (...) {
		PLOGI.printf("[SNR] unknown exception");
		out_snr = std::numeric_limits<double>::quiet_NaN();
		return false;
	}
}


int CImagingSession::IsLumenNormal(cv::Mat image, std::vector<cv::Point> contour, double lumenThresholdMin,
	double lumenThresholdMax, double lumenSnrThreshold, bool showLumenGuide)
{
	AreaParams ap;
	ap.sheathAreaFracMax = lumenThresholdMin;
	ap.areaFracMax = lumenThresholdMax;

	AreaResult result = classify_by_area_only(contour, image.size(), ap);

	if (result.decision == AreaDecision::Accept) {
		double snr = std::numeric_limits<double>::quiet_NaN();
		if (ComputeLumenSNR(image, contour, snr, /*erosion*/3, /*r1*/5, /*r2*/12, /*useLog*/false, /*excludeR*/0)) {
			if (snr < lumenSnrThreshold) {
				result.decision = AreaDecision::SNRLow;
			}

			if (showLumenGuide) {
				cv::putText(image, cv::format("SNR=%.2f < %.2f", snr, lumenSnrThreshold),
					{ 256,256 }, cv::FONT_HERSHEY_SIMPLEX, 0.7, { 0,0,0 }, 3, cv::LINE_AA);
				cv::putText(image, cv::format("SNR=%.2f < %.2f", snr, lumenSnrThreshold),
					{ 256,256 }, cv::FONT_HERSHEY_SIMPLEX, 0.7, { 255,255,255 }, 1, cv::LINE_AA);
			}
		}
	}
	
	if (showLumenGuide)
		draw_area_thresholds_and_contour(image, contour, ap, result);

	return (result.decision == AreaDecision::Accept) ? 1 : 0;
}

std::vector<cv::Point> CImagingSession::GetValidLumenContour(const cv::Mat& imageResultWithoutCompensation, int imgSize, const cv::Mat& centerMask, const cv::Ptr<cv::CLAHE>& clahe, COCTImaging* pImaging) {
	if (!imageResultWithoutCompensation.u) {
		PLOGI.printf("imageResultWithoutCompensation.u == false");
		return std::vector<cv::Point>();
	}

	CConfiguration& config = CConfiguration::GetInstance();
	cv::Mat enhancedImage;
	cv::Mat enhancedCircleImage;
	cv::Mat enhancedCircleImageBGR;

	clahe->apply(imageResultWithoutCompensation, enhancedImage);

	pImaging->CircularizeImage(enhancedImage, enhancedCircleImage);

	cv::cvtColor(enhancedCircleImage, enhancedCircleImageBGR, cv::COLOR_GRAY2BGR);

	IRayLearning* learning = IRayLearning::GetInstance();
	cv::Mat contourImage = learning->FindLumen(enhancedCircleImageBGR);

	std::vector<std::vector<cv::Point>> vContours;
	cv::findContours(contourImage, vContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

	std::vector<cv::Point> validContour;
	std::vector<std::vector<cv::Point>> vCircle;

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

		if (!validContour.empty()) {
			validContour = reconstruct_RemoveLongRuns_WithFarBezier(validContour, config.measurement.nSheathPosition / 2);
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
	cv::Mat circleImage/*, imgZOffset*/, enhancedImage;

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

		if (pImaging == nullptr) {
			PLOGI.printf("plmaging is not initailized");
			return ERROR;
		}

		// threadImaging에서 이미 ZOffset을 적용했으므로 주석처리
		//pImaging->ApplyZOffset(it->second, imgZOffset, pSession->GetZOffset(nFrame));
		pImaging->CircularizeImage(it->second, circleImage);
		cv::cvtColor(circleImage, circleImage, cv::COLOR_GRAY2BGR);

		//lumen
		std::vector<cv::Point> validContour = CImagingSession::GetValidLumenContour(it->second, imgSize, centerMask, clahe, pSession->m_pImaging);

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
			CImagingSession::IsLumenNormal(circleImage, validContour, 0.01, 0.30, 1.0, true);
			cv::imwrite(cv::format("./test/%06d.png", nFrame), circleImage);
		}

		std::vector<cv::Mat> vLumens;
		vLumens.reserve(vContours.size());
		for (size_t i = 0; i < vContours.size(); i++) {
			const std::vector<cv::Point>& contour = vContours[i];
			if (contour.size() > static_cast<size_t>(INT_MAX)) {
				vLumens.push_back(cv::Mat(0, 1, CV_32SC2));
			}
			else {
				cv::Mat matContour = cv::Mat(contour).clone();
				vLumens.push_back(matContour);
			}
		}
		vLumen.push_back(vLumens);

		//sidebranch
		cv::Mat contourSb = learning->FindSidebranch();
		CV_Assert(contourSb.type() == CV_8UC1);
		std::vector<std::vector<cv::Point>> vSbContours;
		cv::findContours(contourSb, vSbContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

		std::vector<cv::Mat> vSidebranchs;
		vSidebranchs.reserve(vSbContours.size());
		for (size_t i = 0; i < vSbContours.size(); i++) {
			const std::vector<cv::Point>& contour = vSbContours[i];
			if (contour.size() > static_cast<size_t>(INT_MAX)) {
				vSidebranchs.push_back(cv::Mat(0, 1, CV_32SC2));
			}
			else {
				cv::Mat matContour = cv::Mat(contour).clone();
				vSidebranchs.push_back(matContour);
			}
		}
		vSidebranch.push_back(vSidebranchs);

		//stent
		std::vector<cv::Rect2f> vStents = learning->FindStent(circleImage);
		cv::Mat mStent((int)vStents.size(), 1, CV_32SC2);
		CV_Assert(mStent.rows == (int)vStents.size());

		for (size_t row = 0; row < vStents.size(); row++) {
			mStent.at<cv::Point>((int)row, 0) = cv::Point(
				(int)(vStents[row].x + vStents[row].width / 2),
				(int)(vStents[row].y + vStents[row].height / 2));
		}

		//pImaging->EraseStentOutLier(mStent);

		vStent.push_back(mStent);

		//guidewire
		std::vector<cv::Rect2f> vGuidewires = learning->FindGuidewire();

		std::vector<cv::Point> centerPoints;
		std::vector<float> Radius;

		pImaging->GetGuideWireCenterPoint(circleImage, vGuidewires, centerPoints, Radius);

		const size_t nGW = std::min({ vGuidewires.size(), centerPoints.size(), Radius.size() });
		cv::Mat mGuidewire((int)nGW, 1, CV_32SC2);
		CV_Assert(mGuidewire.rows == (int)nGW);
		if (nGW > 0)
		{
			for (size_t row = 0; row < nGW; row++) {
				mGuidewire.at<cv::Point>((int)row, 0) = cv::Point(centerPoints[row].x, centerPoints[row].y);
				if (Radius[row] < 0) continue;
				cv::circle(circleImage, centerPoints[row], (int)Radius[row], cv::Scalar(0, 255, 0), 2);
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

	if (pImaging == nullptr) {
		PLOGI.printf("pImaging is not initialized");
		return ERROR;
	}

	CConfiguration& config = CConfiguration::GetInstance();
	const size_t nNumOfSamples = pDataManager->GetNumOfSamples();
	const size_t nDiameter = config.volume.size;
	const size_t nImageSize = nDiameter * nDiameter;
	cv::Mat imgCircle, imgResize/*, imgZOffset*/;

	if (pSession->m_pVolumeData != nullptr)
	{
		delete[] pSession->m_pVolumeData;
	}

	if (nImageSize >= 600 * 600 || nNumOfSamples > 1600) {
		PLOGI.printf("Volume Data Size too big : config.volume.size = %d, nNumOfSamples = %d", nDiameter, nNumOfSamples);
		return ERROR;
	}

	if (nImageSize > 0 &&
		nNumOfSamples > 0 &&
		nImageSize <= SIZE_MAX / nNumOfSamples)
	{
		// codesonar suppr C integer-overflow-mul
		size_t totalSize = nImageSize * nNumOfSamples;
		pSession->m_pVolumeData = new char[totalSize];
	}
	else {
		PLOGE.printf("Requested memory too large or invalid input");
		return ERROR;
	}

	PLOGI.printf("Session #%d volume generation start - %d frames", pSession->m_nSession, nNumOfSamples);
	for (int nFrame = 0; nFrame < nNumOfSamples && pSession->m_pThreadVolumeGeneration->isRun; nFrame++) {
		std::map<int, cv::Mat>::iterator it = pSession->m_mapImage.find(nFrame);
		if (it == pSession->m_mapImage.end()) {
			nFrame--;
			Sleep(DELAY_FOR_WAIT_PROCESS);
			continue;
		}
		
		// threadImaging에서 이미 ZOffset을 적용했으므로 주석처리
		//pImaging->ApplyZOffset(it->second, imgZOffset, pSession->GetZOffset(nFrame));
		pImaging->CircularizeImage(it->second, imgCircle);

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
	if (strBackgroundFile == nullptr || strBackgroundFile[0] == '\0') return nullptr;
	
	FILE* fp = fopen(strBackgroundFile, "rb");
	if (fp == nullptr) {
		return nullptr;
	}

	if (setting.nBufferSize > 1024 * 1024 * 100) {
		PLOGI.printf("BufferSize is too big : %d", setting.nBufferSize);
		fclose(fp);
		return nullptr;
	}

	USHORT* pBackground = new USHORT[setting.nBufferSize];
	size_t size = fread(pBackground, sizeof(USHORT), setting.nBufferSize, fp);

	if (size != setting.nBufferSize) {
		if (feof(fp)) {
			PLOGI.printf("Warning: Reached end of file prematurely");
		}
		else if (ferror(fp)) {
			PLOGI.printf("Error reading file");
		}
		else {
			PLOGI.printf("Unknown fread issue");
		}
	}

	fclose(fp);

	return pBackground;
}