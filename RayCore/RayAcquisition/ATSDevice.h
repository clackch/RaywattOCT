#pragma once
#include "AcquisitionDevice.h"
#include "AlazarApi.h"

class CATSDevice :
	public IAcquisitionDevice
{
private:
	HANDLE	m_hATSBoard;
	double	m_dSamplePerSec;
	U32		m_nBufferIndex;
	U16 **	m_pAcqBuffers;

	U16 *	m_pCurBuffer;
	U16 *	m_pPrevBuffer;

public:
	CATSDevice(Setting);
	virtual ~CATSDevice();

	virtual int InitDevice();
	virtual int CleanUp();

protected:
	virtual int start();
	virtual int stop();
	virtual unsigned short *acquire(int& nCurFrame, int& nTotalFrame);

private:
	BOOL configureBoard(HANDLE boardHandle);
	BOOL configureAcquisition(HANDLE boardHandle);
};

