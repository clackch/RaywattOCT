#pragma once
#include "Config.h"
#include "StepMotorController.h"

/*
* Linear Stage Model : A-LSQ075B-E01
* Reference : https://www.zaber.com/protocol-manual?device=X-LSQ075B-E01&peripheral=N%2FA&version=7.29&protocol=ASCII#topic_physical_units
*/

#define DELAYLINE_BACKWARD_POSITION	(-1)
#define DELAYLINE_FORWARD_POSITION		(1)
#define ZABER_MICROSTEP_SIZE				0.49609375f							// um
#define ZABER_SCALE_MM_TO_POSITION			(1000.f / ZABER_MICROSTEP_SIZE)		// 1000um = 1mm
#define ZABER_SCALE_MMS_TO_VELOCITY			(1.6384f / ZABER_MICROSTEP_SIZE * 1000)
#define ZABER_SCALE_MM_TO_ROTATE			12800

class CSerialPort;
class CZaberController
	: public CStepMotorController
{
public:
	CZaberController();
	virtual ~CZaberController();

	virtual bool Open(tstring strPort);
	virtual bool SetCurrent(int nPos);
	virtual bool IsMoving();
	virtual bool MoveAbsolute(int nPos);
	virtual bool MoveRelative(int nOffset);
	virtual bool SetSpeed(int nVelocity);

	bool Idle();
	bool MoveMicrometer(long long nPos);
	bool RotateRelative(int nPos);
	bool Pull(int nVelocity, int nDistance);

protected:
	virtual void readResponse();
	bool parseZaberState(const char* strResponse, std::string& strState, int& nPos);
	int convertUMtoData(long long nPos);
	int convertMMtoData(int nPos);
	int convertMMtoRotate(int nPos);
	int convertMMStoData(int nVelocity);
};

