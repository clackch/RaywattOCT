#pragma once
#include "Config.h"
#include "CommonProtocol.h"
#include "MotorController.h"

#define MAX_VOLTAGE_RAW_VALUE			4095
#define CM_SM_SPEED_DEFAULT				1000
#define CM_SM_SPEED_MAX					10000
#define CM_SM_SPEED_AUTO				2500
#define DELAYLINE_BACKWARD_POSITION		(-50)
#define DELAYLINE_FORWARD_POSITION		(50)

class CLaserModule
	: public CMotorController,
	public IStepMotorAction,
	public ICommonProtocol
{
private:
	int m_nStepPosition[2];
	int m_nStepSpeed[2];
	unsigned short m_nVOA, m_nVLD;

	int m_nActualPosition[2];
	bool m_isSMMoving[2];
	bool m_bPhotoSensor[6];

public:
	CLaserModule();
	virtual ~CLaserModule();

	virtual bool Connect(void* strPort);
	virtual void Disconnect();

	virtual bool IsMoving(eStepMotorIndex idxMotor);
	virtual bool ReadPosition();
	virtual bool Current(eStepMotorIndex idxMotor, int posStep);
	virtual bool Move(eStepMotorIndex idxMotor, int posStep, bool delay = false, char sensor = 0);
	virtual bool Set(eStepMotorIndex idxMotor, int velStep);

	int GetPosition(eStepMotorIndex idxMotor) { return m_nActualPosition[(int)idxMotor - 1]; }
	int MoveRelative(eStepMotorIndex idxMotor, int nOffset);
	void SetVOA(unsigned short voa);
	void SetVLD(unsigned short vld);

	bool AutoStatePeriod(USHORT interval);
	bool StopStepMotors();
protected:
	static UINT threadReadPacket(LPVOID param);
	void initSetting();
	void parseAutoReportPacket(BYTE* packet, int size);
	void parseSMPacket(BYTE* packet, int size);
	void setVOAVLD();
	virtual void handlePacket();
};

