#pragma once

#include "define.h"
#include "Config.h"
#include <opencv2/opencv.hpp>

class CMessageService;
class COCTImaging;
class CSimulateDevice;
class IDataManager;
class CThread;
class CCutViewManager;
class CImagingSession
{
private:
	CMessageService* m_pMsg;
	int m_nSession;

	COCTImaging* m_pImaging;
	CSimulateDevice* m_pSimDevice;
	IDataManager* m_pDataManager;

	bool m_deleteData;

	CThread* m_pThreadUpdateCutView;
	CCutViewManager* m_pCutView;
	cv::Scalar m_backgroundColor;

private:
	CImagingSession(CMessageService* pMsg, int nSession, bool deleteData = true);
public:
	virtual ~CImagingSession();

	static CImagingSession* CreateSession(CMessageService* pMsg, int nSession, IDataManager *pWriter);
	static CImagingSession* CreateSession(CMessageService* pMsg, int nSession, const char* strFilePath);
	static COCTImaging* CreateColorImaging(CMessageService* msg);

	void EnableCutView(cv::Scalar backgroundColor);

	IDataManager* GetDataManager() { return m_pDataManager; }
	COCTImaging* GetImaging() { return m_pImaging; }
	CCutViewManager* GetCutView() { return m_pCutView; }

	// Asynchronous functions
	int Start();
	int Stop();
	bool IsPaused();
	void SetPause(bool pause);
	void PrevFrame();
	void NextFrame();
	void MoveToFrame(int nFrame);

	// Synchronous functions
	UINT GetImageWidth();
	UINT GetImageHeight();
	UINT GetImageChannels();
	UINT GetImageDepth();
	void* GetImageData(int nFrame);

private:
	static CImagingSession* createSession(CMessageService* pMsg, int nSession, IDataManager* pData, bool deleteData);
	static UINT threadUpdateCutView(LPVOID param);	
};

