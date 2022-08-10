#include "pch.h"
#include "MotorController.h"
#include "Utility.h"

CMotorController* CMotorController::pInstance = NULL;

CMotorController::CMotorController() {
	int result = libusb_init(NULL);

	m_initUsb = (result >= 0) ? true : false;
	m_initMotor = false;
	m_isRun = false;
	m_hUsbHandle = NULL;

	m_pThread = NULL;
}

CMotorController* CMotorController::GetInstance() {
	if (pInstance == NULL) {
		pInstance = new CMotorController();
	}
	return pInstance;
}

CMotorController::~CMotorController() {	
	Disconnect();
	if (m_initUsb) libusb_exit(NULL);
}

bool CMotorController::Connect() {
	libusb_device** pUsbDevices;

	ssize_t cnt = libusb_get_device_list(NULL, &pUsbDevices);
	if (cnt < 0) return false;

	m_initMotor = false;
	for (int i = 0; pUsbDevices[i]; ++i) {
		if (checkUsbDescription(pUsbDevices[i])) {
			libusb_open(pUsbDevices[i], &m_hUsbHandle);
			libusb_claim_interface(m_hUsbHandle, 1);
			m_initMotor = true;
			break;
		}
	}

	if (pUsbDevices) {
		libusb_free_device_list(pUsbDevices, 1);
	}

	if (m_initMotor) {
		BOOL result = FALSE;
		result = CUtility::StartThread(threadReadMotor, m_pThread, (LPVOID)this);

		if (result == FALSE) {
			Disconnect();
			m_initMotor = false;
		}
	}

	return m_initMotor;
}

void CMotorController::Disconnect() {
	CUtility::StopThread(m_pThread);

	if (m_hUsbHandle) {
		libusb_close(m_hUsbHandle);
		m_hUsbHandle = NULL;
	}
}

bool CMotorController::SwitchOn() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_CONTROLWORD, MOTOR_DATA_SWITCH_ON, 2, packet, packetLength);
	result = writeMotor(packet, packetLength);

	getMotorPacket(MOTOR_INDEX_CONTROLWORD, MOTOR_DATA_ENABLE_OPERATION, 2, packet, packetLength);
	result |= writeMotor(packet, packetLength);

	if (result) {
		StopMotor();
	}

	return result;
}

/*
* @param nVelocity : input velocity value (limits apply if needed)
*/
bool CMotorController::PerfomRun(int &nVelocity) {
	char strCommand[MAX_PATH];	BYTE packet[MAX_PATH];
	int packetLength = 0;

	if (nVelocity < 0) {
		// negative
		nVelocity = (nVelocity < -50000) ? -50000 : (nVelocity > -100) ? -100 : nVelocity;
	}
	else {
		// positive
		nVelocity = (nVelocity > 50000) ? 50000 : (nVelocity < 100) ? 100 : nVelocity;
	}

	getMotorPacket(MOTOR_INDEX_TARGETVELOCITY, nVelocity, 4, packet, packetLength);
	bool result = writeMotor(packet, packetLength);

	m_isRun = result;
	return result;
}
bool CMotorController::StopMotor() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_TARGETVELOCITY, 0, 4, packet, packetLength);
	result = writeMotor(packet, packetLength);

	m_isRun = false;
	return result;
}
bool CMotorController::SwitchOff() {
	BYTE packet[MAX_PATH];
	int packetLength = 0;
	bool result = false;

	getMotorPacket(MOTOR_INDEX_CONTROLWORD, MOTOR_DATA_SWITCH_OFF, 2, packet, packetLength);
	result = writeMotor(packet, packetLength);

	return result;
}

UINT CMotorController::threadReadMotor(LPVOID pParam) {
	CMotorController* pMotorController = (CMotorController*)pParam;
	libusb_device_handle* hUsbHandle = pMotorController->m_hUsbHandle;
	BYTE recvBuf[MAX_PATH];

	while (pMotorController->m_pThread->isRun) {
		int nRead = 0;
		int err = libusb_bulk_transfer(hUsbHandle, USB_ENDPOINT_IN, recvBuf, sizeof(recvBuf), &nRead, USB_TIMEOUT);
		if (err == 0) {
			printf(" [Motor] read packet : ");
			for (int i = 0; i < nRead; i++) {
				printf("0x%02x ", recvBuf[i]);
			}
			printf("\n");
		}
		Sleep(100);
	}

	return NOERROR;
}

