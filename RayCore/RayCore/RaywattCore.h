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
	_declspec(dllexport) RayError RayPullbackScan(char *strFilePath);
	_declspec(dllexport) RayError RayLoadCatheter();
	_declspec(dllexport) RayError RayUnloadCatheter();
	_declspec(dllexport) RayError RayEndReview();
	_declspec(dllexport) RayError RayMotorOnOff(bool mode);
	_declspec(dllexport) RayError RayPlayPause();
	_declspec(dllexport) RayError RayPrevFrame();
	_declspec(dllexport) RayError RayNextFrame();
	_declspec(dllexport) RayError RayMoveToFrame(int nFrame);
	_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	_declspec(dllexport) RayError RaySetMode(RayViewMode mode);
	_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value);
	_declspec(dllexport) double RayGetProperty(RayProperty prop);
}