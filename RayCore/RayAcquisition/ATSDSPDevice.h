#pragma once

#include "ATSDevice.h"
#include "AlazarDSP.h"

class CCalibration;
class CATSDSPDevice : public CATSDevice
{
private:
	dsp_module_handle m_fftHandle;
	U32 m_bytesPerBuffer;
	unsigned short* m_pBackgroundFringes;

	CCalibration* m_calibration;

public:
	CATSDSPDevice(Setting, CCalibration*);
	virtual ~CATSDSPDevice();

protected:
	virtual int stop();
	virtual char* acquire(int& nCurFrame, int& nTotalFrame);
	virtual BOOL configureFPGA(HANDLE boardHandle);
	virtual BOOL calculateMemorySize(HANDLE boardHandle, U16 channelMask, U32 recordsPerBuffer, U32& samplesPerRecord, U32& bytesPerBuffer);
	int getFFTLength(unsigned int nAScan);
};

