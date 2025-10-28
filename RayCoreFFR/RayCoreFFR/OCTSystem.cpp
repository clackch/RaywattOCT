#include "Config.h"
#include "OCTSystem.h"
#include "Utility.h"
#include "Configuration.h"
#include "OCTImaging.h"
#include "LabImaging.h"
#include "DataWriter.h"
#include "CutViewManager.h"
#include "IRayLearning.h"
#include "ImagingSession.h"
#include "LookUpTable.h"
#include <fstream>

/*
* COCTSystem
*/
COCTSystem::COCTSystem() {

	m_callback = nullptr;
	m_cbCrossSection = nullptr;
	m_cbLongitude = nullptr;
	m_cbObjectDetection = nullptr;	

	m_isTestMode = false;

	InitializeCriticalSection(&m_csSession);

	CConfiguration& config = CConfiguration::GetInstance();
	if (!config.IsInit()) {
		config.Initialize(_T(".\\raycore.ini"));
	}

	SetLogger(config.logRootPath);
}

/*
* ~COCTSystem
*/
COCTSystem::~COCTSystem() {
	if(m_bInit)
		Stop();
	DeleteCriticalSection(&m_csSession);
}

void COCTSystem::SetLogger(TCHAR* logRootPath) {

	time_t timer = time(nullptr);
	tm t;
	errno_t err = localtime_s(&t, &timer);

	if (err != 0) {
		PLOGI.printf("localtime_s failed with error code: %d", err);
	}

	char rootPath[MAX_PATH] = "";
	WideCharToMultiByte(CP_ACP, 0, logRootPath, MAX_PATH, rootPath, MAX_PATH, nullptr, nullptr);

	char logFile[_MAX_PATH] = "";
	errno_t rc = sprintf_s(        
		logFile,                   
		sizeof(logFile),           
		"%s\\core_%04d-%02d-%02d.log",
		rootPath,
		t.tm_year + 1900,
		t.tm_mon + 1,
		t.tm_mday
	);
	if (rc < 0) {
		PLOGI.printf("Fail to create log file: %d\n", rc);
	}

	printf("plog::init - %s\n", logFile);

#ifdef DEBUG
	plog::init(plog::debug, logFile);
#else
	plog::init(plog::info, logFile);
#endif
}

/*
* Init
*/
RayError COCTSystem::Init() {

	m_pThreadService = nullptr;

	m_pImaging = nullptr;

	m_curSession = SESSION_UNKNOWN;
	for (int i = 0; i < MAX_SESSION_NUM; i++) {
		m_reviewSession[i] = nullptr;
	}
	m_openedSession = nullptr;

	m_prevState = RayScannerState::Initial;
	m_curState = RayScannerState::Initial;

	//Property
	m_fBrightness = 0.0f;
	m_fContrast = 0.5f;
	m_fDegree = 90;

	m_bInit = true;

	return RayError::OK;
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

	CUtility::StartThread(threadService, m_pThreadService, this);

	IImaging::Setting settingImaging = config.imaging;
	settingImaging.Set(settingImaging.nAScan, settingImaging.nBScan);
	PLOGI.printf("settingImaging: nAScan=%ld, nBScan=%ld", settingImaging.nAScan, settingImaging.nBScan);
	m_pImaging = CImagingSession::CreateColorImaging(this, settingImaging, nullptr, ImagingType::Default);
	if (!m_pImaging) {
		PLOGI.printf("Failed to create Imaging LiveView");
		return RayError::WrongSession;
	}
	m_pImaging->SetSession(SESSION_REALTIME);
	m_pImaging->Start();

	return RayError::OK;
}

