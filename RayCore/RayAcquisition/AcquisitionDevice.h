#pragma once

#include <chrono>
#include <locale>

class IImaging;
class IDataManager;
class CThread;
class IAcquisitionDevice
{
public:
	class Setting {
	public:
		unsigned int nAScan;
		unsigned int nBScan;
		unsigned int nLaserSpeed;
		unsigned int nBufferCount;
		unsigned int msTimeOut;
		unsigned int nTriggerDelaySample;
		bool bUseKClock;
		double usGoodClockDuration;
		double usBadClockDuration;
		bool bUseDES;
		unsigned int DAQSamplingRate;
	};

protected:
	bool m_isInit;
	CThread *m_pThread;

	IImaging* m_pImaging;
	IDataManager* m_pWriter;

	std::chrono::system_clock::time_point m_start, m_end;
	double m_fps;
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

	double GetFPS() { return m_fps; }

protected:
	virtual int start() = 0;
	virtual int stop() = 0;
	virtual char *acquire(int &nCurFrame, int &nTotalFrame) = 0;

private:
	static UINT threadAcquire(LPVOID param);
};

