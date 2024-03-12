#include "RJController.h"
#include "COMConnection.h"
#include "Utility.h"

CRJController::CRJController()
{
	m_pThreadState = nullptr;
	m_state = RJState::Disconnected;
	m_nextState = RJState::Disconnected;

	m_nStepPosition[0] = 0;
	m_nStepPosition[1] = 0;
	m_nStepSpeed[0] = 0;
	m_nStepSpeed[1] = 0;
	
	for (int i = 0; i < 6; i++) {
		m_bPhotoSensor[i] = false;
	}
	m_bButton[0] = false;
	m_bButton[1] = false;
	m_bLimitSwitch = false;
	m_nRFIDLength = 0;
}

CRJController::~CRJController()
{
}

bool CRJController::Connect(void *param) {
	if (m_initMotor) return m_initMotor;

	m_pConnection = new CCOMConnection();
	m_initMotor = m_pConnection->Connect(param);
	if (m_initMotor) {
		BOOL result = TRUE;
		result &= CUtility::StartThread(threadReadPacket, m_pThread, (LPVOID)this);
		result &= CUtility::StartThread(threadRJState, m_pThreadState, (LPVOID)this);

		if (result == FALSE) {
			Disconnect();
			m_initMotor = false;
		}
	}
	AutoStatePeriod(100);
	DisplayLCD(eLCDImage::LCD_IMAGE_UNLOADED);

	m_state = RJState::Disconnected;
	m_nextState = RJState::Disconnected;

	return m_initMotor;
}
void CRJController::Disconnect() {
	DisplayLCD(eLCDImage::LCD_IMAGE_BOOTING);
	CMotorController::Disconnect();

	CUtility::StopThread(m_pThreadState);
	m_state = RJState::Disconnected;
}
bool CRJController::Current(int idxMotor, int posMM) {
	if (!m_initMotor || idxMotor < 0 || idxMotor > 2) return false;

	int posStep = ((double)posMM / MM_PER_STEP);

	if (idxMotor == 0) {
		m_nStepPosition[0] = posStep;
		m_nStepPosition[1] = posStep;
	}
	else {
		m_nStepPosition[idxMotor - 1] = posStep;
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_SET_POS, sizeof(int) * 2, serialPacket, packetLength);

	int idxData = RJ_DATA_IDX;
	memcpy(serialPacket + idxData, &m_nStepPosition[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepPosition[1], sizeof(int));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CRJController::Move(int idxMotor, int posMM) {
	if (!m_initMotor || idxMotor < 0 || idxMotor > 2) return false;
	int posStep = ((double)posMM / MM_PER_STEP);

	if (idxMotor == 0) {
		m_nStepPosition[0] = posStep;
		m_nStepPosition[1] = posStep;
	}
	else {
		m_nStepPosition[idxMotor - 1] = posStep;
	}

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SM_RUN, sizeof(int) * 4, serialPacket, packetLength);

	int idxData = RJ_DATA_IDX;
	memcpy(serialPacket + idxData, &m_nStepPosition[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepPosition[1], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepSpeed[0], sizeof(int));
	idxData += sizeof(int);
	memcpy(serialPacket + idxData, &m_nStepSpeed[1], sizeof(int));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}
bool CRJController::Set(int idxMotor, int velocity) {
	if (!m_initMotor || idxMotor < 0 || idxMotor > 2) return false;
	int velStep = ((double)velocity / MM_PER_STEP);

	if (idxMotor == 0) {
		m_nStepSpeed[0] = velStep;
		m_nStepSpeed[1] = velStep;
	}
	else {
		m_nStepSpeed[idxMotor - 1] = velStep;
	}

	return true;
}
bool CRJController::AutoStatePeriod(USHORT interval) {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_SET_AUTO_PERIOD, sizeof(unsigned short), serialPacket, packetLength);

	memcpy(serialPacket + RJ_DATA_IDX, &interval, sizeof(unsigned short));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);

}
bool CRJController::DisplayLCD(eLCDImage image) {
	if (!m_initMotor) return false;

	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_LCD_DISP_IMAGE, sizeof(unsigned short), serialPacket, packetLength);

	memcpy(serialPacket + RJ_DATA_IDX, &image, sizeof(unsigned short));

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
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
UINT CRJController::threadRJState(LPVOID param) {
	CRJController* pRJController = (CRJController*)param;

	while (pRJController->m_pThreadState->isRun) {
		pRJController->updateState();
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
	case RJState::Disconnected:
		if (m_bLimitSwitch) {
			m_nextState = RJState::Connected;
		}
		break;
	case RJState::Connected:
		if (m_bLimitSwitch) {
			if (m_nRFIDLength == 0) {
				ReadRFID();
			}
			else {
				m_nextState = RJState::Loading;
			}
		}
		else {
			m_nextState = RJState::Disconnected;
		}
		break;
	case RJState::Loading:
		if (m_bButton[1]) {
			m_nextState = RJState::Error;
		}
		else if (!m_bLimitSwitch) {
			m_nextState = RJState::Unloading;
		}
		break;
	case RJState::Loaded:
		if (!m_bLimitSwitch || m_bButton[0]) {
			m_nextState = RJState::Unloading;
		}
		break;
	case RJState::Unloading:
		if (m_bButton[1]) {
			m_nextState = RJState::Error;
		}
		else if (m_bButton[0]) {	// for test
			m_nextState = RJState::Disconnected;
		}
		break;
	case RJState::Error:
		if (m_bButton[0]) {	// for test
			m_nextState = RJState::Disconnected;
		}
		break;
	default:
		break;
	}

	if (m_state != m_nextState) {
		updateState(m_nextState);
	}
}

