#include "RFIDProtocol.h"

void RFIDProtocol::setPacketByFID(eFID fid, BYTE* packet, int& packetLength, int uidSize, BYTE* UID, int dataSize, BYTE* data) {
	if (packet == nullptr || dataSize < 0) return;

	int keyLen = 6;
	int keyTypeLen = 1;
	int uidLenLen = 1;
	
	packetLength = dataSize + FIXED_HEADER_FRONT_LEN + FIXED_HEADER_BACK_LEN + keyLen + keyTypeLen + (uidSize == 0 ? 0 : uidSize + uidLenLen);

	packet[0] = RJ_STX;
	packet[LENGTH_IDX] = (BYTE)packetLength;
	packet[FID_IDX] = (BYTE)fid;
	packet[packetLength - 1] = RJ_ETX;
	BYTE* key = new BYTE[keyLen]{ 0X00, 0X00, 0X00, 0X00, 0X00, 0X00 };
	BYTE* keyType = new BYTE[keyTypeLen]{ 0 };
	BYTE* uidLen = new BYTE[uidLenLen]{ HARDWARE_UID_LENGTH };
	int idx = FIXED_HEADER_FRONT_LEN;
	switch (fid)
	{
	case eFID::FID_RFID_GET_STATE:
	case eFID::FID_RFID_GET_KEY:
	case eFID::FID_RFID_GET_STEP:
		idx += AddDataToPacket(packet + idx, keyType, keyTypeLen);
		idx += AddDataToPacket(packet+ idx, key, keyLen);
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
		idx += AddDataToPacket(packet+idx, key, keyLen);
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
