#pragma once

#include "Config.h"
#include <vector>

#define RJ_STX		0xA3
#define RJ_ETX		0xE4
#define CM_STX		0xA4
#define CM_ETX		0xE3
#define LENGTH_IDX	1
#define FID_IDX		2
#define RET_IDX		3
#define PHOTO_IDX	4
#define KEY_IDX		5
#define DATA_IDX	6
#define HEADER_LEN	8

// Firmware Download Protocol Commands
#define REQ_DOWNLOAD_CANCEL		0x00
#define REQ_DOWNLOAD_BEGIN		0x01
#define REQ_DOWNLOAD_END		0x02
#define REQ_DOWNLOAD_BODY		0x03
#define RSP_DOWNLOAD_DONE		0x0A
#define RSP_DOWNLOAD_ING		0x0B
#define RSP_DOWNLOAD_PAUSE		0x0C
#define RSP_DOWNLOAD_FAIL		0x0D
#define RSP_DOWNLOAD_READY		0x0E

// Firmware Download Constants
#define FW_CHUNK_SIZE		240		// Maximum data chunk size per packet
#define FW_MIN_FILE_SIZE	16		// Minimum file size (metadata)
#define FW_METADATA_SIZE	16		// Size of metadata at end of file
#define FW_FLASH_START		0x08010000
#define FW_FLASH_END		0x08080000

// Firmware Download State
enum class eFWDownloadState : BYTE {
	Idle = 0,
	Downloading,
	Success,
	Failed,
	Cancelled
};

// Firmware Metadata Structure (last 16 bytes of firmware file)
struct SFirmwareMetadata {
	UINT hwver;		// Hardware version
	UINT fwver;		// Firmware version
	UINT chkver;	// Checksum verification: ((hwver & 0xffff) << 16) + fwver
	UINT length;	// Flash address (0x08010000 ~ 0x08080000)
};

// Firmware Version Info
struct SFWVersionInfo {
	bool isBootMode;
	BYTE major;
	BYTE minor;
	BYTE patch;
};

// Callback function types
typedef void (*FWProgressCallback)(int progress);		// Progress: 0~100
typedef void (*FWStatusCallback)(int state);			// State change callback

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
	, COMM_READ_ERR
	, COMM_UNTAG_ERR
	, COMM_KEY_ERR
};

enum class eFID : BYTE {
	NO_FID = 0x00
	, FID_AUTO_REPORT = 0x01
	, FID_GET_MAIN_STATE
	, FID_SET_VOAVLD
	, FID_GET_AUTO_PERIOD = 0x10
	, FID_SET_AUTO_PERIOD = 0x11
	, FID_GET_VERSION = 0x12
	, FID_FW_DOWNLOAD = 0x13
	, FID_CPU_RESET = 0x14
	, FID_SM_GET_CONFIG = 0x20
	, FID_SM_SET_CONFIG
	, FID_SM_GET_STATE
	, FID_SM_RUN
	, FID_SM_STOP
	, FID_SM_SET_POS
	, FID_SM_CLEAR_ALMHIS
	, FID_BLDC_GET_STATE = 0x30
	, FID_BLDC_RUN
	, FID_BLDC_STOP
	, FID_BLDC_PASS
	, FID_LCD_GET_STATE = 0x40
	, FID_LCD_SET_STATE
	, FID_LCD_DOWN_IMAGE
	, FID_LCD_DISP_IMAGE
	, FID_RFID_GET_STATE = 0x50
	, FID_SM_ENABLE = 0x51
	, FID_SM_DISABLE = 0x52
	, FID_RFID_USAGE_CLEAR = 0x90
	, FID_RFID_GET_KEY = 0x95
	, FID_RFID_SET_KEY = 0x96
	, FID_RFID_SET_MANUF = 0x97
	, FID_RFID_SET_USAGE = 0x98
	, FID_RFID_SET_UID = 0x99
	, FID_RFID_SET_STEP = 0xA0
	, FID_RFID_GET_STEP = 0xA1
	, FID_RFID_TAGGING = 0xA2
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

enum class eStepMotorIndex : UINT
{
	Both = 0,
	Pullback = 1,
	Polarization = 1,
	Hub = 2,
	DelayLine = 2,
	Max
};

class ICommonProtocol {
protected:
	BYTE m_stx, m_etx;
	std::vector<BYTE> m_vPacket;

public:
	ICommonProtocol(BYTE stx, BYTE etx) { m_stx = stx; m_etx = etx; }
	virtual ~ICommonProtocol() {}

protected:
	virtual void handlePacket() = 0;
	void addPacket(BYTE* packet, int size);
	bool sliceUntilSTX(int index);
	bool parseSerialPacket();
	void getSerialPacket(eFID fid, int dataSize, BYTE* packet, int& packetLength);
	BYTE calcChecksum(BYTE* packet, int length);
};

class IStepMotorAction
{
public:
	virtual bool Current(eStepMotorIndex idx, int posMM) = 0;
	virtual bool Move(eStepMotorIndex idx, int posMM, bool delay, char sensor) = 0;
	virtual bool Set(eStepMotorIndex idx, int velocity) = 0;
};