void CRJController::updateState(RJState state) {
	// postMessage

	switch (state) {
	case RJState::Disconnected:
		DisplayLCD(eLCDImage::LCD_IMAGE_UNLOADED);
		break;
	case RJState::Connected:
		m_nRFIDLength = 0;	// clear RFID info.
		break;
	case RJState::Loading:
		DisplayLCD(eLCDImage::LCD_IMAGE_LOADING);
		break;
	case RJState::Loaded:
		DisplayLCD(eLCDImage::LCD_IMAGE_STANDBY_OFF);
		break;
	case RJState::Unloading:
		DisplayLCD(eLCDImage::LCD_IMAGE_UNLOADING);
		break;
	case RJState::Error:
		DisplayLCD(eLCDImage::LCD_IMAGE_ERROR);
		break;
	default:
		break;
	}
	m_state = m_nextState;
}
void CRJController::addPacket(BYTE* packet, int size) {
	for (int i = 0; i < size; i++) {
		m_vPacket.push_back(packet[i]);
	}
}
bool CRJController::sliceUntilSTX(int index) {
	bool findSTX = false;
	std::vector<char> vPacket;
	for (int i = index; i < m_vPacket.size(); i++) {
		if (m_vPacket[i] == RJ_STX) findSTX = true;
		if (findSTX) vPacket.push_back(m_vPacket[i]);
	}

	m_vPacket.clear();
	if (findSTX) {
		m_vPacket.resize(vPacket.size());
		std::copy(vPacket.begin(), vPacket.end(), m_vPacket.begin());
	}

	return findSTX;
}
bool CRJController::parseSerialPacket() {
	if (m_vPacket.size() > 0) {
		bool findSTX = true;
		if (m_vPacket[0] != RJ_STX) {
			findSTX = sliceUntilSTX(1);
		}

		if (findSTX) {
			for (int idxETX = 0; idxETX < m_vPacket.size(); idxETX++) {
				if (m_vPacket[idxETX] == RJ_ETX)
				{
					BYTE length = m_vPacket[RJ_LENGTH_IDX];
					BYTE checksum = calcChecksum(&m_vPacket[0], length - 2);

					if (idxETX == (length - 1) && checksum == m_vPacket[length - 2]) {
						handlePacket();
						sliceUntilSTX(idxETX);
					}
					else {
						sliceUntilSTX(1);
					}

					break;
				}
			}
		}
	}

	return true;
}
void CRJController::parseRFIDPacket(BYTE* packet, int size) {
	m_nRFIDLength = packet[1];
	memcpy(m_RFID, packet + 2, m_nRFIDLength);

	printf("RFID: ");
	for (int i = 0; i < m_nRFIDLength; i++) {
		printf("0x%x ", m_RFID[i]);
	}
	printf("\n");
}
void CRJController::handlePacket() {
	BYTE length = m_vPacket[RJ_LENGTH_IDX];
	int dataLength = length - RJ_HEADER_LEN;
	eFID fid = (eFID) m_vPacket[RJ_FID_IDX];

	char strTime[MAX_PATH];
	CUtility::GetCurTime(strTime);

	printf("%s\tFID: 0x%02x Sensor: ", strTime, fid);
	// photo sensor state
	for (int i = 0; i < 6; i++) {
		m_bPhotoSensor[i] = m_vPacket[RJ_PHOTO_IDX] & (0x1 << i);
		printf(" %02d", m_bPhotoSensor[i]);
	}
	
	// button, switch state
	m_bButton[0] = m_vPacket[RJ_KEY_IDX] & 0x1;
	m_bButton[1] = m_vPacket[RJ_KEY_IDX] & 0x2;
	m_bLimitSwitch = m_vPacket[RJ_KEY_IDX] & 0x4;

	printf("\tButton: %02d %02d %02d\n", m_bButton[0], m_bButton[1], m_bLimitSwitch);

	switch(fid) {
	case eFID::FID_BLDC_PASS:
		parsePacket(&m_vPacket[RJ_DATA_IDX], dataLength);
		break;
	case eFID::FID_RFID_GET_STATE:
		parseRFIDPacket(&m_vPacket[RJ_DATA_IDX], dataLength);
		break;
	default:
		break;
	}
}
void CRJController::getSerialPacket(eFID fid, int dataSize, BYTE* packet, int& packetLength) {
	if (packet == nullptr || dataSize < 0) return;

	packetLength = dataSize + RJ_HEADER_LEN;

	packet[0] = RJ_STX;
	packet[RJ_LENGTH_IDX] = (BYTE) packetLength;
	packet[RJ_FID_IDX] = (BYTE) fid;
	packet[RJ_RET_IDX] = 0;
	packet[RJ_PHOTO_IDX] = 0;
	packet[RJ_KEY_IDX] = 0;
	packet[packetLength - 1] = RJ_ETX;
}
BYTE CRJController::calcChecksum(BYTE* packet, int length) {
	unsigned int crc = 0x00;
	for (int i = 1; i < length; i++)
	{
		crc += packet[i];
	}

	return (BYTE)crc;
}
bool CRJController::writeMotor(BYTE* packet, int size) {
	if (!m_initMotor) return false;
	
	BYTE serialPacket[MAX_PATH];
	int packetLength;
	getSerialPacket(eFID::FID_BLDC_PASS, size, serialPacket, packetLength);
	memcpy(serialPacket + RJ_DATA_IDX, packet, size);

	BYTE checksum = calcChecksum(serialPacket, packetLength - 2);
	serialPacket[packetLength - 2] = checksum;

	int written = m_pConnection->Write(serialPacket, packetLength);

	return (written == packetLength);
}