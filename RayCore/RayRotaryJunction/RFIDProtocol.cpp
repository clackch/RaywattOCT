#include "RFIDProtocol.h"
#include "RFIDKeyController.h"

RFIDProtocol::SRFIDState RFIDProtocol::aRFIDState;
RFIDMessageData RFIDProtocol::aRFIDMessageData; 
std::mutex RFIDProtocol::mtx;


void RFIDProtocol::resetPacketByFID(eFID fid, BYTE* packet, int& packetLength, RFIDMessageData::Data data) {
	if (packet == nullptr) return;
	int keyTypeLen = 1;
	int uidLenLen = 1;
	packetLength = data.etcLen + FIXED_HEADER_FRONT_LEN + FIXED_HEADER_BACK_LEN + KEY_LEN + keyTypeLen + ((data.aHardwareUIDLen == 0) ? 0 : (CUSTOM_UID_LENGTH + HARDWARE_UID_LENGTH + uidLenLen));
	packet[0] = RJ_STX;
	packet[LENGTH_IDX] = (BYTE)packetLength;
	packet[FID_IDX] = (BYTE)fid;
	packet[packetLength - 1] = RJ_ETX;
	int idx = FIXED_HEADER_FRONT_LEN;

	switch (fid)
	{
	case eFID::FID_RFID_GET_STATE:
	case eFID::FID_RFID_GET_KEY:
	case eFID::FID_RFID_GET_STEP:
		idx += AddDataToPacket(packet + idx, &(data.aKeyType), keyTypeLen);
		idx += AddDataToPacket(packet + idx, aRFIDState.aKeyA, KEY_LEN);

		if (data.etcData != NULL && data.etcLen > 0) {
			AddDataToPacket(packet + idx, data.etcData, data.etcLen);
		}
		break;
	case eFID::FID_RFID_USAGE_CLEAR:
	case eFID::FID_RFID_SET_KEY:
	case eFID::FID_RFID_SET_MANUF:
	case eFID::FID_RFID_SET_USAGE:
	case eFID::FID_RFID_SET_UID:
	case eFID::FID_RFID_SET_STEP:
		idx += AddDataToPacket(packet + idx, &(data.aHardwareUIDLen), uidLenLen);
		idx += AddDataToPacket(packet + idx, data.aUID, data.aHardwareUIDLen+CUSTOM_UID_LENGTH);
		idx += AddDataToPacket(packet + idx, &(data.aKeyType), keyTypeLen);
		idx += AddDataToPacket(packet + idx, aRFIDState.aKeyA, KEY_LEN);
		if (data.etcData != NULL && data.etcLen > 0) {
			AddDataToPacket(packet + idx, data.etcData, data.etcLen);
		}
		break;
	default:
		break;
	}

}

void RFIDProtocol::setPacketByFID(eFID fid, BYTE* packet, int& packetLength, int uidSize, BYTE* UID, int dataSize, BYTE* data, BYTE* inputKey) {
	if (packet == nullptr || dataSize < 0) return;

	int keyTypeLen = 1;
	int uidLenLen = 1;
	
	packetLength = dataSize + FIXED_HEADER_FRONT_LEN + FIXED_HEADER_BACK_LEN + KEY_LEN + keyTypeLen + (uidSize == 0 ? 0 : uidSize + uidLenLen);
	if (fid == eFID::FID_RFID_TAGGING)
		packetLength = FIXED_HEADER_FRONT_LEN + FIXED_HEADER_BACK_LEN;
	packet[0] = RJ_STX;
	packet[LENGTH_IDX] = (BYTE)packetLength;
	packet[FID_IDX] = (BYTE)fid;
	packet[packetLength - 1] = RJ_ETX;
	BYTE* key = ((inputKey!=NULL)?inputKey:aRFIDState.aKeyA);
	BYTE* keyType = new BYTE[keyTypeLen]{ 0 };
	BYTE* uidLen = new BYTE[uidLenLen]{ HARDWARE_UID_LENGTH };
	int idx = FIXED_HEADER_FRONT_LEN;

	RFIDMessageData::Data messageData = { 0 };
	switch (fid)
	{
	case eFID::FID_RFID_GET_STATE:
	case eFID::FID_RFID_GET_KEY:
	case eFID::FID_RFID_GET_STEP:
		idx += AddDataToPacket(packet + idx, keyType, keyTypeLen);
		idx += AddDataToPacket(packet+ idx, key, KEY_LEN);
		memcpy(&(messageData.aKeyType), keyType, keyTypeLen);
		memcpy(&(messageData.aKey), key, KEY_LEN);

		if (data != NULL&&dataSize>0) {
			AddDataToPacket(packet + idx, data, dataSize);
			memcpy(&(messageData.etcData), data, dataSize);
		}
		break;
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
		memcpy(&(messageData.aHardwareUIDLen), uidLen, uidLenLen);
		memcpy(&(messageData.aUID), UID, uidSize);
		memcpy(&(messageData.aKeyType), keyType, keyTypeLen);
		memcpy(&(messageData.aKey), key, KEY_LEN);
		if (data != NULL && dataSize > 0) {
			AddDataToPacket(packet + idx, data, dataSize);
			memcpy(&(messageData.etcData), data, dataSize);
			messageData.etcLen = dataSize;
		}
		break;
	case eFID::FID_RFID_TAGGING:
	default:
		break;
	}
	(aRFIDMessageData.messageMap)[fid] = messageData;
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
	}
	return len;
}

