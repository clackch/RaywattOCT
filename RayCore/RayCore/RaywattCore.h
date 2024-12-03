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
	_declspec(dllexport) RayError RayReadyPullback();
	_declspec(dllexport) RayError RayPullbackScan(char *strFilePath);
	_declspec(dllexport) RayError RayLoadCatheter();
	_declspec(dllexport) RayError RayUnloadCatheter();
	_declspec(dllexport) RayError RayStartLiveView();
	_declspec(dllexport) RayError RayStopLiveView();
	_declspec(dllexport) RayError RayLaserOnOff(bool isOn);

	_declspec(dllexport) RayError RayShowCalibrationGuide(bool show);
	_declspec(dllexport) int RayStartReview(char *strFilePath, double imageResolution, double zOffset);
	_declspec(dllexport) RayError RayStartCompare(char* strFilePath, double imageResolution, double zOffset);
	_declspec(dllexport) RayError RayEndReview();
	_declspec(dllexport) RayError RayEndCompare();
	_declspec(dllexport) RayError RayRestartReview();
	_declspec(dllexport) RayError RaySetSession(int session);
	_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude);
	_declspec(dllexport) RayError RayUnregisterImageCallback();
	_declspec(dllexport) RayError RayRegisterDetectionCallback(FunctionObjPtr cbObjectDetection);
	_declspec(dllexport) RayError RayUnregisterDetectionCallback();
	_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value);
	_declspec(dllexport) double RayGetProperty(RayProperty prop);
	_declspec(dllexport) void* RayGetVolumeData(void* pLumenContours);
	_declspec(dllexport) RayError RayStartLumenDetection();

	_declspec(dllexport) RayError RayOpenImage(char* strFilePath, double imageResolution, double zOffset);
	_declspec(dllexport) RayError RayCloseImage();
	_declspec(dllexport) void *RayGetImageData(int nFrame);
	_declspec(dllexport) void *RayGetLongitudeData(double fDegree);

	_declspec(dllexport) void* RayGetLumenContour(int nFrame);
	_declspec(dllexport) int RayGetNumOfLumenContourPoints(int nFrame);
	_declspec(dllexport) int RayGetNumOfSidebranchContourSize(int nFrame);
	_declspec(dllexport) void* RayGetSidebranchContour(int nFrame, int nSb);
	_declspec(dllexport) int RayGetNumOfSidebranchContourPoints(int nFrame, int nSb);
	_declspec(dllexport) void* RayGetStentPoints(int nFrame);
	_declspec(dllexport) int RayGetNumOfStentPoints(int nFrame);
	_declspec(dllexport) void* RayGetGuidewirePoints(int nFrame);
	_declspec(dllexport) int RayGetNumOfGuidewirePoints(int nFrame);
}