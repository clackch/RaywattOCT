#pragma once
#include "libusb.h"

#define USB_ENDPOINT_IN	    (LIBUSB_ENDPOINT_IN  | 1)   /* endpoint address */
#define USB_ENDPOINT_OUT	0x01	//(LIBUSB_ENDPOINT_OUT | 2)   /* endpoint address */
#define USB_TIMEOUT	        3000        /* Connection timeout (in ms) */
#define MOTOR_INDEX_CONTROLWORD		0x6040
#define MOTOR_INDEX_TARGETVELOCITY	0x60ff
#define MOTOR_DATA_SWITCH_ON		0x0006	// switch on? shut down?
#define MOTOR_DATA_ENABLE_OPERATION	0x000F
#define MOTOR_DATA_SWITCH_OFF		0x000D	// switch off?

class CThread;
class CMotorController
{
private:
	bool m_initUsb;
	bool m_initMotor;
	bool m_isRun;
	libusb_device_handle* m_hUsbHandle;

	CThread* m_pThread;

private:
	static CMotorController* pInstance;

	CMotorController();
public:
	static CMotorController* GetInstance();
	virtual ~CMotorController();

	bool IsConnected() { return m_initMotor; }
	bool Connect();
	void Disconnect();

	bool SwitchOn();
	bool PerformRun(int &nVelocity);
	bool StopMotor();
	bool SwitchOff();
	
	bool IsRun() { return m_isRun; }

private:
	static UINT threadReadMotor(LPVOID param);
	bool checkUsbDescription(libusb_device* dev);
	BYTE calcCRCByte(BYTE u8Byte, BYTE u8CRC);
	bool writeMotor(BYTE* packet, int size);
	void getMotorPacket(unsigned short command, unsigned int data, unsigned int dataSize, BYTE* packet, int& packetLength);
};

