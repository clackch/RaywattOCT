#pragma once
#include "define.h"
#include "AcquisitionDevice.h"
#include "MessageService.h"
#include <vector>
#include <mutex>
#include <tuple>
#include <opencv2/opencv.hpp>

typedef enum {
	SESSION_UNKNOWN = -1,
	SESSION_REVIEW = 0,	// RealTime, Review
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
	COCTImaging* m_pImagingRealtime;

	// Data Manager
	IDataManager* m_pDataWriter;
	tstring m_strFilePath;

	// 3D Volume
	CVolumeGenerator* m_pVolume;

	// Acquisition
	IAcquisitionDevice* m_pAcqDevice;

	// Imaging Session (Review)
	CImagingSession* m_reviewSession[MAX_SESSION_NUM];
	CImagingSession* m_openedSession;

	// Machine Learning
	CRayLearning* m_pLearning;
	std::vector<std::vector<std::vector<cv::Point>>> m_vLumen;

	RayScannerState m_prevState;
	RayScannerState m_curState;
	CatheterState m_cathState;

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;
	cv::Scalar m_backgroundColor;	// for longitude image
	double m_fLowLevel;
	double m_fHighLevel;

public:
	COCTSystem();
	virtual ~COCTSystem();

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
	RayError StartReview(char* strFilePath);
	RayError StartCompare(char* strFilePath);
	RayError EndReview();
	RayError StartLiveView();
	RayError StopLiveView();
	RayError PlayPause();
	RayError PrevFrame();
	RayError NextFrame();
	RayError MoveToFrame(int nFrame);
	RayError RegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	RayError UnregisterImageCallback();
	void* GetVolumeData();
	RayError OpenImage(char* strFilePath);
	RayError CloseImage();
	void* GetImageData(int nFrame);
	void* GetLongitudeData(double fDegree);
	
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
	int connectRotaryJunction();
	int disconnectRotaryJunction();
	void stopAllSessions();
	void closeAllSessions();

protected:
	LRESULT OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgProcessCutView(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateCatheterState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgStartReviewSession(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyProcessDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam);
};

