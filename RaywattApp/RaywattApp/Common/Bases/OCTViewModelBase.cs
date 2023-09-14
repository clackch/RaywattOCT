using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using OpenCvSharp;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;
using static RaywattOCT.RayCoreWrapper;
using Point = OpenCvSharp.Point;

namespace RaywattApp.Common.Bases
{

    public abstract partial class OCTViewModelBase : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OCTViewModelBase));

        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        [ObservableProperty]
        private BitmapSource _crossSectionForCompare;

        protected Mat[] imgCrossSection = new Mat[2];
        protected Scalar[] crossSectionBackground = new Scalar[2];
        protected Mat imgCrossSectionMask;

        [ObservableProperty]
        protected double _crossSectionScale = (1 / Constants.MillimeterPerPixel ) * (Constants.CrossSectionSize / Constants.OCTImageSize);

        [ObservableProperty]
        protected double _crossSectionAngioScale = (1 / Constants.MillimeterPerPixel ) * (Constants.CrossSectionAngio / Constants.OCTImageSize);

        [ObservableProperty]
        private double _crossSection3dScale = 28;

        [ObservableProperty]
        private double _crossSectionCompareScale = 40;

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        protected Mat imgLongitude;
        protected FrameInfo longitudeFrameInfo;

        [ObservableProperty]
        private BitmapSource _lumenProfileImage;

        protected Mat imglumenProfile;

        [ObservableProperty]
        private bool _isPaused = true;

        private Thread threadFuncPlayback;

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
            if (DeviceStatus.IsPaused == false)
            {
                Playback();
            }
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
            imgCrossSection[session] = imgRecv;
        }

        private void OnRecvLongitude(int session, IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv;
            longitudeFrameInfo = new FrameInfo(frameInfo);
        }

        protected bool DrawCrossSectionImage()
        {
            if (imgCrossSection[0] == null) return false;

            CrossSectionImage = DrawCrossSectionWithBackground(imgCrossSection[0], crossSectionBackground[0]);

            return true;
        }
        protected bool DrawCrossSectionForCompare()
        {
            if (imgCrossSection[1] == null) return false;

            CrossSectionForCompare = DrawCrossSectionWithBackground(imgCrossSection[1], crossSectionBackground[1]);

            return true;
        }
        protected void SetCrossSectionBackground(RaySession session, int rgbCode) {
            crossSectionBackground[(int)session] = new Scalar(rgbCode & 0xFF, (rgbCode >> 8) & 0xFF, (rgbCode >> 16) & 0xFF);
        }
        protected bool DrawLongitudeImage()
        {
            if (imgLongitude == null) return false;

            RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);
            if (state != RayScannerState.Review) return false;

            LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
            return true;
        }
        protected bool DrawLumenProfileImage()
        {
            if (imglumenProfile == null) return false;

            RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);
            if (state != RayScannerState.Review) return false;

            LumenProfileImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfile);
            return true;
        }

        private BitmapSource DrawCrossSectionWithBackground(Mat image, Scalar background)
        {
            // Background Masking
            imgCrossSectionMask = (imgCrossSectionMask == null) ? GenerateMask(image) : imgCrossSectionMask;

            Mat imgCrossSectionBackground = image.EmptyClone();
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

        protected bool PrevFrame(RaySession session)
        {
            int nNumOfFrames = (int)RayGetProperty(Property.ImageDepth);
            int nMoveToFrame = DeviceStatus.ReviewImageInfos[(int)session].Current;

            nMoveToFrame--;
            nMoveToFrame = (nMoveToFrame < 0) ? nNumOfFrames - 1 : nMoveToFrame;
            
            return MoveToFrame(session, nMoveToFrame);
        }
        protected bool NextFrame(RaySession session)
        {
            int nNumOfFrames = (int)RayGetProperty(Property.ImageDepth);
            int nMoveToFrame = DeviceStatus.ReviewImageInfos[(int)session].Current;
            
            nMoveToFrame++;
            nMoveToFrame = (nMoveToFrame >= nNumOfFrames) ? 0 : nMoveToFrame;

            return MoveToFrame(session, nMoveToFrame);
        }
        protected void Playback()
        {
            if (DeviceStatus.IsPaused)
            {
                DeviceStatus.IsPaused = false;

                threadFuncPlayback = new Thread(() => ThreadFuncPlayback());
                threadFuncPlayback.Start();
            }
            else
            {
                DeviceStatus.IsPaused = true;

                threadFuncPlayback.Join();
            }

            IsPaused = DeviceStatus.IsPaused;
        }
        protected bool MoveToFrame(RaySession session, int nFrame)
        {
            RayError result = (RayError) RaySetSession(session);
            if (result != RayError.OK) return false;

            IntPtr data = RayGetImageData(nFrame);
            if (data == IntPtr.Zero) return false;

            DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)session];
            Mat img = CommonUtil.ByteMemoryToCvMat(data, imageInfo.Width, imageInfo.Height, imageInfo.Channels);

            imgCrossSection[(int)session] = img;
            DeviceStatus.ReviewImageInfos[(int)session].Current = nFrame;

            return true;
        }
        protected void GetImageInfo(RaySession session)
        {
            RayError result = (RayError) RaySetSession(session);
            if (result != RayError.OK) { return; }

            DeviceStatus.ReviewImageInfo imageInfo = new DeviceStatus.ReviewImageInfo();
            imageInfo.Width = (int)RayGetProperty(Property.ImageWidth);
            imageInfo.Height = (int)RayGetProperty(Property.ImageHeight);
            imageInfo.Channels = (int)RayGetProperty(Property.ImageChannels);
            imageInfo.Total = (int)RayGetProperty(Property.ImageDepth);
            imageInfo.Current = (DeviceStatus.ReviewImageInfos[(int)session] != null) ? DeviceStatus.ReviewImageInfos[(int)session].Current : 0;

            DeviceStatus.ReviewImageInfos[(int)session] = imageInfo;
        }
        private void ThreadFuncPlayback()
        {
            DeviceStatus.CanExit = false;
            while (!DeviceStatus.IsPaused)
            {
                NextFrame(RaySession.Review);
                Thread.Sleep((int)Constants.PlaybackInterval);
            }
            DeviceStatus.CanExit = true;
        }
    }
}
