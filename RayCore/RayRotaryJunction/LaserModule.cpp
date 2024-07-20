#include "LaserModule.h"
#include "RS232Comm.h"
#include "DelayLineComm.h"
#include "Utility.h"
#include <chrono>

extern struct min_context min_ctx;

CLaserModule::CLaserModule() {
	m_pThread = nullptr;
	m_hComTx = INVALID_HANDLE_VALUE;
	m_prevPosition[0] = 0;
	m_prevPosition[1] = 0;
	m_lastTargetPosition[0] = -1;
	m_lastTargetPosition[1] = -1;
}
CLaserModule::~CLaserModule() {
	Close();
}

UINT CLaserModule::threadReadStatus(LPVOID param) {
	CLaserModule* pModule = (CLaserModule*)param;

	while (pModule->m_pThread->isRun)
	{
		pModule->readStatus();
		Sleep(1);
	}

	return NOERROR;
}

void CLaserModule::readStatus() {
	char msgIn[MAX_PATH] = { 0 };
	size_t buf_len = 0;

	COMSTAT st;
	DWORD errmask = 0, eventmask = EV_RXCHAR, ret;
	OVERLAPPED ov;
	int r;

	// first, request comm event when characters arrive
	if (!SetCommMask(m_hComTx, EV_RXCHAR)) return;
	// look if there are characters in the buffer already
	if (!ClearCommError(m_hComTx, &errmask, &st)) return;

	DWORD bytesRead;
	if (st.cbInQue > 0)
	{
		buf_len = inputFromPort(&m_hComTx, msgIn, MAX_PATH);
	}
	else
	{
		buf_len = 0;
	}
	min_poll(&min_ctx, (uint8_t*)msgIn, (uint32_t)buf_len);
}

bool CLaserModule::Open(tstring strPort) {
	COMMTIMEOUTS timeout{}; // A commtimeout struct variable

	Close();

	// change COM Port Format
	size_t offset = strPort.rfind(L"COM");
	tstring strPortNum = strPort.substr(offset + 3);
	wchar_t strCOMPort[MAX_PATH];
	wsprintf(strCOMPort, L"\\\\.\\COM%d", _wtoi(strPortNum.c_str()));
	PLOGI.printf(L"Connect to %s", strCOMPort);

	initPort(&m_hComTx, strCOMPort, 500000, 8, timeout);
	if (!IsOpen()) return false;

	min_init_context(&m_ctx, 0);
	m_ctx.cb = this;
	min_ctx = m_ctx;

	CUtility::StartThread(threadReadStatus, m_pThread, this);

	const uint32_t accTime = 250000;
	const uint32_t velocity = 19200;
	delay_line_Set_Acc_Time((uint8_t) MotorIndex::Polarization, accTime);
	delay_line_Set_Acc_Time((uint8_t) MotorIndex::DelayLine, accTime);
	delay_line_Set_Velocity((uint8_t)MotorIndex::Polarization, velocity);
	delay_line_Set_Velocity((uint8_t)MotorIndex::DelayLine, velocity);

	delay_line_GetDelayLineObj(1); //NEED SET (1) AT BEGIN TO SET UP MAINBOARD TO  CONTINOUS MODE (MEAN SEND ALL DATA OF MAINBOARD CONTINUOUS).

	return true;
}
bool CLaserModule::IsOpen() {
	return (m_hComTx != INVALID_HANDLE_VALUE);
}
void CLaserModule::Close() {
	CUtility::StopThread(m_pThread);

	if (IsOpen()) {
		SetVOA(0);
		SetVLD(0);
		CloseHandle(m_hComTx);
		m_hComTx = INVALID_HANDLE_VALUE;
	}
}

bool CLaserModule::IsMoving(MotorIndex idx) {
	if (idx != MotorIndex::DelayLine && idx != MotorIndex::Polarization) return false;

	int index = (int)idx - 1;
	int actualPosition = (index == 0) ? m_RAM.marshall.position_motor1_actual : m_RAM.marshall.position_motor2_actual;

	return (m_prevPosition[index] != actualPosition);
}
bool CLaserModule::MoveAbsolute(MotorIndex idx, int nPosition) {
	if (!IsOpen()) return false;
	if (idx != MotorIndex::DelayLine && idx != MotorIndex::Polarization) return false;

	PLOGI.printf("Move Motor #%d - %d", idx, nPosition);
	delay_line_Move_single_axis_abs_pos((uint8_t) idx, nPosition);

	m_lastTargetPosition[(int)idx - 1] = nPosition;

	return true;
}
int CLaserModule::MoveRelative(MotorIndex idx, int nOffset) {
	if (idx != MotorIndex::DelayLine && idx != MotorIndex::Polarization) return 0;

	int index = (int)idx - 1;
	int actualPosition = (index == 0) ? m_RAM.marshall.position_motor1_actual : m_RAM.marshall.position_motor2_actual;

	int nPosition = m_lastTargetPosition[(int)idx - 1] + nOffset;

	MoveAbsolute(idx, nPosition);

	return nPosition;
}
void CLaserModule::Home(int nPosition, int nTimeout) {
	delay_line_Move_single_axis_abs_pos((int) MotorIndex::DelayLine, nPosition);
	int index = ((int)MotorIndex::DelayLine) - 1;
	bool sendStop = false;

	int pos = (index == 0) ? m_RAM.marshall.position_motor1_actual : m_RAM.marshall.position_motor2_actual;
	for (int i = 0; i < nTimeout / 10; i++) {
		if (m_RAM.marshall.input_sensor.marshall.U4 == 0) {
			if (!sendStop) {
				pos = (index == 0) ? m_RAM.marshall.position_motor1_actual : m_RAM.marshall.position_motor2_actual;
				PLOGI.printf("Stop! U4: %d - pos: %d", m_RAM.marshall.input_sensor.marshall.U4, pos);

				delay_line_SetStop((int)MotorIndex::DelayLine);
				sendStop = true;
			}
			else {
				delay_line_ClearPosition((int)MotorIndex::DelayLine);
				pos = (index == 0) ? m_RAM.marshall.position_motor1_actual : m_RAM.marshall.position_motor2_actual;

				if (pos == 0) break;
				PLOGI.printf("ClearPosition - pos: %d", pos);
			}
		}
		Sleep(10);
	}
}
void CLaserModule::SetVLD(unsigned short nValue) {
	if (!IsOpen()) return;
	nValue = (nValue < 0) ? 0 : (nValue > MAX_VOLTAGE_RAW_VALUE) ? MAX_VOLTAGE_RAW_VALUE : nValue;

	PLOGI.printf("Visible Laser Power - %d", nValue);
	delay_line_Set_voltage_ld(nValue);
	m_nVLDValue = nValue;
	PLOGI.printf("Visible Laser Power done");
}
void CLaserModule::SetVOA(unsigned short nValue) {
	if (!IsOpen()) return;
	nValue = (nValue < 0) ? 0 : (nValue > MAX_VOLTAGE_RAW_VALUE) ? MAX_VOLTAGE_RAW_VALUE : nValue;

	PLOGI.printf("VOA Power - %d", nValue);
	delay_line_Set_voltage_voa(nValue);
	m_nVOAValue = nValue;

}

