#pragma once
#include "Config.h"
#include "CommonProtocol.h"
#include "MotorController.h"
#include <vector>

#define MAX_VOLTAGE_RAW_VALUE			4095
#define CM_SM_SPEED_DEFAULT				1000
#define CM_SM_SPEED_MAX					9000
#define CM_SM_SPEED_AUTO				2500
#define DELAYLINE_BACKWARD_POSITION		(-50)
#define DELAYLINE_FORWARD_POSITION		(50)
#define DELAY_LINE_HOMING_WORKS			1 
#define DELAY_LINE_UPPER_END_POSITION	90000

class CLaserModule
	: public CMotorController,
	public IStepMotorAction,
	public ICommonProtocol
{
private:
	int m_nStepPosition[2];
	int m_nStepSpeed[2];
	unsigned short m_nVOA, m_nVLD;

	int m_nActualPosition[2];
	bool m_isSMMoving[2];
	bool m_bPhotoSensor[6];

	SFWVersionInfo m_fwVersionInfo;
	eFWDownloadState m_fwDownloadState;
	int m_fwDownloadProgress;
	int m_fwDownloadIndex;
	UINT m_fwDownloadSequence;
	std::vector<BYTE> m_fwImageBuffer;
	FWProgressCallback m_fwProgressCallback;
	FWStatusCallback m_fwStatusCallback;

public:
	CLaserModule();
	virtual ~CLaserModule();

	virtual bool Connect(void* strPort);
	virtual void Disconnect();

	virtual bool IsMoving(eStepMotorIndex idxMotor);
	virtual bool ReadPosition();
	virtual bool Current(eStepMotorIndex idxMotor, int posStep);
	virtual bool Move(eStepMotorIndex idxMotor, int posStep, bool delay = false, char sensor = 0);
	virtual bool Set(eStepMotorIndex idxMotor, int velStep);

	int GetPosition(eStepMotorIndex idxMotor) { return m_nActualPosition[(int)idxMotor - 1]; }
	int MoveRelative(eStepMotorIndex idxMotor, int nOffset);
	void SetVOA(unsigned short voa);
	void SetVLD(unsigned short vld);
	void PrintPhotoSensor();

	bool AutoStatePeriod(USHORT interval);
	bool StopStepMotors();

	bool StartFWDownload(const char* filepath);
	bool CancelFWDownload();
	void SetFWProgressCallback(FWProgressCallback callback);
	void SetFWStatusCallback(FWStatusCallback callback);
	SFWVersionInfo& GetFWVersionInfo();

protected:
	static UINT threadReadPacket(LPVOID param);
	void initSetting();
	void parseAutoReportPacket(BYTE* packet, int size);
	void parseSMPacket(BYTE* packet, int size);
	void setVOAVLD();
	virtual void handlePacket();

	void RxPacketGetVersion(BYTE* buff);
	void RxPacketFWDownload(BYTE* buff);
	bool ValidateFWFile(const char* filepath, SFirmwareMetadata& metadata);
	bool LoadFirmwareData(const char* filepath);
	bool SendFWDownloadStart();
	bool SendFWDataChunk();
	bool SendFWDownloadEnd(bool success);
};

