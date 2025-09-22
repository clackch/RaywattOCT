#pragma once
#include "Connection.h"
#include "WriteTaskController.h"

class CSerialPort;
class CCOMConnection : public IConnection
{
private:
	CSerialPort* m_pPort = nullptr;
	WriteTaskController* m_pWriteManager = nullptr;

public:
	CCOMConnection();
	virtual ~CCOMConnection();

	virtual bool Connect(void* param = nullptr);
	virtual void Disconnect();

	virtual int Write(unsigned char* buffer, int size);
	virtual int Read(unsigned char* buffer);
};

