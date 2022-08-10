using System;
using System.Runtime.InteropServices;

namespace RaywattOCT.Controller
{
    class RayCoreWrapper
    {
        public enum Property : int
        {
            Unknown = 0,
	        Brightness = 1,
	        Contrast,
        }

        public enum ViewMode : int
        {
            Unknown = 0,
	        StandBy,
	        LiveView
        }

        public delegate void CallbackFunction(int request, int response);
        public delegate void CallbackFunctionWithImage(IntPtr data, int width, int height, int channel);
        public static IntPtr ConvertToFunctionPtr(CallbackFunction func) {
            return Marshal.GetFunctionPointerForDelegate(func);
        }
        public static IntPtr ConvertToFunctionPtr(CallbackFunctionWithImage func) {
            return Marshal.GetFunctionPointerForDelegate(func);
        }

        [DllImport("RayCore.dll")]
        public static extern int RayInitialize(IntPtr cbFunction);
        [DllImport("RayCore.dll")]
        public static extern int RaySetMode(ViewMode mode);
        [DllImport("RayCore.dll")]
        public static extern int RayPullbackScan(IntPtr cb);
        [DllImport("RayCore.dll")]
        public static extern int RaySetProperty(Property property, int value);
        [DllImport("RayCore.dll")]
        public static extern int RayGetProperty(Property property);
        [DllImport("RayCore.dll")]
        public static extern int RayLoadCatheter(IntPtr cb);
        [DllImport("RayCore.dll")]
        public static extern int RayUnloadCatheter(IntPtr cb);
        [DllImport("RayCore.dll")]
        public static extern int RayRegisterImageCallback(IntPtr cbCrossSection, IntPtr cbLongitude);
    }
}
