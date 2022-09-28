#pragma once

class CImaging;
class CDataWriter;
class CThread;
class IAcquisitionDevice
{
protected:
	bool m_isInit;
	CThread *m_pThread;

	CImaging* m_pImaging;
	CDataWriter* m_pWriter;
public:
	IAcquisitionDevice();
	virtual ~IAcquisitionDevice();

	bool IsInit() { return m_isInit; }
	void SetImaging(CImaging* pImaging) { m_pImaging = pImaging; }
	void SetWriter(CDataWriter* pWriter) { m_pWriter = pWriter; }

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

