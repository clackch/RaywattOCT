#pragma once
#include "Config.h"
#include "CommonProtocol.h"
#include "MotorController.h"
#include <vector>

#define ENABLE_RFID		false

// position: step, speed: step/s
#define DISTANCE_BETWEEN_MOTORS			2500
#define PULLBACK_MOTOR_POS_INITIAL		19300
#define PULLBACK_MOTOR_POS_LOAD			4000
#define HUB_MOTOR_POS_INITIAL			0
#define STEP_MOTOR_SPEED_DEFAULT		9448
#define STEP_MOTOR_SPEED_LOAD			863
#define PULLBACK_MOTOR_RESOLUTION		0.0254f	/* mm/step */
#define MOTOR_CONTROL_RESOLUTION		8

enum class eRJState {
	Disconnected = 0,
	Connected,
	Validating,
	Loading,
	WaitManualLoad,
	Loaded,
	Unloading,
	Unloaded,
	Error
};

class CMessageService;
class CRJController
	: public CMotorController,
	public IStepMotorAction,
	public ICommonProtocol
{
private:
	CMessageService* m_pMsg;
	CThread* m_pThreadState;
	eRJState m_state;
	eRJState m_nextState;
	eRJState m_recvState;
	bool m_bStateReceived;

	int m_nStepPosition[2];
	int m_nStepSpeed[2];

	bool m_isSMMoving[2];
	bool m_bPhotoSensor[6];
	bool m_bButton[2];	// 0: UNLOCK, 1: STOP
	bool m_bLimitSwitch;
	BYTE m_RFID[MAX_PATH];
	int m_nRFIDLength;

	bool m_bManualMode;	// Manual Load Catheter

public:
	CRJController();
	virtual ~CRJController();

	virtual void SetMessage(CMessageService* pMsg) { m_pMsg = pMsg; }
	virtual void UpdateState(eRJState state);

	virtual bool Connect(void* strPort);
	virtual void Disconnect();

	virtual bool IsMoving();
	virtual bool ReadPosition();
	virtual bool Current(eStepMotorIndex idxMotor, int posStep);
	virtual bool Move(eStepMotorIndex idxMotor, int posStep, bool delay=false, char sensor=0);
	virtual bool Set(eStepMotorIndex idxMotor, int velStep);

	bool StartControl();
	bool AutoStatePeriod(USHORT interval);
	bool StopStepMotors();
	bool DisplayLCD(eLCDImage image);
	bool ReadRFID();
	UINT GetRFIDInfo(BYTE* pRFIDInfo);

	int ConvertMMtoStep(UINT mm);
	void SetManualMode(bool on) { m_bManualMode = on; }
protected:
	static UINT threadRJState(LPVOID param);
	static UINT threadReadPacket(LPVOID param);
	void updateState();
	void updateStateManualMode();
	void updateState(eRJState state);
	bool displayLCD(eLCDImage image);
	void parseSMPacket(BYTE*packet, int size);
	void parseRFIDPacket(BYTE*packet, int size);
	virtual void handlePacket();
	virtual bool writeMotor(BYTE* packet, int size);
};

