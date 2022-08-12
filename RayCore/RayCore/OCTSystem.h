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

enum class AsyncWork {
	Initialize = 0
};

enum class OCTScannerState {
	STATE_NONE = 0,
	STATE_INITIALIZING,
	STATE_INIT_FAILED,
	STATE_HOMING,
	STATE_READY,
	STATE_LOAD_CATHETER,
	STATE_SCANNING,
	STATE_REVIEW,
	STATE_SAVE_DONE,
};

class CThread;
class CMoriaImaging;
class CDataWriter;
class CCutViewManager;
class COCTSystem : public CMessageService
{
private:
	// Thread
	FunctionPtr m_callback;
	
	CThread* m_pThreadService;
	CThread* m_pThreadInitialize;
	CThread* m_pThreadHoming;
	CThread* m_pThreadPullbackScan;
	CThread* m_pThreadSaveRaw;
	CThread* m_pThreadUpdateCutView;
	CThread* m_pThreadLoadCatheter;
	CThread* m_pThreadUnloadCatheter;
	
	// Imaging
	CMoriaImaging* m_pImagingRealtime;
	CMoriaImaging* m_pImagingSimulate;

	// Data Writer
	CDataWriter* m_pDataWriter;
	tstring m_strFilePath;

	// Cut View
	CCutViewManager* m_pCutView;
	int m_nOffsetNavigation;

	// Acquisition
	IAcquisitionDevice* m_pAcqDevice;

	// Simulation
	IAcquisitionDevice* m_pSimDevice;

	OCTScannerState m_curState;

	//Property
	double m_fBrightness;
	double m_fContrast;
	double m_fDegree;

public:
	COCTSystem();
	virtual ~COCTSystem();
	
	RayError RegisterCallback(FunctionPtr cb);
	RayError Initialize();
	RayError PullbackScan();
	RayError LoadCatheter();
	RayError UnloadCatheter();

	//Property
	double GetBrightnessProperty();
	RayError SetBrightnessProperty(double value);
	double GetContrastProperty();
	RayError SetContrastProperty(double value);
	double GetDegreeProperty();
	RayError SetDegreeProperty(double value);

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
	void initialize();
	CMoriaImaging* createColorImaging(CMessageService*);
	int initializeAcqDevice();
	int initializeRotaryJunction();
	void updateCutView(int drawSamples);

protected:
	LRESULT OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateScannerState(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateSaveRaw(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgUpdateCutViewDone(WPARAM wParam, LPARAM lParam);
};

