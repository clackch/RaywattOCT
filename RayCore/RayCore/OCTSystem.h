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
		Loading,
		Enable,
		FindingSheath,
		FindingPeak,
		Calibrated,
		CheckSheath
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
	bool m_bFirstLoad;	// To-Do: RFID 연동해서 동일한 카테터 재연결시에도 FirstLoad 로 인식되게 수정 필요

	// Laser Module
	CLaserModule* m_pLaserModule;
	std::vector<std::pair<int, int>> m_vCalibrationInfo;

	RayScannerState m_prevState;
	RayScannerState m_curState;
	CatheterState m_cathState;

	// Init
	bool m_bInit;

	// Auto Pullback
	bool m_bAutoPullbackOnOff;
	double m_fLumenThresholdMin;
	double m_fLumenThresholdMax;
	double m_fLumenSrnThreshold;
	bool m_bShowLumenGuide;

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
	double m_fPullbackStartTime; // XXX.XXX sec
	int autoCalibrationFranch = 0; // 0 for 2.6, 60 for 1.7
	int catheterRFID;
	cv::Mat m_autoCalibPatch;

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
	bool GetImageCompensation();
	RayError SetImageCompensation(bool value);
	RayError SetImageCompensationControlWindow(bool value);
	double GetFieldOfView();
	RayError SetFieldOfView(double value);
	RayError SetZOffset(double value);
	void SetTestMode(bool isTestMode) { m_isTestMode = isTestMode; }
	bool IsTestMode() { return m_isTestMode; }
	void SetPullbackStartTime(double value) { m_fPullbackStartTime = value; }
	double GetPullbackStartTime() { return m_fPullbackStartTime; }
	int GetPullbackType(int pullbackDistance, int pullbackSpeed);
	double GetAutoPullback();
	RayError SetAutoPullback(double value);
	double GetLumenThresholdMin();
	RayError SetLumenThresholdMin(double value);
	double GetLumenThresholdMax();
	RayError SetLumenThresholdMax(double value);
	double GetShowLumenGuide();
	RayError SetShowLumenGuide(double value);
	double GetLumenSnrThreshold();
	RayError SetLumenSnrThreshold(double value);
	RayError SetRefractiveIndex(double value);
	int GetVelocityPullback();
	RayError SetVelocityPullback(int value);

private:
	// Main Thread
	static UINT threadService(LPVOID param);

	// Work Thread (stop in OnMsgNotifyProcessDone func)
	static UINT threadSaveRaw(LPVOID param);	
	// Rotary Junction Thread (stop in OnMsgDeviceWorkDone func)
	static UINT threadInitializeRotaryJunction(LPVOID param);
	static UINT threadAutoCalibration(LPVOID param);
	static UINT threadPullbackScan(LPVOID param);
	// Catheter related Thread (stop in OnMsgUpdateCatheterState func)
	static UINT threadLoadCatheter(LPVOID param);
	static UINT threadUnloadCatheter(LPVOID param);
	static UINT threadValidateCatheter(LPVOID param);
	static UINT threadManualLoadCatheter(LPVOID param);
	static UINT threadCleanRotaryJunction(LPVOID param);
	static UINT threadRFIDValidation(LPVOID param);

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
	bool waitForStepMotors(bool& runFlag, bool log = false);
	bool waitForStepMotors(eStepMotorIndex idxMotor, bool& runFlag);
	std::vector<std::vector<std::string>> readLoadSequence();
	void autoCalibrationInit(LPVOID param);
	void loadAutoCalibPatch();

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

