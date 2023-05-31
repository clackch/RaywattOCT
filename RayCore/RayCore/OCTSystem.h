#pragma once
#include "define.h"
#include "AcquisitionDevice.h"
#include "StepMotorController.h"
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
class CVolumeGenerator;
class CRayLearning;
class CImagingSession;
class COCTSystem : public CMessageService
{
private:
	enum class CatheterState {
		Unloaded = 0,
		Loaded,
		Enable,
		Calibrated
	};

	// Thread
	FunctionPtr m_callback;
	FunctionImgPtr m_cbCrossSection, m_cbLongitude;
	
	CThread* m_pThreadService;
	CThread* m_pThreadSaveRaw;
	CThread* m_pThreadGenerateVolume;
	CThread* m_pThreadLumenDetection;
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

	// Rotary Junction
	CStepMotorController* m_pStepMotor[STEP_MOTOR_NUM];

	// Machine Learning
	CRayLearning* m_pLearning;
	std::vector<std::vector<cv::Mat>> m_vLumen;

	RayScannerState m_prevState;
	RayScannerState m_curState;
	CatheterState m_cathState;

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;
	cv::Scalar m_backgroundColor;	// for longitude image

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
	RayError PullbackScan(char *strFilePath);
	RayError LoadCatheter();
	RayError UnloadCatheter();
	int StartReview(char* strFilePath);
	RayError StartCompare(char* strFilePath);
	RayError EndReview();
	RayError StartLiveView();
	RayError StopLiveView();
	RayError SetSession(int session);
	RayError PlayPause();
	RayError PrevFrame();
	RayError NextFrame();
	RayError MoveToFrame(int nFrame);
	RayError RegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	RayError UnregisterImageCallback();
	void* GetVolumeData();
	RayError StartLumenDetection();
	RayError OpenImage(char* strFilePath);
	RayError CloseImage();
	void* GetImageData(int nFrame);
	void* GetLongitudeData(double fDegree);
	void* GetLumenContour(int nFrame);
	int GetNumOfLumenContourPoints(int nFrame);
	
	//Property
	RayScannerState GetCurrentState() { return m_curState; }
	double GetBrightness();
	RayError SetBrightness(double value);
	double GetContrast();
	RayError SetContrast(double value);
	double GetDegree();
	RayError SetDegree(double value);
	UINT GetLongitudeBackgroundColor();
	RayError SetLongitudeBackgroundColor(UINT value);
	UINT GetVolumeDepth();
	bool GetMotorOnOff();
	bool GetIsPaused();
	UINT GetImageWidth();
	UINT GetImageHeight();
	UINT GetImageChannels();
	UINT GetImageDepth();
	UINT GetLongitudeImageWidth();
	UINT GetLongitudeImageHeight();
	UINT GetLongitudeImageChannels();

private:
	// Main Thread
	static UINT threadService(LPVOID param);
	// Work Thread (stop in OnMsgNotifyProcessDone, OnMsgUpdateScannerState)
	static UINT threadSaveRaw(LPVOID param);
	static UINT threadGenerateVolume(LPVOID param);
	static UINT threadLumenDetection(LPVOID param);
	
	// Rotary Junction Thread (stop in OnMsgDeviceWorkDone func)
	static UINT threadAutoCalibration(LPVOID param);
	static UINT threadPullbackScan(LPVOID param);
	// Catheter related Thread (stop in OnMsgUpdateCatheterState func)
	static UINT threadLoadCatheter(LPVOID param);
	static UINT threadUnloadCatheter(LPVOID param);
	static UINT threadValidateCatheter(LPVOID param);

	// Imaging & Device
	bool checkConnection();
	int connectAcqDevice();
	int disconnectAcqDevice();
	int startAcqDevice();
	int stopAcqDevice();
	int restartAcqDevice(COCTImaging* pImaging);
	int connectRotaryJunction();
	int disconnectRotaryJunction();
	void stopAllSessions();
	void closeAllSessions();
	void setBrightnessContrastAllSessions();

protected:
	LRESULT OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgProcessCutView(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateCatheterState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgStartReviewSession(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyProcessDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyEventOccured(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam);
};

