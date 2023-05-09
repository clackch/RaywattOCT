#pragma once
#include "AcquisitionDevice.h"

class IDataManager;
class CSimulateDevice :
    public IAcquisitionDevice
{
private:
	IDataManager* m_pDataReader;
	int m_nCurSampleIndex;
	bool m_bPause;

public:
    CSimulateDevice(IDataManager*);
    virtual ~CSimulateDevice();

	virtual int InitDevice();
	virtual int CleanUp();

	void SetPause(bool pause) { m_bPause = pause; }
	bool IsPaused() { return m_bPause; }
	void PrevFrame();
	void NextFrame();
	void SetFrame(int nFrame);

protected:
	virtual int start();
	virtual int stop();
	virtual char* acquire(int& nCurFrame, int& nTotalFrame);
};

