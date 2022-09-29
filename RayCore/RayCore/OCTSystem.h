#pragma once
#include "define.h"
#include "AcquisitionDevice.h"
#include "MessageService.h"
#include <vector>
#include <mutex>
#include <tuple>

#define WM_UPDATE_SCANNER_STATE		(WM_USER + 0x1001)
#define WM_UPDATE_SAVE_RAW			(WM_USER + 0x1002)
#define WM_UPDATE_CUTVIEW_DONE		(WM_USER + 0x1003)

#define CUTVIEW_INTERPOLATION_SCALE		5.7

//#define TEST_VALUE_FILE_PATH			_T("C:\\DataSave\\test\\0710_145631_6028rpm_20mms_2000Aline_ch1.bin")

#ifdef TEST_VALUE_FILE_PATH
class CDataReader;
#endif
class CThread;
class COCTImaging;
class CDataWriter;
class CCutViewManager;
class COCTSystem : public CMessageService
{
private:
	// Thread
	FunctionPtr m_callback;
	FunctionImgPtr m_cbCrossSection, m_cbLongitude;
	
	CThread* m_pThreadService;
	CThread* m_pThreadInitialize;
	CThread* m_pThreadHoming;
	CThread* m_pThreadPullbackScan;
	CThread* m_pThreadSaveRaw;
	CThread* m_pThreadUpdateCutView;
	CThread* m_pThreadLoadCatheter;
	CThread* m_pThreadUnloadCatheter;
	
	// Imaging
	COCTImaging* m_pImagingRealtime;
	COCTImaging* m_pImagingSimulate;

	// Data Writer
	CDataWriter* m_pDataWriter;
	tstring m_strFilePath;

#ifdef TEST_VALUE_FILE_PATH
	CDataReader* m_pDataReader;
#endif

	// Cut View
	CCutViewManager* m_pCutView;
	int m_nOffsetNavigation;

	// Acquisition
	IAcquisitionDevice* m_pAcqDevice;

	// Simulation
	IAcquisitionDevice* m_pSimDevice;

	RayScannerState m_curState;

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;

public:
	COCTSystem();
	virtual ~COCTSystem();

	RayError Start();
	RayError Stop();
	RayError RegisterCallback(FunctionPtr cb);
	RayError Initialize();
	RayError PullbackScan(char *strFilePath);
	RayError LoadCatheter();
	RayError UnloadCatheter();
	RayError EndReview();
	RayError MotorOnOff(bool mode);
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
	bool GetMotorOnOff();
	bool GetIsPaused();

private:
	// Thread
	static UINT threadService(LPVOID param);
	static UINT threadInitialize(LPVOID param);
	static UINT threadHoming(LPVOID param);
	static UINT threadPullbackScan(LPVOID param);
	static UINT threadSaveRaw(LPVOID param);
	static UINT threadUpdateCutView(LPVOID param);
	static UINT threadLoadCatheter(LPVOID param);
	static UINT threadUnloadCatheter(LPVOID param);

	// Imaging
	COCTImaging* createColorImaging(CMessageService*);
	int initializeAcqDevice();
	int initializeRotaryJunction();
	void setMotorOnOff(bool on);
	void updateCutView(int drawSamples);

protected:
	LRESULT OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateCutViewDone(WPARAM wParam, LPARAM lParam);
};

