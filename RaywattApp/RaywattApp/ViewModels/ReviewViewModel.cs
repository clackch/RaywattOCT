using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using RaywattApp.Common.Dialog;
using static RaywattOCT.RayCoreWrapper;
using RaywattApp.Common.Annotation.Models;
using System.Windows;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace RaywattApp.ViewModels
{
    public partial class Indicator : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Indicator));

        public bool isCaptured = false;

        [ObservableProperty]
        public Visibility _isVisible;

        [ObservableProperty]
        public double _x;

        [ObservableProperty]
        public double _y;

        private ICommand _cmdSetCaptured;
        public ICommand CmdSetCaptured
        { 
            get {return _cmdSetCaptured ?? (this._cmdSetCaptured = new RelayCommand<bool>(SetCaptured)); }
        }

        private void SetCaptured(bool isCaptured) 
        {
            this.isCaptured = isCaptured;
        }
    }

    public partial class ReviewViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

        private double degree = 90;
        public double Degree
        {
            get { return degree; }
            set { degree = value; OnPropertyChanged(nameof(Degree)); RaySetProperty(Property.Degree, degree); }
        }

        // size from view
        private double crossSectionWidth;
        private double crossSectionHeight;
        private double crossSectionBigWidth;
        private double crossSectionBigHeight;
        private double crossSectionSmallWidth;
        private double crossSectionSmallHeight;
        private double longitudeWidth;
        private double longitudeIndicatorWidth;

        [ObservableProperty]
        private Indicator _indicatorCrossSection;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private double _pointLongitudeX;

        [ObservableProperty]
        private string _playPauseState;

        [ObservableProperty]
        private bool _isLumenProfile;

        [ObservableProperty]
        private bool _isContourOn;

        [ObservableProperty]
        private bool _isAngioOn;

        private double _rightSideBarExpand;
        public double RightSideBarExpand
        {
            get { return _rightSideBarExpand; }
            set { _rightSideBarExpand = value; OnPropertyChanged(nameof(RightSideBarExpand)); }
        }

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        [ObservableProperty]
        private Visibility _visibleMeasurement;

        private int measurementFrameNumber = -1;
        public int MeasurementFrameNumber 
        { 
            get { return measurementFrameNumber; } 
            set 
            { 
                if(VisibleMeasurement == Visibility.Visible)
                {
                    measurementFrameNumber = value;
                    OnPropertyChanged(nameof(MeasurementFrameNumber));
                }
            } 
        }

        private int outFrameNumber;
        public int OutFrameNumber
        {
            get { return outFrameNumber; }
            set
            {
                outFrameNumber = value;
                RayMoveToFrame(value);
            }
        }

        private List<Measurement> measurements;
        public List<Measurement> Measurements { get { return measurements; } set { measurements = value; OnPropertyChanged(nameof(Measurements)); } }

        [ObservableProperty]
        private ObservableCollection<Bookmark> bookmarks;

        private ICommand _measurementCommand;
        public ICommand MeasurementCommand
        {
            get { return this._measurementCommand ?? (this._measurementCommand = new RelayCommand(ToggleMeasurement, CanToggleMeasurement)); }
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

        private ICommand _toggleLongitudeCommand;
        public ICommand ToggleLongitudeCommand
        {
            get { return this._toggleLongitudeCommand ?? (this._toggleLongitudeCommand = new RelayCommand<string>(ToggleLongitude)); }
        }

        private ICommand _coRegistrationCommand;
        public ICommand CoRegistrationCommand
        {
            get { return this._coRegistrationCommand ?? (this._coRegistrationCommand = new RelayCommand(CoRegistration, CanCoRegistration)); }
        }

        private ICommand _toggleContourCommand;
        public ICommand ToggleContourCommand
        {
            get { return this._toggleContourCommand ?? (this._toggleContourCommand = new RelayCommand(ToggleContour)); }
        }

        private ICommand _toggleAngioCommand;
        public ICommand ToggleAngioCommand
        {
            get { return this._toggleAngioCommand ?? (this._toggleAngioCommand = new RelayCommand(ToggleAngio)); }
        }

        public ReviewViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewViewModel");

            Constants.CurrentPage = Constants.ReviewPage;

            IndicatorCrossSection = new Indicator();
            IndicatorCrossSection.IsVisible = Visibility.Collapsed;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.IsVisible = Visibility.Collapsed;
            IndicatorLongitude.PropertyChanged += OnIndicatorLongitudeMoved;

            VisibleMeasurement = Visibility.Collapsed;
            ExpandLeftUpMenu = true;
            ExpandLeftDownMenu = true;
            ExpandRightMenu = true;
            IsLumenProfile = true;
            IsContourOn = false;
            IsAngioOn = false;
            RightSideBarExpand = Constants.RightSideBarExpandDefaultSize;

            updatePlayPauseState();
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
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];

                SetMeasurements(PatientCase.Id);
            }

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
            timerUpdateImage.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
            Save();

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();
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
                double pauseState = RayGetProperty(Property.IsPaused);
                if(pauseState == 1)
                    VisibleMeasurement = Visibility.Collapsed;

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
                if (IsAngioOn)
                {
                    crossSectionWidth = crossSectionSmallWidth;
                    crossSectionHeight = crossSectionSmallHeight;
                }
                else
                {
                    crossSectionWidth = crossSectionBigWidth;
                    crossSectionHeight = crossSectionBigHeight;
                }

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
                    crossSectionBigWidth = actualWidth;
                    crossSectionBigHeight = actualHeight;
                }
                else if (viewName.Equals("crossSectionImageSmall"))
                {
                    crossSectionSmallWidth = actualWidth;
                    crossSectionSmallHeight = actualHeight;
                }
                else if (viewName.Equals("longitude"))
                {
                    longitudeWidth = actualWidth;
                }
                else if (viewName.Equals("longitudeIndicatorImage")) 
                {
                    longitudeIndicatorWidth = actualHeight; // 90 degree rotated
                }
            }
        }

        protected override void Save()
        {
            _log.Debug("Save");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["comment"] = PatientCase.Comment;
            sqlParameters["vessel"] = PatientCase.Vessel;
            sqlParameters["procedure"] = PatientCase.Procedure;
            sqlParameters["brightness"] = PatientCase.Brightness;
            sqlParameters["contrast"] = PatientCase.Contrast;
            sqlParameters["angio_co_registration"] = PatientCase.AngioCoRegistration;
            sqlParameters["preset_name"] = PatientCase.PresetName;
            sqlParameters["calcium_threshold"] = PatientCase.CalciumThreshold;
            sqlParameters["expansion_calculation"] = PatientCase.ExpansionCalculation;
            sqlParameters["expansion_threshold"] = PatientCase.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = PatientCase.AppositionThreshold;
            PatientCase.Measurements = ConvertMeasurementsToJson();
            sqlParameters["measurements"] = PatientCase.Measurements;
            PatientCase.Bookmarks = JsonConvert.SerializeObject(Bookmarks, Formatting.Indented);
            sqlParameters["bookmarks"] = PatientCase.Bookmarks;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
                _log.Error("Update Error");
        }

        private bool CanToggleMeasurement()
        {
            return !IsAngioOn;
        }

        private void ToggleMeasurement()
        {
            double pauseState = RayGetProperty(Property.IsPaused);

            if(pauseState == 0)
            {
                var result = (RayError)RayPlayPause();
                if (result == RayError.OK)
                {
                    updatePlayPauseState();
                }
            }

            if (VisibleMeasurement == Visibility.Collapsed)
            {
                VisibleMeasurement = Visibility.Visible;
            }
            else
            {
                VisibleMeasurement = Visibility.Collapsed;
            }
        }

        private void SetMeasurements(string id)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            IList<StringModel> jsonAnnotation = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);
            if(jsonAnnotation == null || jsonAnnotation.Count != 1 || String.IsNullOrEmpty(jsonAnnotation[0].ReturnString))
            {
                Measurements = new List<Measurement>();
            }
            else
            {
                Measurements = JsonConvert.DeserializeObject<List<Measurement>>(jsonAnnotation[0].ReturnString);   
            }

            if (jsonAnnotation == null || jsonAnnotation.Count != 1 || String.IsNullOrEmpty(jsonAnnotation[0].ReturnString2))
            {
                Bookmarks = new ObservableCollection<Bookmark>();
            }
            else
            {
                Bookmarks = JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(jsonAnnotation[0].ReturnString2);
            }            
        }

        private string ConvertMeasurementsToJson()
        {
            List<Measurement> measurements = new List<Measurement>();

            foreach(Measurement measurement in Measurements)
            {
                if(measurement.AreaGeometries.Count > 0 || measurement.LengthGeometries.Count > 0 || measurement.TextGeometries.Count > 0)
                {
                    if (measurement.AreaGeometries.Count > 0)
                    {
                        foreach (AreaGeometry geometry in measurement.AreaGeometries)
                        {
                            geometry.PointsAll = null;
                            geometry.Path = null;
                        }
                    }
                    measurements.Add(measurement);
                }
            }

            return JsonConvert.SerializeObject(measurements, Formatting.Indented);
        }

        private void ToggleLongitude(string param)
        {
            if (param.Equals(Constants.LongitudeProfile))
            {
                if(IsLumenProfile)
                    return;

                IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                IsLumenProfile = true;
            }
            else
            {
                if (!IsLumenProfile)
                    return;

                IndicatorCrossSection.IsVisible = Visibility.Visible;
                IsLumenProfile = false;
            }
        }

        private bool CanCoRegistration()
        {
            return IsAngioOn;
        }

        private void CoRegistration()
        {
            _log.Debug("CoRegistration");
        }

        private void ToggleContour()
        {
            IsContourOn = !IsContourOn;
        }

        private void ToggleAngio()
        {
            IsAngioOn = !IsAngioOn;

            if (IsAngioOn)
            {
                RightSideBarExpand = Constants.RightSideBarExpandAngioSize;
                VisibleMeasurement = Visibility.Collapsed;
            }
            else
            {
                RightSideBarExpand = Constants.RightSideBarExpandDefaultSize;
            }

            (CoRegistrationCommand as RelayCommand).NotifyCanExecuteChanged();
            (MeasurementCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);

                if (!IndicatorLongitude.isCaptured) updateNavigator(crossSectionFrameInfo.curFrame, crossSectionFrameInfo.totalFrame);

                FrameNumber = crossSectionFrameInfo.curFrame;
                MeasurementFrameNumber = FrameNumber;

            }
            if (imgLongitude != null)
            {
                RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);

                if (state == RayScannerState.Review)
                {
                    LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
                    if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame) IndicatorLongitude.IsVisible = Visibility.Visible;

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
            else 
            {
                PlayPauseState = _l10n["Pause"];
            }
        }

        private void updateNavigator(int curFrame, int totalFrame)
        {
            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= longitudeWidth;
            IndicatorLongitude.X = curPosition + longitudeIndicatorWidth / 2;
        }

        private void setCurrentFrame(double navigatorPosition)
        {
            double curPosition = (navigatorPosition + longitudeIndicatorWidth / 2) / (double)longitudeWidth;

            if (longitudeFrameInfo != null)
            {
                curPosition *= (longitudeFrameInfo.totalFrame - 1);
                RayMoveToFrame((int)curPosition);
            }
        }
    }
}
