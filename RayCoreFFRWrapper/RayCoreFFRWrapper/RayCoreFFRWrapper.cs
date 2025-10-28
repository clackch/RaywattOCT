using System.Runtime.InteropServices;

namespace RaywattOCT
{
    public class RayCoreFFRWrapper
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
            InvalidFunctionCall,
            HomingFailed,
            RotaryJunctionError,
            AutoCalibError
        };

        public enum Property : int
        {
            Unknown = 0,
            CurrentState = 1,
            Brightness,
            Contrast,
            Colormap,
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
            ImageCompensation,
            ImageCompensationControlWindow,
            FieldOfView,
            LongitudeImageWidth,
            LongitudeImageHeight,
            LongitudeImageChannels,
            PullbackRPM,
            PullbackDistance,
            PullbackSpeed,
            SheathDiameter,
            TestMode,
            ZOffset,
            PullbackStartTime,
            AutoPullback,
            LumenThresholdMin,
            LumenThresholdMax,
            LumenSnrThreshold,
            ShowLumenGuide
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
            ValidateCatheter,
            InitializeRotaryJunction,
            CleanRotaryJunction,
            EnableCatheter
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

        public delegate void CallbackFunction(int request, int response, int param);
        public delegate void CallbackFunctionWithImage(int session, IntPtr data, int width, int height, int channel, int frameInfo, double intensity);
        public delegate void CallbackFunctionForDetection(int frame);

        [DllImport("RayCoreFFR.dll")]
        public static extern int RayStartSystem();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayStopSystem();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayInitSystem();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayRegisterCallback(IntPtr cb);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayUnregisterCallback();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayStartReview(string filePath, double imageResolution, double zOffset);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayStartCompare(string filePath, double imageResolution, double zOffset);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayEndReview();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayEndCompare();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayRestartReview();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RaySetSession(RaySession session);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayRegisterImageCallback(IntPtr cbCrossSection, IntPtr cbLongitude);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayUnregisterImageCallback();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayRegisterDetectionCallback(IntPtr cbOBjectDetection);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayUnregisterDetectionCallback();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RaySetProperty(Property property, double value);
        [DllImport("RayCoreFFR.dll")]
        public static extern double RayGetProperty(Property property);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayStartLumenDetection();
        [DllImport("RayCoreFFR.dll")]
        public static extern int RaySetConfigPath(string filePath);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayOpenImage(string filePath, double imageResolution, double zOffset);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayCloseImage();
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetImageData(int frame);
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetLongitudeData(double degree);
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetLumenContour(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayGetNumOfLumenContourPoints(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayGetNumOfSidebranchContourSize(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetSidebranchContour(int nFrame, int nSb);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayGetNumOfSidebranchContourPoints(int nFrame, int nSb);
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetStentPoints(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayGetNumOfStentPoints(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetGuidewirePoints(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayGetNumOfGuidewirePoints(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern IntPtr RayGetGuidewireRadius(int nFrame);
        [DllImport("RayCoreFFR.dll")]
        public static extern int RayRunImageAnalysis(byte[] data, int width, int height, int channels, int step);

    }
}
