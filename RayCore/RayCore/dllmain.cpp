// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "Config.h"
#include "RaywattCore.h"
#include "OCTSystem.h"
#include "Configuration.h"
#include <stdio.h>

static COCTSystem octSystem;

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
_declspec(dllexport) int RayStartReview(char* strFilePath, double imageResolution, double zOffset) {
    PLOGI.printf("startR1");
    return octSystem.StartReview(strFilePath, imageResolution, zOffset);
}
_declspec(dllexport) RayError RayStartCompare(char* strFilePath, double imageResolution, double zOffset) {
    return octSystem.StartCompare(strFilePath, imageResolution, zOffset);
}
_declspec(dllexport) RayError RayEndReview() {
    PLOGI.printf("endR1");
    return octSystem.EndReview();
}
_declspec(dllexport) RayError RayEndCompare() {
    PLOGI.printf("endC1");
    return octSystem.EndCompare();
}
_declspec(dllexport) RayError RayRestartReview() {
    PLOGI.printf("restartR");
    return octSystem.RestartReview();
}
_declspec(dllexport) RayError RayStartLiveView() {
    PLOGI.printf("startLV");
    return octSystem.StartLiveView();
}
_declspec(dllexport) RayError RayStopLiveView() {
    PLOGI.printf("stopLV");
    return octSystem.StopLiveView();
}
_declspec(dllexport) RayError RayLaserOnOff(bool isOn) {
    PLOGI.printf("laseronoff");
    return octSystem.LaserOnOff(isOn);
}
_declspec(dllexport) RayError RayRJCleanModeOnOff(bool isOn) {
    PLOGI.printf("RJonoff");
    return octSystem.RJCleanModeOnOff(isOn);
}
_declspec(dllexport) RayError RaySetSession(int session) {
    return octSystem.SetSession(session);
}
_declspec(dllexport) RayError RayRegisterImageCallback(FunctionImgPtr cbCrossSection, FunctionImgPtr cbLongitude) {
    return octSystem.RegisterImageCallback(cbCrossSection, cbLongitude);
}
_declspec(dllexport) RayError RayUnregisterImageCallback() {
    PLOGI.printf("unIcb1");
    return octSystem.UnregisterImageCallback();
}
_declspec(dllexport) RayError RayRegisterDetectionCallback(FunctionObjPtr cbObjectDetection) {
    PLOGI.printf("rdcb");
    return octSystem.RegisterDetectionCallback(cbObjectDetection);
}
_declspec(dllexport) RayError RayUnregisterDetectionCallback() {
    PLOGI.printf("udcb");
    return octSystem.UnregisterDetectionCallback();
}
_declspec(dllexport) RayError RaySetProperty(RayProperty prop, double value) {
    PLOGI.printf("SP1");
    CConfiguration& config = CConfiguration::GetInstance();
    PLOGI.printf("SP2");

    switch (prop) {
    case RayProperty::Brightness:
        return octSystem.SetBrightness(value);
    case RayProperty::Contrast:
        return octSystem.SetContrast(value);
    case RayProperty::Colormap:
        return octSystem.SetColormap(value);
    case RayProperty::LongitudeBackgroundColor:
        return octSystem.SetLongitudeBackgroundColor(value);
    case RayProperty::LongitudeDegree:
        return octSystem.SetDegree(value);
    case RayProperty::PullbackRPM:
        config.bldcMotor.velocityPullback = value;
        break;
    case RayProperty::PullbackDistance:
        config.stepMotor.pullbackDistance = (value >= 95) ? 95 : value;
        break;
    case RayProperty::PullbackSpeed:
        config.stepMotor.pullbackSpeed = value;
        break;
    case RayProperty::SheathDiameter:
        octSystem.SetSheathDiameter(value);
        break;
    case RayProperty::ImageThreshold:
        octSystem.SetImageThreshold(value);
        break;
    case RayProperty::ImageRoi:
        octSystem.SetImageRoi(value);
        break;
    case RayProperty::ImageCompensation:
        octSystem.SetImageCompensation(value);
        break;
    case RayProperty::ImageCompensationControlWindow:
        octSystem.SetImageCompensationControlWindow(value);
        break;
    case RayProperty::FieldOfView:
        octSystem.SetFieldOfView(value);
        break;
    case RayProperty::ZOffset:
        octSystem.SetZOffset(value);
        break;
    case RayProperty::TestMode:
        octSystem.SetTestMode((bool) value);
        break;
    case RayProperty::PullbackStartTime:
        octSystem.SetPullbackStartTime(value);
        break;
    default:
        return RayError::InvalidArgument;
    }
    PLOGI.printf("SP3");
    return RayError::OK;
}
_declspec(dllexport) double RayGetProperty(RayProperty prop) {
    PLOGI.printf("GP1");
    CConfiguration& config = CConfiguration::GetInstance();
    PLOGI.printf("GP2, Property Number = %d", prop);
    switch (prop) {
    case RayProperty::CurrentState:
        return (double) octSystem.GetCurrentState();
    case RayProperty::Brightness:
        return octSystem.GetBrightness();
    case RayProperty::Contrast:
        return octSystem.GetContrast();
    case RayProperty::Colormap:
        return octSystem.GetColormap();
    case RayProperty::LongitudeBackgroundColor:
        return octSystem.GetLongitudeBackgroundColor();
    case RayProperty::LongitudeDegree:
        return octSystem.GetDegree();
    case RayProperty::MotorOnOff:
        return octSystem.GetMotorOnOff();
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
    case RayProperty::ImageResolution:
        return octSystem.GetImageResolution();
    case RayProperty::LongitudeImageWidth:
        return octSystem.GetLongitudeImageWidth();
    case RayProperty::LongitudeImageHeight:
        return octSystem.GetLongitudeImageHeight();
    case RayProperty::LongitudeImageChannels:
        return octSystem.GetLongitudeImageChannels();
    case RayProperty::PullbackRPM:
        return config.bldcMotor.velocityPullback;
    case RayProperty::PullbackDistance:
        return config.stepMotor.pullbackDistance;
    case RayProperty::PullbackSpeed:
        return config.stepMotor.pullbackSpeed;
    case RayProperty::SheathDiameter:
        return config.measurement.fSheathRadius * 2;
    case RayProperty::ImageThreshold:
        return octSystem.GetImageThreshold();
    case RayProperty::ImageRoi:
        return octSystem.GetImageRoi();
    case RayProperty::ImageCompensation:
        return octSystem.GetImageCompensation();
    case RayProperty::FieldOfView:
        return octSystem.GetFieldOfView();
    case RayProperty::TestMode:
        return (double) octSystem.IsTestMode();
    case RayProperty::PullbackStartTime:
        return octSystem.GetPullbackStartTime();
    default:
        return (int)RayError::InvalidArgument;
    }
    PLOGI.printf("GP3");
}

