#pragma once
#include "Config.h"

// position: mm, speed: mm/s
#define DELAYLINE_BACKWARD_POSITION	(-400)
#define DELAYLINE_FORWARD_POSITION		(400)
#define DISTANCE_BETWEEN_MOTORS			4
#define PULLBACK_MOTOR_POS_INITIAL		80
#define PULLBACK_MOTOR_POS_LOAD			30
#define HUB_MOTOR_POS_INITIAL			0
#define STEP_MOTOR_SPEED_DEFAULT		30
#define STEP_MOTOR_SPEED_LOAD			4
enum class StepMotorIndex : UINT
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

