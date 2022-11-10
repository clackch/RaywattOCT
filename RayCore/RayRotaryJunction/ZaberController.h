#pragma once
#include "Config.h"

/*
* Linear Stage Model : A-LSQ075B-E01
* Reference : https://www.zaber.com/protocol-manual?device=X-LSQ075B-E01&peripheral=N%2FA&version=7.29&protocol=ASCII#topic_physical_units
*/

#define DELAYLINE_BACKWARD_POSITION	(-1)
#define DELAYLINE_FORWARD_POSITION		(1)
#define ZABER_MICROSTEP_SIZE				0.49609375f							// um
#define ZABER_SCALE_MM_TO_POSITION			(1000.f / ZABER_MICROSTEP_SIZE)		// 1000um = 1mm
#define ZABER_SCALE_MMS_TO_VELOCITY			(1.6384f / ZABER_MICROSTEP_SIZE)
#define ZABER_SCALE_MM_TO_ROTATE			12800

typedef enum {
	ZABER_TYPE_PULLBACK = 0,
	ZABER_TYPE_DELAYLINE,
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

