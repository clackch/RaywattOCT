#pragma once

/*
* 
* DLL Interface
* 
*/
typedef void (*FunctionPtr)(int, int);
typedef void (*FunctionImgPtr)(void *, int, int, int);

enum class RayError {
	OK = 0,
	InvalidArgument = 0xFFFF0000,
};

enum class RayProperty {
	Unknown = 0,
	Brightness = 1,
	Contrast,
};

enum class RayViewMode {
	Unknown = 0,
	StandBy,
	LiveView
};

extern "C" {
	_declspec(dllexport) RayError RayInitialize(FunctionPtr cb);
	_declspec(dllexport) RayError RaySetMode(RayViewMode mode);
	_declspec(dllexport) RayError RayPullbackScan(FunctionPtr cb);
	_declspec(dllexport) RayError RaySetProperty(RayProperty prop, int value);
	_declspec(dllexport) int RayGetProperty(RayProperty prop);
	_declspec(dllexport) RayError RayLoadCatheter(FunctionPtr cb);
	_declspec(dllexport) RayError RayUnloadCatheter(FunctionPtr cb);
	_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
}