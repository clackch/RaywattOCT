#pragma once

#include "CommonProtocol.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <vector>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <map>

#define FIXED_HEADER_FRONT_LEN			3
#define FIXED_HEADER_BACK_LEN			2	
#define HARDWARE_UID_LENGTH				4
#define CUSTOM_UID_LENGTH				4
#define KEY_LEN							6
#define COUNT_LEN						1
#define STEP_LEN						3
#define MANUF_LEN						7
#define REPLY_RESULT_IDX				3
#define MAX_STEP_VALUE					0xFFFFFF

class RFIDMessageData {
private:
public:
	struct Data {
		uint8_t aHardwareUIDLen;
		uint8_t aUID[HARDWARE_UID_LENGTH+CUSTOM_UID_LENGTH];
		uint8_t aKeyType;
		uint8_t aKey[KEY_LEN];
		uint8_t etcData[MAX_PATH];
		uint8_t etcLen;
	};
	std::map <eFID, Data> messageMap;
	eFID lastFID;
};

class RFIDProtocol
{
	struct SRFIDState {
		uint8_t aHardwareUID[HARDWARE_UID_LENGTH];
		uint8_t aCustomUID[CUSTOM_UID_LENGTH];
		uint8_t aMANU[MANUF_LEN];
		uint8_t aKeyA[KEY_LEN];
		uint8_t aKeyB[KEY_LEN];
		int aCNT;
		int aStep;
	};
private:
	static int AddDataToPacket(BYTE* packet, BYTE* data, int len);
	static SRFIDState aRFIDState;
	static RFIDMessageData aRFIDMessageData;
public:
	RFIDProtocol() {}
	~RFIDProtocol() {}

	static void resetPacketByFID(eFID fid, BYTE* packet, int& packetLength, RFIDMessageData::Data data);
	static void setPacketByFID(eFID fid, BYTE* packet, int& packetLength, int uidSize = 0, BYTE* UID = NULL, int dataSize = 0, BYTE* data = NULL, BYTE* key = NULL);
	static void writeKeyChangeLog(unsigned char* changedKey);
	static void setHardwareUID(BYTE* packet, int packetLength);
	static void setCustomUID(BYTE* packet, int packetLength);
	static void setManuf(BYTE* packet, int packetLength);
	static void setCount(BYTE* packet, int packetLength);
	static void setKeyA(BYTE* packet, int packetLength);
	static void setKeyB(BYTE* packet, int packetLength);
	static void setStep(BYTE* packet, int packetLength);
	static bool cmpUID(BYTE* UID, int uidLength);

	static void initState(bool needLoadKey);
	static void printState();
	static RFIDMessageData::Data* getMessageData(eFID fid);
	static void deleteMessageData(eFID fid);
	static void setLastFID(eFID fid);
	static eFID getLastFID();
	static RFIDMessageData::Data* getRecentMessageData();
};
