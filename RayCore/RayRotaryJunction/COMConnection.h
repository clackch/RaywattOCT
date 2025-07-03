#pragma once
#include "Connection.h"
#include "WriteTaskManager.h"

class CSerialPort;
class CCOMConnection : public IConnection
{
private:
	CSerialPort* m_pPort;
	WriteTaskManager* m_pWriteManager;

public:
	CCOMConnection();
	virtual ~CCOMConnection();

	virtual bool Connect(void* param = nullptr);
	virtual void Disconnect();

	virtual int Write(unsigned char* buffer, int size);
	virtual int Read(unsigned char* buffer, int size);
};

