using CommunityToolkit.Mvvm.ComponentModel;
using OpenCvSharp;
using RaywattApp.Common.Util;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.Common.Bases
{

    public abstract partial class OCTViewModelBase : ViewModelBase
    {
        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        [ObservableProperty]
        private BitmapSource _crossSectionForCompare;

        protected Mat[] imgCrossSection = new Mat[2];
        protected FrameInfo[] crossSectionFrameInfo = new FrameInfo[2];

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        protected Mat imgLongitude;
        protected FrameInfo longitudeFrameInfo;

        // to avoid garbage collection
        private CallbackFunctionWithImage cbCrossSection;
        public CallbackFunctionWithImage CBCrossSection => (this.cbCrossSection) ?? (this.cbCrossSection = new CallbackFunctionWithImage(OnRecvCrossSection));

        private CallbackFunctionWithImage cbLongitude;
        public CallbackFunctionWithImage CBLongitude => (this.cbLongitude) ?? (this.cbLongitude = new CallbackFunctionWithImage(OnRecvLongitude));


        public OCTViewModelBase()
        {
        }

        /// <summary>
        /// Navigation 시작시 - 이동 시작하는 화면에서 발생
        /// </summary>
        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            RayUnregisterImageCallback();
        }

        /// <summary>
        /// Navigation 완료시 - 이동 완료된 화면에서 발생
        /// </summary>
        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            RayRegisterImageCallback(
                Marshal.GetFunctionPointerForDelegate(CBCrossSection),
                Marshal.GetFunctionPointerForDelegate(CBLongitude));
        }

        private void OnRecvCrossSection(int session, IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgCrossSection[session] = imgRecv.Clone();
            crossSectionFrameInfo[session] = new FrameInfo(frameInfo);
        }

        private void OnRecvLongitude(int session, IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv.Clone();
            longitudeFrameInfo = new FrameInfo(frameInfo);
        }

        protected bool DrawCrossSectionImage()
        {
            if (imgCrossSection[0] == null) return false;

            CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection[0]);

            return true;
        }
        protected bool DrawCrossSectionForCompare()
        {
            if (imgCrossSection[1] == null) return false;

            CrossSectionForCompare = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection[1]);

            return true;
        }
        protected bool DrawLongitudeImage()
        {
            if (imgLongitude == null) return false;

            RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);
            if (state != RayScannerState.Review) return false;

            LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
            return true;
        }
    }
}
