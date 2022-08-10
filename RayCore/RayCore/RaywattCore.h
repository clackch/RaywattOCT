#pragma once
#include "define.h"
/*
* 
* DLL Interface
* 
*/

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