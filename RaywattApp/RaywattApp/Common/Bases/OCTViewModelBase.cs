using CommunityToolkit.Mvvm.ComponentModel;
using OpenCvSharp;
using RaywattApp.Common.Util;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.Common.Bases
{

    public abstract partial class OCTViewModelBase : ViewModelBase
    {
        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        protected Mat imgCrossSection;
        protected FrameInfo crossSectionFrameInfo;

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        protected Mat imgLongitude;
        protected FrameInfo longitudeFrameInfo;

        private int brightness;
        public int Brightness
        {
            get { return brightness; }
            set { brightness = value; OnPropertyChanged(nameof(Brightness)); setBrightnessContrast(); }
        }

        private int contrast;
        public int Contrast
        {
            get { return contrast; }
            set { contrast = value; OnPropertyChanged(nameof(Contrast)); setBrightnessContrast(); }
        }

        // to avoid garbage collection
        private CallbackFunction cbFunction;
        public CallbackFunction CBFunction => (this.cbFunction) ?? (this.cbFunction = new CallbackFunction(OnMsgCallback));

        private CallbackFunctionWithImage cbCrossSection;
        public CallbackFunctionWithImage CBCrossSection => (this.cbCrossSection) ?? (this.cbCrossSection = new CallbackFunctionWithImage(OnRecvCrossSection));

        private CallbackFunctionWithImage cbLongitude;
        public CallbackFunctionWithImage CBLongitude => (this.cbLongitude) ?? (this.cbLongitude = new CallbackFunctionWithImage(OnRecvLongitude));


        public OCTViewModelBase()
        {
            RayRegisterCallback(Marshal.GetFunctionPointerForDelegate(CBFunction));
            RayRegisterImageCallback(
                Marshal.GetFunctionPointerForDelegate(CBCrossSection),
                Marshal.GetFunctionPointerForDelegate(CBLongitude));
        }

        protected abstract void handleState(RayCallbackRequest request, RayScannerState state);
        protected abstract void handleProgress(RayCallbackRequest request, int progress);
        protected abstract void handleError(RayCallbackRequest request, RayError error);
        protected abstract void handleWorkDone(RayCallbackRequest request, RayWorkItem work);
        protected void setBrightnessContrast()
        {
            double propBrightness = ((double)Brightness / 100) * (BrightnessMax - BrightnessMin) + BrightnessMin;
            double propContrast = ((double)Contrast / 100) * (ContrastMax - ContrastMin) + ContrastMin;

            RaySetProperty(Property.Brightness, propBrightness);
            RaySetProperty(Property.Contrast, propContrast);
        }
        protected virtual void syncWithCoreSystem()
        {
            double propBrightness = RayGetProperty(Property.Brightness);
            double propContrast = RayGetProperty(Property.Contrast);

            this.Brightness = (int)(((propBrightness - BrightnessMin) / (BrightnessMax - BrightnessMin)) * 100);
            this.Contrast = (int)(((propContrast - ContrastMin) / (ContrastMax - ContrastMin)) * 100);
        }

        private void OnMsgCallback(int request, int response)
        {
            switch ((RayCallbackRequest)request) {
                case RayCallbackRequest.State:
                    handleState((RayCallbackRequest)request, (RayScannerState)response);
                    break;
                case RayCallbackRequest.Progress:
                    handleProgress((RayCallbackRequest)request, response);
                    break;
                case RayCallbackRequest.Error:
                    handleError((RayCallbackRequest)request, (RayError)response);
                    break;
                case RayCallbackRequest.WorkDone:
                    handleWorkDone((RayCallbackRequest)request, (RayWorkItem)response);
                    break;
                default:
                    break;
            }
        }

        private void OnRecvCrossSection(IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgCrossSection = imgRecv.Clone();
            crossSectionFrameInfo = new FrameInfo(frameInfo);
        }

        private void OnRecvLongitude(IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv.Clone();
            longitudeFrameInfo = new FrameInfo(frameInfo);
        }

    }
}
