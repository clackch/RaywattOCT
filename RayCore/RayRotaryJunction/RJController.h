#pragma once
#include "Config.h"

#include "RFIDProtocol.h"
#include "MotorController.h"
#include "WriteTaskController.h"
#include <vector>
#include <iomanip>

// position: step, speed: step/s
#define PULLBACK_MAX_DISTANCE			100		/* mm */	
#define DISTANCE_BETWEEN_MOTORS			975
#define PULLBACK_MOTOR_POS_INITIAL		9650
#define HUB_MOTOR_POS_INITIAL			0
#define STEP_MOTOR_SPEED_DEFAULT		4724
#define STEP_MOTOR_SPEED_LOAD			432
#define PULLBACK_MOTOR_RESOLUTION		0.0254f	/* mm/step */
#define MOTOR_CONTROL_RESOLUTION		4

#define RFID_REPLY_DATA_IDX				4
#define RFID_REPLY_LENGTH_IDX			1
#define RFID_MAX_COUNT					99
#define RFID_MANUFACTURER				"RAYWATT"
#define RFID_MANUFACTURER_LEN			7

enum class eRJState {
	None = 0,
	Initializing,
	Disconnected,
	Cleaning,
	Connected,
	Validating,
	Loading,
	WaitManualLoad,
	Loaded,
	Unloading,
	Unloaded,
	Error,
	RFIDError
};

enum RFID_ReadType
{
	DEFAULT,
	KEYS,
	STEP,
	MANUF,
	CNT,
	MANUF_CNT
};

enum RFID_ValidType
{
	VALID,
	INVALID,
	WAITING
};
enum RFID_AnswerType
{
	FAILED,
	ANSWERED,
	PROCEEDING
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
	bool m_bReadInitStatus;
	bool m_isInit;

	int m_nStepPosition[2];
	int m_nStepSpeed[2];

	bool m_isSMMoving[2];
	bool m_bPhotoSensor[6];
	bool m_bButton[2];	// 0: UNLOCK, 1: STOP
	bool m_bLimitSwitch;

	uint8_t m_nRFIDLength;
	uint8_t m_nRFIDUsageCount;
	BYTE m_RFID[MAX_PATH];
	BYTE m_byManufacturerId[MAX_PATH];
	CThread* m_pThreadRFIDTag;

	bool m_bManualMode;	// Manual Load Catheter

	WriteTaskController* m_resendManager;

	int catheterUsage;
public:
	CRJController();
	virtual ~CRJController();

	eRJState GetState() { return m_state; }
	virtual void SetMessage(CMessageService* pMsg) { m_pMsg = pMsg; }
	virtual void UpdateState(eRJState state);

	virtual bool Connect(void* strPort);
	virtual void Disconnect();

	virtual bool IsMoving();
	virtual bool ReadPosition();
	virtual bool Current(eStepMotorIndex idxMotor, int posStep);
	virtual bool Move(eStepMotorIndex idxMotor, int posStep, bool delay=false, char sensor=0);
	virtual bool Set(eStepMotorIndex idxMotor, int velStep);

	const char* GetStateString(eRJState state);
	bool InitialStatusReceived() { return m_bReadInitStatus; }
	bool StartControl();
	bool AutoStatePeriod(USHORT interval);
	bool StopStepMotors();
	bool DisplayLCD(eLCDImage image);
	bool ReadRFID();
	bool IncreaseRFIDUsage(int uidSize, BYTE* UID);
	bool ResetRFIDUsage(int uidSize, BYTE* UID);
	bool ResetRFIDUID(int uidSize, BYTE* UID, int dataSize, BYTE* newUID);
	bool SetRFIDUsage(int uidSize, BYTE* UID, BYTE count);
	bool SetRFIDManuf(int uidSize, BYTE* UID, int dataSize, BYTE* manuf);
	bool SetRFIDKey(int uidSize, BYTE* UID, int dataSize, BYTE* key);
	bool GetRFIDKey();
	bool SetRFIDStep(int uidSize, BYTE* UID, int step);
	bool GetRFIDStep();
	bool GetIsTagging();
	void findCorrectKey();
	void SetCatheterUsage(int usage) { catheterUsage = usage; }
	int GetCatheterUsage() { return catheterUsage;}

	UINT GetRFIDUID(BYTE* pRFIDUID); 
	int GetRFIDCountCurrentState();
	bool GetPhotoSensorOnOff(int index) { return m_bPhotoSensor[index]; }
	RFID_ValidType isValidRFID();
	RFID_AnswerType checkAnswerRFID(RFIDProtocol::SRFIDState state);
	static DWORD WINAPI checkKeyFinding(LPVOID);

	int ConvertMMtoStep(UINT mm);
	void SetManualMode(bool on) { m_bManualMode = on; }

	void changeSMProfileToPullback();
	void changeSMProfileToLoadUnload();
	void DisableStepMotors();
	void moveDelaylineToRFIDPosition();
  
protected:
	void initSetting();
	static UINT threadRJState(LPVOID param);
	static UINT threadReadPacket(LPVOID param);
	static UINT threadReadTag(LPVOID param);
	void updateState();
	void updateStateManualMode();
	void updateState(eRJState state);
	bool displayLCD(eLCDImage image);
	void RxPacketRFIDGetState(BYTE* buff, RFID_ReadType type = DEFAULT);
	void parseSMPacket(BYTE*packet, int size);
	virtual void handlePacket();
	virtual bool writeMotor(BYTE* packet, int size);
	void resendPacket(eFID fid);
	void resendAllSaved();
};

