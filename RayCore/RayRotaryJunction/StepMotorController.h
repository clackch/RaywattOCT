#pragma once
#include "Config.h"

typedef enum {
	STEP_MOTOR_PULLBACK = 0,
	STEP_MOTOR_DELAYLINE,
	STEP_MOTOR_NUM
}MotorType;

class CSerialPort;
class CStepMotorController
{
protected:
	CSerialPort* m_pPort;
	BYTE m_pReadBuffer[MAX_PATH];
	double m_fPosition;

public:
	CStepMotorController();
	virtual ~CStepMotorController();

	virtual bool Open(tstring strPort) = 0;
	virtual bool SetCurrent(int nPosition) = 0;
	virtual bool IsMoving() = 0;
	virtual bool MoveAbsolute(int nPosition) = 0;	// forward (load / unload catheter)
	virtual bool MoveRelative(int nOffset) = 0;	// pullback
	virtual bool SetSpeed(int nVelocity) = 0;

	bool IsOpen();
	void Close();

protected:
	bool sendCommand(const char* strCommand, bool readResponse=false);
	virtual void readResponse() = 0;
};

