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
