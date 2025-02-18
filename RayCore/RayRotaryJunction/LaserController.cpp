#include "LaserController.h"
#include "Config.h"

CLaserController* CLaserController::pInstance = NULL;

CLaserController::CLaserController() {
	APTTYPE aptType;
	APTTYPEQUALIFIER aptQualifier;

	/*hr = CoInitialize(NULL);
	PLOGI.printf("Hr = %x", hr);*/
	HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);

	PLOGI.printf("Hr = %x", hr);
	CLSID myCLSID;
	LPOLESTR clsidString = nullptr;
	hr = StringFromCLSID(myCLSID, &clsidString);
	wprintf(L"CLSID: %s\n", clsidString);
	PLOGI.printf("Hr = %x", hr); 

	return;

	Sleep(2000);
	m_pAxsunOCTControl = IAxsunOCTControlPtr(__uuidof(struct AxsunOCTControl));

	// open network interface and wait 2 seconds (time for connection to be established)
	unsigned long retvallong = m_pAxsunOCTControl->StartNetworkControlInterface();		// retvallong = 0 if successful or = 1047 if the network interface is already open (possibly from a different application)
	PLOGI.printf("StartNetworkControlInterface - %ld", retvallong);
	Sleep(2000);

	// Enumerate the device list (redo this step whenever devices are connected or disconnected)
	// More robust architectures would occasionally poll for device list changes or utilize the "OCTDeviceConnectOrDisconnectEvent" callback to re-enumerate devices
	m_numDevices = enumerateDevices(m_pDeviceList, m_pAxsunOCTControl);
	PLOGI.printf("enumerateDevices - %ld", m_numDevices);
}
CLaserController* CLaserController::GetInstance() {
	if (pInstance == NULL) {
		pInstance = new CLaserController();
	}
	return pInstance;
}
CLaserController::~CLaserController() {
	unsigned long retvallong = 0;
	// close network interface before exiting
	retvallong = m_pAxsunOCTControl->StopNetworkControlInterface();
	CoUninitialize();
}

int CLaserController::LaserOnOff(bool on) {
	unsigned long retvallong = 0;
	// initialize a simple static array to be used as a user device list
	// (This can be done a variety of ways, such as a linked list or other dynamic array if desired.)
	VARIANT_BOOL isConnected = 0;

	// Stop Laser Emission
	isConnected = m_pAxsunOCTControl->ConnectToOCTDevice(searchDeviceList(AXSUN_LASER_DEVICE, m_pDeviceList));		// search device list and connect to laser
	if (isConnected == -1) {
		if (on) {
			PLOGI.printf("Laser On");
			retvallong = m_pAxsunOCTControl->StartScan();
		}
		else {
			PLOGI.printf("Laser Off");
			retvallong = m_pAxsunOCTControl->StopScan();
		}
	}
	else {
		PLOGI.printf("Laser not connected");
	}

	return NOERROR;
}

long CLaserController::searchDeviceList(long whichDevice, unsigned long* myDeviceList) {
	// this function searches the user device list for the first device number matching whichDevice and returns its index within the list if matched
	// if the desired device is not found in the list, the function returns -1
	long deviceIndex = 0;
	while (1) {
		if (myDeviceList[deviceIndex] == whichDevice)
			return deviceIndex;
		else if (deviceIndex >= AXSUN_MAX_DEVICES)
			return -1;
		else
			deviceIndex++;
	}
}

long CLaserController::enumerateDevices(unsigned long* myDeviceList, IAxsunOCTControlPtr pAxsunOCTControl) {
	// query the number of Axsun devices successfully connected to the Control library (includes USB and Ethernet network devices)
	long numDev = pAxsunOCTControl->GetNumberOfOCTDevicesPresent();

	// allocate some temporary string memory to be used later in GetSystemType() function call
	BSTR systemTypeString = SysAllocString(L"");

	// enumerate connected Axsun devices by looping through numDev devices and storing each device type in the myDeviceList array
	VARIANT_BOOL isConnected = 0;
	unsigned long retvallong = 0;
	for (long i = 0; i < numDev; i++) {
		isConnected = pAxsunOCTControl->ConnectToOCTDevice(i);	// isConnected should be -1 if connected successfully to device i
		if (isConnected == -1)
			retvallong = pAxsunOCTControl->GetSystemType(&myDeviceList[i], &systemTypeString);	// populate ith element of device list array with information about the board type (e.g. DAQ = 42 vs. Laser = 40)
	}

	// free string memory
	SysFreeString(systemTypeString);

	return numDev;
}