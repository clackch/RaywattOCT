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

	// Initialize firmware version info
	memset(&m_fwVersionInfo, 0, sizeof(SFWVersionInfo));

	// Initialize firmware download members
	m_fwDownloadState = eFWDownloadState::Idle;
	m_fwDownloadProgress = 0;
	m_fwDownloadIndex = 0;
	m_fwDownloadSequence = 0;
	m_fwProgressCallback = nullptr;
	m_fwStatusCallback = nullptr;
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

	int packetLength = 0;
	getSerialPacket(eFID::FID_SM_GET_STATE, 0, serialPacket, packetLength);

	if (packetLength < 2) {
		PLOGI.printf("packetLength too small");
		return false;
	}

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
void CLaserModule::PrintPhotoSensor() {
	PLOGI.printf("PhotoSensor: %d %d %d", m_bPhotoSensor[0], m_bPhotoSensor[1], m_bPhotoSensor[2]);
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
	case eFID::FID_GET_VERSION:
		RxPacketGetVersion(&m_vPacket[0]);
		break;
	case eFID::FID_FW_DOWNLOAD:
		RxPacketFWDownload(&m_vPacket[0]);
		break;
	default:
		break;
	}
}

SFWVersionInfo& CLaserModule::GetFWVersionInfo() {
	if (!m_initMotor) {
		memset(&m_fwVersionInfo, 0, sizeof(SFWVersionInfo));
		return m_fwVersionInfo;
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_GET_VERSION, 0, serialPacket, packetLength);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	Sleep(400);

	PLOGI.printf("FW Version: %s %d.%d.%d",
		m_fwVersionInfo.isBootMode ? "Boot" : "Main",
		m_fwVersionInfo.major,
		m_fwVersionInfo.minor,
		m_fwVersionInfo.patch);

	return m_fwVersionInfo;
}

void CLaserModule::RxPacketGetVersion(BYTE* buff) {

	if (m_vPacket.size() < (HEADER_LEN + 20)) {
		PLOGI.printf("RxPacketGetVersion Packet length error rxlen = %d", static_cast<int>(m_vPacket.size()));
		return;
	}

	int idx = DATA_IDX;
	// Skip Hardware Version (4 bytes)
	idx += 4;

	UINT data = *reinterpret_cast<UINT*>(&buff[idx]);
	m_fwVersionInfo.isBootMode = ((data & 0xFF000000) != 0);
	m_fwVersionInfo.major = static_cast<BYTE>((data >> 16) & 0xff);
	m_fwVersionInfo.minor = static_cast<BYTE>((data >> 8) & 0xff);
	m_fwVersionInfo.patch = static_cast<BYTE>(data & 0xff);

	PLOGI.printf("FW Version: %s %d.%d.%d",
		m_fwVersionInfo.isBootMode ? "Boot" : "Main",
		m_fwVersionInfo.major,
		m_fwVersionInfo.minor,
		m_fwVersionInfo.patch);
}

void CLaserModule::SetFWProgressCallback(FWProgressCallback callback) {
	m_fwProgressCallback = callback;
}

void CLaserModule::SetFWStatusCallback(FWStatusCallback callback) {
	m_fwStatusCallback = callback;
}

bool CLaserModule::ValidateFWFile(const char* filepath, SFirmwareMetadata& metadata) {
	if (filepath == nullptr) {
		PLOGI.printf("Firmware file path is null");
		return false;
	}

	// Open file
	FILE* fp = nullptr;
	fopen_s(&fp, filepath, "rb");
	if (fp == nullptr) {
		PLOGI.printf("Failed to open firmware file: %s", filepath);
		return false;
	}

	// Get file size
	fseek(fp, 0, SEEK_END);
	long fileSize = ftell(fp);
	fseek(fp, 0, SEEK_SET);

	// Check minimum file size
	if (fileSize < FW_MIN_FILE_SIZE) {
		PLOGI.printf("Firmware file too small: %d bytes", fileSize);
		fclose(fp);
		return false;
	}

	// Read metadata from last 16 bytes
	fseek(fp, fileSize - FW_METADATA_SIZE, SEEK_SET);
	size_t readSize = fread(&metadata, 1, FW_METADATA_SIZE, fp);
	fclose(fp);

	if (readSize != FW_METADATA_SIZE) {
		PLOGI.printf("Failed to read firmware metadata");
		return false;
	}

	// Validate checksum: chkver == ((hwver & 0xffff) << 16) + fwver
	UINT expectedChkver = ((metadata.hwver & 0xFFFF) << 16) + metadata.fwver;
	if (metadata.chkver != expectedChkver) {
		PLOGI.printf("Firmware checksum validation failed: expected 0x%08X, got 0x%08X", expectedChkver, metadata.chkver);
		return false;
	}

	/*
	if (metadata.length < FW_FLASH_START || metadata.length >= 0x08080000) {
		PLOGI.printf("Firmware address out of range: 0x%08X", metadata.length);
		return false;
	}

	UINT adjustedLength = metadata.length - 0x08010000;
	if (adjustedLength != (UINT)(fileSize - 4)) {
		PLOGI.printf("Firmware file size mismatch: expected %u, got %ld (fileSize-4=%ld)", adjustedLength, fileSize, fileSize - 4);
		return false;
	}
	*/

	UINT adjustedLength = metadata.length - 0x08000000;

	PLOGI.printf("Firmware validation passed: HW=0x%08X, FW=0x%08X, Size=%ld, Length=0x%08X",
		metadata.hwver, metadata.fwver, fileSize, metadata.length);
	return true;
}

