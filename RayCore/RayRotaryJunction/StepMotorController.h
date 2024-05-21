#pragma once
#include "Config.h"

// position: mm, speed: mm/s
#define DELAYLINE_BACKWARD_POSITION	(-400)
#define DELAYLINE_FORWARD_POSITION		(400)
#define DISTANCE_BETWEEN_MOTORS			2000
#define PULLBACK_MOTOR_POS_INITIAL		10000
#define PULLBACK_MOTOR_POS_LOAD			3000
#define HUB_MOTOR_POS_INITIAL			0
#define STEP_MOTOR_SPEED_DEFAULT		863
#define STEP_MOTOR_SPEED_LOAD			863
enum class eStepMotorIndex : UINT
{
	Both = 0,
	Pullback = 1,
	Hub = 2,
	Max
};

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
	virtual bool IsMoving() = 0;

	bool IsOpen();
	void Close();

protected:
	bool sendCommand(const char* strCommand, bool readResponse=false);
	virtual void readResponse() = 0;
};

class IStepMotorAction
{
public:
	virtual bool Current(eStepMotorIndex idx, int posMM) = 0;
	virtual bool Move(eStepMotorIndex idx, int posMM, bool delay) = 0;
	virtual bool Set(eStepMotorIndex idx, int velocity) = 0;
};