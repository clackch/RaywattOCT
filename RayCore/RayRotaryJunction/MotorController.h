#pragma once
#include "Config.h"
#include "Connection.h"

#define MOTOR_INDEX_CONTROLWORD		0x6040
#define MOTOR_INDEX_STATUSWORD		0x6041
#define MOTOR_INDEX_TARGETVELOCITY	0x60ff
#define MOTOR_DATA_SWITCH_ON		0x0006
#define MOTOR_DATA_ENABLE_OPERATION	0x000F
#define MOTOR_DATA_SWITCH_OFF		0x000D

class CThread;
class CMotorController
{
private:
	IConnection* m_pConnection;
	bool m_initMotor;
	bool m_isRun;

	CThread* m_pThread;

private:
	static CMotorController* pInstance;

	CMotorController();
public:
	static CMotorController* GetInstance();
	virtual ~CMotorController();

	bool IsConnected() { return m_initMotor; }
	bool Connect(void* param = nullptr);
	void Disconnect();

	bool SwitchOn();
	bool PerformRun(int &nVelocity);
	bool StopMotor();
	bool SwitchOff();
	
	bool IsRun() { return m_isRun; }

private:
	static UINT threadReadMotor(LPVOID param);
	BYTE calcCRCByte(BYTE u8Byte, BYTE u8CRC);
	bool writeMotor(BYTE* packet, int size);
	void getMotorPacket(unsigned short command, unsigned int data, unsigned int dataSize, BYTE* packet, int& packetLength);
};

