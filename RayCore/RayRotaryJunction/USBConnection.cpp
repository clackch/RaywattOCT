#include "USBConnection.h"
#include "Utility.h"

CUSBConnection::CUSBConnection()
{
	int result = libusb_init(nullptr);
	m_initUsb = (result >= 0) ? true : false;
	m_hUsbHandle = nullptr;
}
CUSBConnection::~CUSBConnection()
{
	if (m_initUsb) libusb_exit(nullptr);
}

bool CUSBConnection::Connect(void* param)
{
	bool result = false;

	libusb_device** pUsbDevices = nullptr;
	ssize_t cnt = libusb_get_device_list(nullptr, &pUsbDevices);

	if (cnt < 0) return result;
	if (pUsbDevices == nullptr) return result;

	for (int i = 0; pUsbDevices[i]; i++) {
		if (checkUsbDescription(pUsbDevices[i])) {
			libusb_open(pUsbDevices[i], &m_hUsbHandle);
			libusb_claim_interface(m_hUsbHandle, 1);
			result = true;
			break;
		}
	}

	libusb_free_device_list(pUsbDevices, 1);

	return result;
}
void CUSBConnection::Disconnect()
{
	if (m_hUsbHandle) {
		libusb_close(m_hUsbHandle);
		m_hUsbHandle = nullptr;
	}
}

int CUSBConnection::Write(unsigned char* buffer, int size)
{
	if (m_hUsbHandle == nullptr) return 0;
	if (buffer == nullptr) return 0;

	int writeSize = 0;
	int ret = libusb_bulk_transfer(m_hUsbHandle, USB_ENDPOINT_OUT, buffer, size, &writeSize, USB_TIMEOUT);
	//Error handling
	switch (ret) {
	case NOERROR:
		PLOGI.printf("send %d bytes to device", size);
		break;
	case LIBUSB_ERROR_TIMEOUT:
		PLOGI.printf("ERROR in bulk write: %d Timeout", ret);
		break;
	case LIBUSB_ERROR_PIPE:
		PLOGI.printf("ERROR in bulk write: %d Pipe", ret);
		break;
	case LIBUSB_ERROR_OVERFLOW:
		PLOGI.printf("ERROR in bulk write: %d Overflow", ret);
		break;
	case LIBUSB_ERROR_NO_DEVICE:
		PLOGI.printf("ERROR in bulk write: %d No Device", ret);
		break;
	default:
		PLOGI.printf("ERROR in bulk write: %d", ret);
		break;
	}

	return writeSize;
}
int CUSBConnection::Read(unsigned char* buffer, int size)
{
	if (m_hUsbHandle == nullptr) return 0;
	if (buffer == nullptr) return 0;

	int nRead = 0;
	int err = libusb_bulk_transfer(m_hUsbHandle, USB_ENDPOINT_IN, buffer, size, &nRead, USB_TIMEOUT);

	if (err != 0) {
		PLOGI.printf("USB Read Error : %d", err);
	}

	return nRead;
}

bool CUSBConnection::checkUsbDescription(libusb_device* dev) {
	if (dev != nullptr) {
		struct libusb_device_descriptor desc;
		libusb_device_handle* handle = nullptr;
		char description[256];
		unsigned char string[256];
		int ret;
		uint8_t i;

		ret = libusb_get_device_descriptor(dev, &desc);
		if (ret < 0) {
			PLOGI.printf("failed to get device descriptor");
		}

		ret = libusb_open(dev, &handle);
		if (ret != LIBUSB_SUCCESS) {
			PLOGI.printf("Failed to open device: %s", libusb_error_name(ret));
		}

		bool found = false;
		if (desc.iManufacturer) {
			ret = libusb_get_string_descriptor_ascii(handle, desc.iManufacturer, string, sizeof(string));
			if (ret > 0) {
				if (strcmp((const char*)string, "Dr. Fritz Faulhaber GmbH") == 0) {
					found = true;
				}
			}
			else {
				PLOGI.printf("Failed to get manufacturer string: %s", libusb_error_name(ret));
			}
		}

		if (handle != nullptr) {
			libusb_close(handle);
			return true;
		}
	}

	return false;
}
