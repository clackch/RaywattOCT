#pragma once
#include <stdint.h>
/// <summary>
/// you should porting uart to min protocol if use this lib in linux.
/// </summary>
/// 
/// 
/// 
/// 
/// 
/// With application need low letency. You should use delay_line_GetDelayLineObj with mode CONTINUOUS mode.
/// 

/// <summary>
/// Set cmnd: motor [idMotor] move to [pos] pulse
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
/// <param name="pos">[pulse]</param>
void delay_line_Move_single_axis_abs_pos(uint8_t idMotor, int32_t pos);

/// <summary>
/// Set voltage of voa with raw value. You need to check DAC resolution.
/// </summary>
/// <param name="value">0 - 4095 (12bit)</param>
void delay_line_Set_voltage_voa(uint16_t value);

/// <summary>
/// Set voltage of ld with raw value. You need to check DAC resolution.
/// </summary>
/// <param name="value">0 - 4095 (12bit)</param>
void delay_line_Set_voltage_ld(uint16_t value);

/// <summary>
/// Set voltage of ld and voa with raw value. You need to check DAC resolution.
/// </summary>
/// <param name="valueVOA">0 - 4095 (12bit)</param>
/// <param name="valueLD">0 - 4095 (12bit)</param>
void delay_line_Set_voltage_VOAwLD(uint16_t valueVOA, uint16_t valueLD);

/// <summary>
/// Set accelerate value
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
/// <param name="value">pulse per second^2</param>
void delay_line_Set_Acc_Time(uint8_t idMotor, uint32_t value);

/// <summary>
/// Set velocity value
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
/// <param name="value">pps (pulse per second)</param>
void delay_line_Set_Velocity(uint8_t idMotor, uint16_t value);

/// <summary>
/// Set mainboard to mode origin.
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
/// <param name="value">equal -1 or 1</param>
void delay_line_MoveToOrigin(uint8_t idMotor, int8_t value);

/// <summary>
/// This function will clear position of motor (idMotor) to 0
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
void delay_line_ClearPosition(uint8_t idMotor);

/// <summary>
/// This function will make motor begin stop with rampdown
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
void delay_line_SetStop(uint8_t idMotor);

/// <summary>
/// This function will get actual position of motor (idMotor)
/// </summary>
/// <param name="idMotor">equal [1 or 2]</param>
void delay_line_GetActualPos(uint8_t idMotor);

/// <summary>
/// This function will get actual voltage voa raw (0 - 4095)
/// </summary>
void delay_line_GetActualVoltageVOA();

/// <summary>
/// This function will get actual voltage ld raw (0 - 4095)
/// </summary>
void delay_line_GetActualVoltageLD();

/// <summary>
/// This function will get Status of mainboard
/// </summary>
void delay_line_GetStatus();

/// <summary>
/// This function will get all data of mainboard to PC. With mode continuous of once time mode.
/// </summary>
/// <param name="value"> 
/// 1: Continuous
/// 2: Once time
/// </param>
void delay_line_GetDelayLineObj(uint8_t value);


void delay_line_Ping();