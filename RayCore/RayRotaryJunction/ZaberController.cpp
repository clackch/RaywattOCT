#include "ZaberController.h"
#include "SerialPort.h"
#include <vector>
#include <sstream>

CZaberController::CZaberController() {}
CZaberController::~CZaberController() {}

/*
* @param strPort : COM Port - ex) "COM1"
*/
bool CZaberController::Open(tstring strPort) {
	Close();

	bool result = m_pPort->OpenPort(strPort);

	if (result) {
		result &= m_pPort->ConfigurePort(115200, 8, FALSE, NOPARITY, ONESTOPBIT);
		result &= m_pPort->SetCommunicationTimeouts(MAXWORD, MAXWORD, 100, 0, 100);
	}

	return result;
}

/*
* @returns true, if Zaber position is Idle
*/
bool CZaberController::IsMoving() {
	bool result = sendCommand("/get pos\n");

	if (result) {
		std::string strState;
		int nPos = 0;

		if (parseZaberState((char*)m_pReadBuffer, strState, nPos)) {
			if (strState.compare("IDLE") == 0) return true;
		}
	}
	return false;
}

/*
* @param nPos : absolute position (mm)
*/
bool CZaberController::MoveAbsolute(int nPos) {
	char strCommand[MAX_PATH];
	sprintf(strCommand, "/move abs %d\n", convertMMtoData(nPos));

	return sendCommand(strCommand);
}

/*
* @param nPos : relative position (mm)
*/
bool CZaberController::MoveRelative(int nOffset) {
	char strCommand[MAX_PATH];
	sprintf(strCommand, "/move rel %d\n", convertMMtoData(nOffset));

	return sendCommand(strCommand);
}

/*
* @param nVelocity : velocity (mm/s)
*/
bool CZaberController::SetSpeed(int nVelocity) {
	bool result = false;
	char strCommand[MAX_PATH];

	sprintf(strCommand, "/set maxspeed %d\n", convertMMStoData(nVelocity));
	result &= sendCommand(strCommand);

	return result;
}

bool CZaberController::Idle() {
	return sendCommand("/home\n");
}

/*
* @param nPos : absolute position (um)
*/
bool CZaberController::MoveMicrometer(long long nPos) {
	char strCommand[MAX_PATH];
	sprintf(strCommand, "/move abs %d\n", convertUMtoData(nPos));

	return sendCommand(strCommand);
}

/*
* @param nPos : relative position (mm)
*/
bool CZaberController::RotateRelative(int nPos) {
	char strCommand[MAX_PATH];
	sprintf(strCommand, "/move rel %d\n", convertMMtoRotate(nPos));

	return sendCommand(strCommand);
}

/*
* @param nVelocity : velocity (mm/s)
* @param nDistance : distance (mm)
*/
bool CZaberController::Pull(int nVelocity, int nDistance) {
	bool result = false;
	char strCommand[MAX_PATH];

	sprintf(strCommand, "/move vel %d\n", convertMMStoData(nVelocity) * -1);
	result &= sendCommand(strCommand);

	return result;
}

void CZaberController::readResponse() {
	BYTE buf = 0x00;
	int nRead = 0;

	while (true) {
		if (m_pPort->ReadByte(buf)) {
			if (buf == '@') {
				nRead = 0;
			}
			m_pReadBuffer[nRead] = buf;
			nRead++;
			if (buf == '\n') {
				m_pReadBuffer[nRead] = '\0';
				break;
			}
		}
	}
}
bool CZaberController::parseZaberState(const char* strResponse, std::string& strState, int& nPos) {
	std::string strLine(strResponse);
	std::vector<std::string> result;
	std::stringstream strStream(strLine);
	std::string token;

	while (getline(strStream, token, ' ')) {
		result.push_back(token);
	}

	if (result.size() == 6) {
		strState = result.at(3);
		nPos = atoi(result.at(5).c_str());

		return true;
	}
	return false;
}
int CZaberController::convertUMtoData(long long nPos) {
	return (nPos * ZABER_SCALE_MM_TO_POSITION / 1000.f);
}
int CZaberController::convertMMtoData(int nPos) {
	return (nPos * ZABER_SCALE_MM_TO_POSITION);
}
int CZaberController::convertMMtoRotate(int nPos) {
	return (nPos * ZABER_SCALE_MM_TO_ROTATE);	
}
int CZaberController::convertMMStoData(int nVelocity) {
	return (nVelocity * ZABER_SCALE_MMS_TO_VELOCITY);
}