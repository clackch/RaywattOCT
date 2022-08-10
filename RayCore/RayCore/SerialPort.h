#pragma once

class CSerialPort {
public:     
	CSerialPort();     
	~CSerialPort(); 
private:     
	bool	m_isOpen;
	HANDLE  m_hComm;    
	DCB     m_dcb;     
	COMMTIMEOUTS m_CommTimeouts;     
	BOOL    m_bPortReady;     
	bool    m_bWriteRC;     
	bool    m_bReadRC;     
	DWORD   m_iBytesWritten;     
	DWORD   m_iBytesRead;     
	DWORD   m_dwBytesRead;     
	BYTE    m_nWriteData[256];
	//BOOL ch_state[4];
	//BOOL comm_status = OFF;
public:     
	bool IsOpen() { return m_isOpen; }
	bool OpenPort(tstring portname);
	void ClosePort();     
	bool ReadByte(BYTE &resp);     
	bool ReadByte(BYTE* &resp, UINT size);
	bool WriteByte(BYTE *pBuff);
	bool WriteByte(BYTE *pBuff, UINT nByte);
	int SetLevel(int nChannel, int nValue);     
	int LightOnOff(int nChannel, bool bOn);     
	bool SetCommunicationTimeouts(DWORD ReadIntervalTimeout, DWORD ReadTotalTimeoutMultiplier, DWORD ReadTotalTimeoutConstant, DWORD WriteTotalTimeoutMultiplier, DWORD WriteTotalTimeoutConstant);
	bool ConfigurePort(DWORD BaudRate, BYTE ByteSize, DWORD fParity, BYTE  Parity, BYTE StopBits);     
};