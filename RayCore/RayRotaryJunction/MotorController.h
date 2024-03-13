#pragma once
#include "Config.h"
#include "Connection.h"

#define MOTOR_INDEX_CONTROLWORD			0x6040
#define MOTOR_INDEX_STATUSWORD			0x6041
#define MOTOR_INDEX_MODESOFOPERATION	0x6060
#define MOTOR_INDEX_ACTUALVELOCITY		0x606C
#define MOTOR_INDEX_TARGETVELOCITY		0x60ff
#define MOTOR_DATA_SWITCH_ON			0x0006
#define MOTOR_DATA_ENABLE_OPERATION		0x000F
#define MOTOR_DATA_SWITCH_OFF			0x000D
#define MOTOR_DATA_MODE_POSITION		0x01
#define MOTOR_DATA_MODE_VELOCITY		0x03

class CThread;
class CMotorController
{
protected:
	IConnection* m_pConnection;
	bool m_initMotor;
	bool m_isRun;
	int m_nActualVelocity;
	CThread* m_pThread;

public:
	CMotorController();
	virtual ~CMotorController();

	bool IsConnected() { return m_initMotor; }
	virtual bool Connect(void* param = nullptr);
	virtual void Disconnect();

	bool SetModeOfOperation(char mode);
	bool SwitchOn();
	virtual bool PerformRun(int &nVelocity);
	virtual bool StopMotor();
	bool SwitchOff();
	bool ReadActualVelocity();
	
	bool IsRun() { return m_isRun; }
	int GetActualVelocity() { return m_nActualVelocity; }

protected:
	static UINT threadReadMotor(LPVOID param);
	BYTE calcCRCByte(BYTE u8Byte, BYTE u8CRC);
	virtual bool writeMotor(BYTE* packet, int size);
	void getMotorPacket(unsigned short command, unsigned int data, unsigned int dataSize, BYTE* packet, int& packetLength);
	bool parsePacket(BYTE* packet, int size);
};

class CMotorControllerStub
	: public CMotorController
{
public:
	CMotorControllerStub(){}
	virtual ~CMotorControllerStub() {}

	virtual bool PerformRun(int& nVelocity) { m_isRun = true; return true; }
	virtual bool StopMotor() { m_isRun = false; return true; }
};