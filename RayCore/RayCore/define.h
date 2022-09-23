#pragma once

#define DELAY_FOR_STOP_THREAD			50

typedef void (*FunctionPtr)(int, int);
typedef void (*FunctionImgPtr)(void*, int, int, int, int);

enum class RayError {
	OK = 0,
	InvalidArgument = -1000,
	WrongOCTScannerState,
	NotPausedState
};

enum class RayProperty {
	Unknown = 0,
	CurrentState = 1,
	Brightness,
	Contrast,
	Degree,
	MotorOnOff,
	IsPaused,
	LoadCatheterTime
};

enum class RayViewMode {
	Unknown = 0,
	StandBy,
	LiveView
};

enum class RayCallbackRequest {
	Unknown = 0,
	State,
	Progress
};

enum class RayScannerState {
	None = 0,
	Initializing,
	InitializeFailed,
	Homing,
	Ready,
	LoadCatheter,
	Scanning,
	Review,
	SaveDone
};