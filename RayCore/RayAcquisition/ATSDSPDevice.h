#pragma once

#include "ATSDevice.h"
#include "AlazarDSP.h"

class CATSDSPDevice : public CATSDevice
{
private:
	dsp_module_handle m_fftHandle;
	U32 m_bytesPerBuffer;
	unsigned short* m_pBackgroundFringes;

public:
	CATSDSPDevice();
	virtual ~CATSDSPDevice();

protected:
	virtual unsigned short* acquire(int& nCurFrame, int& nTotalFrame);
	virtual BOOL configureFPGA(HANDLE boardHandle);
	virtual BOOL calculateMemorySize(HANDLE boardHandle, U16 channelMask, U32 recordsPerBuffer, U32& samplesPerRecord, U32& bytesPerBuffer);
};

