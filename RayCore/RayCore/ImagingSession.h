#pragma once

#include "define.h"

class CMessageService;
class COCTImaging;
class CSimulateDevice;
class IDataManager;
class CImagingSession
{
private:
	CMessageService* m_pMsg;
	int m_nSession;

	COCTImaging* m_pImaging;
	CSimulateDevice* m_pSimDevice;
	IDataManager* m_pDataManager;

	bool m_deleteData;

private:
	CImagingSession(CMessageService* pMsg, int nSession, bool deleteData = true);
public:
	virtual ~CImagingSession();

	static CImagingSession* CreateSession(CMessageService* pMsg, int nSession, IDataManager *pWriter);
	static CImagingSession* CreateSession(CMessageService* pMsg, int nSession, const char* strFilePath);
	static COCTImaging* CreateColorImaging(CMessageService* msg);

	IDataManager* GetDataManager() { return m_pDataManager; }
	void EnableWorkItem(RayWorkItem item, bool enable);
	int Start();
	int Stop();
	bool IsPaused();
	void SetPause(bool pause);
	void PrevFrame();
	void NextFrame();
	void MoveToFrame(int nFrame);

private:
	static CImagingSession* createSession(CMessageService* pMsg, int nSession, IDataManager* pData, bool deleteData);
	
};