bool CLaserModule::LoadFirmwareData(const char* filepath) {
	FILE* fp = nullptr;
	fopen_s(&fp, filepath, "rb");
	if (fp == nullptr) {
		PLOGI.printf("Failed to open firmware file for reading");
		return false;
	}

	// Get file size
	fseek(fp, 0, SEEK_END);
	long fileSize = ftell(fp);
	fseek(fp, 0, SEEK_SET);

	// Load entire file into buffer
	m_fwImageBuffer.resize(fileSize);
	size_t readSize = fread(m_fwImageBuffer.data(), 1, fileSize, fp);
	fclose(fp);

	if (readSize != (size_t)fileSize) {
		PLOGI.printf("Failed to read firmware file: read %zu of %ld bytes", readSize, fileSize);
		m_fwImageBuffer.clear();
		return false;
	}

	PLOGI.printf("Loaded firmware file: %ld bytes", fileSize);
	return true;
}

bool CLaserModule::SendFWDownloadStart() {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	// Command (1 byte) + Size (4 bytes) = 5 bytes total data
	getSerialPacket(eFID::FID_FW_DOWNLOAD, 5, serialPacket, packetLength);

	int idx = DATA_IDX;
	serialPacket[idx++] = REQ_DOWNLOAD_BEGIN;

	// Send file size (4 bytes, little endian)
	UINT fileSize = (UINT)m_fwImageBuffer.size();
	memcpy(&serialPacket[idx], &fileSize, sizeof(UINT));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	PLOGI.printf("Firmware download start command sent: size=%u bytes", fileSize);
	return (written == packetLength);
}

