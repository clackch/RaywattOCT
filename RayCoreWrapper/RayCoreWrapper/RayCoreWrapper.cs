using System;
using System.Runtime.InteropServices;

namespace RaywattOCT
{
    public class RayCoreWrapper
    {
        public enum RayError : int
        {
            OK = 0,
            SystemRunning = -1000,
            SystemNotRunning,
            DeviceNotConnected,
            DeviceDisconnected,
            DeviceBusy,
            CatheterUnloaded,
            CatheterNotValid,
            InvalidArgument,
            WrongState,
            WrongSession,
            InvalidFunctionCall
        };

        public enum Property : int
        {
            Unknown = 0,
            CurrentState = 1,
            Brightness,
            Contrast,
            LongitudeBackgroundColor,
            LongitudeDegree,
            MotorOnOff,
            LoadCatheterTime,
            VolumeWidth,
            VolumeHeight,
            VolumeDepth,
            ImageWidth,
            ImageHeight,
            ImageChannels,
            ImageDepth,
            ImageResolution,
            LongitudeImageWidth,
            LongitudeImageHeight,
            LongitudeImageChannels,
            PullbackRPM,
            PullbackDistance,
            PullbackSpeed,
            SheathDiameter,
            TestMode
        }

        public enum RayCallbackRequest : int
        {
            Unknown = 0,
            State,
            ProgressSave,
            Error,
            Event,
            WorkDone
        };

        public enum RayScannerState : int
        {
            Initial = 0,
            Default,
            Scanning,
            Review
        };

        public enum RayEvent : int { 
            Unknown = 0,
            CatheterConnected,
            CatheterLoading,
            CatheterUnloading
        };

        public enum RayWorkItem : int
        {
            Unknown = 0,
            StartService,
            SaveRawData,
            OCTImaging,
            GenerateCutView,
            DetectLumen,
            GenerateVolume,
            AutoCalibration,
            Recording,
            Pullback,
            LoadCatheter,
            UnloadCatheter,
            ValidateCatheter
        };

        public enum RaySession : int
        {  
            Unknown = -1,
            Review,
            Compare
        }

        public class FrameInfo {
            public int curFrame;
            public int totalFrame;
            public FrameInfo(int frameInformation)
            {
                curFrame = (frameInformation >> 16) & 0x00FFFF;
                totalFrame = (frameInformation) & 0x00FFFF;
            }
            public FrameInfo(int curFrame, int totalFrame)
            { 
                this.curFrame = curFrame;
                this.totalFrame = totalFrame;
            }
        };

        public static string ConfigFilePath = "./raycore.ini";
        public static double BrightnessMin = 0.0f;
        public static double BrightnessMax = 100.0f;
        public static double ContrastMin = 0.5f;
        public static double ContrastMax = 3.0f;

        public delegate void CallbackFunction(int request, int response, int param);
        public delegate void CallbackFunctionWithImage(int session, IntPtr data, int width, int height, int channel, int frameInfo);
        public delegate void CallbackFunctionForDetection(int frame);

        [DllImport("RayCore.dll")]
        public static extern int RayStartSystem();
        [DllImport("RayCore.dll")]
        public static extern int RayStopSystem();
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterCallback(IntPtr cb);
        [DllImport("RayCore.dll")]
        public static extern int RayUnregisterCallback();
        [DllImport("RayCore.dll")]
        public static extern int RayConnectDevices();
        [DllImport("RayCore.dll")]
        public static extern int RayDisconnectDevices();
        [DllImport("RayCore.dll")]
        public static extern int RayAutoCalibration();
        [DllImport("RayCore.dll")]
        public static extern int RayManualCalibration(bool moveForward);        
        [DllImport("RayCore.dll")]
        public static extern int RayShowCalibrationGuide(bool show);
        [DllImport("RayCore.dll")]
        public static extern int RayReadyPullback();
        [DllImport("RayCore.dll")]
        public static extern int RayPullbackScan(string filePath);
        [DllImport("RayCore.dll")]
        public static extern int RayLoadCatheter();
        [DllImport("RayCore.dll")]
        public static extern int RayUnloadCatheter();
        [DllImport("RayCore.dll")]
        public static extern int RayStartReview(string filePath);
        [DllImport("RayCore.dll")]
        public static extern int RayStartCompare(string filePath);
        [DllImport("RayCore.dll")]
        public static extern int RayEndReview();
        [DllImport("RayCore.dll")]
        public static extern int RayStartLiveView();
        [DllImport("RayCore.dll")]
        public static extern int RayStopLiveView();
        [DllImport("RayCore.dll")]
        public static extern int RayLaserOnOff(bool isOn);
        [DllImport("RayCore.dll")]
        public static extern int RaySetSession(RaySession session);
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterImageCallback(IntPtr cbCrossSection, IntPtr cbLongitude);
        [DllImport("RayCore.dll")]
        public static extern int RayUnregisterImageCallback();
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterDetectionCallback(IntPtr cbOBjectDetection);
        [DllImport("RayCore.dll")]
        public static extern int RayUnregisterDetectionCallback();
        [DllImport("RayCore.dll")]
        public static extern int RaySetProperty(Property property, double value);
        [DllImport("RayCore.dll")]
        public static extern double RayGetProperty(Property property);
        [DllImport("RayCore.dll")]
        public static extern IntPtr RayGetVolumeData(IntPtr lumenContours);
        [DllImport("RayCore.dll")]
        public static extern int RayStartLumenDetection();
        [DllImport("RayCore.dll")]
        public static extern int RayOpenImage(string filePath);
        [DllImport("RayCore.dll")]
        public static extern int RayCloseImage();
        [DllImport("RayCore.dll")]
        public static extern IntPtr RayGetImageData(int frame);
        [DllImport("RayCore.dll")]
        public static extern IntPtr RayGetLongitudeData(double degree);
        [DllImport("RayCore.dll")]
        public static extern IntPtr RayGetLumenContour(int nFrame);
        [DllImport("RayCore.dll")]
        public static extern int RayGetNumOfLumenContourPoints(int nFrame);
    }
}
