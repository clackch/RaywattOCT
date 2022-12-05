#pragma once
#include "define.h"
#include "AcquisitionDevice.h"
#include "MessageService.h"
#include <vector>
#include <mutex>
#include <tuple>
#include <opencv2/opencv.hpp>

#define WM_UPDATE_SCANNER_STATE		(WM_USER + 0x1001)
#define WM_UPDATE_SAVE_RAW			(WM_USER + 0x1002)
#define WM_NOTIFY_PROCESS_DONE		(WM_USER + 0x1003)
#define WM_NOTIFY_DEVICE_WORK_DONE	(WM_USER + 0x1004)
#define WM_NOTIFY_ERROR_OCCURED		(WM_USER + 0x1005)
#define WM_UPDATE_CATHETER_STATE	(WM_USER + 0x1006)

#define CUTVIEW_INTERPOLATION_SCALE		5.7


class CThread;
class COCTImaging;
class CCutViewManager;
class CVolumeGenerator;
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
	CThread* m_pThreadUpdateCutView;
	CThread* m_pThreadGenerateVolume;
	CThread* m_pThreadRotaryJunction;
	
	// Imaging
	COCTImaging* m_pImagingRealtime;
	COCTImaging* m_pImagingSimulate;

	// Data Manager
	IDataManager* m_pSimulationData;
	tstring m_strFilePath;

	// Cut View
	CCutViewManager* m_pCutView;
	int m_nOffsetNavigation;

	// 3D Volume
	CVolumeGenerator* m_pVolume;

	// Acquisition
	IAcquisitionDevice* m_pAcqDevice;

	// Simulation
	IAcquisitionDevice* m_pSimDevice;

	RayScannerState m_prevState;
	RayScannerState m_curState;
	CatheterState m_cathState;

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;
	cv::Scalar m_backgroundColor;
	double m_fLowLevel;
	double m_fHighLevel;

public:
	COCTSystem();
	virtual ~COCTSystem();

	// Call from dll only
	RayError Start();
	RayError Stop();
	RayError RegisterCallback(FunctionPtr cb);
	RayError ConnectDevices();
	RayError DisconnectDevices();
	RayError AutoCalibration();
	RayError ManualCalibration(bool forward);
	RayError ShowCalibrationGuide(bool enable);
	RayError PullbackScan(char *strFilePath);
	RayError LoadCatheter();
	RayError UnloadCatheter();
	RayError StartReview(char* strFilePath);
	RayError EndReview();
	RayError StartLiveView();
	RayError StopLiveView();
	RayError PlayPause();
	RayError PrevFrame();
	RayError NextFrame();
	RayError MoveToFrame(int nFrame);
	RayError RegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);

	//Property
	RayScannerState GetCurrentState() { return m_curState; }
	double GetBrightness();
	RayError SetBrightness(double value);
	double GetContrast();
	RayError SetContrast(double value);
	double GetDegree();
	RayError SetDegree(double value);
	UINT GetBackgroundColor();
	RayError SetBackgroundColor(UINT value);
	UINT GetVolumeDepth();
	double GetLowLevel();
	RayError SetLowLevel(double value);
	double GetHighLevel();
	RayError SetHighLevel(double value);
	void* GetVolumeData();
	bool GetMotorOnOff();
	bool GetIsPaused();

private:
	// Main Thread
	static UINT threadService(LPVOID param);
	// Work Thread (stop in OnMsgNotifyProcessDone, OnMsgUpdateScannerState)
	static UINT threadSaveRaw(LPVOID param);
	static UINT threadUpdateCutView(LPVOID param);
	static UINT threadGenerateVolume(LPVOID param);
	
	// Rotary Junction Thread (stop in OnMsgDeviceWorkDone func)
	static UINT threadAutoCalibration(LPVOID param);
	static UINT threadPullbackScan(LPVOID param);
	// Catheter related Thread (stop in OnMsgUpdateCatheterState func)
	static UINT threadLoadCatheter(LPVOID param);
	static UINT threadUnloadCatheter(LPVOID param);
	static UINT threadValidateCatheter(LPVOID param);

	// Imaging & Device
	COCTImaging* createColorImaging(CMessageService*);
	bool checkConnection();
	int connectAcqDevice();
	int disconnectAcqDevice();
	int startAcqDevice();
	int stopAcqDevice();
	int connectRotaryJunction();
	int disconnectRotaryJunction();
	void updateCutView(int drawSamples);
	void prepareSimulation(IDataManager* pDataManager);
	void terminateSimulation();

protected:
	LRESULT OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateCatheterState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyProcessDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgDeviceWorkDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgNotifyErrorOccured(WPARAM wParam, LPARAM lParam);
};

