#pragma once
#include "import.h"

#define DELAY_FOR_WAIT_PROCESS			5
#define DELAY_FOR_STOP_THREAD			50

typedef void (*FunctionPtr)(int, int, int);
typedef void (*FunctionImgPtr)(int, void*, int, int, int, int, double);
typedef void (*FunctionObjPtr)(int);

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
	WrongSession,
	InvalidFunctionCall,
	HomingFailed,
	RotaryJunctionError,
	AutoCalibError
};

enum class RayProperty {
	Unknown = 0,
	CurrentState = 1,
	Brightness,
	Contrast,
	Colormap,
	LongitudeBackgroundColor,
	LongitudeDegree,
	MotorOnOff,
	LoadCatheterTime,
	VolumeWidth,
	VolumeHeight,
	VolumeDepth,
	ImageWidth,
	ImageHeight,
	ImageChannels,
	ImageDepth,
	ImageResolution,
	ImageCompensation,
	ImageCompensationControlWindow,
	FieldOfView,
	LongitudeImageWidth,
	LongitudeImageHeight,
	LongitudeImageChannels,
	PullbackRPM,
	PullbackDistance,
	PullbackSpeed,
	SheathDiameter,
	TestMode,
	ZOffset,
	PullbackStartTime,
	AutoPullback,
	LumenThresholdMin,
	LumenThresholdMax,
	LumenSnrThreshold,
	ShowLumenGuide
};

enum class RayCallbackRequest {
	Unknown = 0,
	State,
	ProgressSave,
	Error,
	Event,
	WorkDone
};

enum class RayScannerState {
	Initial = 0,
	Default,
	Scanning,
	Review
};

enum class RayEvent {
	Unknown = 0,
	CatheterConnected,
	CatheterLoading,
	CatheterUnloading,
};

enum class RayWorkItem {
	Unknown = 0,
	StartService,
	SaveRawData,
	OCTImaging,
	GenerateCutView,
	DetectLumen,
	GenerateVolume,
	AutoCalibration,
	Recording,
	Pullback,
	LoadCatheter,
	UnloadCatheter,
	ValidateCatheter,
	InitializeRotaryJunction,
	CleanRotaryJunction,
	EnableCatheter
};

enum class RaySession {
	Unknown = -1,
	Review,
	Compare
};