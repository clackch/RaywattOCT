
// RaywattLabDlg.h: 헤더 파일
//

#pragma once
#include "CommonDlg.h"
#include "Imaging.h"
#include "AcquisitionDevice.h"
#include "ArduinoController.h"
#include "LaserModule.h"
#include "ScopeView.h"
#include "RotaryJunctionDlg.h"
#include "MessageService.h"
#include <opencv2/opencv.hpp>
#include <plog/Log.h>

#define WM_SAVE_CALIBRATION_FRAME		(WM_USER + 0x2001)
#define WM_SAVE_CALIBRATION_DONE		(WM_USER + 0x2002)
#define WM_PULLBACK_DONE				(WM_USER + 0x2003)

#define BACKGROUND_FILEPATH		".\\BACKGROUND.bin"

class CLabImaging;
class CDataWriter;
class CDataReader;
class CVideoWriter;
// CRaywattLabDlg 대화 상자
class CRaywattLabDlg : public CDialogEx, CCommonDlg, CMessageService
{
private:
	// Service
	CThread* m_pThreadService;

	// Acquisition
	IAcquisitionDevice* m_pAcqDevice;

	// Simulation
	IAcquisitionDevice* m_pSimDevice;

	// Imaging
	CLabImaging* m_pImagingRealtime;
	CLabImaging* m_pImagingSimulate;
	int m_isRealtime;

	// Data Writer
	CDataWriter* m_pDataWriter;
	FILE* m_pFFTFile;
	std::vector<unsigned short*> m_vFFTData;

	// Data Reader
	CDataReader* m_pDataReader;

	// Rotary Junction
	CArduinoController* m_pRotaryJunction;
	CLaserModule* m_pLaserModule;

	// UI Components
	CListBox m_listPatientData;
	ToggleButton m_btnLoadData;
	ToggleButton m_btnPlayData;
	ToggleButton m_btnSaveData;
	ToggleButton m_btnOpenRotaryJunction;
	CStatic m_pictOCTImage;
	ScopeView m_scopeView;
	ScopeView m_scopeViewFFT;
	int m_radioImageShape;
	int m_radioImageColor;
	BOOL m_chkImageHotColor;
	BOOL m_chkShowGuide;
	CSliderCtrl m_sliderBrightness;
	CSliderCtrl m_sliderContrast;
	CSliderCtrl m_sliderLowLevel;
	CSliderCtrl m_sliderHighLevel;
	BOOL m_chkInitMotor;
	BOOL m_chkInitStage;

	// Patient Data
	CString m_strPatientPath;
	CString m_strPatientName;

	// Rotary Junction
	CRotaryJunctionDlg m_dlgRotaryJunction;

	// Calibration
	CThread* m_pThreadCalibration;
	CString m_strCalibrationPrefix;
	char* m_pFrameBuffer;

	// Calibration Test
	CString m_strCalibPath;
	std::vector<CString> m_vCalibList;
	int m_nCurCalibIndex;
	tstring m_strCurCalibration;

	// Pullback
	CThread* m_pThreadPullback;

	bool m_bInitialized;

// 생성입니다.
public:
	CRaywattLabDlg(CWnd* pParent = nullptr);	// 표준 생성자입니다.

// 대화 상자 데이터입니다.
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_RAYWATTLAB_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV 지원입니다.

private:
	void setLogger(TCHAR* logRootPath);
	int initializeDevices();
	void updatePatientDataList();
	void initScopeViewLayout();
	void updateBrightnessContrast(CLabImaging *pImaging);
	void updateLevel(CLabImaging* pImaging);
	CString generateFileName(CString strPath, CString strExtension, CString strPrefix = _T(""));
	CString getLoadedFilePath();
	CString splitFileName(CString strFilePath);
	CLabImaging* createImaging(IImaging::Setting imaging);
	USHORT* readBackground(const char* strBackgroundFile, IImaging::Setting setting);
	void findFileByExtension(CString strFolder, CString strExt, std::vector<CString>& vList);
	void updateMeasurement(USHORT nPeakValue, int nPeakIndex, int nLineWidth, USHORT nNoisePower);
	void drawGuideLine(cv::Mat image);

	/*
	* threadService
	*/
	static UINT threadService(LPVOID param);
	static UINT threadSaveCalibration(LPVOID param);
	static UINT threadPullback(LPVOID param);

// 구현입니다.
protected:
	HICON m_hIcon;

	// 생성된 메시지 맵 함수
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg void OnDestroy();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()

	LRESULT OnMsgProcessOCTDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgSaveCalibrationFrame(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgSaveCalibrationDone(WPARAM wParam, LPARAM lParam);
	LRESULT OnMsgPullbackDone(WPARAM wParam, LPARAM lParam);
public:
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	afx_msg void OnBnClickedButtonAdminInitialize();
	afx_msg void OnBnClickedButtonOpenDataFolder();
	afx_msg void OnBnClickedButtonLoadSelectedData();
	afx_msg void OnBnClickedButtonPlayLoadedData();
	afx_msg void OnBnClickedButtonSaveData();
	afx_msg void OnBnClickedButtonSaveVideo();
	afx_msg void OnBnClickedButtonSaveTif();
	afx_msg void OnBnClickedButtonSavePng();
	afx_msg void OnBnClickedRadioImageCircle();
	afx_msg void OnBnClickedRadioImageRectangle();
	afx_msg void OnBnClickedRadioColorBlack();
	afx_msg void OnBnClickedRadioColorWhite();
	afx_msg void OnBnClickedCheckHotColor();
	afx_msg void OnBnClickedCheckShowGuide();
	afx_msg void OnNMCustomdrawSliderBrightness(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnNMCustomdrawSliderContrast(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnNMCustomdrawSliderLowlevel(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnNMCustomdrawSliderHighlevel(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnBnClickedButtonOpenRotaryJunction();
	afx_msg void OnBnClickedButtonSaveCalibration();
	afx_msg void OnBnClickedButtonChangeCalibration();
	afx_msg void OnBnClickedCheckBackgroundSubtract();
	afx_msg void OnBnClickedButtonOpenCalibFolder();
	afx_msg void OnBnClickedButtonMeasure();
	afx_msg void OnBnClickedCheckInitMotor();
	afx_msg void OnBnClickedCheckInitStage();
	afx_msg void OnBnClickedButtonPullback();
	afx_msg void OnBnClickedButtonRestartAcquisition();
};
