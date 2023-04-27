#pragma once
#include "Config.h"
#include "StepMotorController.h"

#define HAYDON_PULLBACK_LIMIT	870

class CArduinoController
	: public CStepMotorController
{
private:
	double m_fTargetPosition;
public:
	CArduinoController();
	virtual ~CArduinoController();

	virtual bool Open(tstring strPort);
	virtual bool IsMoving();
	virtual bool MoveAbsolute(int nPos);
	virtual bool MoveRelative(int nOffset);
	virtual bool SetSpeed(int nVelocity);

protected:
	virtual void readResponse();
	bool parseResponse(const char* strResponse);
};

