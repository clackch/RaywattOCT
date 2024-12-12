#pragma once
#include "define.h"
#include "AcquisitionDevice.h"
#include "MessageService.h"
#include "RJController.h"
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
class CVolumeGenerator;
class IRayLearning;
class CImagingSession;
class CLaserModule;
class COCTSystem : public CMessageService
{
private:
	enum class CatheterState {
		Unloaded = 0,
		Loaded,
		Enable,
		FindingSheath,
		FindingPeak,
		Calibrated
	};

	// Thread
	FunctionPtr m_callback;
	FunctionImgPtr m_cbCrossSection, m_cbLongitude;
	FunctionObjPtr m_cbObjectDetection;
	
	CThread* m_pThreadService;
	CThread* m_pThreadSaveRaw;
	CThread* m_pThreadRotaryJunction;
	
	// Imaging
	COCTImaging* m_pImagingRealtime;	// Pullback or LiveView
	COCTImaging* m_pImagingPullback;
	COCTImaging* m_pImagingLiveView;

	// Data Manager
	IDataManager* m_pDataWriter;
	tstring m_strFilePath;

	// 3D Volume
	CVolumeGenerator* m_pVolume;

	// Acquisition
	IAcquisitionDevice* m_pAcqDevice;

	// Imaging Session (Review)
	SessionType m_curSession;
	CImagingSession* m_reviewSession[MAX_SESSION_NUM];
	CImagingSession* m_openedSession;
	CRITICAL_SECTION m_csSession;

	// Rotary Junction
	CRJController* m_pRJController;

	// Laser Module
	CLaserModule* m_pLaserModule;
	std::vector<std::pair<int, int>> m_vCalibrationInfo;

	RayScannerState m_prevState;
	RayScannerState m_curState;
	CatheterState m_cathState;

	// Auto Pullback (Flushing Detection)
	double m_fReferenceIntensity[4];
	double m_fCurrentIntensity[4];

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;
	double m_fColormap;
	cv::Scalar m_backgroundColor;	// for longitude image
	double m_fImageThreshold = 99.99;
	bool m_bImageCompensation = true;
	bool m_bImageCompensationControlWindow;
	bool m_bImageLumenVignetting;
	double m_fImageRoi = 2.f;
	double m_fFieldOfView;
	bool m_isTestMode;

public:
	COCTSystem();
	virtual ~COCTSystem();
	void SetLogger(TCHAR*);

	// Call from dll only
	RayError Start();
	RayError Stop();
	RayError RegisterCallback(FunctionPtr cb);
	RayError UnregisterCallback();
	RayError ConnectDevices();
	RayError DisconnectDevices();
	RayError AutoCalibration();
	RayError ManualCalibration(bool forward);
	RayError ShowCalibrationGuide(bool enable);
	RayError ReadyPullback();
	RayError PullbackScan(char *strFilePath);
	RayError LoadCatheter();
	RayError UnloadCatheter();
	int StartReview(char* strFilePath, double imageResolution, double zOffset);
	RayError StartCompare(char* strFilePath, double imageResolution, double zOffset);
	RayError EndReview();
	RayError EndCompare();
	RayError RestartReview();
	RayError StartLiveView();
	RayError StopLiveView();
	RayError LaserOnOff(bool isOn);
	RayError RJCleanModeOnOff(bool isOn);
	RayError SetSession(int session);
	RayError RegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	RayError UnregisterImageCallback();
	RayError RegisterDetectionCallback(FunctionObjPtr cbObjectDetection);
	RayError UnregisterDetectionCallback();
	void* GetVolumeData(void* pLumenContours = nullptr);
	RayError StartLumenDetection();
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
	UINT GetVolumeDepth();
	bool GetMotorOnOff();
	UINT GetImageWidth();
	UINT GetImageHeight();
	UINT GetImageChannels();
	UINT GetImageDepth();
	double GetImageResolution();
	UINT GetLongitudeImageWidth();
	UINT GetLongitudeImageHeight();
	UINT GetLongitudeImageChannels();
	RayError SetSheathDiameter(double value);
	double GetImageThreshold();
	RayError SetImageThreshold(double value);
	double GetImageRoi();
	RayError SetImageRoi(double value);
	bool GetImageCompensation();
	RayError SetImageCompensation(bool value);
	bool GetImageLumenVignetting();
	RayError SetImageCompensationControlWindow(bool value);
	RayError SetImageLumenVignetting(bool value);
	double GetFieldOfView();
	RayError SetFieldOfView(double value);
	RayError SetZOffset(double value);
	void SetTestMode(bool isTestMode) { m_isTestMode = isTestMode; }
	bool IsTestMode() { return m_isTestMode; }

private:
	// Main Thread
	static UINT threadService(LPVOID param);

	// Work Thread (stop in OnMsgNotifyProcessDone func)
	static UINT threadSaveRaw(LPVOID param);	
	// Rotary Junction Thread (stop in OnMsgDeviceWorkDone func)
	static UINT threadAutoCalibration(LPVOID param);
	static UINT threadPullbackScan(LPVOID param);
	// Catheter related Thread (stop in OnMsgUpdateCatheterState func)
	static UINT threadLoadCatheter(LPVOID param);
	static UINT threadUnloadCatheter(LPVOID param);
	static UINT threadValidateCatheter(LPVOID param);
	static UINT threadManualLoadCatheter(LPVOID param);
	static UINT threadCleanRotaryJunction(LPVOID param);

	// Imaging & Device
	bool checkConnection();
	int connectAcqDevice();
	int disconnectAcqDevice();
	int startAcqDevice();
	int stopAcqDevice();
	int restartAcqDevice(COCTImaging* pImaging);
	int connectRotaryJunction();
	int disconnectRotaryJunction();
	int controlRotaryJunction(eRJState state);
	void stopAllSessions();
	void closeAllSessions();
	void setBrightnessContrastAllSessions();
	void redrawCutView();
	void laserOnOff(bool isOn);
	bool waitForStepMotors(bool& runFlag);
	bool waitForStepMotors(eStepMotorIndex idxMotor, bool& runFlag);
	void calculateIntensity(cv::Mat image);

protected:
	LRESULT OnMsgProcessCrossSection(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgProcessCutView(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgProcessDetection(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateCatheterState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateRJState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgStartReviewSession(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyProcessDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyEventOccured(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam);
};

