#include "MotorController.h"
#include "USBConnection.h"
#include "COMConnection.h"
#include "Utility.h"

CMotorController::CMotorController() {
	m_initMotor = false;
	m_isRun = false;

	m_pConnection = nullptr;
	m_pThread = nullptr;
}

CMotorController::~CMotorController() {	
	Disconnect();
	
	if (m_pConnection != nullptr) delete m_pConnection;
}

bool CMotorController::Connect(void* param) {
	if (m_initMotor) return m_initMotor;

	m_pConnection = new CUSBConnection();
	m_initMotor = m_pConnection->Connect(param);
	if (m_initMotor) {
		BOOL result = FALSE;
		result = CUtility::StartThread(threadReadMotor, m_pThread, (LPVOID)this);

		if (result == FALSE) {
			Disconnect();
			m_initMotor = false;
		}
	}

	return m_initMotor;
}

void CMotorController::Disconnect() {

	CUtility::StopThread(m_pThread);

	if (m_pConnection != nullptr) {
		m_pConnection->Disconnect();
		delete m_pConnection;
		m_pConnection = nullptr;
	}
	m_initMotor = false;
}

bool CMotorController::SetModeOfOperation(char mode) {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_MODESOFOPERATION, mode, 1, packet, packetLength);
	result = writeMotor(packet, packetLength);

	return result;
}
bool CMotorController::SwitchOn() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_STATUSWORD, 0, 0, packet, packetLength);
	result = writeMotor(packet, packetLength);

	getMotorPacket(MOTOR_INDEX_CONTROLWORD, MOTOR_DATA_SWITCH_ON, 2, packet, packetLength);
	result = writeMotor(packet, packetLength);

	getMotorPacket(MOTOR_INDEX_CONTROLWORD, MOTOR_DATA_ENABLE_OPERATION, 2, packet, packetLength);
	result |= writeMotor(packet, packetLength);

	if (result) {
		StopMotor();
	}

	return result;
}

/*
* @param nVelocity : input velocity value (limits apply if needed)
*/
bool CMotorController::PerformRun(int &nVelocity) {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	nVelocity = (nVelocity > 50000) ? 50000 : (nVelocity < 0) ? 0 : nVelocity;

	getMotorPacket(MOTOR_INDEX_TARGETVELOCITY, nVelocity, 4, packet, packetLength);
	result = writeMotor(packet, packetLength);

	m_isRun = (result && nVelocity != 0);
	return result;
}
bool CMotorController::StopMotor() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_TARGETVELOCITY, 0, 4, packet, packetLength);
	result = writeMotor(packet, packetLength);

	m_isRun = false;
	return result;
}
bool CMotorController::SwitchOff() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_CONTROLWORD, MOTOR_DATA_SWITCH_OFF, 2, packet, packetLength);
	result = writeMotor(packet, packetLength);

	return result;
}
bool CMotorController::ReadActualVelocity() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_ACTUALVELOCITY, 0, 0, packet, packetLength);
	result = writeMotor(packet, packetLength);

	return result;
}

UINT CMotorController::threadReadMotor(LPVOID pParam) {
	CMotorController* pMotorController = (CMotorController*)pParam;
	const char sof = 0x53;
	BYTE recvBuf[MAX_PATH];
	int offset = 0;

	while (pMotorController->m_pThread->isRun) {
		int readSize = pMotorController->m_pConnection->Read(recvBuf + offset);
		if (readSize > 0) {
			int totalSize = offset + readSize;
			if (pMotorController->parsePacket(recvBuf, totalSize)) {
				char strBuffer[MAX_PATH];
				int nLength = 0;
				for (int i = 0; i < totalSize; i++) {
					sprintf(strBuffer + nLength, "0x0%02x ", recvBuf[i]);
					nLength = strlen(strBuffer);
				}
				strBuffer[nLength] = '\0';
				printf(" [Motor] read packet : %s\n", strBuffer);

				offset = 0;
			}
			else {
				offset += readSize;
				if (recvBuf[0] != sof) {
					printf("buffer rearrange!!\n");
					for (int i = 0; i < totalSize; i++) {
						if (recvBuf[i] == sof) {
							memcpy(recvBuf, recvBuf + i, totalSize - i);
							offset = totalSize - i;
						}
					}
				}
			}
		}

		Sleep(100);
	}

	return NOERROR;
}

BYTE CMotorController::calcCRCByte(BYTE u8Byte, BYTE u8CRC){
	const BYTE polynomial = 0xD5;
	u8CRC = u8CRC ^ u8Byte;
	for (BYTE i = 0; i < 8; i++)
	{
		if (u8CRC & 0x01) {
			u8CRC = (u8CRC >> 1) ^ polynomial;
		}
		else {
			u8CRC >>= 1;
		}
	}
	return u8CRC;
}

bool CMotorController::writeMotor(BYTE* packet, int size) {
	if (!m_initMotor) return false;

	int written = m_pConnection->Write(packet, size);

	Sleep(50);

	return (written == size);
}

void CMotorController::getMotorPacket(unsigned short command, unsigned int data, unsigned int dataSize, BYTE* packet, int& packetLength) {
	const BYTE sof = (BYTE)0x53;
	BYTE length = 0x07;	// length (1byte) + node (1byte) + mode (1byte) + index (2byte) + subindex (1byte) + crc (1byte)
	BYTE node = 0x01;	// 0x00 ~ 0xff
	BYTE mode = (command == MOTOR_INDEX_ACTUALVELOCITY) ? 0x01 : 0x02;	// 0x02 : write, 0x01 : read
	unsigned short index = command;
	const BYTE subindex = 0x00;
	const BYTE eof = (BYTE)0x45;

	length += dataSize;
	packetLength = 0;

	packet[packetLength++] = sof;
	packet[packetLength++] = length;
	packet[packetLength++] = node;
	packet[packetLength++] = mode;
	packet[packetLength++] = index & 0xFF;
	packet[packetLength++] = (index >> 8) & 0xFF;
	packet[packetLength++] = subindex;
	if (dataSize == 2) {
		*(unsigned short*)(packet + packetLength) = (unsigned short)data;
	}
	else {
		*(unsigned int*)(packet + packetLength) = (unsigned int)data;
	}
	packetLength += dataSize;

	BYTE crc = 0xFF;	//start value
	for (int i = 1; i < length; i++) {
		crc = calcCRCByte(packet[i], crc);
	}
	packet[packetLength++] = crc;
	packet[packetLength++] = eof;
}

bool CMotorController::parsePacket(BYTE* packet, int size) {
	const char sof = 0x53;
	const char eof = 0x45;

	int offset = 0;
	if (packet[offset++] == sof) {
		BYTE length = packet[offset];
		if (size <= offset + length) return false;
		if (packet[offset + length] == eof){
			offset++;
			BYTE node = packet[offset++];
			BYTE mode = packet[offset++];
			unsigned short command = packet[offset++];
			command |= (packet[offset++] << 8);

			BYTE subIndex = packet[offset++];
			if (command == MOTOR_INDEX_ACTUALVELOCITY)
			{
				m_nActualVelocity = 0;
				for (int i = 0; i < 4; i++) {
					m_nActualVelocity |= (packet[offset++] << (i * 8));
				}
				printf("Velocity: %d (0x%x)\n", m_nActualVelocity, m_nActualVelocity);
			}
		
			return true;
		}
	}

	return false;
}