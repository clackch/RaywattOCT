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
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Annotation.Util;
using Point = System.Windows.Point;

namespace RaywattApp.ViewModels
{   
    public partial class ReviewViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

        private double degree;
        public double Degree
        {
            get { return degree; }
            set { degree = value; OnPropertyChanged(nameof(Degree)); RaySetProperty(Property.LongitudeDegree, degree); }
        }

        // size from view
        private Point crossSectionCenterBig = new Point();
        private Point crossSectionCenterSmall = new Point();
        private Point longitudeCoordinate = new Point();

        [ObservableProperty]
        private Indicator _indicatorCrossSection;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private double _pointLongitudeX;

        [ObservableProperty]
        private bool _isPaused;

        private double _rightSideBarExpand;
        public double RightSideBarExpand
        {
            get { return _rightSideBarExpand; }
            set { _rightSideBarExpand = value; OnPropertyChanged(nameof(RightSideBarExpand)); }
        }

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private int measurementFrameNumber = -1;
        public int MeasurementFrameNumber 
        { 
            get { return measurementFrameNumber; } 
            set 
            { 
                if(ReviewStatus.IsMeasurementOn)
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
    
        private string _measurementCommand;
        public string MeasurementCommand { get { return _measurementCommand; } set { _measurementCommand = value; OnPropertyChanged(nameof(MeasurementCommand)); } }

        private List<Measurement> measurements;
        public List<Measurement> Measurements { get { return measurements; } set { measurements = value; OnPropertyChanged(nameof(Measurements)); } }

        private bool isLongitudeMeasurementInit;

        private ObservableCollection<LengthGeometry> _lModeLengthGeometries;
        public ObservableCollection<LengthGeometry> LModeLengthGeometries { get { return _lModeLengthGeometries; } set { _lModeLengthGeometries = value; OnPropertyChanged(nameof(LModeLengthGeometries)); } }

        private List<TextGeometry> _lModeTextGeometries;
        public List<TextGeometry> LModeTextGeometries { get { return _lModeTextGeometries; } set { _lModeTextGeometries = value; OnPropertyChanged(nameof(LModeTextGeometries)); } }

        private double _lModeIndicatorX;
        public double LModeIndicatorX 
        { 
            get { return _lModeIndicatorX; } 
            set { _lModeIndicatorX = value; OnPropertyChanged(nameof(LModeIndicatorX)); setCurrentFrame(value); } 
        }

        private ICommand _toggleMeasurementCommand;
        public ICommand ToggleMeasurementCommand
        {
            get { return this._toggleMeasurementCommand ?? (this._toggleMeasurementCommand = new RelayCommand(ToggleMeasurement)); }
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
            get { return this._cmdViewSizeChanged ?? (this._cmdViewSizeChanged = new RelayCommand<object>(ViewSizeChanged)); }
        }

        private ICommand _toggleLongitudeCommand;
        public ICommand ToggleLongitudeCommand
        {
            get { return this._toggleLongitudeCommand ?? (this._toggleLongitudeCommand = new RelayCommand<bool>(ToggleLongitude)); }
        }

        private ICommand _coRegistrationCommand;
        public ICommand CoRegistrationCommand
        {
            get { return this._coRegistrationCommand ?? (this._coRegistrationCommand = new RelayCommand(CoRegistration)); }
        }

        private ICommand _toggleContourStentCommand;
        public ICommand ToggleContourStentCommand
        {
            get { return this._toggleContourStentCommand ?? (this._toggleContourStentCommand = new RelayCommand(ToggleContourStent)); }
        }

        private ICommand _toggleAngioCommand;
        public ICommand ToggleAngioCommand
        {
            get { return this._toggleAngioCommand ?? (this._toggleAngioCommand = new RelayCommand<bool>(ToggleAngio)); }
        }

        public ReviewViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewViewModel");

            Constants.CurrentPage = Constants.ReviewPage;

            IndicatorCrossSection = new Indicator();
            IndicatorCrossSection.IsVisible = Visibility.Collapsed;
            IndicatorCrossSection.IsCrossSection = true;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Collapsed;

            ExpandLeftUpMenu = true;
            ExpandLeftDownMenu = true;
            ExpandRightMenu = true;
            RightSideBarExpand = Constants.RightSideBarExpandDefaultSize;

            isLongitudeMeasurementInit = false;

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
                if (data.ContainsKey("reviewStatus"))
                {
                    ReviewStatus = (ReviewStatus)data["reviewStatus"];
                    ToggleAngio(ReviewStatus.IsAngioOn);
                    if (ReviewStatus.IsLumenProfile)
                        IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                    else
                        IndicatorCrossSection.IsVisible = Visibility.Visible;
                }
                else
                {
                    ReviewStatus = new ReviewStatus();
                }
                ReviewStatus.CurrentPage = Constants.ReviewPage;
                Degree = PatientCase.IndicatorDegree;

                SetAnnotation(PatientCase.Id);
                
                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);              
                SetCrossSectionBackground(0, (ReviewStatus.IsAngioOn) ? Constants.CardBackgroundColor : Constants.BackgroundColor);
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
                    ReviewStatus.IsMeasurementOn = false;

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

            if (indicator.IsCaptured)
            {
                Point crossSectionCenter;
                if (ReviewStatus.IsAngioOn)
                {
                    crossSectionCenter = crossSectionCenterSmall;
                }
                else
                {
                    crossSectionCenter = crossSectionCenterBig;
                }

                indicator.SetDirection(crossSectionCenter, Degree);
                if (!indicator.IsValid) return;

                Point headerSidePoint = new Point(indicator.X, indicator.Y);
                if (indicator.OppositeCaptured)
                {
                    double xOffset = indicator.X - crossSectionCenter.X;
                    double yOffset = indicator.Y - crossSectionCenter.Y;

                    headerSidePoint.X = crossSectionCenter.X - xOffset;
                    headerSidePoint.Y = crossSectionCenter.Y - yOffset;
                }

                double pointX = crossSectionCenter.X - headerSidePoint.X;
                double pointY = crossSectionCenter.Y - headerSidePoint.Y;
                Degree = (int)(Math.Atan2(pointY, pointX) * 180 / Math.PI);
            }
        }

