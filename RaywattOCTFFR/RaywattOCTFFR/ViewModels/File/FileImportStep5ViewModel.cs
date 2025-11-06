using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System;
using System.Collections.Generic;
using static RaywattOCT.RayCoreFFRWrapper;
using System.Windows.Navigation;
using System.Windows;
using RaywattOCTFFR.Common.Util;
using OpenCvSharp;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using RaywattOCTFFR.Common.Annotation.Util;
using Point = System.Windows.Point;
using RaywattOCTFFR.Common.Annotation.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep5ViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep5ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private CallbackFunctionForDetection cbMlData;
        public CallbackFunctionForDetection CbMlData => (this.cbMlData) ?? (this.cbMlData = new CallbackFunctionForDetection(OnRecvMlData));

        private bool isMlDataFrontDone;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        [ObservableProperty]
        private List<double> _guideWireRadiusList;

        public FileImportStep5ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportStep4ViewModel");

            Constants.CurrentPage = Constants.FileImportStep5Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Collapsed;
            IndicatorLongitude.IsEnabled = false;

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;

            DeviceStatus.IsLumenLoaded = false;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
            base.OnNavigated(sender, navigatedEventArgs);

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                if (data.TryGetValue("fileImport", out var fileImportObj) && fileImportObj is FileImport fileImportData)
                {
                    FileImport = fileImportData;
                    PatientCase = FileImport.PatientCase;

                    InitializeImportData(data);

                    StopPlayback();

                    DrawSheathIndicator(PatientCase.SheathDiameter * CommonUtil.GetZOffsetScale(PatientCase.ZOffset));

                    CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault) * CommonUtil.GetZOffsetScale(PatientCase.ZOffset);

                    Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, PatientCase.NumOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
                    Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, PatientCase.NumOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);
                    Section.CalcMean(LumenContours, PatientCase.SectionProximal, PatientCase.SectionDistal);

                    InitializeMlData();

                    if (Constants.ImportTypeRaw.Equals(PatientCase.ImportType))
                    {
                        RayError result = (RayError)RayRegisterDetectionCallback(Marshal.GetFunctionPointerForDelegate(CbMlData));
                        if (result != RayError.OK)
                        {
                            _log.Error("RayRegisterDetectionCallback Error");
                        }

                        RayStartLumenDetection();
                    }
                    else
                    {
                        Task.Run(() => MlDataProcess());
                    }                        
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            base.OnNavigating(sender, navigationEventArgs);

            if (Constants.ImportTypeRaw.Equals(PatientCase.ImportType))
            {
                RayError result = (RayError)RayUnregisterDetectionCallback();
                if (result != RayError.OK)
                {
                    _log.Error("RayUnregisterDetectionCallback Error");
                }

                if (this.isEndReview)
                    RayEndReview();
            }
        }

        protected override void Back()
        {
            _log.Debug("Back");

            this.isEndReview = false;

            MoveImportPage(Constants.FileImportStep4Page);
        }

        protected override void Next()
        {
            _log.Debug("Next");

            this.isEndReview = false;

            //MoveImportPage(Constants.ReviewPage);
        }

        private void OnRecvMlData(int frame)
        {
            if (LumenContours == null || LumenContours.Count != PatientCase.NumOfFrames || frame <= 0)
                return;

            if (!this.isMlDataFrontDone)
            {
                this.isMlDataFrontDone = true;

                for (int curFrame = 0; curFrame < frame; curFrame++)
                {
                    GetMlData(curFrame);
                }
            }

            GetMlData(frame);

            Application.Current.Dispatcher.Invoke(() =>
            {
                MoveToFrame((int)RaySession.Review, frame);

                if (PatientCase.NumOfFrames - 1 == frame)
                {
                    SetLumenProfileInit();
                    MinimalValueChanged();
                    Section.Proximal.IsEnabled = true;
                    Section.Distal.IsEnabled = true;
                    IndicatorLongitude.IsEnabled = true;
                }

                DrawLumenProfile(frame);
            });

            if (PatientCase.NumOfFrames - 1 == frame)
            {
                DeviceStatus.IsLumenLoaded = true;
                Playback();
            }
        }

        private async void MlDataProcess()
        {
            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
            int total = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;

            for (int idx = 0; idx < total; idx++)
            {
                Mat gray = ConvertTo8BitGray(CrossSectionImages[idx]);

                //전송 메타데이터 계산
                int w = gray.Cols;
                int h = gray.Rows;
                int ch = 1;                     // 강제 Gray
                int step = (int)gray.Step();    // 보통 w * 1 이지만 step 사용이 안전

                //byte[]로 복사 (step*h 크기!)
                var bytes = new byte[step * h];
                Marshal.Copy(gray.Data, bytes, 0, bytes.Length);

                //ML Process(Segment/Detect)
                RayError result = (RayError)RayRunImageAnalysis(bytes, w, h, ch, step);

                GetMlData(idx, true);

                // UI 작업만 Dispatcher에서 실행
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MoveToFrame((int)RaySession.Review, idx);

                    if (idx == total - 1)
                    {
                        SetLumenProfileInit();
                        MinimalValueChanged();
                        Section.Proximal.IsEnabled = true;
                        Section.Distal.IsEnabled = true;
                        IndicatorLongitude.IsEnabled = true;
                    }

                    DrawLumenProfile(idx);
                });
            }

            DeviceStatus.IsLumenLoaded = true;
            Playback();
        }

        private Mat ConvertTo8BitGray(Mat src)
        {
            Mat src8;
            if (src.Depth() == MatType.CV_8U)
            {
                src8 = src;
            }
            else if (src.Depth() == MatType.CV_16U)
            {
                // 16U → 8U (빠른 방법: 비트 시프트에 해당하는 스케일)
                src8 = new Mat();
                src.ConvertTo(src8, MatType.CV_8U, 1.0 / 256.0, 0.0);
            }
            else if (src.Depth() == MatType.CV_32F)
            {
                using var tmp = new Mat();
                Cv2.Normalize(src, tmp, 0, 255, NormTypes.MinMax);
                src8 = new Mat();
                tmp.ConvertTo(src8, MatType.CV_8U);
            }
            else
            {
                // 필요 시 다른 depth 케이스 추가
                src8 = new Mat();
                src.ConvertTo(src8, MatType.CV_8U);
            }

            // 2) 채널을 1채널 Gray로 강제
            Mat gray = src8;
            if (src8.Channels() == 3)
            {
                gray = new Mat();
                Cv2.CvtColor(src8, gray, ColorConversionCodes.BGR2GRAY);
            }
            else if (src8.Channels() == 4)
            {
                gray = new Mat();
                Cv2.CvtColor(src8, gray, ColorConversionCodes.BGRA2GRAY);
            }
            // 1채널이면 그대로 gray 사용

            // 3) 연속(Continuous) 메모리 보장
            if (!gray.IsContinuous())
                gray = gray.Clone();

            return gray;
        }

        private void InitializeMlData()
        {
            LumenContours = new List<LumenContour>();
            LumenSidebranches = new List<LumenSidebranch>();
            LumenStents = new List<LumenStent>();
            LumenGuidewires = new List<LumenGuidewire>();
            GuideWireRadiusList = new List<double>();

            for (int i = 0; i < PatientCase.NumOfFrames; i++)
            {
                //lumen
                LumenContour lumenContour = new LumenContour();
                DiameterInfo diameterInfo = new DiameterInfo();
                diameterInfo.value = 0.0;

                lumenContour.MlContour.Points = new List<Point>();
                lumenContour.MlContour.MaxDiameter = diameterInfo;
                lumenContour.MlContour.MinDiameter = diameterInfo;
                lumenContour.Points = new List<Point>();
                lumenContour.MaxDiameter = diameterInfo;
                lumenContour.MinDiameter = diameterInfo;

                LumenContours.Add(lumenContour);

                //sidebranch
                LumenSidebranch lumenSidebranch = new LumenSidebranch();
                LumenSidebranches.Add(lumenSidebranch);

                //stent
                LumenStent lumenStent = new LumenStent();
                LumenStents.Add(lumenStent);

                //guidewire
                LumenGuidewire lumenGuidewire = new LumenGuidewire();
                LumenGuidewires.Add(lumenGuidewire);
            }
        }

        private void GetMlData(int frameInfo, bool isGetZero = false)
        {
            int frameNumber = isGetZero ? 0 : frameInfo;

            //lumen
            int num = RayGetNumOfLumenContourPoints(frameNumber);
            if (num > 2)
            {
                IntPtr contour = RayGetLumenContour(frameNumber);
                if (contour == IntPtr.Zero) return;

                Mat matContour = CommonUtil.ByteMemoryToCvMat(contour, 1, num, 2);

                LumenContours[frameInfo].MlContour.Points = new List<Point>();
                for (int row = 0; row < matContour.Rows; row++)
                {
                    Vec2i point = matContour.At<Vec2i>(0, row);
                    LumenContours[frameInfo].MlContour.Points.Add(new Point(point.Item0, point.Item1));
                }
                ContourMeasurement contourMeasurement = new ContourMeasurement();
                contourMeasurement.Measure(LumenContours[frameInfo].MlContour, (int)Constants.OCTImageSize, (int)Constants.OCTImageSize);
                if (LumenContours[frameInfo].MlContour.Valid)
                {
                    contourMeasurement.CalculateDiameter(LumenContours[frameInfo].MlContour);
                }
                LumenContours[frameInfo].ResetLumenContour();
            }

            //side branch
            int sbSize = RayGetNumOfSidebranchContourSize(frameNumber);
            if (sbSize > 0)
            {
                LumenSidebranches[frameInfo].Points = new List<List<Point>>();
                for (int i = 0; i < sbSize; i++)
                {
                    int height = RayGetNumOfSidebranchContourPoints(frameNumber, i);
                    if (height > 2)
                    {
                        IntPtr contour = RayGetSidebranchContour(frameNumber, i);
                        if (contour == IntPtr.Zero) return;

                        Mat matContour = CommonUtil.ByteMemoryToCvMat(contour, 1, height, 2);

                        List<Point> contourPoints = new List<Point>();
                        for (int row = 0; row < matContour.Rows; row++)
                        {
                            Vec2i point = matContour.At<Vec2i>(0, row);
                            contourPoints.Add(new Point(point.Item0, point.Item1));
                        }
                        LumenSidebranches[frameInfo].Points.Add(contourPoints);
                    }
                }
            }

            //stent
            int stentHeight = RayGetNumOfStentPoints(frameNumber);
            if (stentHeight > 0)
            {
                IntPtr contour = RayGetStentPoints(frameNumber);
                if (contour == IntPtr.Zero) return;

                LumenStents[frameInfo].Points = new List<Point>();
                LumenStents[frameInfo].AppositionLength = new List<double>();

                Mat mat = CommonUtil.ByteMemoryToCvMat(contour, 1, stentHeight, 2);
                OpenCvSharp.Point[][] lumenContours = CommonUtil.GetLumenContours(LumenContours[frameInfo].Points);

                if (lumenContours != null)
                {
                    for (int row = 0; row < mat.Rows; row++)
                    {
                        Vec2i point = mat.At<Vec2i>(0, row);
                        LumenStents[frameInfo].Points.Add(new Point(point.Item0, point.Item1));
                        LumenStents[frameInfo].AppositionLength.Add(CommonUtil.GetAppositionLength(lumenContours, new OpenCvSharp.Point(point.Item0, point.Item1)));
                    }
                }
            }

            //guide wire
            int guidewireHeight = RayGetNumOfGuidewirePoints(frameNumber);
            if (guidewireHeight > 0)
            {
                IntPtr contour = RayGetGuidewirePoints(frameNumber);
                if (contour == IntPtr.Zero) return;

                IntPtr radius = RayGetGuidewireRadius(frameNumber);
                if (radius == IntPtr.Zero) return;

                Mat mat = CommonUtil.ByteMemoryToCvMat(contour, 1, guidewireHeight, 2);
                LumenGuidewires[frameInfo].Points = new List<Point>();

                unsafe
                {
                    float* doublePtr = (float*)radius.ToPointer();
                    for (int row = 0; row < mat.Rows; row++)
                    {
                        Vec2i point = mat.At<Vec2i>(0, row);
                        if (point.Item0 < 0 || point.Item0 < 0)
                        {
                            LumenGuidewires[frameInfo].Points.Add(new Point(0, 0));
                        }
                        else
                        {
                            LumenGuidewires[frameInfo].Points.Add(new Point(point.Item0, point.Item1));
                        }
                        GuideWireRadiusList.Add(*(doublePtr + row));
                    }
                }
            }
        }
    }
}
