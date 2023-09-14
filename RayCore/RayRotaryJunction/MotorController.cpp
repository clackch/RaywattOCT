#include "MotorController.h"
#include "USBConnection.h"
#include "COMConnection.h"
#include "Utility.h"

CMotorController* CMotorController::pInstance = nullptr;

CMotorController::CMotorController() {
	m_pConnection = new CCOMConnection();
	m_initMotor = false;
	m_isRun = false;

	m_pThread = nullptr;
}

CMotorController* CMotorController::GetInstance() {
	if (pInstance == nullptr) {
		pInstance = new CMotorController();
	}
	return pInstance;
}

CMotorController::~CMotorController() {	
	Disconnect();
	
	if (m_pConnection != nullptr) delete m_pConnection;
}

bool CMotorController::Connect(void* param) {
	if (m_initMotor) return m_initMotor;

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

	m_pConnection->Disconnect();
	m_initMotor = false;
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
	char strCommand[MAX_PATH];	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	if (nVelocity < 0) {
		// negative
		nVelocity = (nVelocity < -50000) ? -50000 : (nVelocity > -100) ? -100 : nVelocity;
	}
	else {
		// positive
		nVelocity = (nVelocity > 50000) ? 50000 : (nVelocity < 100) ? 100 : nVelocity;
	}

	getMotorPacket(MOTOR_INDEX_TARGETVELOCITY, nVelocity, 4, packet, packetLength);
	result = writeMotor(packet, packetLength);

	m_isRun = result;
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

UINT CMotorController::threadReadMotor(LPVOID pParam) {
	CMotorController* pMotorController = (CMotorController*)pParam;
	BYTE recvBuf[MAX_PATH];

	while (pMotorController->m_pThread->isRun) {
		int readSize = pMotorController->m_pConnection->Read(recvBuf);
		if (readSize > 0) {
			char strBuffer[MAX_PATH];
			int nLength = 0;
			for (int i = 0; i < readSize; i++) {
				sprintf(strBuffer + nLength, "0x0%02x ", recvBuf[i]);
				nLength = strlen(strBuffer);
			}
			strBuffer[nLength] = '\0';
			PLOGI.printf(" [Motor] read packet : %s", strBuffer);
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

	return (written == size);
}

void CMotorController::getMotorPacket(unsigned short command, unsigned int data, unsigned int dataSize, BYTE* packet, int& packetLength) {
	const BYTE sof = (BYTE)0x53;
	BYTE length = 0x07;	// length (1byte) + node (1byte) + mode (1byte) + index (2byte) + subindex (1byte) + crc (1byte)
	BYTE node = 0x01;	// 0x00 ~ 0xff
	BYTE mode = 0x02;	// 0x02 : write, 0x01 : read
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