bool CMotorController::checkUsbDescription(libusb_device* dev) {
	if (dev != NULL) {
		struct libusb_device_descriptor desc;
		libusb_device_handle* handle = NULL;
		char description[256];
		unsigned char string[256];
		int ret;
		uint8_t i;

		ret = libusb_get_device_descriptor(dev, &desc);
		if (ret < 0) {
			fprintf(stderr, "failed to get device descriptor");
			return false;
		}

		ret = libusb_open(dev, &handle);
		if (LIBUSB_SUCCESS == ret) {
			if (desc.iManufacturer) {
				ret = libusb_get_string_descriptor_ascii(handle, desc.iManufacturer, string, sizeof(string));
				if (strcmp((const char*)string, "Dr. Fritz Faulhaber GmbH") == 0) {
					libusb_close(handle);
					return true;
				}
			}
		}
	}

	return false;
}

BYTE CMotorController::calcCRCByte(BYTE u8Byte, BYTE u8CRC){
	const BYTE polynomial = 0xD5;
	u8CRC = u8CRC ^ u8Byte;
	for (BYTE i = 0; i < 8; i++)
	{
		if (u8CRC & 0x01) {
			u8CRC = (u8CRC >> 1) ^ polynomial;
		}
		else {
			u8CRC >>= 1;
		}
	}
	return u8CRC;
}

bool CMotorController::writeMotor(BYTE* packet, int size) {
	if (m_hUsbHandle == NULL) return false;

	int writeSize = 0;
	int ret = libusb_bulk_transfer(m_hUsbHandle, USB_ENDPOINT_OUT, packet, size, &writeSize, USB_TIMEOUT);
	//Error handling
	switch (ret) {
	case 0:
		printf("send %d bytes to device\n", size);
		return true;
	case LIBUSB_ERROR_TIMEOUT:
		printf("ERROR in bulk write: %d Timeout\n", ret);
		break;
	case LIBUSB_ERROR_PIPE:
		printf("ERROR in bulk write: %d Pipe\n", ret);
		break;
	case LIBUSB_ERROR_OVERFLOW:
		printf("ERROR in bulk write: %d Overflow\n", ret);
		break;
	case LIBUSB_ERROR_NO_DEVICE:
		printf("ERROR in bulk write: %d No Device\n", ret);
		break;
	default:
		printf("ERROR in bulk write: %d\n", ret);
		break;
	}

	return false;
}

void CMotorController::getMotorPacket(unsigned short command, unsigned int data, unsigned int dataSize, BYTE* packet, int& packetLength) {
	const BYTE sof = (BYTE)0x53;
	BYTE length = 0x07;	// length (1byte) + node (1byte) + mode (1byte) + index (2byte) + subindex (1byte) + crc (1byte)
	BYTE node = 0x01;	// 0x00 ~ 0xff
	BYTE mode = 0x02;	// 0x02 : write, 0x01 : read
	unsigned short index = command;
	const BYTE subindex = 0x00;
	const BYTE eof = (BYTE)0x45;

	length += dataSize;
	packetLength = 0;

	packet[packetLength++] = sof;
	packet[packetLength++] = length;
	packet[packetLength++] = node;
	packet[packetLength++] = mode;
	packet[packetLength++] = index & 0xFF;
	packet[packetLength++] = (index >> 8) & 0xFF;
	packet[packetLength++] = subindex;
	if (dataSize == 2) {
		*(unsigned short*)(packet + packetLength) = (unsigned short)data;
	}
	else {
		*(unsigned int*)(packet + packetLength) = (unsigned int)data;
	}
	packetLength += dataSize;

	BYTE crc = 0xFF;	//start value
	for (int i = 1; i < length; i++) {
		crc = calcCRCByte(packet[i], crc);
	}
	packet[packetLength++] = crc;
	packet[packetLength++] = eof;
}