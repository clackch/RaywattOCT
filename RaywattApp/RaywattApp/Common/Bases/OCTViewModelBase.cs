using CommunityToolkit.Mvvm.ComponentModel;
using OpenCvSharp;
using RaywattApp.Common.Util;
using RaywattOCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RaywattApp.Common.Bases
{

    public abstract partial class OCTViewModelBase : ViewModelBase
    {
        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        protected Mat imgCrossSection;
        protected RayCoreWrapper.FrameInfo crossSectionFrameInfo;

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        protected Mat imgLongitude;
        protected RayCoreWrapper.FrameInfo longitudeFrameInfo;

        // to avoid garbage collection
        private RayCoreWrapper.CallbackFunction cbFunction;
        public RayCoreWrapper.CallbackFunction CBFunction => (this.cbFunction) ?? (this.cbFunction = new RayCoreWrapper.CallbackFunction(OnMsgCallback));

        private RayCoreWrapper.CallbackFunctionWithImage cbCrossSection;
        public RayCoreWrapper.CallbackFunctionWithImage CBCrossSection => (this.cbCrossSection) ?? (this.cbCrossSection = new RayCoreWrapper.CallbackFunctionWithImage(OnRecvCrossSection));

        private RayCoreWrapper.CallbackFunctionWithImage cbLongitude;
        public RayCoreWrapper.CallbackFunctionWithImage CBLongitude => (this.cbLongitude) ?? (this.cbLongitude = new RayCoreWrapper.CallbackFunctionWithImage(OnRecvLongitude));


        public OCTViewModelBase()
        {
            RayCoreWrapper.RayRegisterCallback(Marshal.GetFunctionPointerForDelegate(CBFunction));
            RayCoreWrapper.RayRegisterImageCallback(
                Marshal.GetFunctionPointerForDelegate(CBCrossSection),
                Marshal.GetFunctionPointerForDelegate(CBLongitude));
        }

        protected abstract void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState state);
        protected abstract void handleProgress(RayCoreWrapper.RayCallbackRequest request, int progress);
        protected abstract void handleError(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayError error);
        protected abstract void handleWorkDone(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayWorkItem work);

        private void OnMsgCallback(int request, int response)
        {
            switch ((RayCoreWrapper.RayCallbackRequest)request) {
                case RayCoreWrapper.RayCallbackRequest.State:
                    handleState((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayScannerState)response);
                    break;
                case RayCoreWrapper.RayCallbackRequest.Progress:
                    handleProgress((RayCoreWrapper.RayCallbackRequest)request, response);
                    break;
                case RayCoreWrapper.RayCallbackRequest.Error:
                    handleError((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayError)response);
                    break;
                case RayCoreWrapper.RayCallbackRequest.WorkDone:
                    handleWorkDone((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayWorkItem)response);
                    break;
                default:
                    break;
            }
        }

        private void OnRecvCrossSection(IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.byteMemoryToCvMat(data, width, height, ch);
            imgCrossSection = imgRecv.Clone();
            crossSectionFrameInfo = new RayCoreWrapper.FrameInfo(frameInfo);
        }

        private void OnRecvLongitude(IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.byteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv.Clone();
            longitudeFrameInfo = new RayCoreWrapper.FrameInfo(frameInfo);
        }

    }
}
