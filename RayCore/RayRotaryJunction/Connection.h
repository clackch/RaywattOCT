#pragma once

class IConnection {
public:
	IConnection() {}
	virtual ~IConnection() {}

	virtual bool Connect(void* param = nullptr) = 0;
	virtual void Disconnect() = 0;

	virtual int Write(unsigned char* buffer, int size) = 0;
	virtual int Read(unsigned char* buffer, int size) = 0;
};