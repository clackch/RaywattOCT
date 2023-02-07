#pragma once
#include "define.h"
/*
* 
* DLL Interface
* 
*/

extern "C" {
	_declspec(dllexport) RayError RayStartSystem();
	_declspec(dllexport) RayError RayStopSystem();
	_declspec(dllexport) RayError RayRegisterCallback(FunctionPtr cb);
	_declspec(dllexport) RayError RayUnregisterCallback();
	_declspec(dllexport) RayError RayConnectDevices();
	_declspec(dllexport) RayError RayDisconnectDevices();
	_declspec(dllexport) RayError RayAutoCalibration();
	_declspec(dllexport) RayError RayManualCalibration(bool moveForward);
	_declspec(dllexport) RayError RayPullbackScan(char *strFilePath);
	_declspec(dllexport) RayError RayLoadCatheter();
	_declspec(dllexport) RayError RayUnloadCatheter();
	_declspec(dllexport) RayError RayStartLiveView();
	_declspec(dllexport) RayError RayStopLiveView();

	_declspec(dllexport) RayError RayShowCalibrationGuide(bool show);
	_declspec(dllexport) RayError RayStartReview(char *strFilePath);
	_declspec(dllexport) RayError RayStartCompare(char* strFilePath);
	_declspec(dllexport) RayError RayEndReview();
	_declspec(dllexport) RayError RayPlayPause();
	_declspec(dllexport) RayError RayPrevFrame();
	_declspec(dllexport) RayError RayNextFrame();
	_declspec(dllexport) RayError RayMoveToFrame(int nFrame);
	_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	_declspec(dllexport) RayError RayUnregisterImageCallback();
	_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value);
	_declspec(dllexport) double RayGetProperty(RayProperty prop);
	_declspec(dllexport) void* RayGetVolumeData();

	_declspec(dllexport) RayError RayOpenImage(char* strFilePath);
	_declspec(dllexport) RayError RayCloseImage();
	_declspec(dllexport) void *RayGetImageData(int nFrame);
}