#include "DelayLineComm.h"
#include <stdint.h>
#include "min.h"
#include "MemoryStruct.h"

// global variable
struct min_context min_ctx;

//--------------------------------------
void delay_line_Move_single_axis_abs_pos(uint8_t idMotor, int32_t pos)
{
	Move_single_axis_abs_pos_u dataConfig;
	dataConfig.marshall.idMotor = idMotor;
	dataConfig.marshall.pos = pos;

	min_send_frame(&min_ctx, CTRL_CODE_MOVE_SINGLE_AXIS_ABS_POS, (const uint8_t*)(dataConfig.unmarshall), sizeof(Move_single_axis_abs_pos_t));
	min_send_frame(&min_ctx, CTRL_CODE_MOVE_SINGLE_AXIS_ABS_POS, (const uint8_t*)(dataConfig.unmarshall), sizeof(Move_single_axis_abs_pos_t));
}

void delay_line_Set_voltage_voa(uint16_t value)
{
	convertData_u<uint16_t> data;
	data.marshall = value;

	min_send_frame(&min_ctx, CTRL_CODE_SET_VOLTAGE_VOA_RAW, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint16_t>));
	min_send_frame(&min_ctx, CTRL_CODE_SET_VOLTAGE_VOA_RAW, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint16_t>));
}

void delay_line_Set_voltage_ld(uint16_t value)
{
	convertData_u<uint16_t> data;
	data.marshall = value;

	min_send_frame(&min_ctx, CTRL_CODE_SET_VOLTAGE_LD_RAW, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint16_t>));
	min_send_frame(&min_ctx, CTRL_CODE_SET_VOLTAGE_LD_RAW, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint16_t>));
}

void delay_line_Set_voltage_VOAwLD(uint16_t valueVOA, uint16_t valueLD)
{
	Set_voltage_VOAwLD_u data;
	data.marshall.voltLDraw = valueLD;
	data.marshall.voltVOAraw = valueVOA;

	min_send_frame(&min_ctx, CTRL_CODE_SET_VOLTAGE_VOA_LD, (const uint8_t*)(data.unmarshall), sizeof(Set_voltage_VOAwLD_t));
}

void delay_line_Set_Acc_Time(uint8_t idMotor, uint32_t value)
{
	Set_ACC_Time_u data;
	data.marshall.idMotor = idMotor;
	data.marshall.ppss = value;

	min_send_frame(&min_ctx, CTRL_CODE_SET_ACC_TIME, (const uint8_t*)(data.unmarshall), sizeof(Set_ACC_Time_t));
}

void delay_line_Set_Velocity(uint8_t idMotor, uint16_t value)
{
	Set_Velocity_u data;
	data.marshall.idMotor = idMotor;
	data.marshall.velocity = value;

	min_send_frame(&min_ctx, CTRL_CODE_SET_VELOCITY, (const uint8_t*)(data.unmarshall), sizeof(Set_Velocity_t));
}

void delay_line_MoveToOrigin(uint8_t idMotor, int8_t value)
{
	Move_to_org_u data;
	data.marshall.idMotor = idMotor;
	data.marshall.dir = value;

	min_send_frame(&min_ctx, CTRL_CODE_MOVE_2_ORG, (const uint8_t*)(data.unmarshall), sizeof(Move_to_org_t));
}

void delay_line_ClearPosition(uint8_t idMotor)
{
	convertData_u<uint8_t> data;
	data.marshall = idMotor;

	min_send_frame(&min_ctx, CTRL_CODE_CLEAR_POSITION, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint8_t>));
	min_send_frame(&min_ctx, CTRL_CODE_CLEAR_POSITION, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint8_t>));
}

void delay_line_SetStop(uint8_t idMotor)
{
	convertData_u<uint8_t> data;
	data.marshall = idMotor;
	min_send_frame(&min_ctx, CTRL_CODE_STOP, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint8_t>));
	min_send_frame(&min_ctx, CTRL_CODE_STOP, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint8_t>));
}

void delay_line_GetActualPos(uint8_t idMotor)
{
	convertData_u<uint8_t> data;
	data.marshall = idMotor;

	min_send_frame(&min_ctx, CTRL_CODE_GET_ACTUAL_POS, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint8_t>));
}

void delay_line_GetActualVoltageVOA()
{
	min_send_frame(&min_ctx, CTRL_CODE_GET_ACTUAL_VOLTAGE_VOA_RAW, NULL, 0);
}

void delay_line_GetActualVoltageLD()
{
	min_send_frame(&min_ctx, CTRL_CODE_GET_ACTUAL_VOLTAGE_LD_RAW, NULL, 0);
}

void delay_line_GetStatus()
{
	min_send_frame(&min_ctx, CTRL_CODE_GET_STATUS, NULL, 0);
}

void delay_line_GetDelayLineObj(uint8_t value)
{
	convertData_u<uint8_t> data;
	data.marshall = value; //1 mean continuous | 0 mean once time

	min_send_frame(&min_ctx, CTRL_CODE_GET_DELAY_LINE_OBJ, (const uint8_t*)(data.unmarshall), sizeof(convertData_u<uint8_t>));
}

void delay_line_Ping()
{
	min_send_frame(&min_ctx, CTRL_CODE_PING, NULL, 0);
}

//--------------------------------------------------
