#pragma once
#include "Config.h"
#include "StepMotorController.h"

#define DELAY_BETWEEN_COMMAND	2000

class CArduinoController
	: public CStepMotorController,
	public IStepMotorAction
{
private:
	double m_pPosition[(UINT)eStepMotorIndex::Max];
	double m_fTargetPosition;
	UINT m_nSpeed;
public:
	CArduinoController();
	virtual ~CArduinoController();

	virtual bool Open(tstring strPort);
	virtual bool IsMoving();

	virtual bool Current(eStepMotorIndex idx, int nPosition);
	virtual bool Move(eStepMotorIndex idx, int nPos, bool delay=true);	// forward (load / unload catheter)
	virtual bool Set(eStepMotorIndex idx, int nVelocity);

protected:
	virtual void readResponse();
	bool parseResponse(const char* strResponse);
};

