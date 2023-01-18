#pragma once
#include "import.h"

#define DELAY_FOR_STOP_THREAD			50

typedef void (*FunctionPtr)(int, int);
typedef void (*FunctionImgPtr)(void*, int, int, int, int);

enum class RayError {
	OK = 0,
	SystemRunning = -1000,
	SystemNotRunning,
	DeviceNotConnected,
	DeviceDisconnected,
	DeviceBusy,
	CatheterUnloaded,
	CatheterNotValid,
	InvalidArgument,
	WrongState,
	NotPaused,
};

enum class RayProperty {
	Unknown = 0,
	CurrentState = 1,
	Brightness,
	Contrast,
	BackgroundColor,
	Degree,
	MotorOnOff,
	IsPaused,
	LoadCatheterTime,
	VolumeWidth,
	VolumeHeight,
	VolumeDepth
};

enum class RayCallbackRequest {
	Unknown = 0,
	State,
	Progress,
	Error,
	WorkDone
};

enum class RayScannerState {
	Initial = 0,
	Default,
	Scanning,
	Review
};

enum class RayWorkItem {
	Unknown = 0,
	SaveRawData,
	UpdateCutView,
	GenerateVolume,
	LumenDetection,
	AutoCalibration,
	Pullback,
	LoadCatheter,
	UnloadCatheter,
	ValidateCatheter
};