/*
* Stop
*/
RayError COCTSystem::Stop() {
	PLOGI.printf("Stop threads");
	CUtility::StopThread(m_pThreadService);

	PLOGI.printf("Close All Sessions");
	closeAllSessions();
	if (m_openedSession != nullptr) {
		delete m_openedSession;
		m_openedSession = nullptr;
	}

	PLOGI.printf("Stop Imaging");
	if (m_pImaging != nullptr) {
		m_pImaging->Stop();
		delete m_pImaging;
		m_pImaging = nullptr;
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
* StartReview
* return N (>0) when current state & argument is right.
* return Error Code (<0) when something is wrong.
*/
int COCTSystem::StartReview(char* strFilePath, double imageResolution, double zOffset) {
	if (m_curState == RayScannerState::Initial || m_curState == RayScannerState::Default) {
		if (imageResolution == 0.0f) return (int)RayError::InvalidArgument;

		CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_REVIEW, strFilePath, imageResolution);
		if (pSession == nullptr) {
			PLOGE.printf("InvalidArgument : %s", strFilePath);
			return (int)RayError::InvalidArgument;
		}
		pSession->LoadZOffset(strFilePath);
		pSession->SetZOffset((int)zOffset);

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
RayError COCTSystem::StartCompare(char* strFilePath, double imageResolution, double zOffset) {
	if (m_curState != RayScannerState::Review) return RayError::WrongState;

	CImagingSession* pSession = CImagingSession::CreateSession(this, SESSION_COMPARE, strFilePath, imageResolution);
	if (pSession == nullptr) {
		return RayError::InvalidArgument;
	}
	pSession->LoadZOffset(strFilePath);
	pSession->SetZOffset((int)zOffset);

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

RayError COCTSystem::EndCompare()
{
	if (m_reviewSession[SESSION_COMPARE] != nullptr) {
		m_reviewSession[SESSION_COMPARE]->Stop();
	}

	return RayError::OK;
}

RayError COCTSystem::RestartReview()
{
	if (m_curState == RayScannerState::Review) {

		m_reviewSession[SESSION_REVIEW]->StopThreadForRestart();
		m_reviewSession[SESSION_REVIEW]->StartCutViewUpdate(m_backgroundColor);
		m_reviewSession[SESSION_REVIEW]->StartObjectDetection();

		return RayError::OK;
	}

	return RayError::WrongState;
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
* StartLumenDetection
*/
RayError COCTSystem::StartLumenDetection() {
	if (m_curState == RayScannerState::Review)
	{
		if (m_reviewSession[SESSION_REVIEW] == nullptr) return RayError::WrongSession;
		m_reviewSession[SESSION_REVIEW]->StopThreadObjectDetection();
		m_reviewSession[SESSION_REVIEW]->StartObjectDetection();
		PLOGI.printf("Start object detection manually.");
	}

	return RayError::WrongState;
}

RayError COCTSystem::SetConfigPath(char* strPath) {
	CConfiguration& config = CConfiguration::GetInstance();

	if (strPath == nullptr || !CUtility::IsExist(strPath, false)) {
		return RayError::InvalidArgument;
	}
	config.SetPath(CUtility::StringToWstring(strPath));

	return RayError::OK;
}

/*
* OpenImage
*/
RayError COCTSystem::OpenImage(char* strFilePath, double imageResolution, double zOffset) {
	PLOGI.printf("OpenImage");
	CloseImage();

	CImagingSession *pSession = CImagingSession::CreateSession(this, SESSION_UNKNOWN, strFilePath, imageResolution);
	if (pSession == nullptr) {
		return RayError::InvalidArgument;
	}
	pSession->LoadZOffset(strFilePath);
	pSession->SetZOffset((int)zOffset);

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
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if (m_vLumen.empty()) return nullptr;

		return m_vLumen.ptr();
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetLumenContour(nFrame);
	}
}

/*
* GetNumOfLumenContourPoints
*/
int COCTSystem::GetNumOfLumenContourPoints(int nFrame) {
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if (m_vLumen.empty()) return 0;

		return m_vLumen.cols * m_vLumen.rows;
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetNumOfLumenContourPoints(nFrame);
	}	
}

/*
* GetNumOfSidebranchContourSize
*/
int COCTSystem::GetNumOfSidebranchContourSize(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		return m_vSidebranch.size();
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetNumOfSidebranchContourSize(nFrame);
	}	
}

/*
* GetSidebranchContour
*/
void* COCTSystem::GetSidebranchContour(int nFrame, int nSb){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if (m_vSidebranch.size() <= 0) return nullptr;

		return m_vSidebranch.at(nSb).ptr();
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetSidebranchContour(nFrame, nSb);
	}	
}

/*
* GetNumOfSidebranchContourPoints
*/
int COCTSystem::GetNumOfSidebranchContourPoints(int nFrame, int nSb){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if (m_vSidebranch.size() <= 0) return 0;

		cv::Mat matContour = m_vSidebranch.at(nSb);
		return matContour.cols * matContour.rows;
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetNumOfSidebranchContourPoints(nFrame, nSb);
	}	
}
/*
* GetStentPoints
*/
void* COCTSystem::GetStentPoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if(m_vStent.empty()) return nullptr;

		return m_vStent.ptr();
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetStentPoints(nFrame);
	}	
}
/*
* GetNumOfStentPoints
*/
int COCTSystem::GetNumOfStentPoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if (m_vStent.empty()) return 0;

		return m_vStent.cols * m_vStent.rows;
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetNumOfStentPoints(nFrame);
	}	
}
/*
* GetGuidewirePoints
*/
void* COCTSystem::GetGuidewirePoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if(m_vGuidewire.empty()) return nullptr;

		return m_vGuidewire.ptr();
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetGuidewirePoints(nFrame);
	}	
}
/*
* GetNumOfGuidewirePoints
*/
int COCTSystem::GetNumOfGuidewirePoints(int nFrame){
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if (m_vGuidewire.empty()) return 0;

		return m_vGuidewire.cols * m_vGuidewire.rows;
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetNumOfGuidewirePoints(nFrame);
	}
}

/*
* GetNumOfGuidewirePoints
*/