bool RFIDProtocol::cmpUID(BYTE* UID, int hardwardUIDSize) {
	std::lock_guard<std::mutex> lock(mtx);
	if (hardwardUIDSize != HARDWARE_UID_LENGTH) return false;

	for (int i = 0; i < hardwardUIDSize +CUSTOM_UID_LENGTH; i++) {
		if (i < HARDWARE_UID_LENGTH && UID[i] != aRFIDState.aHardwareUID[i]
			|| UID[i] != aRFIDState.aCustomUID[i - HARDWARE_UID_LENGTH]) return false;
	}
	return true;
}

void RFIDProtocol::setHardwareUID(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != HARDWARE_UID_LENGTH) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aHardwareUID[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setCustomUID(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != CUSTOM_UID_LENGTH) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aCustomUID[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setManuf(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != MANUF_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aMANU[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setCount(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != COUNT_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aCNT = packet[cycle];
	}
}
void RFIDProtocol::setKeyA(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != KEY_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aKeyA[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setKeyB(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != KEY_LEN) return;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aKeyB[cycle] = packet[cycle];
	}
}
void RFIDProtocol::setStep(BYTE* packet, int packetLength) {
	std::lock_guard<std::mutex> lock(mtx);
	if (packetLength != STEP_LEN) return;
	aRFIDState.aStep = 0;
	for (int cycle = 0; cycle < packetLength; cycle++) {
		aRFIDState.aStep *= 0x100;
		aRFIDState.aStep += packet[cycle];
	}
}
BYTE RFIDProtocol::getCount(BYTE* uid, int hardwardUIDSize) {
	std::lock_guard<std::mutex> lock(mtx);
	if (!RFIDProtocol::cmpUID_NOLOCK(uid, hardwardUIDSize)) return 255;
	return aRFIDState.aCNT;
}

bool RFIDProtocol::cmpUID_NOLOCK(BYTE* UID, int hardwardUIDSize) {
	if (hardwardUIDSize != HARDWARE_UID_LENGTH) return false;

	for (int i = 0; i < hardwardUIDSize + CUSTOM_UID_LENGTH; i++) {
		if (i < HARDWARE_UID_LENGTH && UID[i] != aRFIDState.aHardwareUID[i]
			|| UID[i] != aRFIDState.aCustomUID[i - HARDWARE_UID_LENGTH]) return false;
	}
	return true;
}

void RFIDProtocol::printState() {
	std::lock_guard<std::mutex> lock(mtx);
	printStateData(aRFIDState);
}

void RFIDProtocol::printStateData(RFIDProtocol::SRFIDState stateData) {
	std::stringstream strStream;

	for (int idx = 0; idx < HARDWARE_UID_LENGTH; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(stateData.aHardwareUID[idx]);
	}
	PLOGI.printf("Hardware UID : %s", strStream.str());
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < CUSTOM_UID_LENGTH; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(stateData.aCustomUID[idx]);
	}
	PLOGI.printf("Custom UID : %s", strStream.str());
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < MANUF_LEN; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(stateData.aMANU[idx]);
	}
	PLOGI.printf("Manufacturer : %s", strStream.str());
	strStream.str("");

	for (int idx = 0; idx < KEY_LEN; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(stateData.aKeyA[idx]);
	}
	PLOGI.printf("keyA : %s", strStream.str());
	strStream.clear();
	strStream.str("");

	for (int idx = 0; idx < KEY_LEN; idx++) {
		strStream << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(stateData.aKeyB[idx]);
	}
	PLOGI.printf("keyB : %s", strStream.str());
	strStream.clear();
	strStream.str("");

	PLOGI.printf("count : %d", stateData.aCNT);
	PLOGI.printf("step : %d\n", stateData.aStep);
}

void RFIDProtocol::initState(bool needLoadKey) {
	std::lock_guard<std::mutex> lock(mtx);
	memset(aRFIDState.aHardwareUID, 0x00, HARDWARE_UID_LENGTH);
	memset(aRFIDState.aCustomUID, 0x00, CUSTOM_UID_LENGTH);
	memset(aRFIDState.aMANU, 0x00, MANUF_LEN);
	memset(aRFIDState.aKeyB, 0x00, KEY_LEN);
	aRFIDState.aCNT = 0;
	aRFIDState.aStep = 0;
	aRFIDState.findingKey = false;

	if(needLoadKey)
		RFIDKeyController::loadFirstKey(aRFIDState.aKeyA);
}

void RFIDProtocol::deleteMessageData(eFID fid) {
	RFIDMessageData::Data* tnsData = getMessageData(fid);

	if (tnsData == nullptr) return;

	aRFIDMessageData.messageMap.erase(fid);
}

RFIDMessageData::Data* RFIDProtocol::getMessageData(eFID fid) {
	auto it = aRFIDMessageData.messageMap.find(fid);
	if (it != aRFIDMessageData.messageMap.end()) {
		return &(it->second);
	}
	return nullptr;
}

bool RFIDProtocol::getFindingKeyStatus() {
	std::lock_guard<std::mutex> lock(mtx);
	return aRFIDState.findingKey;
}

void RFIDProtocol::setFindingKeyStatus(bool status) {
	std::lock_guard<std::mutex> lock(mtx);
	aRFIDState.findingKey = status;
}

RFIDMessageData::Data* RFIDProtocol::getRecentMessageData(eFID fid){
	if (fid == eFID::NO_FID) return nullptr;
	auto it = aRFIDMessageData.messageMap.find(fid);
	if (it != aRFIDMessageData.messageMap.end()) {
		return &(it->second);
	}
	return nullptr;
}
void RFIDProtocol::addFailedFID(eFID fid) {
	aRFIDMessageData.failedFID.push_back(fid);
}
eFID RFIDProtocol::popFailedFID() {
	if (aRFIDMessageData.failedFID.empty()) return eFID::NO_FID;
	eFID result = aRFIDMessageData.failedFID.front();
	aRFIDMessageData.failedFID.pop_front();
	return result;
}
void RFIDProtocol::clearFailedFID() {
	aRFIDMessageData.failedFID.clear();
}

void RFIDProtocol::getCurRFIDData(RFIDProtocol::SRFIDState* txState) {
	std::lock_guard<std::mutex> lock(mtx);
	for (int i = 0; i < CUSTOM_UID_LENGTH; i++) {
		txState->aCustomUID[i] = aRFIDState.aCustomUID[i];
	}
	for (int i = 0; i < HARDWARE_UID_LENGTH; i++) {
		txState->aHardwareUID[i] = aRFIDState.aHardwareUID[i];
	}
	for (int i = 0; i < KEY_LEN; i++) {
		txState->aKeyA[i] = aRFIDState.aKeyA[i];
	}
	for (int i = 0; i < KEY_LEN; i++) {
		txState->aKeyB[i] = aRFIDState.aKeyB[i];
	}
	for (int i = 0; i < MANUF_LEN; i++) {
		txState->aMANU[i] = aRFIDState.aMANU[i];
	}
	txState->aStep = aRFIDState.aStep;
	txState->aCNT = aRFIDState.aCNT;
	txState->findingKey = aRFIDState.findingKey;
	return;
}
