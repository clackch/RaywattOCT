#pragma once

#import "C:\\Program Files\\Axsun\\Axsun OCT Control\\AxsunOCTControl.tlb"
using namespace AxsunOCTControl;

#define AXSUN_MAX_DEVICES		5
#define AXSUN_LASER_DEVICE		40

class CLaserController
{
private:
	static CLaserController* pInstance;
	IAxsunOCTControlPtr m_pAxsunOCTControl;
	unsigned long m_pDeviceList[AXSUN_MAX_DEVICES];
	long m_numDevices;
	bool m_bInitialized;
private:
	CLaserController();

public:
	static CLaserController* GetInstance();
	virtual ~CLaserController();

	long GetNumDevices() { return m_numDevices; }
	int LaserOnOff(bool on);

private:
	long searchDeviceList(long whichDevice, unsigned long* myDeviceList);
	long enumerateDevices(unsigned long* myDeviceList, IAxsunOCTControlPtr pAxsunOCTControl);
	bool IsInitialized() const { return m_bInitialized; }
};