_declspec(dllexport) void* RayGetVolumeData(void* pLumenContours) {
    PLOGI.printf("getVD");
    return octSystem.GetVolumeData(pLumenContours);
}
_declspec(dllexport) RayError RayStartLumenDetection() {
    PLOGI.printf("startLD");
    return octSystem.StartLumenDetection();
}

_declspec(dllexport) RayError RayOpenImage(char* strFilePath, double imageResolution, double zOffset) {
    PLOGI.printf("openImage");
    return octSystem.OpenImage(strFilePath, imageResolution, zOffset);
}
_declspec(dllexport) RayError RayCloseImage() {
    PLOGI.printf("closeImage");
    return octSystem.CloseImage();
}
_declspec(dllexport) void* RayGetImageData(int nFrame) {
    PLOGI.printf("getID");
    return octSystem.GetImageData(nFrame);
}
_declspec(dllexport) void* RayGetLongitudeData(double fDegree) {
    PLOGI.printf("getLD");
    return octSystem.GetLongitudeData(fDegree);
}
_declspec(dllexport) void* RayGetLumenContour(int nFrame) {
    PLOGI.printf("getLC");
    return octSystem.GetLumenContour(nFrame);
}
_declspec(dllexport) int RayGetNumOfLumenContourPoints(int nFrame) {
    PLOGI.printf("getNLCP");
    return octSystem.GetNumOfLumenContourPoints(nFrame);
}
_declspec(dllexport) int RayGetNumOfSidebranchContourSize(int nFrame){
    PLOGI.printf("getNSCS");
    return octSystem.GetNumOfSidebranchContourSize(nFrame);
}
_declspec(dllexport) void* RayGetSidebranchContour(int nFrame, int nSb){
    PLOGI.printf("getSC");
    return octSystem.GetSidebranchContour(nFrame, nSb);
}
_declspec(dllexport) int RayGetNumOfSidebranchContourPoints(int nFrame, int nSb){
    PLOGI.printf("getNSCP");
    return octSystem.GetNumOfSidebranchContourPoints(nFrame, nSb);
}
_declspec(dllexport) void* RayGetStentPoints(int nFrame) {
    PLOGI.printf("getStentP");
    return octSystem.GetStentPoints(nFrame);
}
_declspec(dllexport) int RayGetNumOfStentPoints(int nFrame) {
    PLOGI.printf("getNstrentP");
    return octSystem.GetNumOfStentPoints(nFrame);
}
_declspec(dllexport) void* RayGetGuidewirePoints(int nFrame) {
    PLOGI.printf("getGWP");
    return octSystem.GetGuidewirePoints(nFrame);
}
_declspec(dllexport) int RayGetNumOfGuidewirePoints(int nFrame) {
    PLOGI.printf("getNGWP");
    return octSystem.GetNumOfGuidewirePoints(nFrame);
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
        /*AllocConsole();
        freopen("CONOUT$", "w", stdout);*/
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

