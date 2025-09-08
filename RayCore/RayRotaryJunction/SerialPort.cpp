#include "SerialPort.h" 

CSerialPort::CSerialPort() { 
	m_isOpen = false;
}     
CSerialPort::~CSerialPort() { } 

bool CSerialPort::OpenPort(tstring portname) {
	tstring filename = _T("//./") + portname;
	m_hComm = CreateFile(filename.c_str(),
		GENERIC_READ | GENERIC_WRITE,
		0,
		0,
		OPEN_EXISTING, 
		0,      
		0);    

	m_isOpen = (m_hComm != INVALID_HANDLE_VALUE) ? true : false;
	return m_isOpen;
}  

bool CSerialPort::ConfigurePort(DWORD BaudRate, BYTE ByteSize, DWORD fParity,
	BYTE Parity, BYTE StopBits) {  
	if ((m_bPortReady = GetCommState(m_hComm, &m_dcb)) == 0) 
	{       
		//MessageBox(L"GetCommState Error", L"Error", MB_OK + MB_ICONERROR);  
		CloseHandle(m_hComm);     
		m_isOpen = false;
		return false;    
	}      
	m_dcb.BaudRate = BaudRate;    
	m_dcb.ByteSize = ByteSize;    
	m_dcb.Parity = Parity;  
	m_dcb.StopBits = StopBits;
	m_dcb.fBinary = true;   
	m_dcb.fParity = fParity;   
	m_dcb.fOutX = false;   
	m_dcb.fInX = false;  
	m_dcb.fNull = false;   
	m_dcb.fAbortOnError = false; // true; // za_serial.c ¿¡¼­ false
	m_dcb.fOutxCtsFlow = false;  
	m_dcb.fOutxDsrFlow = false;  
	m_dcb.fDtrControl = DTR_CONTROL_ENABLE;  
	m_dcb.fDsrSensitivity = false;  
	m_dcb.fRtsControl = RTS_CONTROL_ENABLE;
	m_bPortReady = SetCommState(m_hComm, &m_dcb);   
	if (m_bPortReady == 0)   
	{   
		MessageBox((HWND)"SetCommState Error",NULL, L"Error", MB_OK + MB_ICONERROR);  
		CloseHandle(m_hComm);
		m_isOpen = false;
		return false;  
	}     
	return true; 
}  

bool CSerialPort::SetCommunicationTimeouts(DWORD ReadIntervalTimeout,  
	DWORD ReadTotalTimeoutMultiplier, DWORD ReadTotalTimeoutConstant, 
	DWORD WriteTotalTimeoutMultiplier, DWORD WriteTotalTimeoutConstant) { 
	if ((m_bPortReady = GetCommTimeouts(m_hComm, &m_CommTimeouts)) == 0)   
		return false;   
	m_CommTimeouts.ReadIntervalTimeout = ReadIntervalTimeout; 
	m_CommTimeouts.ReadTotalTimeoutConstant = ReadTotalTimeoutConstant;  
	m_CommTimeouts.ReadTotalTimeoutMultiplier = ReadTotalTimeoutMultiplier;   
	m_CommTimeouts.WriteTotalTimeoutConstant = WriteTotalTimeoutConstant;   
	m_CommTimeouts.WriteTotalTimeoutMultiplier = WriteTotalTimeoutMultiplier;   
	m_bPortReady = SetCommTimeouts(m_hComm, &m_CommTimeouts);   
	if (m_bPortReady == 0)  
	{   
		MessageBox((HWND)"StCommTimeouts function failed", NULL, L"Com Port Error", MB_OK + MB_ICONERROR);   
		CloseHandle(m_hComm);
		m_isOpen = false;
		return false;   
	}  
	return true; 
}  

bool CSerialPort::WriteByte(BYTE *pBuff) {   
	m_iBytesWritten = 0;  
	BYTE temp[9] = { NULL };    
	for (int i = 0; i < 9;i++)  
	{     
		temp[i] = pBuff[i];  
	}    
	if (WriteFile(m_hComm, temp, 9, &m_iBytesWritten, NULL) == 0)   
		return false;    
	else    
		return true; 
}

bool CSerialPort::WriteByte(BYTE *pBuff, UINT nByte) {
	m_iBytesWritten = 0;
	if (WriteFile(m_hComm, pBuff, nByte, &m_iBytesWritten, NULL) == 0) {
		PLOGI.printf("WriteFile Failed - 0x%x", GetLastError());
		return false;
	}
	else
		return true;
}

bool CSerialPort::ReadByte(BYTE &resp) {
	BYTE rx;  
	resp = 0;  
	DWORD dwBytesTransferred = 0;  
	if (ReadFile(m_hComm, &rx, 1, &dwBytesTransferred, 0))  
	{    
		if (dwBytesTransferred == 1)    
		{         
			resp = rx;        
			return true;  
		}    
	}   
	PLOGI.printf("ReadFile Failed - 0x%x", GetLastError());
	return false; 
} 

int CSerialPort::ReadByte(BYTE* &resp, UINT size) {  
	DWORD dwBytesTransferred = 0;    
	if (ReadFile(m_hComm, resp, size, &dwBytesTransferred, 0)) 
	{
		return dwBytesTransferred;
	}     
	return 0;
}    

void CSerialPort::ClosePort() { 
	CloseHandle(m_hComm);
	m_isOpen = false;
	return; 
}