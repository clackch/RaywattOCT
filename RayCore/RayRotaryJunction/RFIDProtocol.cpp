#include "RFIDProtocol.h"

void RFIDProtocol::setPacketByFID(eFID fid, BYTE* packet, int& packetLength, int uidSize, BYTE* UID, int dataSize, BYTE* data) {
	if (packet == nullptr || dataSize < 0) return;

	int keyLen = 6;
	int keyTypeLen = 1;
	int uidLen = 8;
	packetLength = dataSize + FIXED_HEADER_FRONT_LEN + FIXED_HEADER_BACK_LEN + uidSize + keyLen+ keyTypeLen;

	packet[0] = RJ_STX;
	packet[LENGTH_IDX] = (BYTE)packetLength;
	packet[FID_IDX] = (BYTE)fid;
	packet[packetLength - 1] = RJ_ETX;
	BYTE* key = new BYTE[keyLen]{ 0X00,0X00, 0X00, 0X00, 0X00, 0X00 };
	BYTE* keyType = new BYTE[keyTypeLen]{ 0 };
	BYTE* uid = new BYTE[uidLen]{ 0,0,0,0,0,0,0,0 };
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
		idx += AddDataToPacket(packet + idx, UID, uidLen);
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



int RFIDProtocol::AddDataToPacket(BYTE* packet, BYTE* data, int len) {
	for (int i = 0; i < len; i++) {
		*(packet + i) = *(data + i);
		printf("%02x ", *(data + i));
	}
	printf("\n");
	return len;
}
