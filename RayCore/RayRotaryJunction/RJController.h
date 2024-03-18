#pragma once
#include "Config.h"
#include "MotorController.h"
#include "StepMotorController.h"
#include <vector>

#define RJ_STX		0xA3
#define RJ_ETX		0xE4
#define RJ_LENGTH_IDX	1
#define RJ_FID_IDX		2
#define RJ_RET_IDX		3
#define RJ_PHOTO_IDX	4
#define RJ_KEY_IDX		5
#define RJ_DATA_IDX		6
#define RJ_HEADER_LEN	8

#define MM_PER_STEP		0.02f

enum class eCOMM_RJ : BYTE {
	COMM_SUCCESS = 0
	, COMM_CSUM_FAIL
	, COMM_UNKNOWN_FID
	, COMM_PARAM_ERROR
	, COMM_TIMEOUT
	, COMM_DOWN_STATE_ERR
	, COMM_DOWN_SERIAL_ERR
	, COMM_DOWN_SIZE_ERR
	, COMM_FLASH_ERASE_ERR
	, COMM_FLASH_WRITE_ERR
};

enum class eFID : BYTE {
	FID_AUTO_REPORT = 0x01
	, FID_GET_AUTO_PERIOD = 0x10
	, FID_SET_AUTO_PERIOD = 0x11
	, FID_SM_GET_CONFIG = 0x20
	, FID_SM_SET_CONFIG
	, FID_SM_GET_STATE
	, FID_SM_RUN
	, FID_SM_STOP
	, FID_SM_SET_POS
	, FID_BLDC_GET_STATE = 0x30
	, FID_BLDC_RUN
	, FID_BLDC_STOP
	, FID_BLDC_PASS
	, FID_LCD_GET_STATE = 0x40
	, FID_LCD_SET_STATE
	, FID_LCD_DOWN_IMAGE
	, FID_LCD_DISP_IMAGE
	, FID_RFID_GET_STATE = 0x50
};

enum class eSFID : BYTE {
	SFID_LCD_BL_OFF = 0
	, SFID_LCD_BL_ON
	, SFID_LCD_SETUP
};

enum class eLCDImage : USHORT {
	LCD_IMAGE_BOOTING = 0
	, LCD_IMAGE_UNLOADED
	, LCD_IMAGE_LOADING
	, LCD_IMAGE_STANDBY_ON
	, LCD_IMAGE_STANDBY_OFF
	, LCD_IMAGE_LIVEVIEW
	, LCD_IMAGE_PULLBACK
	, LCD_IMAGE_UNLOADING
	, LCD_IMAGE_ERROR
};

enum class eRJState {
	Disconnected = 0,
	Connected,
	Validating,
	Loading,
	Loaded,
	Unloading,
	Unloaded,
	Error
};

class CMessageService;
class CRJController
	: public CMotorController,
	public IStepMotorAction
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

	std::vector<BYTE> m_vPacket;
	bool m_isSMMoving[2];
	bool m_bPhotoSensor[6];
	bool m_bButton[2];	// 0: UNLOCK, 1: STOP
	bool m_bLimitSwitch;
	BYTE m_RFID[MAX_PATH];
	int m_nRFIDLength;

public:
	CRJController();
	virtual ~CRJController();

	virtual void SetMessage(CMessageService* pMsg) { m_pMsg = pMsg; }
	virtual void UpdateState(eRJState state);

	virtual bool Connect(void* strPort);
	virtual void Disconnect();

	virtual bool IsMoving();
	virtual bool ReadPosition();
	virtual bool Current(eStepMotorIndex idxMotor, int posMM);
	virtual bool Move(eStepMotorIndex idxMotor, int posMM, bool delay=false);
	virtual bool Set(eStepMotorIndex idxMotor, int velocity);

	bool StartControl();
	bool AutoStatePeriod(USHORT interval);
	bool StopStepMotors();
	bool DisplayLCD(eLCDImage image);
	bool ReadRFID();
	UINT GetRFIDInfo(BYTE* pRFIDInfo);
protected:
	static UINT threadRJState(LPVOID param);
	static UINT threadReadPacket(LPVOID param);
	void updateState();
	void updateState(eRJState state);
	bool displayLCD(eLCDImage image);
	void addPacket(BYTE* packet, int size);
	bool sliceUntilSTX(int index);
	bool parseSerialPacket();
	void parseSMPacket(BYTE*packet, int size);
	void parseRFIDPacket(BYTE*packet, int size);
	void handlePacket();
	void getSerialPacket(eFID fid, int dataSize, BYTE* packet, int &packetLength);
	BYTE calcChecksum(BYTE* packet, int length);
	virtual bool writeMotor(BYTE* packet, int size);
};

