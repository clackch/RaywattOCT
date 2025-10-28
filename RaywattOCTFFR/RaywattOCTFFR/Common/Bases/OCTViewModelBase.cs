using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using OpenCvSharp;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static RaywattOCT.RayCoreFFRWrapper;
using Point = OpenCvSharp.Point;

namespace RaywattOCTFFR.Common.Bases
{
    public abstract partial class OCTViewModelBase : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OCTViewModelBase));

        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        [ObservableProperty]
        private ObservableCollection<Mat> _crossSectionImages;

        protected Mat[] imgCrossSection = new Mat[2];
        protected Scalar[] crossSectionBackground = new Scalar[2];
        protected Mat imgCrossSectionMask;

        [ObservableProperty]
        protected double _crossSectionScale;

        [ObservableProperty]
        protected double _crossSectionScaleIndicator;

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        protected Mat imgLongitude;
        protected FrameInfo longitudeFrameInfo;

        [ObservableProperty]
        private BitmapSource _lumenProfileImage;

        [ObservableProperty]
        private BitmapSource _lumenProfileImageExtra;

        protected Mat imglumenProfile;

        protected Mat imglumenProfileExtra;

        [ObservableProperty]
        private BitmapSource _angioImage;

        [ObservableProperty]
        private BitmapSource _sheathIndicator;

        [ObservableProperty]
        private bool _isPaused = true;

        [ObservableProperty]
        private int _frameNumberForInit;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

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

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.PlaybackInterval);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);            
        }

        /// <summary>
        /// Navigation 시작시 - 이동 시작하는 화면에서 발생
        /// </summary>
        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            if (!DeviceStatus.IsPaused)
            {
                Playback();
            }
            RayError result = (RayError)RayUnregisterImageCallback();
            if (result != RayError.OK) 
            {
                _log.Error("RayUnregisterImageCallback Error");
            }
        }

        /// <summary>
        /// Navigation 완료시 - 이동 완료된 화면에서 발생
        /// </summary>
        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            RayError result = (RayError)RayRegisterImageCallback(
                Marshal.GetFunctionPointerForDelegate(CBCrossSection),
                Marshal.GetFunctionPointerForDelegate(CBLongitude));
            if (result != RayError.OK)
            {
                _log.Error("RayRegisterImageCallback Error");
            }
        }

        private void OnRecvCrossSection(int session, IntPtr data, int width, int height, int ch, int frameInfo, double isCleared)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgCrossSection[session] = imgRecv;
        }

        private void OnRecvLongitude(int session, IntPtr data, int width, int height, int ch, int frameInfo, double intensity)
        {
            Mat imgRecv = CommonUtil.ByteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv;
            longitudeFrameInfo = new FrameInfo(frameInfo);

            FrameNumberForInit = longitudeFrameInfo.curFrame - 1;

            Application.Current.Dispatcher.Invoke(() =>
            {
                DrawLongitudeImage();
                UpdateLumenProfile();
            });
        }

        protected bool DrawCrossSectionImage()
        {
            if (imgCrossSection[0] == null) return false;

            CrossSectionImage = DrawCrossSectionWithBackground(imgCrossSection[0], crossSectionBackground[0]);

            return true;
        }

        protected void SetCrossSectionBackground(RaySession session, int rgbCode) {
            crossSectionBackground[(int)session] = new Scalar(rgbCode & 0xFF, (rgbCode >> 8) & 0xFF, (rgbCode >> 16) & 0xFF);
        }

        protected bool DrawLongitudeImage()
        {
            if (imgLongitude == null) return false;

            LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
            return true;
        }

        protected bool DrawLumenProfileImage()
        {
            if (imglumenProfile == null) return false;

            LumenProfileImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfile);
            return true;
        }

        protected bool DrawLumenProfileImageExtra()
        {
            if (imglumenProfileExtra == null) return false;

            LumenProfileImageExtra = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileExtra);
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

        protected void DrawSheathIndicator(double sheathDiameter)
        {
            SheathIndicator = CommonUtil.DrawSheathIndicator((int)Constants.CrossSectionSize, sheathDiameter);
        }

        private static Mat GenerateMask(Mat image)
        {
            Mat mask = image.EmptyClone();
            Point center = new Point(mask.Width / 2, mask.Height / 2);

            mask.SetTo(Scalar.White);
            Cv2.Circle(mask, center, mask.Width / 2, Scalar.Black, -1);

            return mask;
        }

        protected bool PrevFrame(RaySession session)
        {
            int nNumOfFrames;

            if (CrossSectionImages != null && CrossSectionImages.Count > 0)
            {
                nNumOfFrames = CrossSectionImages.Count;
            }
            else
            {
                nNumOfFrames = (int)RayGetProperty(Property.ImageDepth);
            }
                
            int nMoveToFrame = DeviceStatus.ReviewImageInfos[(int)session].Current;

            nMoveToFrame--;
            nMoveToFrame = (nMoveToFrame < 0) ? nNumOfFrames - 1 : nMoveToFrame;
            
            return MoveToFrame(session, nMoveToFrame);
        }

        protected bool NextFrame(RaySession session)
        {
            int nNumOfFrames;

            if (CrossSectionImages != null && CrossSectionImages.Count > 0)
            {
                nNumOfFrames = CrossSectionImages.Count;
            }
            else
            {
                nNumOfFrames = (int)RayGetProperty(Property.ImageDepth);
            }
                
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

                timerUpdateImage.Start();
            }
            else
            {
                DeviceStatus.IsPaused = true;

                if(timerUpdateImage.IsEnabled)
                    timerUpdateImage.Stop();
            }

            IsPaused = DeviceStatus.IsPaused;
        }

        protected virtual bool MoveToFrame(RaySession session, int nFrame)
        {
            if (nFrame < 0)
                return false;

            Mat img = new Mat();

            if (CrossSectionImages != null && CrossSectionImages.Count > 0)
            {
                if(CrossSectionImages.Count > nFrame)
                    img = CrossSectionImages[nFrame];
            }
            else
            {
                RayError result = (RayError)RaySetSession(session);
                if (result != RayError.OK)
                {
                    _log.Debug("OCTViewModelBase MoveToFrame : result != RayError.OK");
                    return false;
                }

                IntPtr data = RayGetImageData(nFrame);
                if (data == IntPtr.Zero)
                {
                    _log.Debug("OCTViewModelBase MoveToFrame : data == IntPtr.Zero");
                    return false;
                }

                DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)session];
                img = CommonUtil.ByteMemoryToCvMat(data, imageInfo.Width, imageInfo.Height, imageInfo.Channels);
            }
              
            imgCrossSection[(int)session] = img;
            DeviceStatus.ReviewImageInfos[(int)session].Current = nFrame;

            UpdateCrossSectionImage();

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

        protected virtual void UpdateCrossSectionImage() { }

        protected virtual void UpdateLumenProfile() { }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DeviceStatus.CanExit = false;
            if (!DeviceStatus.IsPaused)
            {
                NextFrame(RaySession.Review);
            }
            DeviceStatus.CanExit = true;
        }

        protected static Mat BuildLongitude(IReadOnlyList<Mat> frames, int totalCount, double angleDeg, int thickness = 1)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (frames.Count == 0) throw new InvalidOperationException("frames에 최소 1장이 필요합니다.");
            if (totalCount <= 0) throw new ArgumentOutOfRangeException(nameof(totalCount));
            if (thickness <= 0) throw new ArgumentOutOfRangeException(nameof(thickness));

            var first = frames[0];
            if (first.Empty()) throw new ArgumentException("첫 프레임이 비어 있습니다.", nameof(frames));

            int H = first.Rows, W = first.Cols;
            var type = first.Type();

            // 중심/반경
            var c = new Point2f((W - 1) / 2f, (H - 1) / 2f);
            int rad = (int)Math.Floor(Math.Min(Math.Min(c.X, W - 1 - c.X), Math.Min(c.Y, H - 1 - c.Y)));
            rad = Math.Max(rad, 1);

            // θ(시계, 0°=위) → 화면 좌표 단위벡터
            double th = angleDeg * Math.PI / 180.0;
            double dx = Math.Sin(th);   // 0°→0, 90°→+1
            double dy = -Math.Cos(th);  // 0°→-1(위), 180°→+1(아래)

            // 두께 방향(직교) 벡터
            double vx = Math.Cos(th);
            double vy = Math.Sin(th);

            int outH = 2 * rad + 1;
            using var mapX = new Mat(outH, thickness, MatType.CV_32FC1);
            using var mapY = new Mat(outH, thickness, MatType.CV_32FC1);

            // ★ 포인트: 행 0 이 θ 방향의 가장 바깥(+rad)이 되도록 r = +rad .. -rad 로 매핑
            float cx = c.X, cy = c.Y, half = (thickness - 1) * 0.5f;
            unsafe
            {
                for (int row = 0; row < outH; row++)
                {
                    int r = rad - row; // +rad → ... → -rad
                    float bx = cx + (float)(dx * r);
                    float by = cy + (float)(dy * r);

                    float* px = (float*)mapX.Ptr(row);
                    float* py = (float*)mapY.Ptr(row);
                    for (int k = 0; k < thickness; k++)
                    {
                        float off = (k - half); // 가운데 정렬
                        px[k] = bx + (float)(vx * off);
                        py[k] = by + (float)(vy * off);
                    }
                }
            }

            // 출력 버퍼: [outH x (totalCount*thickness)]
            var output = new Mat(outH, totalCount * thickness, type, new Scalar(0x23, 0x23, 0x23));
            var inter = InterpolationFlags.Linear;

            int fill = Math.Min(frames.Count, totalCount);
            for (int i = 0; i < fill; i++)
            {
                var f = frames[i];
                if (f.Empty()) continue;

                using var slice = new Mat();
                Cv2.Remap(f, slice, mapX, mapY, inter, BorderTypes.Constant, Scalar.All(0));

                using var dst = new Mat(output, new OpenCvSharp.Rect(i * thickness, 0, thickness, outH));
                slice.CopyTo(dst);
            }

            return output; // 호출측 Dispose
        }
    }
}