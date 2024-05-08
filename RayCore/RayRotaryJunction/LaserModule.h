#pragma once
#include "Config.h"
#include "min.h"
#include "MemoryStruct.h"

#define MAX_VOLTAGE_RAW_VALUE	4095

enum class MotorIndex {
	DelayLine = 1,
	Polarization = 2,
};

class CThread;
class CLaserModule : public min_callback
{
private:
	CThread* m_pThread;

	struct min_context m_ctx;

	HANDLE m_hComTx;
	ObjDelayLineData_u m_RAM;

	BYTE m_pReadBuffer[MAX_PATH];
	int m_prevPosition[2];
	int m_lastTargetPosition[2];
	int m_nVLDValue;
	int m_nVOAValue;

private:
	static UINT threadReadStatus(LPVOID param);
	void readStatus();

public:
	CLaserModule();
	virtual ~CLaserModule();

	// Common
	bool Open(tstring strPort);
	bool IsOpen();
	void Close();

	// Step Motor (Delay-line & Polarization Control)
	bool IsMoving(MotorIndex idx);
	int GetPosition(MotorIndex idx) { return (idx == MotorIndex::DelayLine) ? m_RAM.marshall.position_motor2_actual : m_RAM.marshall.position_motor1_actual; }
	bool MoveAbsolute(MotorIndex idx, int nPosition);
	int MoveRelative(MotorIndex idx, int nOffset);

	// VLD
	void SetVLD(unsigned short nValue);
	
	// VOA
	void SetVOA(unsigned short nValue);

public:
	// CALLBACK. Handle incoming MIN frame
	virtual void min_application_handler(uint8_t min_id, uint8_t const* min_payload, uint8_t len_payload, uint8_t port);

	// CALLBACK. Must return current time in milliseconds.
	// Typically a tick timer interrupt will increment a 32-bit variable every 1ms (e.g. SysTick on Cortex M ARM devices).
	virtual uint32_t min_time_ms(void);

	// CALLBACK. Must return current buffer space in the given port. Used to check that a frame can be
	// queued.
	virtual uint16_t min_tx_space(uint8_t port);

	// CALLBACK. Send a byte on the given line.
	virtual void min_tx_byte(uint8_t port, uint8_t byte);

	// CALLBACK. Indcates when frame transmission is finished; useful for buffering bytes into a single serial call.
	virtual void min_tx_start(uint8_t port);
	virtual void min_tx_finished(uint8_t port);
};