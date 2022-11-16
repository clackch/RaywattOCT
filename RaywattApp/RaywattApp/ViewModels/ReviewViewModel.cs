using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Linq;
using System.Windows.Threading;
using RaywattApp.Common.Dialog;
using RaywattApp.Views.Dialog;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class Indicator : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Indicator));

        public bool isCaptured = false;
        [ObservableProperty]
        public string _isVisible;
        [ObservableProperty]
        public double _x;
        [ObservableProperty]
        public double _y;

        private ICommand _cmdSetCaptured;
        public ICommand CmdSetCaptured
        { 
            get {return _cmdSetCaptured ?? (this._cmdSetCaptured = new RelayCommand<bool>(SetCaptured)); }
        }

        private void SetCaptured(bool isCaptured) {
            this.isCaptured = isCaptured;
        }
    }

    public partial class ReviewViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private double degree = 90;
        public double Degree
        {
            get { return degree; }
            set { degree = value; OnPropertyChanged(nameof(Degree)); RaySetProperty(Property.Degree, degree); }
        }

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

        // size from view
        private double crossSectionWidth;
        private double crossSectionHeight;
        private double longitudeWidth;
        private double longitudeIndicatorWidth;

        [ObservableProperty]
        private Indicator _indicatorCrossSection;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private double _pointLongitudeX;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private string _playPauseState;

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _endReviewCommand;
        public ICommand EndReviewCommand
        {
            get { return this._endReviewCommand ?? (this._endReviewCommand = new RelayCommand(EndReview)); }
        }

        private ICommand _editCaseCommand;
        public ICommand EditCaseCommand
        {
            get { return this._editCaseCommand ?? (this._editCaseCommand = new RelayCommand(EditCase)); }
        }

        private ICommand _cmdPlayback;
        public ICommand CmdPlayback
        { 
            get { return this._cmdPlayback ?? (this._cmdPlayback = new RelayCommand<object>(Playback)); }
        }

        private ICommand _cmdRotateIndicator;
        public ICommand CmdRotateIndicator
        {
            get { return this._cmdRotateIndicator ?? (this._cmdRotateIndicator = new RelayCommand<object>(RotateIndicator)); }
        }

        private ICommand _cmdMoveIndicator;
        public ICommand CmdMoveIndicator
        {
            get { return this._cmdMoveIndicator ?? (this._cmdMoveIndicator = new RelayCommand<object>(MoveIndicator)); }
        }

        private ICommand _cmdViewSizeChanged;
        public ICommand CmdViewSizeChanged
        { 
            get { return this._cmdViewSizeChanged ?? (this._cmdViewSizeChanged = new RelayCommand<object[]>(ViewSizeChanged)); }
        }

        public ReviewViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.ReviewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            IndicatorCrossSection = new Indicator();
            IndicatorCrossSection.IsVisible = "Visible";

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.IsVisible = "Hidden";
            IndicatorLongitude.PropertyChanged += OnIndicatorLongitudeMoved;

            updatePlayPauseState();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];

                RaySetProperty(Property.BackgroundColor, 0xFFFFFF);
                RayStartReview(PatientCase.Image);

                syncWithCoreSystem();
            }

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(5);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
            timerUpdateImage.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            RayEndReview();
        }
        private void OnIndicatorLongitudeMoved(object sender, EventArgs e)
        {
            setCurrentFrame(IndicatorLongitude.X);
        }

        private void Playback(object param)
        {
            string action = (string)param;
            RayError result = RayError.OK;

            if (action.ToLower().Equals("prev"))
            {
                result = (RayError)RayPrevFrame();
            }
            else if (action.ToLower().Equals("next"))
            {
                result = (RayError)RayNextFrame();
            }
            else if (action.ToLower().Equals("play"))
            {
                result = (RayError)RayPlayPause();
                if (result == RayError.OK)
                {
                    updatePlayPauseState();
                }
            }
        }

        private void RotateIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.isCaptured)
            {
                double pointX = crossSectionWidth / 2 - indicator.X;
                double pointY = crossSectionHeight / 2 - indicator.Y;
                Degree = (int)(Math.Atan2(pointY, pointX) * 180 / Math.PI);
            }
        }
        private void MoveIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.isCaptured && PointLongitudeX >= 0)
            {
                indicator.X = PointLongitudeX + longitudeIndicatorWidth / 2;
            }
        }
        private void ViewSizeChanged(object[] param)
        {
            if (param != null && param.Length == 3) {
                string viewName = (string)param[0];
                double actualWidth = (double)param[1];
                double actualHeight = (double)param[2];

                if (viewName.Equals("crossSectionImage"))
                {
                    crossSectionWidth = actualWidth;
                    crossSectionHeight = actualHeight;
                }
                else if (viewName.Equals("longitudeImage"))
                {
                    longitudeWidth = actualWidth;
                }
                else if (viewName.Equals("longitudeIndicatorImage")) 
                {
                    longitudeIndicatorWidth = actualHeight; // 90 degree rotated
                }
            }
        }

        private void EndReview()
        {
            _log.Debug("EndReview");

            RayFinalize();

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["comment"] = PatientCase.Comment;
            sqlParameters["vessel"] = PatientCase.Vessel;
            sqlParameters["procedure"] = PatientCase.Procedure;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);

            if (nRows == 1)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
            }
        }

        private void EditCase()
        {
            _log.Debug("EditCase");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["vessel"] = PatientCase.Vessel;
            parameter["procedure"] = PatientCase.Procedure;
            parameter["physicianName"] = PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;
            var result = _dialogService.OpenDialog(new EditCaseInfoDialogControl(), parameter);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                PatientCase.Vessel = data["vessel"].ToString();
                PatientCase.Procedure = data["procedure"].ToString();
                PatientCase.PhysicianName = data["physicianName"].ToString();
                PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                PatientCase.Comment = data["comment"].ToString();
            }
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);

                if (!IndicatorLongitude.isCaptured) updateNavigator(crossSectionFrameInfo.curFrame, crossSectionFrameInfo.totalFrame);
            }
            if (imgLongitude != null)
            {
                RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);

                if (state == RayScannerState.Review)
                {
                    LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
                    if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame) IndicatorLongitude.IsVisible = "Visible";

                }
            }
        }
        private void updatePlayPauseState()
        {
            double pauseState = RayGetProperty(Property.IsPaused);

            if (pauseState != 0)
            {
                PlayPauseState = _l10n["Play"];
            }
            else {
                PlayPauseState = _l10n["Pause"];
            }
        }
        private void updateNavigator(int curFrame, int totalFrame)
        {
            double curPosition = (double)curFrame / totalFrame;
            curPosition = (curFrame == totalFrame - 1) ? 1 : curPosition;
            curPosition *= longitudeWidth;
            IndicatorLongitude.X = curPosition + longitudeIndicatorWidth / 2;
        }
        private void setCurrentFrame(double navigatorPosition)
        {
            double curPosition = (navigatorPosition + longitudeIndicatorWidth / 2) / (double)longitudeWidth;

            if (longitudeFrameInfo != null)
            {
                curPosition *= longitudeFrameInfo.totalFrame;
                RayMoveToFrame((int)curPosition);
            }
        }
        private void setBrightnessContrast()
        {
            double propBrightness = ((double)Brightness / 100) * (BrightnessMax - BrightnessMin) + BrightnessMin;
            double propContrast = ((double)Contrast / 100) * (ContrastMax - ContrastMin) + ContrastMin;

            RaySetProperty(Property.Brightness, propBrightness);
            RaySetProperty(Property.Contrast, propContrast);
        }
        private void syncWithCoreSystem()
        {
            double propBrightness = RayGetProperty(Property.Brightness);
            double propContrast = RayGetProperty(Property.Contrast);

            this.Brightness = (int)(((propBrightness - BrightnessMin) / (BrightnessMax - BrightnessMin)) * 100);
            this.Contrast = (int)(((propContrast - ContrastMin) / (ContrastMax - ContrastMin)) * 100);
        }


        protected override void handleState(RayCallbackRequest request, RayScannerState state)
        {
            switch (state)
            {
                case RayScannerState.Review:
                    updatePlayPauseState();
                    break;
                default:
                    break;
            }
        }

        protected override void handleProgress(RayCallbackRequest request, int progress)
        {
        }

        protected override void handleError(RayCallbackRequest request, RayError error)
        {
        }

        protected override void handleWorkDone(RayCallbackRequest request, RayWorkItem work)
        {
        }
    }
}
