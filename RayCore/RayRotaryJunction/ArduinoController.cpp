#include "ArduinoController.h"
#include "SerialPort.h"
#include <vector>
#include <sstream>

CArduinoController::CArduinoController()
{
	m_fTargetPosition = 0.0f;
}
CArduinoController::~CArduinoController()
{}

bool CArduinoController::Open(tstring strPort)
{
	Close();

	bool result = m_pPort->OpenPort(strPort);

	if (result) {
		result &= m_pPort->ConfigurePort(9600, 8, FALSE, NOPARITY, ONESTOPBIT);
		result &= m_pPort->SetCommunicationTimeouts(MAXWORD, MAXWORD, 100, 0, 100);
	}

	return result;
}
bool CArduinoController::SetCurrent(int nPosition)
{
	char strCommand[MAX_PATH];
	sprintf(strCommand, "current %d\n", (int)nPosition);
	printf("[SetCurrent] %s", strCommand);

	return sendCommand(strCommand);
}
bool CArduinoController::IsMoving()
{
	if (m_fPosition != m_fTargetPosition)
	{
		readResponse();
	}

	return (m_fPosition == m_fTargetPosition);
}
bool CArduinoController::MoveAbsolute(int nPos)
{
	char strCommand[MAX_PATH];
	m_fTargetPosition = nPos;	// unit: 1mm
	m_fTargetPosition = (m_fTargetPosition < 0) ? 0 : (m_fTargetPosition > HAYDON_PULLBACK_LIMIT) ? HAYDON_PULLBACK_LIMIT : m_fTargetPosition;
	sprintf(strCommand, "move %d\n", (int)m_fTargetPosition);
	printf("[MoveAbsolute] %s", strCommand);

	return sendCommand(strCommand);
}
bool CArduinoController::MoveRelative(int nOffset)
{
	char strCommand[MAX_PATH];
	m_fTargetPosition = m_fPosition + (nOffset * 10);	// unit: 0.1mm
	m_fTargetPosition = (m_fTargetPosition < 0) ? 0 : (m_fTargetPosition > HAYDON_PULLBACK_LIMIT) ? HAYDON_PULLBACK_LIMIT : m_fTargetPosition;
	sprintf(strCommand, "move_delay %d\n", (int)m_fTargetPosition);
	printf("[MoveRelative] %s", strCommand);

	return sendCommand(strCommand);
}
bool CArduinoController::SetSpeed(int nVelocity)
{
	char strCommand[MAX_PATH];
	sprintf(strCommand, "set %d\n", nVelocity);
	printf("[SetSpeed] %s", strCommand);

	return sendCommand(strCommand, false);
}

void CArduinoController::readResponse()
{
	BYTE buf = 0x00;
	int nRead = 0;

	while (true) {
		if (m_pPort->ReadByte(buf)) {
			m_pReadBuffer[nRead] = buf;
			nRead++;
			if (buf == '\n') {
				m_pReadBuffer[nRead] = '\0';
				parseResponse((const char*) m_pReadBuffer);
				printf("[readResponse] %s\n", m_pReadBuffer);
				break;
			}
		}
	}
}
bool CArduinoController::parseResponse(const char* strResponse)
{
	std::string strLine(strResponse);
	std::vector<std::string> result;
	std::stringstream strStream(strLine);
	std::string token;

	while (getline(strStream, token, ' ')) {
		result.push_back(token);
	}

	if (result.size() >= 2) {
		m_fPosition = atoi(result.at(1).c_str());
		return true;
	}
	return false;
}