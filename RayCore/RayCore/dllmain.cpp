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
_declspec(dllexport) RayError RayInitialize() {
    return octSystem.Initialize();
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
_declspec(dllexport) RayError RayEndReview() {
    return octSystem.EndReview();
}
_declspec(dllexport) RayError RayMotorOnOff(bool mode) {
    return octSystem.MotorOnOff(mode);
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
_declspec(dllexport) RayError RaySetMode(RayViewMode mode) {
    return RayError::OK;
}
_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value) {

    switch (prop) {
    case RayProperty::Brightness:
        return octSystem.SetBrightness(value);
    case RayProperty::Contrast:
        return octSystem.SetContrast(value);
    case RayProperty::Degree:
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
    case RayProperty::Degree:
        return octSystem.GetDegree();
    case RayProperty::MotorOnOff:
        return octSystem.GetMotorOnOff();
    case RayProperty::IsPaused:
        return octSystem.GetIsPaused();
    case RayProperty::LoadCatheterTime:
        return config.GetLoadCatheterTime();
    default:
        return (int)RayError::InvalidArgument;
    }
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

