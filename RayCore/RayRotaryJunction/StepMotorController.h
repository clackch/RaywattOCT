#pragma once
#include "Config.h"

#define DISTANCE_BETWEEN_MOTORS	4
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
	virtual bool SetCurrent(StepMotorIndex idx, int nPosition) = 0;
	virtual bool IsMoving() = 0;
	virtual bool MoveAbsolute(StepMotorIndex idx, int nPosition) = 0;	// forward (load / unload catheter)
	virtual bool MoveRelative(StepMotorIndex idx, int nOffset) = 0;	// pullback
	virtual bool SetSpeed(StepMotorIndex idx, int nVelocity) = 0;

	bool IsOpen();
	void Close();

protected:
	bool sendCommand(const char* strCommand, bool readResponse=false);
	virtual void readResponse() = 0;
};

