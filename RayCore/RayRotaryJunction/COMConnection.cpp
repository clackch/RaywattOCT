#include "COMConnection.h"
#include "SerialPort.h"
#include "Utility.h"

CCOMConnection::CCOMConnection() {
	m_pPort = new CSerialPort();
	/*if (m_pWriteManager == nullptr) {
		m_pWriteManager = new WriteTaskController(1);
		m_pWriteManager->start();
	}*/
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
	if (m_pWriteManager != nullptr) {
		m_pWriteManager->stop();
	}
	if (m_pPort->IsOpen()) {
		m_pPort->ClosePort();
	}
}

int CCOMConnection::Write(unsigned char* buffer, int size) 
{
	if (m_pPort == nullptr || !m_pPort->IsOpen()) return 0;
	if (buffer == nullptr) return 0;
	
	/*BYTE* copied = new BYTE[size];
	std::memcpy(copied, buffer, size);
	bool result = m_pWriteManager->addTask([=]() {
		m_pPort->WriteByte(copied, size);
		delete[] copied;
		});
	
	PLOGI.printf("port[%p] : tasknum[%02x] : %d %d", m_pPort, buffer[2], m_pWriteManager->getTaskNum(), size);*/
	std::stringstream strStream;
	if (/*buffer[2] == 0x23*/true) {
		for (int i = 0; i < size; i++) {
			strStream << std::uppercase << std::hex << static_cast<int>(buffer[i]) << " ";
		}
		//PLOGI.printf("print : %s", strStream.str().c_str());
	}
	bool result = m_pPort->WriteByte(buffer, size);
	tstring portName = m_pPort->getPortName();

#ifdef UNICODE
	// TSTRING == std::wstring
	std::wstring wport = portName;
	std::string port(wport.begin(), wport.end()); // 단순 변환 (ASCII만 확실히 안전)
#else
	// TSTRING == std::string
	std::string port = portName;
#endif
	PLOGI.printf("print(%s)[%s] : %s", (result? "success":"fail"), port.c_str(), strStream.str().c_str());

	return result ? size : 0;
}

int CCOMConnection::Read(unsigned char* buffer)
{
	if (m_pPort == nullptr || !m_pPort->IsOpen()) return 0;
	if (buffer == nullptr) return 0;
	int nRead = m_pPort->ReadByte(buffer, sizeof(buffer));
	return nRead;
}