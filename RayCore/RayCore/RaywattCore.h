#pragma once
#include "define.h"
/*
* 
* DLL Interface
* 
*/

extern "C" {
	_declspec(dllexport) RayError RayRegisterCallback(FunctionPtr cb);
	_declspec(dllexport) RayError RayInitialize();
	_declspec(dllexport) RayError RayPullbackScan();
	_declspec(dllexport) RayError RayLoadCatheter();
	_declspec(dllexport) RayError RayUnloadCatheter();
	_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	_declspec(dllexport) RayError RaySetMode(RayViewMode mode);
	_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value);
	_declspec(dllexport) double RayGetProperty(RayProperty prop);
}