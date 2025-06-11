#include "RFIDProtocol.h"
#include "RFIDKeyController.h"

RFIDProtocol::SRFIDState RFIDProtocol::aRFIDState;

void RFIDProtocol::setPacketByFID(eFID fid, BYTE* packet, int& packetLength, int uidSize, BYTE* UID, int dataSize, BYTE* data) {
	if (packet == nullptr || dataSize < 0) return;

	int keyTypeLen = 1;
	int uidLenLen = 1;
	
	packetLength = dataSize + FIXED_HEADER_FRONT_LEN + FIXED_HEADER_BACK_LEN + KEY_LEN + keyTypeLen + (uidSize == 0 ? 0 : uidSize + uidLenLen);

	packet[0] = RJ_STX;
	packet[LENGTH_IDX] = (BYTE)packetLength;
	packet[FID_IDX] = (BYTE)fid;
	packet[packetLength - 1] = RJ_ETX;
	BYTE* key = aRFIDState.aKeyA;
	BYTE* keyType = new BYTE[keyTypeLen]{ 0 };
	BYTE* uidLen = new BYTE[uidLenLen]{ HARDWARE_UID_LENGTH };
	int idx = FIXED_HEADER_FRONT_LEN;
	switch (fid)
	{
	case eFID::FID_RFID_GET_STATE:
	case eFID::FID_RFID_GET_KEY:
	case eFID::FID_RFID_GET_STEP:
		idx += AddDataToPacket(packet + idx, keyType, keyTypeLen);
		idx += AddDataToPacket(packet+ idx, key, KEY_LEN);
		if (data != NULL&&dataSize>0) {
			AddDataToPacket(packet + idx, data, dataSize);
		}
		break;
	case eFID::FID_RFID_USAGE_INCREMENT:
	case eFID::FID_RFID_USAGE_CLEAR:
	case eFID::FID_RFID_SET_KEY:
	case eFID::FID_RFID_SET_MANUF:
	case eFID::FID_RFID_SET_USAGE:
	case eFID::FID_RFID_SET_UID:
	case eFID::FID_RFID_SET_STEP:
		idx += AddDataToPacket(packet + idx, uidLen, uidLenLen);
		idx += AddDataToPacket(packet + idx, UID, uidSize);
		idx += AddDataToPacket(packet + idx, keyType, keyTypeLen);
		idx += AddDataToPacket(packet+idx, key, KEY_LEN);
		if (data != NULL && dataSize > 0) {
			AddDataToPacket(packet + idx, data, dataSize);
		}
		break;
	default:
		break;
	}
}


void RFIDProtocol::writeKeyChangeLog(unsigned char* changedKey)
{
	std::ofstream logFile("./keyLog.txt", std::ios::app);
	if (!logFile.is_open()) {
		return;
	}
	auto now = std::chrono::system_clock::now();
	auto in_time_t = std::chrono::system_clock::to_time_t(now);
	auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;

	std::tm local_tm;
#ifdef _WIN32
	localtime_s(&local_tm, &in_time_t);
#else
	localtime_r(&in_time_t, &local_tm);
#endif
	logFile << "Changed Key("
		<< std::setfill('0')
		<< std::setw(4) << local_tm.tm_year + 1900 << "/"
		<< std::setw(2) << local_tm.tm_mon + 1 << "/"
		<< std::setw(2) << local_tm.tm_mday << " || "
		<< std::setw(2) << local_tm.tm_hour << ":"
		<< std::setw(2) << local_tm.tm_min << ":"
		<< std::setw(2) << local_tm.tm_sec << "."
		<< std::setw(4) << ms.count() << ")\t : ";
	for (int idx = 0; idx < KEY_LEN*2; idx++) {
		if (idx == KEY_LEN) logFile << " ";
		logFile << std::uppercase << std::setw(2) << std::setfill('0') << std::hex << (int)changedKey[idx];
	}
	logFile << "\n";
}

int RFIDProtocol::AddDataToPacket(BYTE* packet, BYTE* data, int len) {
	for (int i = 0; i < len; i++) {
		*(packet + i) = *(data + i);
		printf("%02x ", *(data + i));
	}
	printf("\n");
	return len;
}


void RFIDProtocol::setHardwareUID(BYTE* packet, int packetLength) {
	if (packetLength != HARDWARE_UID_LENGTH) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aHardwareUID[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setCustomUID(BYTE* packet, int packetLength) {
	if (packetLength != CUSTOM_UID_LENGTH) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aCustomUID[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setManuf(BYTE* packet, int packetLength) {
	if (packetLength != MANUF_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aMANU[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setCount(BYTE* packet, int packetLength) {
	if (packetLength != COUNT_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aCNT = packet[cycle];
	}
}
void RFIDProtocol::setKeyA(BYTE* packet, int packetLength) {
	if (packetLength != KEY_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aKeyA[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setKeyB(BYTE* packet, int packetLength) {
	if (packetLength != KEY_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aKeyB[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setStep(BYTE* packet, int packetLength) {
	if (packetLength != STEP_LEN) return;
	aRFIDState.aStep = 0;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aStep *= 0x100;
		aRFIDState.aStep += packet[cycle];
	}
}

void RFIDProtocol::printState() {
	std::stringstream strStream;

	for (int idx = 0; idx < HARDWARE_UID_LENGTH; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(aRFIDState.aHardwareUID[idx]);
	}
	std::cout << "Hardware UID :" << strStream.str() << std::endl;
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < CUSTOM_UID_LENGTH; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(aRFIDState.aCustomUID[idx]);
	}
	std::cout << "Custom UID :" << strStream.str() << std::endl;
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < MANUF_LEN; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(aRFIDState.aMANU[idx]);
	}
	std::cout << "Manufacturer :" << strStream.str() << std::endl;
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < KEY_LEN; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(aRFIDState.aKeyA[idx]);
	}
	std::cout << "keyA :" << strStream.str() << std::endl; 
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < KEY_LEN; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(aRFIDState.aKeyB[idx]);
	}
	std::cout << "keyB :" << strStream.str() << std::endl;
	strStream.clear();
	strStream.str("");

	std::cout << "count : " << aRFIDState.aCNT << std::endl;
	std::cout << "step : " << aRFIDState.aStep << std::endl;
}

void RFIDProtocol::initState() {
	memset(aRFIDState.aHardwareUID, 0x00, HARDWARE_UID_LENGTH);
	memset(aRFIDState.aCustomUID, 0x00, CUSTOM_UID_LENGTH);
	memset(aRFIDState.aMANU, 0x00, MANUF_LEN);
	memset(aRFIDState.aKeyA, 0x00, KEY_LEN);
	memset(aRFIDState.aKeyB, 0x00, KEY_LEN);
	aRFIDState.aCNT = 0;
	aRFIDState.aStep = 0;
	RFIDKeyController::loadFirstKey(aRFIDState.aKeyA);
}