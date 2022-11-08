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
using OpenCvSharp;
using System.Windows.Media.Imaging;
using RaywattOCT;
using RaywattApp.Common.Util;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using RaywattApp.Common.Dialog;
using RaywattApp.Views.Dialog;
using RaywattApp.Common.Converters;
using System.Reflection;
using System.Diagnostics.Contracts;

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
            set { degree = value; OnPropertyChanged(nameof(Degree)); RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.Degree, degree); }
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
        private Dictionary<string, string> _vesselComboBox;

        [ObservableProperty]
        private Dictionary<string, string> _procedureComboBox;

        [ObservableProperty]
        private Dictionary<string, string> _physicianComboBox;

        [ObservableProperty]
        private string _currentVessel;

        private string previousVesselKey;

        private string vesselOther;

        private bool vesselOpened;

        private bool vesselDialogOpened;

        [ObservableProperty]
        private string _currentProcedure;

        private string previousProcedureKey;

        private string procedureOther;

        private bool proceduereOpened;

        [ObservableProperty]
        private string _currentPhysician;

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

        private ICommand _vesselChangedCommand;
        public ICommand VesselChangedCommand
        {
            get { return this._vesselChangedCommand ?? (this._vesselChangedCommand = new RelayCommand<KeyValuePair<string, string>>(ChangedVessel)); }
        }

        private ICommand _vesselOpenedCommand;
        public ICommand VesselOpenedCommand
        {
            get { return this._vesselOpenedCommand ?? (this._vesselOpenedCommand = new RelayCommand<KeyValuePair<string, string>>(OpenVessel)); }
        }

        private ICommand _vesselClosedCommand;
        public ICommand VesselClosedCommand
        {
            get { return this._vesselClosedCommand ?? (this._vesselClosedCommand = new RelayCommand<KeyValuePair<string, string>>(CloseVessel)); }
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

            VesselComboBox = new Dictionary<string, string>();

            ProcedureComboBox = new Dictionary<string, string>();

            PhysicianComboBox = new Dictionary<string, string>();

            vesselOpened = false;

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

                SetInit();

                RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.BackgroundColor, 0xFFFFFF);
                RayCoreWrapper.RayStartReview(PatientCase.Image);
            }

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(5);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
            timerUpdateImage.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            RayCoreWrapper.RayEndReview();
        }
        private void OnIndicatorLongitudeMoved(object sender, EventArgs e)
        {
            setCurrentFrame(IndicatorLongitude.X);
        }

        private void SetInit()
        {
            Dictionary<string, string> procedure = CodeDefinition.Codes["PROC"];

            CurrentVessel = CodeDefinition.Codes["VESS"].FirstOrDefault(x => x.Value == PatientCase.Vessel).Key;
            
            if (CurrentVessel == null)
            {
                VesselComboBox = GetVesselList(PatientCase.Vessel);
                CurrentVessel = "$OTH";
                vesselOther = PatientCase.Vessel;
            }
            else
            {
                VesselComboBox = GetVesselList();
            }
            
            previousVesselKey = CurrentVessel;

            foreach (var item in procedure)
            {
                ProcedureComboBox[item.Key] = item.Value;
            }
            CurrentProcedure = CodeDefinition.Codes["PROC"].FirstOrDefault(x => x.Value == PatientCase.Procedure).Key;

            if (CurrentProcedure == null)
            {
                ProcedureComboBox["$OTH"] = PatientCase.Procedure;
                CurrentProcedure = "$OTH";
            }

            previousProcedureKey = CurrentProcedure;
        }

        private void ChangedVessel(KeyValuePair<string, string> selectedVessel)
        {
            _log.Debug("ChangedVessel");

            if (vesselOpened || vesselDialogOpened)
                return;

            if (selectedVessel.Key == "$OTH")
            {
                vesselDialogOpened = true;

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Vessel"];
                parameter["other"] = vesselOther;
                var result = _dialogService.OpenDialog(new EditOctInfoDialogControl(), parameter);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                {
                    Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                    vesselOther = data["other"].ToString();

                    if (vesselOther != "")
                    {
                        VesselComboBox = GetVesselList(vesselOther);
                        CurrentVessel = "$OTH";
                        previousVesselKey = CurrentVessel;
                    }
                }
                else
                {
                    VesselComboBox = GetVesselList();
                    CurrentVessel = previousVesselKey;
                }

                vesselDialogOpened = false;
            }
            else if(previousVesselKey == "$OTH")
            {
                VesselComboBox = GetVesselList();
                previousVesselKey = selectedVessel.Key;
            }
            else
            {
                previousVesselKey = selectedVessel.Key;
            }
        }

        private void OpenVessel(KeyValuePair<string, string> selectedVessel)
        {
            _log.Debug("OpenVessel");

            if (selectedVessel.Key == "$OTH")
            {
                VesselComboBox = GetVesselList();
                vesselOpened = true;
                CurrentVessel = "$OTH";
                vesselOpened = false;
            }
        }

        private void CloseVessel(KeyValuePair<string, string> selectedVessel)
        {
            _log.Debug("CloseVessel");

            if (selectedVessel.Key == "$OTH")
            {
                VesselComboBox = GetVesselList(vesselOther);
                vesselOpened = true;
                CurrentVessel = "$OTH";
                vesselOpened = false;
            }
        }

        private void Playback(object param)
        {
            string action = (string)param;
            RayCoreWrapper.RayError result = RayCoreWrapper.RayError.OK;

            if (action.ToLower().Equals("prev"))
            {
                result = (RayCoreWrapper.RayError)RayCoreWrapper.RayPrevFrame();
            }
            else if (action.ToLower().Equals("next"))
            {
                result = (RayCoreWrapper.RayError)RayCoreWrapper.RayNextFrame();
            }
            else if (action.ToLower().Equals("play"))
            {
                result = (RayCoreWrapper.RayError)RayCoreWrapper.RayPlayPause();
                if (result == RayCoreWrapper.RayError.OK)
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

        public Dictionary<string, string> GetVesselList(string? other = null)
        {
            Dictionary<string, string> vessel = CodeDefinition.Codes["VESS"];
            Dictionary<string, string> vesselComboBox = new Dictionary<string, string>();
            foreach (var item in vessel)
            {
                vesselComboBox[item.Key] = item.Value;
            }

            if(other != null)
            {
                vesselComboBox["$OTH"] = other;
            }

            return vesselComboBox;
        }

        private void EndReview()
        {
            _log.Debug("EndReview");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["comment"] = PatientCase.Comment;

            if(CurrentVessel == "$OTH")
            {
                sqlParameters["vessel"] = vesselOther;
            }
            else
            {
                sqlParameters["vessel"] = CurrentVessel;
            }
            
            sqlParameters["procedure"] = CurrentProcedure;

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
            parameter["physicianName"] = PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;
            var result = _dialogService.OpenDialog(new EditCaseInfoDialogControl(), parameter);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
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
                RayCoreWrapper.RayScannerState state = (RayCoreWrapper.RayScannerState)RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.CurrentState);

                if (state == RayCoreWrapper.RayScannerState.Review)
                {
                    LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
                    if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame) IndicatorLongitude.IsVisible = "Visible";

                }
            }
        }
        private void updatePlayPauseState()
        {
            double pauseState = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.IsPaused);

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
                RayCoreWrapper.RayMoveToFrame((int)curPosition);
            }
        }
        private void setBrightnessContrast()
        {
            double propBrightness = ((double)Brightness / 100) * (RayCoreWrapper.BrightnessMax - RayCoreWrapper.BrightnessMin) + RayCoreWrapper.BrightnessMin;
            double propContrast = ((double)Contrast / 100) * (RayCoreWrapper.ContrastMax - RayCoreWrapper.ContrastMin) + RayCoreWrapper.ContrastMin;

            RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.Brightness, propBrightness);
            RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.Contrast, propContrast);
        }


        protected override void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState state)
        {
            switch (state)
            {
                case RayCoreWrapper.RayScannerState.Review:
                    updatePlayPauseState();
                    break;
                default:
                    break;
            }
        }

        protected override void handleProgress(RayCoreWrapper.RayCallbackRequest request, int progress)
        {
        }

        protected override void handleError(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayError error)
        {
        }

        protected override void handleWorkDone(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayWorkItem work)
        {
        }
    }
}
