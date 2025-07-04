#include "COMConnection.h"
#include "SerialPort.h"
#include "Utility.h"

CCOMConnection::CCOMConnection() {
	m_pPort = new CSerialPort();
	m_pWriteManager = new WriteTaskController(1);
}
CCOMConnection::~CCOMConnection() {
	if (m_pPort != nullptr) {
		delete m_pPort;
	}
}

bool CCOMConnection::Connect(void* param) {
	tstring strPort = (TCHAR*)param;
	bool result = m_pPort->OpenPort(strPort);

	if (result) {
		result &= m_pPort->ConfigurePort(115200, 8, FALSE, NOPARITY, ONESTOPBIT);
		result &= m_pPort->SetCommunicationTimeouts(50, 100, 10, 50, 10); // MAXWORD, MAXWORD, 100, 0, 100);
	}

	return result;
}

void CCOMConnection::Disconnect() {
	if (m_pPort->IsOpen()) {
		m_pPort->ClosePort();
	}
	m_pWriteManager->stop();
}

int CCOMConnection::Write(unsigned char* buffer, int size) 
{
	if (m_pPort == nullptr || !m_pPort->IsOpen()) return 0;
	if (buffer == nullptr) return 0;
	bool result = true;
	BYTE* copied = new BYTE[size];
	std::memcpy(copied, buffer, size);
	m_pWriteManager->addTask([=]() {
		m_pPort->WriteByte(copied, size);
		delete[] copied;
		});
	return (result) ? size : 0;
}

int CCOMConnection::Read(unsigned char* buffer, int size)
{
	if (m_pPort == nullptr || !m_pPort->IsOpen()) return 0;
	if (buffer == nullptr) return 0;
	int nRead = m_pPort->ReadByte(buffer, size);
	return nRead;
}