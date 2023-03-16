using CommunityToolkit.Mvvm.ComponentModel;
using OpenCvSharp;
using RaywattApp.Common.Util;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using static RaywattOCT.RayCoreWrapper;
using Point = OpenCvSharp.Point;

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
        protected Scalar[] crossSectionBackground = new Scalar[2];
        protected Mat imgCrossSectionBackground;
        protected Mat imgCrossSectionMask;

        [ObservableProperty]
        protected double _crossSectionScale = 65;

        [ObservableProperty]
        protected double _crossSectionAngioScale = 60;

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
            for (int i = 0; i < crossSectionBackground.Length; i++) {
                crossSectionBackground[i] = new Scalar(0, 0, 0);
            }
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

            // Allocate at first
            imgCrossSectionMask = (imgCrossSectionMask == null) ? GenerateMask(imgRecv) : imgCrossSectionMask;
            imgCrossSectionBackground = (imgCrossSectionBackground == null) ? imgRecv.EmptyClone() : imgCrossSectionBackground;

            imgCrossSection[session] = imgRecv;
            crossSectionFrameInfo[session] = new FrameInfo(frameInfo);
        }

        private void OnRecvLongitude(int session, IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv;
            longitudeFrameInfo = new FrameInfo(frameInfo);
        }

        protected bool DrawCrossSectionImage()
        {
            if (imgCrossSection[0] == null || imgCrossSectionBackground == null || imgCrossSectionMask == null) return false;

            CrossSectionImage = DrawCrossSectionWithBackground(imgCrossSection[0], crossSectionBackground[0]);

            return true;
        }
        protected bool DrawCrossSectionForCompare()
        {
            if (imgCrossSection[1] == null || imgCrossSectionBackground == null || imgCrossSectionMask == null) return false;

            CrossSectionForCompare = DrawCrossSectionWithBackground(imgCrossSection[1], crossSectionBackground[1]);

            return true;
        }
        protected void SetCrossSectionBackground(uint session, int rgbCode) {
            if (session >= crossSectionBackground.Length) return;

            crossSectionBackground[session] = new Scalar(rgbCode & 0xFF, (rgbCode >> 8) & 0xFF, (rgbCode >> 16) & 0xFF);
        }
        protected bool DrawLongitudeImage()
        {
            if (imgLongitude == null) return false;

            RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);
            if (state != RayScannerState.Review) return false;

            LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
            return true;
        }

        private BitmapSource DrawCrossSectionWithBackground(Mat image, Scalar background)
        {
            // Background Masking
            imgCrossSectionBackground.SetTo(background);
            Cv2.CopyTo(imgCrossSectionBackground, image, imgCrossSectionMask);

            BitmapSource bitmap = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(image);

            return bitmap;
        }

        private Mat GenerateMask(Mat image)
        {
            Mat mask = image.EmptyClone();
            Point center = new Point(mask.Width / 2, mask.Height / 2);

            mask.SetTo(Scalar.White);
            Cv2.Circle(mask, center, mask.Width / 2, Scalar.Black, -1);

            return mask;
        }
    }
}
