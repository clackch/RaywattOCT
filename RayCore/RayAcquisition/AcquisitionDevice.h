#pragma once

class IImaging;
class IDataManager;
class CThread;
class IAcquisitionDevice
{
protected:
	bool m_isInit;
	CThread *m_pThread;

	IImaging* m_pImaging;
	IDataManager* m_pWriter;
public:
	IAcquisitionDevice();
	virtual ~IAcquisitionDevice();

	bool IsInit() { return m_isInit; }
	void SetImaging(IImaging* pImaging) { m_pImaging = pImaging; }
	void SetWriter(IDataManager* pWriter) { m_pWriter = pWriter; }

	virtual int InitDevice() = 0;
	virtual int CleanUp() = 0;

	int StartAcquisition();
	int StopAcquisition();

protected:
	virtual int start() = 0;
	virtual int stop() = 0;
	virtual unsigned short *acquire(int & nCurFrame, int &nTotalFrame) = 0;

private:
	static UINT threadAcquire(LPVOID param);
};

