#pragma once


#pragma region memory_map

typedef struct _photo_sensor_input
{
	uint16_t U4 : 1;
	uint16_t U2 : 1;
	uint16_t U7 : 1;
	uint16_t reserved : 13;
} PHOTO_SENSOR_INPUT_t;

typedef union _photo_sensor_u {
	PHOTO_SENSOR_INPUT_t marshall;
	uint16_t unmarshall[1];
} PHOTO_SENSOR_INPUT_u;

#define CTRL_CODE_MOVE_SINGLE_AXIS_ABS_POS              0X00
#define CTRL_CODE_SET_VOLTAGE_VOA_RAW                   0X01
#define CTRL_CODE_SET_VOLTAGE_LD_RAW                    0X03
#define CTRL_CODE_SET_VOLTAGE_VOA_LD                    0X04
#define CTRL_CODE_SET_ACC_TIME                          0X05
#define CTRL_CODE_SET_VELOCITY                          0X07
#define CTRL_CODE_MOVE_2_ORG                            0x08
#define CTRL_CODE_CLEAR_POSITION                        0x09
#define CTRL_CODE_STOP                                  0x0A

#define CTRL_CODE_GET_ACTUAL_POS                        0x30
#define CTRL_CODE_GET_ACTUAL_VOLTAGE_VOA_RAW            0x32
#define CTRL_CODE_GET_ACTUAL_VOLTAGE_LD_RAW             0x33
#define CTRL_CODE_GET_STATUS                            0x34
#define CTRL_CODE_GET_DELAY_LINE_OBJ                    0x35

#define CTRL_CODE_PING                                  0x36


typedef struct _ObjDelayStatus_t
{
    uint16_t status_error_all : 1;
    uint16_t status_hw_limit_motor1_pos : 1;
    uint16_t status_hw_limit_motor1_neg : 1;
    uint16_t status_hw_limit_motor2_pos : 1;
    uint16_t status_hw_limit_motor2_neg : 1;
    uint16_t status_org_returning_motor1 : 1;
    uint16_t status_org_returning_motor2 : 1;
    uint16_t status_inposition : 1;
    uint16_t status_error_output_voltage_VOA : 1;
    uint16_t status_error_output_voltage_LD : 1;
    uint16_t reserved2 : 6;
} ObjDelayStatus_t;

typedef union _ObjDelayStatus_u
{
    /* data */
    ObjDelayStatus_t marshall;
    uint16_t unmarshall[1];
}ObjDelayStatus_u;

typedef struct _ObjDelayLineData_t
{
    /* data */
    uint32_t param_acc_tim_1;
    uint16_t param_velocity_1;
    int32_t  position_motor1_sp;

    uint32_t param_acc_tim_2;
    uint16_t param_velocity_2;
    int32_t  position_motor2_sp;

    uint16_t voltage_VOA_sp; //raw
    uint16_t voltage_LD_sp;

    int32_t  position_motor1_actual;
    int32_t  position_motor2_actual;

    uint16_t voltage_VOA_actual;
    uint16_t voltage_LD_actual;

    PHOTO_SENSOR_INPUT_u input_sensor;
    ObjDelayStatus_u status;
} ObjDelayLineData_t;

typedef union _ObjDelayLineData_u
{
    /* data */
    ObjDelayLineData_t marshall;
    uint8_t unmarshall[sizeof(ObjDelayLineData_t)];
}ObjDelayLineData_u;

//-----------------------------------------------------------
typedef struct _ObjCtrlAction_t
{
    uint32_t isMoveSingleAxis1AbsPos : 1;
    uint32_t isMoveSingleAxis2AbsPos : 1;
    uint32_t isSetVoltageVOAraw : 1;
    uint32_t isSetVoltageLDraw : 1;
    uint32_t isSetAccTime1 : 1;
    uint32_t isSetAccTime2 : 1;
    uint32_t isSetVelocity1 : 1;
    uint32_t isSetVelocity2 : 1;
    uint32_t isMoveOrigin1 : 1;
    uint32_t isMoveOrigin2 : 1;
    uint32_t isClearPosition1 : 1;
    uint32_t isClearPosition2 : 1;

    uint32_t isSetStop1 : 1;
    uint32_t isSetStop2 : 1;

    uint32_t isGetActualPos1 : 1;
    uint32_t isGetActualPos2 : 1;
    uint32_t isGetActualVoltVOAraw : 1;
    uint32_t isGetActualVoltLDraw : 1;
    uint32_t isGetContinuousObj : 1;
    uint32_t isGetOnceTimeObj : 1;
    uint32_t reserved : 12;
} ObjCtrlAction_t;

typedef union _ObjCtrlAction_u
{
    /* data */
    ObjCtrlAction_t marshall;
    uint32_t unmarshall[1];
}ObjCtrlAction_u;

//---------------------------------------------struct for control
#pragma pack(push, 1)
typedef struct _Move_single_axis_abs_pos_t
{
    uint8_t idMotor;
    int32_t pos;
} Move_single_axis_abs_pos_t;
#pragma pack(pop)

typedef struct _Set_voltage_VOAwLD_t
{
    uint16_t voltVOAraw;
    uint16_t voltLDraw;
}Set_voltage_VOAwLD_t;

#pragma pack(push, 1)
typedef struct _Set_ACC_Time_t
{
    uint8_t idMotor;
    uint32_t ppss;
} Set_ACC_Time_t;
#pragma pack(pop)

#pragma pack(push, 1)
typedef struct _Set_Velocity_t
{
    uint8_t idMotor;
    uint32_t velocity;
}Set_Velocity_t;
#pragma pack(pop)


typedef struct _Move_to_org_t
{
    uint8_t idMotor;
    int8_t dir;
}Move_to_org_t;
//------------------------------------------------define union control

typedef union _Move_single_axis_abs_pos_u
{
    Move_single_axis_abs_pos_t marshall;
    uint8_t unmarshall[sizeof(Move_single_axis_abs_pos_t)];
} Move_single_axis_abs_pos_u;

typedef union _Set_voltage_VOAwLD_u
{
    Set_voltage_VOAwLD_t marshall;
    uint8_t unmarshall[sizeof(Set_voltage_VOAwLD_t)];
}Set_voltage_VOAwLD_u;

typedef union _Set_ACC_Time_u
{
    Set_ACC_Time_t marshall;
    uint8_t unmarshall[sizeof(Set_ACC_Time_t)];
}Set_ACC_Time_u;

typedef union _Set_Velocity_u
{
    Set_Velocity_t marshall;
    uint8_t unmarshall[sizeof(Set_Velocity_t)];
}Set_Velocity_u;

typedef union _Move_to_org_u
{
    Move_to_org_t marshall;
    uint8_t unmarshall[sizeof(Move_to_org_t)];
}Move_to_org_u;

//----------------------------------------------------------------------------
template <typename T>
union convertData_u
{
    uint8_t unmarshall[sizeof(T)];
    T marshall;
};


//extern min_context min_ctx;

#pragma endregion