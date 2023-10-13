#pragma once
#include "Connection.h"
#include "libusb.h"

#define USB_ENDPOINT_IN	    (LIBUSB_ENDPOINT_IN  | 1)   /* endpoint address */
#define USB_ENDPOINT_OUT	0x01	//(LIBUSB_ENDPOINT_OUT | 2)   /* endpoint address */
#define USB_TIMEOUT	        3000        /* Connection timeout (in ms) */

class CUSBConnection : public IConnection
{
private:
	bool m_initUsb;
	libusb_device_handle* m_hUsbHandle;

public:
	CUSBConnection();
	virtual ~CUSBConnection();

	virtual bool Connect(void* param = nullptr);
	virtual void Disconnect();

	virtual int Write(unsigned char* buffer, int size);
	virtual int Read(unsigned char* buffer);

private:
	bool checkUsbDescription(libusb_device* dev);
};

