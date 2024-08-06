#pragma once
#include "AcquisitionDevice.h"
#include "AlazarApi.h"

class CATSDevice :
	public IAcquisitionDevice
{
protected:
	Setting m_setting;

	HANDLE	m_hATSBoard;
	double	m_dSamplePerSec;
	U32		m_nBufferIndex;
	U16 **	m_pAcqBuffers;
	U32		m_nAdmaFlags;

	U16 *	m_pCurBuffer;
	U16 *	m_pPrevBuffer;

public:
	CATSDevice(Setting);
	virtual ~CATSDevice();

	virtual int InitDevice();
	virtual int CleanUp();

	void SetSetting(Setting setting) { m_setting = setting; }
	Setting GetSetting() { return m_setting; }

protected:
	virtual int start();
	virtual int stop();
	virtual char *acquire(int& nCurFrame, int& nTotalFrame);

private:
	BOOL calibrateBoard(HANDLE boardHandle);
	BOOL configureBoard(HANDLE boardHandle);
	BOOL configureAcquisition(HANDLE boardHandle);

protected:
	virtual BOOL configureFPGA(HANDLE boardHandle) { return TRUE; }
	virtual BOOL calculateMemorySize(HANDLE boardHandle, U16 channelMask, U32 recordsPerBuffer, U32& samplesPerRecord, U32& bytesPerBuffer);
};

