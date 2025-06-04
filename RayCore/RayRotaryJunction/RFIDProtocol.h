#pragma once

#include "CommonProtocol.h">
#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <vector>
#include <chrono>
#include <ctime>
#include <iomanip>

#define FIXED_HEADER_FRONT_LEN			3
#define FIXED_HEADER_BACK_LEN			2	
#define HARDWARE_UID_LENGTH				4
#define CUSTOM_UID_LENGTH				4
#define KEY_LEN							6
#define COUNT_LEN						1
#define STEP_LEN						3
#define MANUF_LEN						7
#define MAX_STEP_VALUE					0xFFFFFF

class RFIDProtocol
{
private:
	int static AddDataToPacket(BYTE* packet, BYTE* data, int len);
public:
	RFIDProtocol() {}
	~RFIDProtocol() {}
	
	void static setPacketByFID(eFID fid, BYTE* packet, int& packetLength, int uidSize = 0, BYTE* UID = NULL, int dataSize = 0, BYTE* data = NULL);
	void static writeKeyChangeLog(unsigned char* changedKey);
};