        private void MoveIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.IsCaptured)
            {
                double x = PointLongitudeX - longitudeCoordinate.X;

                if (x >= 0 && x < Constants.LongitudeWidth)
                {
                    indicator.X = x - Constants.LongitudeIndicatorWidth / 2;
                    setCurrentFrame(x);
                }
            }
        }

        private void ViewSizeChanged(object param)
        {
            if (param != null)
            {
                FrameworkElement frameworkElement = (FrameworkElement)param;

                Point pointWindow = Application.Current.MainWindow.PointToScreen(new Point(0, 0));
                Point point = frameworkElement.PointToScreen(new Point(0, 0));
                point.X -= pointWindow.X;
                point.Y -= pointWindow.Y;

                if (frameworkElement.Name.Equals("crossSectionImage"))
                {
                    crossSectionCenterBig.X = point.X + (frameworkElement.ActualWidth / 2);
                    crossSectionCenterBig.Y = point.Y + (frameworkElement.ActualHeight / 2);
                }
                else if (frameworkElement.Name.Equals("crossSectionImageSmall"))
                {
                    crossSectionCenterSmall.X = point.X + (frameworkElement.ActualWidth / 2);
                    crossSectionCenterSmall.Y = point.Y + (frameworkElement.ActualHeight / 2);
                }
                else if (frameworkElement.Name.Equals("lumenProfile") || frameworkElement.Name.Equals("lMode"))
                {
                    longitudeCoordinate = point;
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
            sqlParameters["angio_co_registration"] = PatientCase.AngioCoRegistration;
            PatientCase.IndicatorDegree = Degree;
            sqlParameters["indicator_degree"] = PatientCase.IndicatorDegree;
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

            ReviewStatus.IsMeasurementOn = !ReviewStatus.IsMeasurementOn;
        }

        private void SetAnnotation(string id)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            IList<StringModel> jsonAnnotation = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);

            if (jsonAnnotation != null && jsonAnnotation.Count == 1)
            {
                List<Measurement>? measurements = new List<Measurement>();
                Measurement lModeMeasurement = new Measurement();
                AnnotationConverter.ConvertFromJsonString(jsonAnnotation[0].ReturnString, out measurements, out lModeMeasurement);

                Measurements = measurements;
                LModeLengthGeometries = lModeMeasurement.LengthGeometries == null ? new ObservableCollection<LengthGeometry>() : lModeMeasurement.LengthGeometries; 
                LModeTextGeometries = lModeMeasurement.TextGeometries == null ? new List<TextGeometry>() : lModeMeasurement.TextGeometries;
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

            //Cross-section
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

            //Longitude
            Measurement longitudeMeasurement = new Measurement();
            longitudeMeasurement.FrameNumber = -1;
            longitudeMeasurement.LengthGeometries = LModeLengthGeometries;
            longitudeMeasurement.TextGeometries = LModeTextGeometries;
            measurements.Add(longitudeMeasurement);

            return JsonConvert.SerializeObject(measurements, Formatting.Indented);
        }

        private void ToggleLongitude(bool isLumenProfile)
        {
            ReviewStatus.IsLumenProfile = isLumenProfile;
            IndicatorCrossSection.IsVisible = (isLumenProfile) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CoRegistration()
        {
            _log.Debug("CoRegistration");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewAngioCoRegPage) { Parameter = parameter });
        }

        private void ToggleContourStent()
        {
            ReviewStatus.IsContourStentOn = !ReviewStatus.IsContourStentOn;
        }

        private void ToggleAngio(bool isAngioOn)
        {
            ReviewStatus.IsAngioOn = isAngioOn;

            SetCrossSectionBackground(0, (ReviewStatus.IsAngioOn) ? Constants.CardBackgroundColor : Constants.BackgroundColor);

            if (ReviewStatus.IsAngioOn)
            {
                RightSideBarExpand = Constants.RightSideBarExpandAngioSize;
                ReviewStatus.IsMeasurementOn = false;
            }
            else
            {
                RightSideBarExpand = Constants.RightSideBarExpandDefaultSize;
            }

            (ToggleMeasurementCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (DrawCrossSectionImage())
            {
                if (!IndicatorLongitude.IsCaptured) updateNavigator(crossSectionFrameInfo[0].curFrame, crossSectionFrameInfo[0].totalFrame);

                FrameNumber = crossSectionFrameInfo[0].curFrame;
                MeasurementFrameNumber = FrameNumber;
            }
            if (DrawLongitudeImage())
            {
                // when generating longitude image is completed
                if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame)
                {
                    IndicatorLongitude.IsVisible = Visibility.Visible;

                    if (!isLongitudeMeasurementInit)
                    {
                        MeasurementCommand = Constants.MeasureDrawAll;
                        isLongitudeMeasurementInit = true;
                    }
                }
            }
        }

        private void updatePlayPauseState()
        {
            double pauseState = RayGetProperty(Property.IsPaused);

            IsPaused = (pauseState == 0);
        }

        private void updateNavigator(int curFrame, int totalFrame)
        {
            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= Constants.LongitudeWidth;
            IndicatorLongitude.X = curPosition - Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.CenterX = curPosition;
        }

        private void setCurrentFrame(double navigatorPosition)
        {
            double curPosition = navigatorPosition / Constants.LongitudeWidth;

            if (longitudeFrameInfo != null)
            {
                curPosition *= (longitudeFrameInfo.totalFrame - 1);
                curPosition = Math.Round(curPosition);
                RayMoveToFrame((int)curPosition);
            }
        }
    }
}