// CALLBACK. Handle incoming MIN frame
void CLaserModule::min_application_handler(uint8_t min_id, uint8_t const* min_payload, uint8_t len_payload, uint8_t port) {
	static int prevU2 = 0;
	static int prevU4 = 0;
	static int prevU7 = 0;

	switch (min_id)
	{
	case CTRL_CODE_GET_DELAY_LINE_OBJ:
	{
		memcpy(m_RAM.unmarshall, min_payload, len_payload);

#if 1
		if (prevU2 != m_RAM.marshall.input_sensor.marshall.U2 ||
			prevU4 != m_RAM.marshall.input_sensor.marshall.U4 ||
			prevU7 != m_RAM.marshall.input_sensor.marshall.U7) {
			PLOGI.printf("U4: %d U7: %d U2: %d",
				m_RAM.marshall.input_sensor.marshall.U4,
				m_RAM.marshall.input_sensor.marshall.U7,
				m_RAM.marshall.input_sensor.marshall.U2);
		}
		prevU2 = m_RAM.marshall.input_sensor.marshall.U2;
		prevU4 = m_RAM.marshall.input_sensor.marshall.U4;
		prevU7 = m_RAM.marshall.input_sensor.marshall.U7;
#endif
#if 1
		if (m_prevPosition[0] != m_RAM.marshall.position_motor1_actual) {
			PLOGI.printf(" POS1 : %d", m_RAM.marshall.position_motor1_actual);
		}
		m_prevPosition[0] = m_RAM.marshall.position_motor1_actual;
		m_prevPosition[1] = m_RAM.marshall.position_motor2_actual;
#endif
	}
	break;
	case CTRL_CODE_GET_ACTUAL_POS:
	{
		Move_single_axis_abs_pos_u data;
		memcpy(data.unmarshall, min_payload, len_payload);

		if (data.marshall.idMotor == 1) {
			m_prevPosition[0] = m_RAM.marshall.position_motor1_actual;
			m_RAM.marshall.position_motor1_actual = data.marshall.pos;
		}
		if (data.marshall.idMotor == 2) {
			m_prevPosition[1] = m_RAM.marshall.position_motor2_actual;
			m_RAM.marshall.position_motor2_actual = data.marshall.pos;
		}
	}
	break;
	case CTRL_CODE_GET_ACTUAL_VOLTAGE_VOA_RAW:
	{
		convertData_u<uint16_t> data;
		memcpy(data.unmarshall, min_payload, len_payload);

		m_RAM.marshall.voltage_VOA_actual = data.marshall;
	}
	break;
	case CTRL_CODE_GET_ACTUAL_VOLTAGE_LD_RAW:
	{
		convertData_u<uint16_t> data;
		memcpy(data.unmarshall, min_payload, len_payload);

		m_RAM.marshall.voltage_LD_actual = data.marshall;
	}
	break;
	case CTRL_CODE_GET_STATUS:
	{
		ObjDelayStatus_u data;
		memcpy(data.unmarshall, min_payload, len_payload);

		m_RAM.marshall.status = data;
	}
	break;
	case CTRL_CODE_PING:
	{
		PLOGI.printf("PING Received.");
	}
	break;
	default:
		break;
	}
}

uint32_t CLaserModule::min_time_ms(void) {
	static auto init_time = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()); //dont change this value

	std::chrono::milliseconds ms = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch() - init_time);
	return ms.count();
}

uint16_t CLaserModule::min_tx_space(uint8_t port) {
	// Ignore 'port' because we have just one context. But in a bigger application
	// with multiple ports we could make an array indexed by port to select the serial
	// port we need to use.
	//uint16_t n = UART_USE.availableForWrite();

	uint16_t n = 256;

	return n;
}

void CLaserModule::min_tx_byte(uint8_t port, uint8_t byte) {
	// Ignore 'port' because we have just one context.
	outputToPort(&m_hComTx, &byte, 1);
}

void CLaserModule::min_tx_start(uint8_t port) {}
void CLaserModule::min_tx_finished(uint8_t port) {}