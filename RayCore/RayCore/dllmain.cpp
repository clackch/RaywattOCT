// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "pch.h"
#include "RaywattCore.h"
#include "OCTSystem.h"
#include <stdio.h>

static COCTSystem octSystem;

_declspec(dllexport) RayError RayInitialize(FunctionPtr cb) {
    return octSystem.Initialize(cb);
}
_declspec(dllexport) RayError RaySetMode(RayViewMode mode) {
    return RayError::OK;
}
_declspec(dllexport) RayError RayPullbackScan(FunctionPtr cb) {
    return RayError::OK;
}
_declspec(dllexport) RayError RaySetProperty(RayProperty prop, int value) {
    return RayError::OK;
}
_declspec(dllexport) int RayGetProperty(RayProperty prop) {
    const int brightness = 50;  // test data
    const int contrast = 10;    // test data

    switch (prop) {
    case RayProperty::Brightness:
        return brightness;
    case RayProperty::Contrast:
        return contrast;
    default:
        return (int)RayError::InvalidArgument;
    }
}
_declspec(dllexport) RayError RayLoadCatheter(FunctionPtr cb) {
    printf("RayLoadCatheter called.\n");
    return RayError::OK;
}
_declspec(dllexport) RayError RayUnloadCatheter(FunctionPtr cb) {
    printf("RayUnloadCatheter called.\n");
    return RayError::OK;
}
_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude) {
    printf("RayRegisterImageCallback called.\n");
    return RayError::OK;
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

