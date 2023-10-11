#pragma once
#include "Config.h"
#include "StepMotorController.h"

#define DELAY_BETWEEN_COMMAND	2000

class CArduinoController
	: public CStepMotorController
{
private:
	double m_pPosition[(UINT)StepMotorIndex::Max];
	double m_fTargetPosition;
	UINT m_nSpeed;
public:
	CArduinoController();
	virtual ~CArduinoController();

	virtual bool Open(tstring strPort);
	virtual bool IsMoving();

	bool SetCurrent(StepMotorIndex idx, int nPosition);
	bool MoveAbsolute(StepMotorIndex idx, int nPos);	// forward (load / unload catheter)
	bool MoveRelative(StepMotorIndex idx, int nOffset);	// pullback
	bool SetSpeed(StepMotorIndex idx, int nVelocity);

protected:
	virtual void readResponse();
	bool parseResponse(const char* strResponse);
};

