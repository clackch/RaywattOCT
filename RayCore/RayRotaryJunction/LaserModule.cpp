#include "LaserModule.h"
#include "COMConnection.h"
#include "Utility.h"
#include <chrono>

CLaserModule::CLaserModule()
	:ICommonProtocol(CM_STX, CM_ETX)
{
	m_nStepPosition[0] = 0;
	m_nStepPosition[1] = 0;
	m_nStepSpeed[0] = 0;
	m_nStepSpeed[1] = 0;
	m_nVOA = m_nVLD = 0;

	m_nActualPosition[0] = 0;
	m_nActualPosition[1] = 0;
	m_isSMMoving[0] = false;
	m_isSMMoving[1] = false;
	for (int i = 0; i < 6; i++) {
		m_bPhotoSensor[i] = false;
	}
}

CLaserModule::~CLaserModule()
{
	Disconnect();
}
bool CLaserModule::Connect(void* param) {
	if (m_initMotor) return m_initMotor;

	m_pConnection = new CCOMConnection();
	m_initMotor = m_pConnection->Connect(param);
	if (m_initMotor) {
		AutoStatePeriod(10);
		initSetting();
		BOOL result = CUtility::StartThread(threadReadPacket, m_pThread, (LPVOID)this);

		if (result == FALSE) {
			Disconnect();
			m_initMotor = false;
		}
	}

	return m_initMotor;
}
void CLaserModule::Disconnect() {
	CMotorController::Disconnect();
}
bool CLaserModule::IsMoving(eStepMotorIndex idxMotor) {
	bool isMoving = (idxMotor == eStepMotorIndex::Both) ? (m_isSMMoving[0] || m_isSMMoving[1]) : m_isSMMoving[(int)idxMotor - 1];
	if (isMoving) {
		ReadPosition();
	}
	return isMoving;
}
bool CLaserModule::ReadPosition() {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];

	int packetLength;
	getSerialPacket(eFID::FID_SM_GET_STATE, 0, serialPacket, packetLength);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CLaserModule::Current(eStepMotorIndex idxMotor, int posStep) {
	if (!m_initMotor) return false;

	PLOGI.printf("StepMotor #%d Current: %d", idxMotor, posStep);

	if (idxMotor == eStepMotorIndex::Both) {
		m_nStepPosition[0] = posStep;
		m_nStepPosition[1] = posStep;
	}
	else {
		m_nStepPosition[(int)idxMotor - 1] = posStep;
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_SET_POS, sizeof(int) * 2, serialPacket, packetLength);

	int idxData = DATA_IDX;
	memcpy(serialPacket + idxData, &m_nStepPosition[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepPosition[1], sizeof(int));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CLaserModule::Move(eStepMotorIndex idxMotor, int posStep, bool delay, char sensor) {
	if (!m_initMotor) return false;

	PLOGI.printf("StepMotor #%d Move: %d (Speed - #1: %d, #2: %d step/s", idxMotor, posStep, m_nStepSpeed[0], m_nStepSpeed[1]);

	char sensorStop[2] = { 0x00, 0x00 };
	if (idxMotor == eStepMotorIndex::Both) {
		m_nStepPosition[0] = posStep;
		m_nStepPosition[1] = posStep;
		m_isSMMoving[0] = true;
		m_isSMMoving[1] = true;
		sensorStop[0] = sensor;
		sensorStop[1] = sensor;
	}
	else {
		m_nStepPosition[(int)idxMotor - 1] = posStep;
		m_isSMMoving[(int)idxMotor - 1] = true;
		sensorStop[(int)idxMotor - 1] = sensor; // sensor == 2 : DelayLine upperside, sensor == 3 : DelayLine downside
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_RUN, sizeof(int) * 4 + 2, serialPacket, packetLength);

	int idxData = DATA_IDX;
	memcpy(serialPacket + idxData, &m_nStepPosition[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepPosition[1], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepSpeed[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepSpeed[1], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &sensorStop[0], sizeof(char));
	idxData += sizeof(char);
	memcpy(serialPacket + idxData, &sensorStop[1], sizeof(char));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CLaserModule::Set(eStepMotorIndex idxMotor, int velStep) {
	if (!m_initMotor) return false;

	PLOGI.printf("velocity: %d", velStep);
	if (idxMotor == eStepMotorIndex::Both) {
		m_nStepSpeed[0] = velStep;
		m_nStepSpeed[1] = velStep;
	}
	else {
		m_nStepSpeed[(int)idxMotor - 1] = velStep;
	}

	return true;
}
int CLaserModule::MoveRelative(eStepMotorIndex idxMotor, int nOffset) {
	if (idxMotor != eStepMotorIndex::DelayLine && idxMotor != eStepMotorIndex::Polarization) return 0;

	int index = (int)idxMotor - 1;
	int actualPosition = m_nActualPosition[index];

	int nLastTargetPos = m_nStepPosition[index];
	int nPosition = actualPosition + nOffset;

	Move(idxMotor, nPosition);

	return nPosition;
}
void CLaserModule::SetVOA(unsigned short voa) {
	m_nVOA = voa;
	setVOAVLD();
}
void CLaserModule::SetVLD(unsigned short vld) {
	m_nVLD = vld;
	setVOAVLD();
}
bool CLaserModule::AutoStatePeriod(USHORT interval) {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SET_AUTO_PERIOD, sizeof(unsigned short) * 2 + 1, serialPacket, packetLength);

	const char chkAutoByChanged = false;
	const unsigned short autoHoldOff = 10;

	int idxData = DATA_IDX;
	memcpy(serialPacket + idxData, &interval, sizeof(unsigned short));
	idxData += sizeof(unsigned short);
	memcpy(serialPacket + idxData, &chkAutoByChanged, sizeof(char));
	idxData += sizeof(char);
	memcpy(serialPacket + idxData, &autoHoldOff, sizeof(unsigned short));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CLaserModule::StopStepMotors() {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_STOP, sizeof(BYTE) * 2, serialPacket, packetLength);

	BYTE stopIdx[2] = { 0x02, 0x02 };	// 0x02: Stop Immediately
	memcpy(serialPacket + DATA_IDX, stopIdx, sizeof(BYTE) * 2);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
UINT CLaserModule::threadReadPacket(LPVOID param) {
	CLaserModule* pRJController = (CLaserModule*)param;
	BYTE recvBuf[MAX_PATH];
	int offset = 0;

	while (pRJController->m_pThread->isRun) {
		int readSize = pRJController->m_pConnection->Read(recvBuf + offset);
		if (readSize > 0) {
			pRJController->addPacket(recvBuf, readSize);
			pRJController->parseSerialPacket();
		}

		Sleep(1);
	}

	return NOERROR;
}

void CLaserModule::initSetting() {
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_SET_CONFIG, (sizeof(int) * 7 + sizeof(char) * 2) * 2, serialPacket, packetLength);

	const int minSpeed = 100;
	const int maxSpeed = 10000;
	const int accTime = 1;
	const int accStep = 100;
	const int decTime = 1;
	const int decStep = 1000;
	const int minStep = 10;
	const char accType = 1;	// profile
	const char decType = 0; // linear

	int offset = 0;
	for (int i = 0; i < 2; i++) {
		memcpy(serialPacket + DATA_IDX + offset, &minSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &maxSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &minStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accType, sizeof(char)); offset += sizeof(char);
		memcpy(serialPacket + DATA_IDX + offset, &decType, sizeof(char)); offset += sizeof(char);
	}

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);
	if (written != packetLength)
	{
		PLOGI.printf("Written size is not matched. (%d / %d bytes)", written, packetLength);
	}
}
void CLaserModule::parseAutoReportPacket(BYTE* packet, int size) {
	int offset = 0;
	for (int i = 0; i < 2; i++) {
		m_isSMMoving[i] = packet[offset]; offset++;

		int curPos = 0;
		for (int j = 0; j < 4; j++) {
			curPos |= (packet[offset + j] << (j * 8));
		}
		m_nActualPosition[i] = curPos;
		offset += 4;
	}
	memcpy(&m_nVOA, packet + offset, sizeof(unsigned short));
	offset += sizeof(unsigned short) * 2;	// voa output (2byte), voa input (2byte)
	memcpy(&m_nVLD, packet + offset, sizeof(unsigned short));
	offset += sizeof(unsigned short) * 2;	// vld output (2byte), vld input (2byte)
}
void CLaserModule::parseSMPacket(BYTE* packet, int size) {
	int offset = 0;
	for (int i = 0; i < 2; i++) {
		m_isSMMoving[i] = packet[offset]; offset++;

		int curPos = 0;
		for (int j = 0; j < 4; j++) {
			curPos |= (packet[offset + j] << (j * 8));
		}
		PLOGI.printf("StepMotor #%d (%s): %d", i, ((m_isSMMoving[i]) ? "Moving" : "Stop"), curPos);
		m_nActualPosition[i] = curPos;
		offset += 13;	// current pos (4byte), target pos (4byte), current speed (4byte), stop condition (1byte, photo-sensor)
	}
}
void CLaserModule::setVOAVLD()
{
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SET_VOAVLD, sizeof(unsigned short) * 2, serialPacket, packetLength);

	memcpy(serialPacket + DATA_IDX, &m_nVOA, sizeof(unsigned short));
	memcpy(serialPacket + DATA_IDX + 2, &m_nVLD, sizeof(unsigned short));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	PLOGI.printf("VOA: %d, VLD: %d", m_nVOA, m_nVLD);
	if (written != packetLength)
	{
		PLOGI.printf("Written size is not matched. (%d / %d bytes)", written, packetLength);
	}
}
void CLaserModule::handlePacket() {
	BYTE length = m_vPacket[LENGTH_IDX];
	int dataLength = length - HEADER_LEN;
	eFID fid = (eFID)m_vPacket[FID_IDX];

	char strTime[MAX_PATH];
	CUtility::GetCurTime(strTime);

	// photo sensor state
	for (int i = 0; i < 6; i++) {
		m_bPhotoSensor[i] = m_vPacket[PHOTO_IDX] & (0x1 << i);
	}

	switch (fid) {
	case eFID::FID_AUTO_REPORT:
		parseAutoReportPacket(&m_vPacket[DATA_IDX], dataLength);
		break;
	case eFID::FID_SM_GET_STATE:
		parseSMPacket(&m_vPacket[DATA_IDX], dataLength);
		break;
	default:
		break;
	}
}