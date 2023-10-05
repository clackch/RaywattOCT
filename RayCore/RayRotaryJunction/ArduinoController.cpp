#include "ArduinoController.h"
#include "SerialPort.h"
#include <vector>
#include <sstream>

CArduinoController::CArduinoController()
{
	m_pPosition[(UINT)StepMotorIndex::Pullback] = 0;
	m_pPosition[(UINT)StepMotorIndex::Hub] = 0;
	m_fTargetPosition = 0.0f;
	m_nSpeed = 1.f;
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
bool CArduinoController::SetCurrent(StepMotorIndex idx, int nPosition)
{
	char strCommand[MAX_PATH];
	sprintf(strCommand, "current %d %d\n", idx, (int)nPosition);

	bool result = sendCommand(strCommand);
	Sleep(DELAY_BETWEEN_COMMAND);

	m_pPosition[(UINT)idx] = nPosition;
	if (idx == StepMotorIndex::Both) {
		m_pPosition[(UINT)StepMotorIndex::Pullback] = nPosition;
		m_pPosition[(UINT)StepMotorIndex::Hub] = nPosition;
	}

	return result;
}
bool CArduinoController::IsMoving()
{
	if (m_fPosition != m_fTargetPosition)
	{
		readResponse();
	}

	return (m_fPosition == m_fTargetPosition);
}
bool CArduinoController::MoveAbsolute(StepMotorIndex idx, int nPos)
{
	double prevPosition = (idx == StepMotorIndex::Both) ? m_pPosition[(UINT)StepMotorIndex::Pullback] : m_pPosition[(UINT)idx];
	UINT distance = abs((int)nPos - (int)prevPosition);
	double time = ((double)distance / (double)m_nSpeed) * 1000.f;

	if (distance == 0) return true;

	char strCommand[MAX_PATH];
	sprintf(strCommand, "move %d %d\n", idx, (int)nPos);

	bool result = sendCommand(strCommand);
	Sleep(DELAY_BETWEEN_COMMAND);

	// wait while moving
	Sleep((long)time);
	PLOGI.printf("  > %d to %d : %dmm, %ldms", (int)prevPosition, nPos, distance, (long)time);

	m_fTargetPosition = nPos;	// unit: 1mm
	m_pPosition[(UINT)idx] = nPos;
	if (idx == StepMotorIndex::Both) {
		m_pPosition[(UINT)StepMotorIndex::Pullback] = nPos;
		m_pPosition[(UINT)StepMotorIndex::Hub] = nPos;
	}

	return result;
}
bool CArduinoController::MoveRelative(StepMotorIndex idx, int nOffset)
{
	double prevPosition = (idx == StepMotorIndex::Both) ? m_pPosition[(UINT)StepMotorIndex::Pullback] : m_pPosition[(UINT)idx];
	UINT nPosition = prevPosition + nOffset;
	PLOGI.printf("prevPosition : %d, offset : %d", (int)prevPosition, nOffset);

	return MoveAbsolute(idx, nPosition);
}
bool CArduinoController::SetSpeed(StepMotorIndex idx, int nVelocity)
{
	char strCommand[MAX_PATH];
	sprintf(strCommand, "set %d %d\n", idx, nVelocity);

	m_nSpeed = nVelocity;

	bool result = sendCommand(strCommand);
	Sleep(DELAY_BETWEEN_COMMAND);

	return result;
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
				PLOGI.printf("[readResponse] %s\n", m_pReadBuffer);
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