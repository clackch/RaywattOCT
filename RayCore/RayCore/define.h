#pragma once

#define DELAY_FOR_STOP_THREAD			50

typedef void (*FunctionPtr)(int, int);
typedef void (*FunctionImgPtr)(void*, int, int, int);

enum class RayError {
	OK = 0,
	InvalidArgument = -1000,
};

enum class RayProperty {
	Unknown = 0,
	Brightness = 1,
	Contrast,
	Degree,
	LoadCatheterTime
};

enum class RayViewMode {
	Unknown = 0,
	StandBy,
	LiveView
};

enum class RayCallbackRequest {
	Unkown = 0,
	State
};

enum class RayCallbackResponse {
	Unkown = 0,
	IntitializeFailed,
	Initializing,
	Homing,
	Ready,
	LoadCatheter,
	Scanning,
	ScanDone
};