void* COCTSystem::GetGuidewireRadius(int nFrame) {
	if (m_reviewSession[SESSION_REVIEW] == nullptr) {
		if(m_vGuidewireRadius.empty()) return nullptr;

		return static_cast<void*>(m_vGuidewireRadius.data());
	}
	else {
		return m_reviewSession[SESSION_REVIEW]->GetGuidewireRadius(nFrame);
	}	
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
* SetImageCompensationControlWindow
*/
RayError COCTSystem::SetImageCompensationControlWindow(bool value)
{
	m_bImageCompensationControlWindow = value;

	CConfiguration& config = CConfiguration::GetInstance();

	COCTImaging::SetImageCompensationControlWindow(m_bImageCompensationControlWindow, config.imaging);

	m_bImageCompensationControlWindow = 0;

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
* SetZOffset
*/
RayError COCTSystem::SetZOffset(double value)
{
	if (m_reviewSession[SESSION_REVIEW] != nullptr)
	{
		if (m_reviewSession[SESSION_REVIEW]->GetImaging() != nullptr)
		{
			m_reviewSession[SESSION_REVIEW]->SetZOffset((int)value);
		}
	}

	return RayError::OK;
}

RayError COCTSystem::RunImageAnalysis(unsigned char* data, int width, int height, int channels, int step)
{
	CConfiguration& config = CConfiguration::GetInstance();
	IRayLearning* learning = IRayLearning::GetInstance();

	//Source Image from App
	int imgSize = config.imaging.nCircleSize;
	cv::Mat img(height, width, CV_8UC1, const_cast<unsigned char*>(data), step);
	cv::resize(img, img, cv::Size(imgSize, imgSize), 0, 0, cv::INTER_LINEAR);

	//Initialize
	m_vSidebranch.clear();
	m_vGuidewireRadius.clear();

	cv::Point center(imgSize / 2, imgSize / 2);
	cv::Mat centerMask = cv::Mat::zeros(imgSize, imgSize, CV_8UC1);
	cv::circle(centerMask, center, 1, cv::Scalar(255), cv::FILLED);
	cv::Ptr<cv::CLAHE> clahe = cv::createCLAHE(3.5, cv::Size(4, 4));

	//lumen
	std::vector<cv::Point> contour = CImagingSession::GetValidLumenContour(img.clone(), imgSize, centerMask, clahe, nullptr);
	PLOGI.printf("contour : %d", contour.size());
	if (!contour.empty()) {
		cv::Mat matContour(contour.size(), 1, CV_32SC2);
		for (size_t row = 0; row < contour.size(); row++) {
			matContour.at<cv::Point>(row, 0) = contour[row];
		}
		m_vLumen = matContour;
	}

	//sidebranch
	cv::Mat contourSb = learning->FindSidebranch();
	std::vector<std::vector<cv::Point>> vSbContours;
	cv::findContours(contourSb, vSbContours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);
	for (int i = 0; i < vSbContours.size(); i++) {
		std::vector<cv::Point> contour = vSbContours.at(i);
		cv::Mat matContour(contour.size(), 1, CV_32SC2);
		for (size_t row = 0; row < contour.size(); row++) {
			matContour.at<cv::Point>(row, 0) = contour[row];
		}
		m_vSidebranch.push_back(matContour);
	}	

	//Gray to RGB
	cv::cvtColor(img, img, cv::COLOR_GRAY2BGR);

	//stent
	std::vector<cv::Rect2f> vStents = learning->FindStent(img);
	cv::Mat mStent(vStents.size(), 1, CV_32SC2);
	for (size_t row = 0; row < vStents.size(); row++) {
		mStent.at<cv::Point>(row, 0) = cv::Point(vStents[row].x + vStents[row].width / 2, vStents[row].y + vStents[row].height / 2);
	}	
	m_vStent = mStent;
	
	//guidewire
	std::vector<cv::Rect2f> vGuidewires = learning->FindGuidewire();
	std::vector<cv::Point> centerPoints;
	std::vector<float> Radius;
	cv::Mat mGuidewire(vGuidewires.size(), 1, CV_32SC2);
	m_pImaging->GetGuideWireCenterPoint(img, vGuidewires, centerPoints, Radius);
	if (centerPoints.size() > 0) {
		for (size_t row = 0; row < vGuidewires.size(); row++) {
			mGuidewire.at<cv::Point>(row, 0) = cv::Point(centerPoints[row].x, centerPoints[row].y);
			if (Radius[row] < 0) continue;
			cv::circle(img, centerPoints[row], static_cast<int>(Radius[row]), cv::Scalar(0, 255, 0), 2);
		}		
		m_vGuidewireRadius = Radius;
	}
	else {
		m_vGuidewireRadius = std::vector<float>(1);
	}
	m_vGuidewire = mGuidewire;
	
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
	lut.Load("LUT_orange.csv");

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
		case WM_START_REVIEW_SESSION:
		{
			pSystem->OnMsgStartReviewSession(wParam, lParam);
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

	m_pImaging->SetBrightnessContrast(m_fBrightness, m_fContrast);

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
		closeAllSessions();
		break;
	case RayScannerState::Scanning:
		break;
	case RayScannerState::Review:		
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
		break;
	case RayWorkItem::StartService:
	case RayWorkItem::OCTImaging:
	case RayWorkItem::GenerateCutView:
	case RayWorkItem::DetectLumen:
		break;
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
		PLOGI.printf("CUtility::StopThread(m_pThreadRotaryJunction)");
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