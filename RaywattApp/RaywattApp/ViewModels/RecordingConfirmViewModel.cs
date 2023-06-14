using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Services;
using System.Windows.Navigation;
using System;
using System.Collections.Generic;
using static RaywattOCT.RayCoreWrapper;
using System.Windows.Threading;
using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Annotation.Util;
using RaywattApp.Common.Util;

namespace RaywattApp.ViewModels
{
    public partial class RecordingConfirmViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingConfirmViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private bool _isPullbackDone = false;

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _redoPullbackCommand;
        public ICommand RedoPullbackCommand
        {
            get { return this._redoPullbackCommand ?? (this._redoPullbackCommand = new RelayCommand(RedoPullback)); }
        }

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand
        {
            get { return this._confirmCommand ?? (this._confirmCommand = new RelayCommand(Confirm)); }
        }

        public RecordingConfirmViewModel(SqlManager sqlManager)
        {
            _log.Debug("RecordingConfirmViewModel");

            Constants.CurrentPage = Constants.RecordingConfirmPage;

            _sqlManager = sqlManager;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                PatientCase = (PatientCase)data["patientCase"];

                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();
        }

        private void RedoPullback()
        {
            _log.Debug("RedoPullback");

            DeviceStatus.IsLumenLoaded = true;

            RayEndReview();

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingLiveViewPage) { Parameter = parameter });
        }

        private void Confirm()
        {
            _log.Debug("Confirm");

            RaySetSession(RaySession.Review);
            int numOfFrames = (int) RayGetProperty(Property.ImageDepth);

            RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

            //TO-DO 초기값 정의 필요
            PatientCase.PhysicianName = Constants.NotSelected;
            PatientCase.AccessionNumber = "";
            PatientCase.AccessionName = "";
            PatientCase.Comment = "";
            PatientCase.Vessel = Constants.NotSelectedCode;
            PatientCase.ThumbnailNo = 1;
            PatientCase.StillImageYn = "N";
            PatientCase.AngioCoRegistration = DeviceStatus.IsAngioConnected;
            PatientCase.IndicatorDegree = 90;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            SetDetailStatusInit();
            parameter["prevStatus"] = PrevStatus;
            ReviewStatus reviewStatus = new ReviewStatus();
            reviewStatus.NumberOfFrames = numOfFrames;
            parameter["reviewStatus"] = reviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPresetPage) { Parameter = parameter });
        }

        private void SetDetailStatusInit()
        {
            PrevStatus.DetailSelectedGroup = null;
            PrevStatus.DetailPageOffset = 0;
            PrevStatus.DetailPageGroup = 1;
            PrevStatus.DetailPageNumber = 0;
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DrawCrossSectionImage();
            if (DrawLongitudeImage())
            {
                // when generating longitude image is completed
                if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame)
                {
                    PatientCase.Bookmark = "[]";
                    PatientCase.CrossSection = "[]";
                    PatientCase.Longitude = "";
                    PatientCase.LumenContour = getLumenContours();
                    IsPullbackDone = true;
                }
            }
        }

        private List<LumenContour> getLumenContours()
        {
            int numOfFrames = (int)RayGetProperty(Property.ImageDepth);

            List<LumenContour> lumenContours = new List<LumenContour>();
            for (int curFrame = 0; curFrame < numOfFrames; curFrame++)
            {
                int num = RayGetNumOfLumenContourPoints(curFrame);
                if (num > 0)
                {
                    IntPtr contour = RayGetLumenContour(curFrame);
                    if (contour == IntPtr.Zero) continue;

                    Mat matContour = CommonUtil.ByteMemoryToCvMat(contour, 1, num, 2);

                    LumenContour lumenContour = new LumenContour();
                    lumenContour.MlContour.Points = new List<System.Windows.Point>();
                    for (int row = 0; row < matContour.Rows; row++)
                    {
                        Vec2i point = matContour.At<Vec2i>(0, row);
                        lumenContour.MlContour.Points.Add(new System.Windows.Point(point.Item0, point.Item1));
                    }
                    ContourMeasurement contourMeasurement = new ContourMeasurement();
                    contourMeasurement.Measure(lumenContour.MlContour, (int)Constants.OCTImageSize, (int)Constants.OCTImageSize);
                    if (lumenContour.MlContour.Valid)
                    {
                        contourMeasurement.CalculateDiameter(lumenContour.MlContour);
                    }
                    lumenContour.CopyMlToLumenContour();
                    lumenContours.Add(lumenContour);
                }
            }

            return lumenContours;
        }

    }
}
