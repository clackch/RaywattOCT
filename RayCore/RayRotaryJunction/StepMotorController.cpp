#include "StepMotorController.h"
#include "SerialPort.h"

CStepMotorController::CStepMotorController()
{
	m_pPort = new CSerialPort();
	m_fPosition = 0.0f;
}
CStepMotorController::~CStepMotorController()
{
	Close();
	if (m_pPort != nullptr) {
		delete m_pPort;
		m_pPort = nullptr;
	}
}
bool CStepMotorController::IsOpen()
{
	if (m_pPort != nullptr) {
		return m_pPort->IsOpen();
	}
	return false;
}
void CStepMotorController::Close() 
{
	if (m_pPort->IsOpen()) {
		m_pPort->ClosePort();
	}
}
bool CStepMotorController::sendCommand(const char* strCommand, bool readResponse) {
	if (!m_pPort->IsOpen()) {
		return false;
	}

	bool result = m_pPort->WriteByte((BYTE*)strCommand, strlen(strCommand));

	if (result && readResponse) {
		this->readResponse();
	}
	return result;
}