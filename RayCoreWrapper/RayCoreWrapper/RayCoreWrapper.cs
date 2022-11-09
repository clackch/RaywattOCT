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
            InvalidArgument,
            WrongOCTScannerState,
            NotPausedState,
            DeviceNotConnected,
            InitializeFailed,
            WrongFilePath
        };

        public enum Property : int
        {
            Unknown = 0,
            CurrentState = 1,
            Brightness,
            Contrast,
            BackgroundColor,
            Degree,
            MotorOnOff,
            IsPaused,
            LoadCatheterTime,
            VolumeWidth,
            VolumeHeight,
            VolumeDepth
        }

        public enum ViewMode : int
        {
            Unknown = 0,
	        StandBy,
	        LiveView
        }

        public enum RayCallbackRequest : int
        {
            Unknown = 0,
            State,
            Progress,
            Error,
            WorkDone
        };

        public enum RayScannerState : int
        {
            None = 0,
            Initializing,
            LiveView,
            AutoCalibration,
            Homing,
            Ready,
            LoadCatheter,
            Scanning,
            Review
        };

        public enum RayWorkItem : int
        {
            Unknown = 0,
            GenerateVolume
        };

        public class FrameInfo {
            public int curFrame;
            public int totalFrame;
            public FrameInfo(int frameInformation)
            {
                curFrame = (frameInformation >> 16) & 0x00FFFF;
                totalFrame = (frameInformation) & 0x00FFFF;
            }
        };

        public static string ConfigFilePath = "./raycore.ini";
        public static double BrightnessMin = 0.0f;
        public static double BrightnessMax = 100.0f;
        public static double ContrastMin = 0.5f;
        public static double ContrastMax = 3.0f;

        public delegate void CallbackFunction(int request, int response);
        public delegate void CallbackFunctionWithImage(IntPtr data, int width, int height, int channel, int frameInfo);

        [DllImport("RayCore.dll")]
        public static extern int RayStartSystem();
        [DllImport("RayCore.dll")]
        public static extern int RayStopSystem();
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterCallback(IntPtr cb);
        [DllImport("RayCore.dll")]
        public static extern int RayConnectDevices();
        [DllImport("RayCore.dll")]
        public static extern int RayInitialize();
        [DllImport("RayCore.dll")]
        public static extern int RayShowCalibrationGuide(bool show);
        [DllImport("RayCore.dll")]
        public static extern int RayPreparePullback();
        [DllImport("RayCore.dll")]
        public static extern int RayPullbackScan(string filePath);
        [DllImport("RayCore.dll")]
        public static extern int RayLoadCatheter();
        [DllImport("RayCore.dll")]
        public static extern int RayUnloadCatheter();
        [DllImport("RayCore.dll")]
        public static extern int RayStartReview(string filePath);
        [DllImport("RayCore.dll")]
        public static extern int RayEndReview();
        [DllImport("RayCore.dll")]
        public static extern int RayMotorOnOff(bool mode);
        [DllImport("RayCore.dll")]
        public static extern int RayPlayPause();
        [DllImport("RayCore.dll")]
        public static extern int RayPrevFrame();
        [DllImport("RayCore.dll")]
        public static extern int RayNextFrame();
        [DllImport("RayCore.dll")]
        public static extern int RayMoveToFrame(int frame);
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterImageCallback(IntPtr cbCrossSection, IntPtr cbLongitude);
        [DllImport("RayCore.dll")]
        public static extern int RaySetMode(ViewMode mode);
        [DllImport("RayCore.dll")]
        public static extern int RaySetProperty(Property property, double value);
        [DllImport("RayCore.dll")]
        public static extern double RayGetProperty(Property property);
        [DllImport("RayCore.dll")]
        public static extern IntPtr RayGetVolumeData();
    }
}
