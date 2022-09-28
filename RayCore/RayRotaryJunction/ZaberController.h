#pragma once
#include "Config.h"

#define INTERFEROMETER_BACKWARD_POSITION	(-1)
#define INTERFEROMETER_FORWARD_POSITION		(1)
#define ZABER_SCALE_MM_TO_POSITION			2015.74f
#define ZABER_SCALE_MMS_TO_VELOCITY			3302.6f
#define ZABER_SCALE_MM_TO_ROTATE			12800

typedef enum {
	ZABER_TYPE_PULLBACK = 0,
	ZABER_TYPE_INTERFEROMETER,
	ZABER_TYPE_NUM
}ZaberType;

class CSerialPort;
class CZaberController
{
private:
	CSerialPort* m_pZaber;
	BYTE m_pReadBuffer[MAX_PATH];

private:
	static CZaberController* pInstance[ZABER_TYPE_NUM];
	CZaberController();

public:
	static CZaberController* GetInstance(ZaberType type);
	virtual ~CZaberController();

	bool IsOpen();
	bool Open(tstring strPort);
	void Close();

	bool Idle();
	bool Move(int nPos);
	bool MoveRelative(int nPos);
	bool RotateRelative(int nPos);
	bool Pull(int nVelocity, int nDistance);
	bool SetSpeed(int nVelocity);
	bool GetZaberStatus();

private:
	bool sendCommand(const char* strCommand);
	void readZaber();
	bool parseZaberState(const char *strResponse, std::string& strState, int& nPos);
	int convertMMtoData(int nPos);
	int convertMMtoRotate(int nPos);
	int convertMMStoData(int nVelocity);
};

