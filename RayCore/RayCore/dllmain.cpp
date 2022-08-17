// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "pch.h"
#include "RaywattCore.h"
#include "OCTSystem.h"
#include "MoriaConfiguration.h"
#include <stdio.h>

static COCTSystem octSystem;
CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();

_declspec(dllexport) RayError RayRegisterCallback(FunctionPtr cb) {
    return octSystem.RegisterCallback(cb);
}
_declspec(dllexport) RayError RayInitialize() {
    return octSystem.Initialize();
}
_declspec(dllexport) RayError RayPullbackScan() {
    return octSystem.PullbackScan();
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
_declspec(dllexport) RayError RayPrevOctFrame() {
    return octSystem.PrevOctFrame();
}
_declspec(dllexport) RayError RayNextOctFrame() {
    return octSystem.NextOctFrame();
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
    case RayProperty::Brightness:
        return octSystem.GetBrightness();
    case RayProperty::Contrast:
        return octSystem.GetContrast();
    case RayProperty::Degree:
        return octSystem.GetDegree();
    case RayProperty::MoterOnOff:
        return octSystem.GetMotorOnOff();
    case RayProperty::PlayPause:
        return octSystem.GetPlayPause();
    case RayProperty::LoadCatheterTime:
        return pConfig.GetLoadCatheterTime();
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

