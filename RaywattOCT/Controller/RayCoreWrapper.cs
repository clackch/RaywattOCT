using System;
using System.Runtime.InteropServices;

namespace RaywattOCT.Controller
{
    public class RayCoreWrapper
    {
        enum RayError
        {
            OK = 0,
	        InvalidArgument = -1000,
        };

        public enum Property : int
        {
            Unknown = 0,
	        Brightness = 1,
	        Contrast,
            Degree,
            LoadCatheterTime
        }

        public enum ViewMode : int
        {
            Unknown = 0,
	        StandBy,
	        LiveView
        }

        public enum RayCallbackRequest
        {
            Unkown = 0,
	        State
        };

        public enum RayCallbackResponse
        {
            Unkown = 0,
	        IntitializeFailed,
	        Initializing,
	        Homing,
	        Ready,
	        LoadCatheter,
	        Scanning,
	        ScanDone
        };

        public delegate void CallbackFunction(int request, int response);
        public delegate void CallbackFunctionWithImage(IntPtr data, int width, int height, int channel);

        [DllImport("RayCore.dll")]
        public static extern int RayRegisterCallback(IntPtr cb);
        [DllImport("RayCore.dll")]
        public static extern int RayInitialize();
        [DllImport("RayCore.dll")]
        public static extern int RayPullbackScan();
        [DllImport("RayCore.dll")]
        public static extern int RayLoadCatheter();
        [DllImport("RayCore.dll")]
        public static extern int RayUnloadCatheter();
        [DllImport("RayCore.dll")]
        public static extern int RayEndReview();
        [DllImport("RayCore.dll")]
        public static extern int RayMotorOnOff(bool mode);
        [DllImport("RayCore.dll")]
        public static extern int RayPlayPause();
        [DllImport("RayCore.dll")]
        public static extern int RayPrevOctFrame();
        [DllImport("RayCore.dll")]
        public static extern int RayNextOctFrame();
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterImageCallback(IntPtr cbCrossSection, IntPtr cbLongitude);
        [DllImport("RayCore.dll")]
        public static extern int RaySetMode(ViewMode mode);
        [DllImport("RayCore.dll")]
        public static extern int RaySetProperty(Property property, double value);
        [DllImport("RayCore.dll")]
        public static extern double RayGetProperty(Property property);
    }
}
