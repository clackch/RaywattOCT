// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "Config.h"
#include "RaywattCore.h"
#include "OCTSystem.h"
#include "Configuration.h"
#include <stdio.h>

static COCTSystem octSystem;
CConfiguration& config = CConfiguration::GetInstance();

_declspec(dllexport) RayError RayStartSystem() {
    return octSystem.Start();
}
_declspec(dllexport) RayError RayStopSystem() {
    return octSystem.Stop();
}
_declspec(dllexport) RayError RayRegisterCallback(FunctionPtr cb) {
    return octSystem.RegisterCallback(cb);
}
_declspec(dllexport) RayError RayUnregisterCallback() {
    return octSystem.UnregisterCallback();
}
_declspec(dllexport) RayError RayConnectDevices() {
    return octSystem.ConnectDevices();
}
_declspec(dllexport) RayError RayDisconnectDevices() {
    return octSystem.DisconnectDevices();
}
_declspec(dllexport) RayError RayAutoCalibration() {
    return octSystem.AutoCalibration();
}
_declspec(dllexport) RayError RayManualCalibration(bool moveForward) {
    return octSystem.ManualCalibration(moveForward);
}
_declspec(dllexport) RayError RayShowCalibrationGuide(bool show) {
    return octSystem.ShowCalibrationGuide(show);
}
_declspec(dllexport) RayError RayReadyPullback() {
    return octSystem.ReadyPullback();
}
_declspec(dllexport) RayError RayPullbackScan(char* strFilePath) {
    return octSystem.PullbackScan(strFilePath);
}
_declspec(dllexport) RayError RayLoadCatheter() {
    return octSystem.LoadCatheter();
}
_declspec(dllexport) RayError RayUnloadCatheter() {
    return octSystem.UnloadCatheter();
}
_declspec(dllexport) int RayStartReview(char* strFilePath) {
    return octSystem.StartReview(strFilePath);
}
_declspec(dllexport) RayError RayStartCompare(char* strFilePath) {
    return octSystem.StartCompare(strFilePath);
}
_declspec(dllexport) RayError RayEndReview() {
    return octSystem.EndReview();
}
_declspec(dllexport) RayError RayStartLiveView() {
    return octSystem.StartLiveView();
}
_declspec(dllexport) RayError RayStopLiveView() {
    return octSystem.StopLiveView();
}
_declspec(dllexport) RayError RaySetSession(int session) {
    return octSystem.SetSession(session);
}
_declspec(dllexport) RayError RayPlayPause() {
    return octSystem.PlayPause();
}
_declspec(dllexport) RayError RayPrevFrame() {
    return octSystem.PrevFrame();
}
_declspec(dllexport) RayError RayNextFrame() {
    return octSystem.NextFrame();
}
_declspec(dllexport) RayError RayMoveToFrame(int nFrame) {
    return octSystem.MoveToFrame(nFrame);
}
_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude) {
    return octSystem.RegisterImageCallback(cbCrossSection, cbLongitude);
}
_declspec(dllexport) RayError RayUnregisterImageCallback() {
    return octSystem.UnregisterImageCallback();
}
_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value) {

    switch (prop) {
    case RayProperty::Brightness:
        return octSystem.SetBrightness(value);
    case RayProperty::Contrast:
        return octSystem.SetContrast(value);
    case RayProperty::LongitudeBackgroundColor:
        return octSystem.SetLongitudeBackgroundColor(value);
    case RayProperty::LongitudeDegree:
        return octSystem.SetDegree(value);
    default:
        return RayError::InvalidArgument;
    }
}
_declspec(dllexport) double RayGetProperty(RayProperty prop) {

    switch (prop) {
    case RayProperty::CurrentState:
        return (double) octSystem.GetCurrentState();
    case RayProperty::Brightness:
        return octSystem.GetBrightness();
    case RayProperty::Contrast:
        return octSystem.GetContrast();
    case RayProperty::LongitudeBackgroundColor:
        return octSystem.GetLongitudeBackgroundColor();
    case RayProperty::LongitudeDegree:
        return octSystem.GetDegree();
    case RayProperty::MotorOnOff:
        return octSystem.GetMotorOnOff();
    case RayProperty::IsPaused:
        return octSystem.GetIsPaused();
    case RayProperty::LoadCatheterTime:
        return config.GetLoadCatheterTime();
    case RayProperty::VolumeWidth:
        return config.volume.size;
    case RayProperty::VolumeHeight:
        return config.volume.size;
    case RayProperty::VolumeDepth:
        return octSystem.GetVolumeDepth();
    case RayProperty::ImageWidth:
        return octSystem.GetImageWidth();
    case RayProperty::ImageHeight:
        return octSystem.GetImageHeight();
    case RayProperty::ImageChannels:
        return octSystem.GetImageChannels();
    case RayProperty::ImageDepth:
        return octSystem.GetImageDepth();
    case RayProperty::LongitudeImageWidth:
        return octSystem.GetLongitudeImageWidth();
    case RayProperty::LongitudeImageHeight:
        return octSystem.GetLongitudeImageHeight();
    case RayProperty::LongitudeImageChannels:
        return octSystem.GetLongitudeImageChannels();
    default:
        return (int)RayError::InvalidArgument;
    }
}

_declspec(dllexport) void* RayGetVolumeData() {
    return octSystem.GetVolumeData();
}
_declspec(dllexport) RayError RayStartLumenDetection() {
    return octSystem.StartLumenDetection();
}

_declspec(dllexport) RayError RayOpenImage(char* strFilePath) {
    return octSystem.OpenImage(strFilePath);
}
_declspec(dllexport) RayError RayCloseImage() {
    return octSystem.CloseImage();
}
_declspec(dllexport) void* RayGetImageData(int nFrame) {
    return octSystem.GetImageData(nFrame);
}
_declspec(dllexport) void* RayGetLongitudeData(double fDegree) {
    return octSystem.GetLongitudeData(fDegree);
}
_declspec(dllexport) void* RayGetLumenContour(int nFrame) {
    return octSystem.GetLumenContour(nFrame);
}
_declspec(dllexport) int RayGetNumOfLumenContourPoints(int nFrame) {
    return octSystem.GetNumOfLumenContourPoints(nFrame);
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

