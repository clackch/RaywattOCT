#include "RJController.h"
#include "COMConnection.h"
#include "Utility.h"
#include "MessageService.h"

CRJController::CRJController()
	:ICommonProtocol(RJ_STX, RJ_ETX)
{
	m_pMsg = nullptr;
	m_pThreadState = nullptr;
	m_state = eRJState::None;
	m_nextState = eRJState::None;
	m_recvState = eRJState::None;
	m_bStateReceived = false;
	m_bReadInitStatus = false;
	m_isInit = false;

	m_nStepPosition[0] = 0;
	m_nStepPosition[1] = 0;
	m_nStepSpeed[0] = 0;
	m_nStepSpeed[1] = 0;
	
	m_isSMMoving[0] = false;
	m_isSMMoving[1] = false;
	for (int i = 0; i < 6; i++) {
		m_bPhotoSensor[i] = false;
	}
	m_bButton[0] = false;
	m_bButton[1] = false;
	m_bLimitSwitch = false;
	m_nRFIDLength = 0;

	m_bManualMode = false;
}

CRJController::~CRJController()
{
	Disconnect();
}
void CRJController::UpdateState(eRJState state)
{
	if (m_state == eRJState::Error) return;
	m_recvState = state;
	m_bStateReceived = true;
}
bool CRJController::Connect(void *param) {
	if (m_initMotor) return m_initMotor;

	m_pConnection = new CCOMConnection();
	m_initMotor = m_pConnection->Connect(param);
	if (m_initMotor) {
		BOOL result = CUtility::StartThread(threadReadPacket, m_pThread, (LPVOID)this);

		if (result == FALSE) {
			Disconnect();
			m_initMotor = false;
		}
	}
	displayLCD(eLCDImage::LCD_IMAGE_BOOTING);

	m_state = eRJState::Initializing;
	m_nextState = eRJState::Initializing;

	return m_initMotor;
}
void CRJController::Disconnect() {
	DisplayLCD(eLCDImage::LCD_IMAGE_BOOTING);
	CMotorController::Disconnect();

	CUtility::StopThread(m_pThreadState);
	m_state = eRJState::Disconnected;
}
bool CRJController::IsMoving() {
	if (m_isSMMoving[0] || m_isSMMoving[1]) {
		ReadPosition();
		return true;
	}
	return false;
}
bool CRJController::ReadPosition() {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];

	int packetLength;
	getSerialPacket(eFID::FID_SM_GET_STATE, 0, serialPacket, packetLength);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);	
}
bool CRJController::Current(eStepMotorIndex idxMotor, int posStep) {
	if (!m_initMotor) return false;
	if (m_state == eRJState::Error) return false;

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
bool CRJController::Move(eStepMotorIndex idxMotor, int posStep, bool delay, char sensor) {
	if (!m_initMotor) return false;
	if (m_state == eRJState::Error) return false;

	PLOGI.printf("StepMotor #%d Move: %d", idxMotor, posStep);

	if (idxMotor == eStepMotorIndex::Both) {
		m_nStepPosition[0] = posStep;
		m_nStepPosition[1] = posStep;
		m_isSMMoving[0] = true;
		m_isSMMoving[1] = true;
	}
	else {
		m_nStepPosition[(int)idxMotor - 1] = posStep;
		m_isSMMoving[(int)idxMotor - 1] = true;
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_RUN, sizeof(int) * 4, serialPacket, packetLength);

	int idxData = DATA_IDX;
	memcpy(serialPacket + idxData, &m_nStepPosition[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepPosition[1], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepSpeed[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepSpeed[1], sizeof(int));

	serialPacket[PHOTO_IDX] = sensor;

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CRJController::Set(eStepMotorIndex idxMotor, int velStep) {
	if (!m_initMotor) return false;
	if (m_state == eRJState::Error) return false;

	if (idxMotor == eStepMotorIndex::Both) {
		m_nStepSpeed[0] = velStep;
		m_nStepSpeed[1] = velStep;
	}
	else {
		m_nStepSpeed[(int)idxMotor - 1] = velStep;
	}

	return true;
}
const char* CRJController::GetStateString(eRJState state)
{
	const char* strState[] = { "None", "Initializing", "Disconnected", "Cleaning", "Connected", "Validating", "Loading", "WaitManualLoad", "Loaded", "Unloading", "Unloaded", "Error" };
	return strState[(int)state];
}
bool CRJController::StartControl() {
	if (!m_initMotor) return false;
	if (m_pThreadState != nullptr) return true;

	AutoStatePeriod(50);
	initSetting();
	bool result = CUtility::StartThread(threadRJState, m_pThreadState, (LPVOID)this);

	return result;
}
bool CRJController::AutoStatePeriod(USHORT interval) {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SET_AUTO_PERIOD, sizeof(unsigned short), serialPacket, packetLength);

	memcpy(serialPacket + DATA_IDX, &interval, sizeof(unsigned short));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CRJController::StopStepMotors() {
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
bool CRJController::DisplayLCD(eLCDImage image) {
	if (!m_initMotor) return false;
	if (m_state == eRJState::Error) return false;

	return displayLCD(image);
}
bool CRJController::ReadRFID() {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_RFID_GET_STATE, 0, serialPacket, packetLength);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
UINT CRJController::GetRFIDInfo(BYTE* pRFIDInfo) {
	if (pRFIDInfo == nullptr) return 0;
	if (m_nRFIDLength == 0) return 0;

	memcpy(pRFIDInfo, m_RFID, m_nRFIDLength);
	return m_nRFIDLength;
}
int CRJController::ConvertMMtoStep(UINT mm) {
	return floor((float)mm / (float)PULLBACK_MOTOR_RESOLUTION * (float)MOTOR_CONTROL_RESOLUTION);
}
void CRJController::initSetting() {
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_SET_CONFIG, (sizeof(int) * 7) * 2, serialPacket, packetLength);

	const int minSpeed = 315;
	const int maxSpeed = 157480;
	const int accTime = 1;
	const int accStep = 100;
	const int decTime = 1;
	const int decStep = 0;
	const int minStep = 100;

	int offset = 0;
	for (int i = 0; i < 2; i++) {
		memcpy(serialPacket + DATA_IDX + offset, &minSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &maxSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &minStep, sizeof(int)); offset += sizeof(int);
	}

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);
	if (written != packetLength)
	{
		PLOGI.printf("Written size is not matched. (%d / %d bytes)", written, packetLength);
	}
}
UINT CRJController::threadRJState(LPVOID param) {
	CRJController* pRJController = (CRJController*)param;

	while (pRJController->m_pThreadState->isRun) {
		if (pRJController->m_bStateReceived) {
			pRJController->updateState(pRJController->m_recvState);
			pRJController->m_bStateReceived = false;
		}
		else {
			if (pRJController->m_bManualMode) {
				pRJController->updateStateManualMode();
			}
			else {
				pRJController->updateState();
			}
		}
		Sleep(50);
	}

	return NOERROR;
}
UINT CRJController::threadReadPacket(LPVOID param) {
	CRJController* pRJController = (CRJController*)param;
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
void CRJController::updateState() {
	switch (m_state) {
	case eRJState::Initializing:
		if (m_bLimitSwitch) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::Disconnected:
		if (m_bLimitSwitch) {
			m_nextState = eRJState::Connected;
		}
		break;
	case eRJState::Cleaning:
		if (m_bLimitSwitch || m_bButton[1]) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::Connected:
		if (m_bLimitSwitch) {
#if ENABLE_RFID
			if (m_nRFIDLength == 0) {
				ReadRFID();
			}
			else {
				m_nextState = eRJState::Validating;
			}
#else
			m_nextState = eRJState::Validating;
#endif
		}
		else {
			m_nextState = eRJState::Disconnected;
		}
		break;
	case eRJState::Validating:
		break;
	case eRJState::Loading:
		if (m_bButton[1] || !m_bLimitSwitch) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::Loaded:
		if (!m_bLimitSwitch || m_bButton[1]) {
			PLOGI.printf("Error occured: limitSwitch(%d), stopButton(%d)", m_bLimitSwitch, m_bButton[0]);
			Current(eStepMotorIndex::Pullback, DISTANCE_BETWEEN_MOTORS);
			m_nextState = eRJState::Error;
		}
		if (m_bButton[0]) {
			m_nextState = eRJState::Unloading;
		}
		break;
	case eRJState::Unloading:
		if (m_bButton[1]) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::Unloaded:
		if (!m_bLimitSwitch) {
			m_nextState = eRJState::Disconnected;
		}
		break;
	case eRJState::Error:
		if (!m_isInit) {
			if (!m_bLimitSwitch) m_nextState = eRJState::Initializing;
		}
		else if (m_bButton[0]) {
			m_nextState = eRJState::Unloading;
		}
		break;
	default:
		break;
	}

	if (m_state != m_nextState) {
		updateState(m_nextState);
	}
}

void CRJController::updateStateManualMode() {
	switch (m_state) {
	case eRJState::Initializing:
		if (m_bLimitSwitch) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::Disconnected:
		break;
	case eRJState::Connected:
		break;
	case eRJState::Validating:
		break;
	case eRJState::Loading:
		if (m_bButton[1]) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::WaitManualLoad:
		if (m_bButton[1]) {	// Press STOP Button to confirm Loading
			Current(eStepMotorIndex::Pullback, 0);
			Move(eStepMotorIndex::Pullback, 500);
			Sleep(500);
			m_nextState = eRJState::Loaded;
		}
		if (m_bButton[0]) {
			m_nextState = eRJState::Unloading;
		}
		break;
	case eRJState::Loaded:
		if (!m_bLimitSwitch || m_bButton[1]) {
			PLOGI.printf("Error occured: limitSwitch(%d), stopButton(%d)", m_bLimitSwitch, m_bButton[1]);
			Current(eStepMotorIndex::Pullback, DISTANCE_BETWEEN_MOTORS);
			m_nextState = eRJState::Error;
		}
		if (m_bButton[0]) {
			m_nextState = eRJState::Unloading;
		}
		break;
	case eRJState::Unloading:
		if (m_bButton[1]) {
			m_nextState = eRJState::Error;
		}
		break;
	case eRJState::Unloaded:
		if (!m_bLimitSwitch) {
			m_nextState = eRJState::Disconnected;
		}
		break;
	case eRJState::Error:
		if (!m_isInit) {
			if (!m_bLimitSwitch) m_nextState = eRJState::Initializing;
		}
		else if (m_bButton[0]) {
			m_nextState = eRJState::Unloading;
		}
		break;
	default:
		break;
	}

	if (m_state != m_nextState) {
		updateState(m_nextState);
	}
}
void CRJController::updateState(eRJState state) {
	PLOGI.printf("state: %s", GetStateString(state));
	switch (state) {
	case eRJState::Initializing:
		displayLCD(eLCDImage::LCD_IMAGE_BOOTING);
		break;
	case eRJState::Disconnected:
	case eRJState::Unloaded:
		m_isInit = true;
		displayLCD(eLCDImage::LCD_IMAGE_UNLOADED);
		break;
	case eRJState::Cleaning:
		displayLCD(eLCDImage::LCD_IMAGE_BOOTING);	// To-Do: change LCD image
		break;
	case eRJState::Connected:
		m_nRFIDLength = 0;	// clear RFID info.
		break;
	case eRJState::Validating:
		break;
	case eRJState::Loading:
		displayLCD(eLCDImage::LCD_IMAGE_LOADING);
		break;
	case eRJState::WaitManualLoad:
		break;
	case eRJState::Loaded:
		displayLCD(eLCDImage::LCD_IMAGE_STANDBY_OFF);
		break;
	case eRJState::Unloading:
		displayLCD(eLCDImage::LCD_IMAGE_UNLOADING);
		break;
	case eRJState::Error:
		displayLCD(eLCDImage::LCD_IMAGE_ERROR);
		StopMotor();
		StopStepMotors();
		break;
	default:
		break;
	}
	m_state = m_nextState = state;
	if (m_pMsg != nullptr) m_pMsg->postPriorMessage(WM_UPDATE_RJ_STATE, (WPARAM)m_state);
}
bool CRJController::displayLCD(eLCDImage image) {
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_LCD_DISP_IMAGE, sizeof(unsigned short), serialPacket, packetLength);

	memcpy(serialPacket + DATA_IDX, &image, sizeof(unsigned short));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
void CRJController::parseSMPacket(BYTE* packet, int size) {
	int offset = 0;
	for (int i = 0; i < 2; i++) {
		m_isSMMoving[i] = packet[offset]; offset++;
		
		int curPos = 0;
		for (int j = 0; j < 4; j++) {
			curPos |= (packet[offset + j] << (j * 8));
		}
		//PLOGI.printf("StepMotor #%d (%s): %d", i, ((m_isSMMoving[i]) ? "Moving" : "Stop"), curPos);
		offset += 12;	// current pos (4byte), target pos (4byte), current speed (4byte)
	}
}
void CRJController::parseRFIDPacket(BYTE* packet, int size) {
	m_nRFIDLength = packet[1];
	memcpy(m_RFID, packet + 2, m_nRFIDLength);
}
void CRJController::handlePacket() {
	BYTE length = m_vPacket[LENGTH_IDX];
	int dataLength = length - HEADER_LEN;
	eFID fid = (eFID) m_vPacket[FID_IDX];

	char strTime[MAX_PATH];
	CUtility::GetCurTime(strTime);

	// photo sensor state
	for (int i = 0; i < 6; i++) {
		m_bPhotoSensor[i] = m_vPacket[PHOTO_IDX] & (0x1 << i);
	}
	
	// button, switch state
	m_bButton[0] = m_vPacket[KEY_IDX] & 0x1;
	m_bButton[1] = m_vPacket[KEY_IDX] & 0x2;
	m_bLimitSwitch = m_vPacket[KEY_IDX] & 0x4;

	//PLOGI.printf("\tButton: %02d %02d %02d\n", m_bButton[0], m_bButton[1], m_bLimitSwitch);

	switch(fid) {
	case eFID::FID_SM_GET_STATE:
		parseSMPacket(&m_vPacket[DATA_IDX], dataLength);
		break;
	case eFID::FID_BLDC_PASS:
		parsePacket(&m_vPacket[DATA_IDX], dataLength);
		break;
	case eFID::FID_RFID_GET_STATE:
		parseRFIDPacket(&m_vPacket[DATA_IDX], dataLength);
		break;
	default:
		break;
	}

	m_bReadInitStatus = true;
}
bool CRJController::writeMotor(BYTE* packet, int size) {
	if (!m_initMotor) return false;
	
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_BLDC_PASS, size, serialPacket, packetLength);
	memcpy(serialPacket + DATA_IDX, packet, size);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}

void CRJController::changeSMProfileToPullback() {
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_SET_CONFIG, (sizeof(int) * 7) * 2, serialPacket, packetLength);

	const int minSpeed = 315;
	const int maxSpeed = 157480;
	const int accTime = 30;
	const int accStep = 100;
	const int decTime = 30;
	const int decStep = 0;
	const int minStep = 100;

	int offset = 0;
	for (int i = 0; i < 2; i++) {
		memcpy(serialPacket + DATA_IDX + offset, &minSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &maxSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &minStep, sizeof(int)); offset += sizeof(int);
	}

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);
	if (written != packetLength)
	{
		PLOGI.printf("Written size is not matched. (%d / %d bytes)", written, packetLength);
	}
}

void CRJController::changeSMProfileToLoadUnload() {
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_SET_CONFIG, (sizeof(int) * 7) * 2, serialPacket, packetLength);

	const int minSpeed = 315;
	const int maxSpeed = 157480;
	const int accTime = 1;
	const int accStep = 100;
	const int decTime = 1;
	const int decStep = 0;
	const int minStep = 100;

	int offset = 0;
	for (int i = 0; i < 2; i++) {
		memcpy(serialPacket + DATA_IDX + offset, &minSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &maxSpeed, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &accStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decTime, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &decStep, sizeof(int)); offset += sizeof(int);
		memcpy(serialPacket + DATA_IDX + offset, &minStep, sizeof(int)); offset += sizeof(int);
	}

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);
	if (written != packetLength)
	{
		PLOGI.printf("Written size is not matched. (%d / %d bytes)", written, packetLength);
	}
}