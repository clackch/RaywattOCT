#pragma once
#include "define.h"
#include "AcquisitionDevice.h"
#include "MessageService.h"
#include <vector>
#include <mutex>
#include <tuple>
#include <opencv2/opencv.hpp>
#include <time.h>
#include "plog/Initializers/RollingFileInitializer.h"

typedef enum {
	SESSION_UNKNOWN = RaySession::Unknown,
	SESSION_REALTIME = 0,
	SESSION_REVIEW = 0,
	SESSION_COMPARE,
	MAX_SESSION_NUM
}SessionType;

class CThread;
class COCTImaging;
class IRayLearning;
class CImagingSession;
class COCTSystem : public CMessageService
{
private:
	// Thread
	FunctionPtr m_callback;
	FunctionImgPtr m_cbCrossSection, m_cbLongitude;
	FunctionObjPtr m_cbObjectDetection;
	
	CThread* m_pThreadService;
	
	// Imaging
	COCTImaging* m_pImaging;

	// Imaging Session (Review)
	SessionType m_curSession;
	CImagingSession* m_reviewSession[MAX_SESSION_NUM];
	CImagingSession* m_openedSession;
	CRITICAL_SECTION m_csSession;

	RayScannerState m_prevState;
	RayScannerState m_curState;

	// Init
	bool m_bInit;

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;
	double m_fColormap;
	cv::Scalar m_backgroundColor;	// for longitude image
	bool m_bImageCompensation = true;
	bool m_bImageCompensationControlWindow;
	double m_fFieldOfView;
	bool m_isTestMode;

	//ML
	cv::Mat m_vLumen;
	std::vector<cv::Mat> m_vSidebranch;
	cv::Mat m_vStent;
	cv::Mat m_vGuidewire;
	std::vector<float> m_vGuidewireRadius;

public:
	COCTSystem();
	virtual ~COCTSystem();
	void SetLogger(TCHAR*);

	// Call from dll only
	RayError Start();
	RayError Stop();
	RayError Init();
	RayError RegisterCallback(FunctionPtr cb);
	RayError UnregisterCallback();
	int StartReview(char* strFilePath, double imageResolution, double zOffset);
	RayError StartCompare(char* strFilePath, double imageResolution, double zOffset);
	RayError EndReview();
	RayError EndCompare();
	RayError RestartReview();
	RayError SetSession(int session);
	RayError RegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	RayError UnregisterImageCallback();
	RayError RegisterDetectionCallback(FunctionObjPtr cbObjectDetection);
	RayError UnregisterDetectionCallback();
	RayError StartLumenDetection();
	RayError SetConfigPath(char* strPath);
	RayError OpenImage(char* strFilePath, double imageResolution, double zOffset);
	RayError CloseImage();
	void* GetImageData(int nFrame);
	void* GetLongitudeData(double fDegree);
	void* GetLumenContour(int nFrame);
	int GetNumOfLumenContourPoints(int nFrame);
	int GetNumOfSidebranchContourSize(int nFrame);
	void* GetSidebranchContour(int nFrame, int nSb);
	int GetNumOfSidebranchContourPoints(int nFrame, int nSb);
	void* GetStentPoints(int nFrame);
	int GetNumOfStentPoints(int nFrame);
	void* GetGuidewirePoints(int nFrame);
	int GetNumOfGuidewirePoints(int nFrame);
	void* GetGuidewireRadius(int nFrame);
	
	//Property
	RayScannerState GetCurrentState() { return m_curState; }
	double GetBrightness();
	RayError SetBrightness(double value);
	double GetContrast();
	RayError SetContrast(double value);
	double GetColormap();
	RayError SetColormap(double value);
	double GetDegree();
	RayError SetDegree(double value);
	UINT GetLongitudeBackgroundColor();
	RayError SetLongitudeBackgroundColor(UINT value);
	UINT GetImageWidth();
	UINT GetImageHeight();
	UINT GetImageChannels();
	UINT GetImageDepth();
	double GetImageResolution();
	UINT GetLongitudeImageWidth();
	UINT GetLongitudeImageHeight();
	UINT GetLongitudeImageChannels();
	bool GetImageCompensation();
	RayError SetImageCompensation(bool value);
	RayError SetImageCompensationControlWindow(bool value);
	double GetFieldOfView();
	RayError SetFieldOfView(double value);
	RayError SetZOffset(double value);
	void SetTestMode(bool isTestMode) { m_isTestMode = isTestMode; }
	bool IsTestMode() { return m_isTestMode; }

	RayError RunImageAnalysis(unsigned char* data, int width, int height, int channels, int step);

private:
	// Main Thread
	static UINT threadService(LPVOID param);

	// Imaging & Device
	void stopAllSessions();
	void closeAllSessions();
	void setBrightnessContrastAllSessions();
	void redrawCutView();

protected:
	LRESULT OnMsgProcessCutView(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgProcessDetection(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgStartReviewSession(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyProcessDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyEventOccured(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam);
};