bool CLaserModule::SendFWDataChunk() {
	if (!m_initMotor) return false;
	if (m_fwDownloadIndex >= (int)m_fwImageBuffer.size()) {
		return false;
	}

	int nRemain = m_fwImageBuffer.size() - m_fwDownloadIndex;
	if (nRemain > FW_CHUNK_SIZE) {
		nRemain = FW_CHUNK_SIZE;
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;

	getSerialPacket(eFID::FID_FW_DOWNLOAD, 5 + nRemain, serialPacket, packetLength);

	int idx = DATA_IDX;
	serialPacket[idx++] = REQ_DOWNLOAD_BODY;

	memcpy(&serialPacket[idx], &m_fwDownloadSequence, sizeof(int));
	idx += sizeof(int);

	// Copy firmware data chunk
	memcpy(&serialPacket[idx], &m_fwImageBuffer[m_fwDownloadIndex], nRemain);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	if (written == packetLength) {
		m_fwDownloadIndex += nRemain;
		m_fwDownloadSequence++;

		m_fwDownloadProgress = (m_fwDownloadIndex * 100) / m_fwImageBuffer.size();

		if (m_fwProgressCallback != nullptr) {
			m_fwProgressCallback(m_fwDownloadProgress);
		}

		PLOGI.printf("Firmware chunk sent: seq=%d, index=%d/%zu (%d%%)",
			m_fwDownloadSequence - 1, m_fwDownloadIndex, m_fwImageBuffer.size(), m_fwDownloadProgress);
	}

	return (written == packetLength);
}

bool CLaserModule::SendFWDownloadEnd(bool success) {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;

	getSerialPacket(eFID::FID_FW_DOWNLOAD, 5, serialPacket, packetLength);

	int dataIdx = DATA_IDX;

	serialPacket[dataIdx] = success ? REQ_DOWNLOAD_END : REQ_DOWNLOAD_CANCEL;

	*reinterpret_cast<int*>(&serialPacket[dataIdx + 1]) = 0;

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	PLOGI.printf("Firmware download end command sent: %s (cmd=0x%02X, seq=0)",
		success ? "SUCCESS" : "CANCEL",
		serialPacket[dataIdx]);

	return (written == packetLength);
}

bool CLaserModule::StartFWDownload(const char* filepath) {
	if (!m_initMotor) {
		PLOGI.printf("Cannot start firmware download: device not connected");
		return false;
	}

	if (m_fwDownloadState == eFWDownloadState::Downloading) {
		PLOGI.printf("Firmware download already in progress");
		return false;
	}

	// Validate firmware file
	SFirmwareMetadata metadata;
	if (!ValidateFWFile(filepath, metadata)) {
		m_fwDownloadState = eFWDownloadState::Failed;
		if (m_fwStatusCallback != nullptr) {
			m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
		}
		return false;
	}

	// Load firmware data
	if (!LoadFirmwareData(filepath)) {
		m_fwDownloadState = eFWDownloadState::Failed;
		if (m_fwStatusCallback != nullptr) {
			m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
		}
		return false;
	}

	// Initialize download state
	m_fwDownloadState = eFWDownloadState::Downloading;
	m_fwDownloadProgress = 0;
	m_fwDownloadIndex = 0;
	m_fwDownloadSequence = 1; // Start from sequence 1

	// Notify state change
	if (m_fwStatusCallback != nullptr) {
		m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
	}

	// Send start command
	if (!SendFWDownloadStart()) {
		PLOGI.printf("Failed to send firmware download start command");
		m_fwDownloadState = eFWDownloadState::Failed;
		if (m_fwStatusCallback != nullptr) {
			m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
		}
		return false;
	}

	PLOGI.printf("Firmware download started: %s", filepath);
	return true;
}

bool CLaserModule::CancelFWDownload() {
	if (m_fwDownloadState != eFWDownloadState::Downloading) {
		return false;
	}

	PLOGI.printf("Firmware download cancelled by user");

	SendFWDownloadEnd(false);

	m_fwDownloadState = eFWDownloadState::Cancelled;
	m_fwDownloadIndex = 0;
	m_fwDownloadSequence = 0;
	m_fwDownloadProgress = 0;
	m_fwImageBuffer.clear();

	if (m_fwStatusCallback != nullptr) {
		m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
	}

	return true;
}

void CLaserModule::RxPacketFWDownload(BYTE* buff) {
	if (m_vPacket.size() < (5 + HEADER_LEN)) {
		PLOGI.printf("RxPacketFWDownload Packet length error rxlen = %d", static_cast<int>(m_vPacket.size()));
		return;
	}

	if (m_fwDownloadState != eFWDownloadState::Downloading) {
		return;
	}

	BYTE response = buff[DATA_IDX];

	switch (response) {
	case REQ_DOWNLOAD_CANCEL:
		PLOGI.printf("Firmware download cancelled");
		m_fwDownloadState = eFWDownloadState::Cancelled;
		m_fwDownloadIndex = 0;
		m_fwDownloadSequence = 0;
		m_fwDownloadProgress = 0;
		m_fwImageBuffer.clear();

		if (m_fwStatusCallback != nullptr) {
			m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
		}
		break;

	case REQ_DOWNLOAD_BEGIN:
		PLOGI.printf("Firmware download begin acknowledged, sending first chunk");
		if (!SendFWDataChunk()) {
			PLOGI.printf("Failed to send first chunk");
			m_fwDownloadState = eFWDownloadState::Failed;
			m_fwImageBuffer.clear();
			if (m_fwStatusCallback != nullptr) {
				m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
			}
		}
		break;

	case RSP_DOWNLOAD_ING:
		PLOGI.printf("Firmware download in progress, sending next chunk");
		if (m_fwDownloadIndex >= (int)m_fwImageBuffer.size()) {
			PLOGI.printf("All firmware data sent, sending end command");
			SendFWDownloadEnd(true);

			m_fwDownloadState = eFWDownloadState::Success;
			m_fwDownloadProgress = 100;
			m_fwImageBuffer.clear();

			if (m_fwProgressCallback != nullptr) {
				m_fwProgressCallback(100);
			}
			if (m_fwStatusCallback != nullptr) {
				m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
			}
		}
		else {
			if (!SendFWDataChunk()) {
				PLOGI.printf("Failed to send firmware chunk");
				m_fwDownloadState = eFWDownloadState::Failed;
				m_fwImageBuffer.clear();

				if (m_fwStatusCallback != nullptr) {
					m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
				}
			}
		}
		break;

	case REQ_DOWNLOAD_END:
		PLOGI.printf("Firmware download end acknowledged");
		break;

	case RSP_DOWNLOAD_DONE:
		PLOGI.printf("Firmware download completed successfully");
		m_fwDownloadState = eFWDownloadState::Success;
		m_fwDownloadProgress = 100;
		m_fwImageBuffer.clear();

		if (m_fwProgressCallback != nullptr) {
			m_fwProgressCallback(100);
		}
		if (m_fwStatusCallback != nullptr) {
			m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
		}
		break;

	case RSP_DOWNLOAD_PAUSE:
	case RSP_DOWNLOAD_FAIL:
	case RSP_DOWNLOAD_READY:
	default:
		PLOGI.printf("Firmware download failed or unknown response: 0x%02X", response);
		m_fwDownloadState = eFWDownloadState::Failed;
		m_fwImageBuffer.clear();

		if (m_fwStatusCallback != nullptr) {
			m_fwStatusCallback(static_cast<int>(m_fwDownloadState));
		}
		break;
	}
}