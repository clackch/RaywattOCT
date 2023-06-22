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
using Point = System.Windows.Point;
using System.Linq;
using OpenCvSharp;
using RaywattApp.Common.Util;
using System.Threading;
using RaywattApp.Common.Annotation.Util;

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

        private double _rightSideBarExpand;
        public double RightSideBarExpand
        {
            get { return _rightSideBarExpand; }
            set { _rightSideBarExpand = value; OnPropertyChanged(nameof(RightSideBarExpand)); }
        }

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();
        private Thread? threadWaitLumenDetection = null;
        private bool runWaitLumenDetection = false;

        private int outFrameNumber;
        public int OutFrameNumber
        {
            get { return outFrameNumber; }
            set
            {
                outFrameNumber = value;
                MoveToFrame(RaySession.Review, value);
            }
        }

        private string _measurementCommand;
        public string MeasurementCommand { get { return _measurementCommand; } set { _measurementCommand = value; OnPropertyChanged(nameof(MeasurementCommand)); } }

        private bool _measurementCommandOff;
        public bool MeasurementCommandOff { get { return _measurementCommandOff; } set { _measurementCommandOff = value; OnPropertyChanged(nameof(MeasurementCommandOff)); } }

        private List<Measurement> measurements = new List<Measurement>();
        public List<Measurement> Measurements { get { return measurements; } set { measurements = value; OnPropertyChanged(nameof(Measurements)); } }

        private bool isLongitudeMeasurementInit;

        private ObservableCollection<LengthGeometry> _lModeLengthGeometries = new ObservableCollection<LengthGeometry>();
        public ObservableCollection<LengthGeometry> LModeLengthGeometries { get { return _lModeLengthGeometries; } set { _lModeLengthGeometries = value; OnPropertyChanged(nameof(LModeLengthGeometries)); } }

        private List<TextGeometry> _lModeTextGeometries = new List<TextGeometry>();
        public List<TextGeometry> LModeTextGeometries { get { return _lModeTextGeometries; } set { _lModeTextGeometries = value; OnPropertyChanged(nameof(LModeTextGeometries)); } }

        [ObservableProperty]
        private LumenContour _currentLumenContour;

        private List<LumenContour> _lumenContours = new List<LumenContour>();
        public List<LumenContour> LumenContours { get { return _lumenContours; } set { _lumenContours = value; OnPropertyChanged(nameof(LumenContours)); } }

        private string _lumenContourCommand;
        public string LumenContourCommand { get { return _lumenContourCommand; } set { _lumenContourCommand = value; OnPropertyChanged(nameof(LumenContourCommand)); } }

        [ObservableProperty]
        private Zoom _zoomAngio = new Zoom(Constants.CrossSectionAngio / Constants.OCTImageSize);

        private double _lModeIndicatorX;
        public double LModeIndicatorX 
        { 
            get { return _lModeIndicatorX; } 
            set { _lModeIndicatorX = value; OnPropertyChanged(nameof(LModeIndicatorX)); setCurrentFrame(value); } 
        }

        private int _brightness;
        public int Brightness
        {
            get { return _brightness; }
            set { 
                _brightness = value;
                OnPropertyChanged(nameof(Brightness));
                RaySetProperty(Property.Brightness, value);
                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current); 
            }
        }

        private int _contrast;
        public int Contrast
        {
            get { return _contrast; }
            set { 
                _contrast = value; 
                OnPropertyChanged(nameof(Contrast));
                RaySetProperty(Property.Contrast, value);
                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
            }
        }

        private bool hasAnnotation = true;

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

        private ICommand _zoomInCommand;
        public ICommand ZoomInCommand
        {
            get { return this._zoomInCommand ?? (this._zoomInCommand = new RelayCommand(ZoomIn)); }
        }

        private ICommand _zoomOutCommand;
        public ICommand ZoomOutCommand
        {
            get { return this._zoomOutCommand ?? (this._zoomOutCommand = new RelayCommand(ZoomOut)); }
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

            CurrentLumenContour = new LumenContour();
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
                ReviewStatus = (ReviewStatus)data["reviewStatus"];

                ToggleAngio(ReviewStatus.IsAngioOn);
                ToggleLongitude(ReviewStatus.IsLumenProfile);
                
                ReviewStatus.CurrentPage = Constants.ReviewPage;

                GetImageInfo(RaySession.Review);

                Degree = PatientCase.IndicatorDegree;
                Brightness = PatientCase.Brightness;
                Contrast = PatientCase.Contrast;
                SetAnnotation();
                SetCrossSectionBackground(RaySession.Review, (ReviewStatus.IsAngioOn) ? Constants.CardBackgroundColor : Constants.BackgroundColor);

                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
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

            if (threadWaitLumenDetection != null && threadWaitLumenDetection.IsAlive)
                runWaitLumenDetection = false;
        }

        private void Playback(object param)
        {
            string action = (string)param;

            if (action.ToLower().Equals("prev"))
            {
                PrevFrame(RaySession.Review);
            }
            else if (action.ToLower().Equals("next"))
            {
                NextFrame(RaySession.Review);
            }
            else if (action.ToLower().Equals("play"))
            {
                Playback();
                if (!IsPaused) ReviewStatus.IsMeasurementOn = false;
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
                if (indicator.IsLongitudeClicked)
                {
                    indicator.IsLongitudeClicked = false;
                    return;
                }

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
            PatientCase.Brightness = Brightness;
            sqlParameters["brightness"] = PatientCase.Brightness;
            PatientCase.Contrast = Contrast;
            sqlParameters["contrast"] = PatientCase.Contrast;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
            {
                _log.Error("Update Error");
            }
            else
            {
                PatientCase.LumenContour = LumenContours;

                sqlParameters.Clear();
                sqlParameters["id"] = PatientCase.Id;
                PatientCase.CrossSection = ConvertMeasurementsToJson(Measurements);
                sqlParameters["cross_section"] = PatientCase.CrossSection;
                PatientCase.Longitude = ConvertLongitudeToJson();
                sqlParameters["longitude"] = PatientCase.Longitude;
                PatientCase.Bookmark = JsonConvert.SerializeObject(Bookmarks, Formatting.Indented);
                sqlParameters["bookmark"] = PatientCase.Bookmark;

                if (this.hasAnnotation)
                {
                    nRows = _sqlManager.UpdatePatientCaseAnnotationWithoutLumenContour(sqlParameters);
                }
                else
                {
                    sqlParameters["lumen_contour"] = PatientCase.StrLumenContour;
                    nRows = _sqlManager.UpsertPatientCaseAnnotation(sqlParameters);
                }

                if (nRows == 0)
                {
                    _log.Error("Update Error");
                }
            }
        }

        private void ToggleMeasurement()
        {
            if(DeviceStatus.IsPaused == false)
            {
                Playback();
            }

            ReviewStatus.IsMeasurementOn = !ReviewStatus.IsMeasurementOn;
        }

        private void SetAnnotation()
        {
            // Initialize with empty objects
            for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
            {
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
            }

            if (PatientCase.Bookmark != null && PatientCase.CrossSection != null && PatientCase.Longitude != null && PatientCase.LumenContour != null)
            {
                Measurements = JsonConvert.DeserializeObject<List<Measurement>>(PatientCase.CrossSection);

                if(PatientCase.Longitude != "")
                {
                    Measurement lModeMeasurement = JsonConvert.DeserializeObject<Measurement>(PatientCase.Longitude);
                    LModeLengthGeometries = lModeMeasurement.LengthGeometries;
                    LModeTextGeometries = lModeMeasurement.TextGeometries;
                }
                else
                {
                    this.hasAnnotation = false;
                }

                Bookmarks = JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(PatientCase.Bookmark);

                LumenContours = PatientCase.LumenContour;
                imglumenProfile = CommonUtil.MakeLumenProfileImage(LumenContours);
            }
            else
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = PatientCase.Id;
                IList<PatientCaseAnnotation> patientCaseAnnotations = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);

                if (patientCaseAnnotations != null && patientCaseAnnotations.Count == 1)
                {
                    if (!string.IsNullOrEmpty(patientCaseAnnotations[0].CrossSection))
                    {
                        Measurements = JsonConvert.DeserializeObject<List<Measurement>>(patientCaseAnnotations[0].CrossSection);
                    }

                    if (!string.IsNullOrEmpty(patientCaseAnnotations[0].Longitude))
                    {
                        Measurement lModeMeasurement = JsonConvert.DeserializeObject<Measurement>(patientCaseAnnotations[0].Longitude);
                        LModeLengthGeometries = lModeMeasurement.LengthGeometries;
                        LModeTextGeometries = lModeMeasurement.TextGeometries;
                    }

                    if (!string.IsNullOrEmpty(patientCaseAnnotations[0].Bookmark))
                    {
                        Bookmarks = JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(patientCaseAnnotations[0].Bookmark);
                    }

                    if (!string.IsNullOrEmpty(patientCaseAnnotations[0].LumenContour))
                    {
                        DeviceStatus.IsLumenDetected = true;
                        DeviceStatus.IsLumenLoaded = false;

                        Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile(patientCaseAnnotations[0].LumenContour));
                        threadMakeLumenProfile.Start();
                    }
                    else
                    {
                        this.hasAnnotation = false;

                        DeviceStatus.IsLumenDetected = false;
                        DeviceStatus.IsLumenLoaded = false;
                        RayStartLumenDetection();

                        threadWaitLumenDetection = new Thread(new ThreadStart(threadFuncWaitLumenDetection));
                        threadWaitLumenDetection.Start();
                    }
                }
                else 
                {
                    this.hasAnnotation = false;

                    DeviceStatus.IsLumenDetected = false;
                    DeviceStatus.IsLumenLoaded = false;
                    RayStartLumenDetection();

                    threadWaitLumenDetection = new Thread(new ThreadStart(threadFuncWaitLumenDetection));
                    threadWaitLumenDetection.Start();
                }
            }

            for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
            {
                Measurement measurement = new Measurement();
                measurement.FrameNumber = i;
                measurement.AreaGeometries = new ObservableCollection<AreaGeometry>();
                measurement.LengthGeometries = new ObservableCollection<LengthGeometry>();
                measurement.TextGeometries = new List<TextGeometry>();
                Measurements.Add(measurement);
            }

            Measurements = Measurements.DistinctBy(x => x.FrameNumber).OrderBy(x => x.FrameNumber).ToList();
        }

        private void ThreadMakeLumenProfile(string lumenContour)
        {
            LumenContours = JsonConvert.DeserializeObject<List<LumenContour>>(lumenContour);
            imglumenProfile = CommonUtil.MakeLumenProfileImage(LumenContours);

            if (ReviewStatus.IsContourStentOn)
                LumenContourCommand = Constants.LumenContourDraw;
            else
                LumenContourCommand = Constants.LumenContourCurrentInit;

            DeviceStatus.IsLumenLoaded = true;
        }

        private string ConvertMeasurementsToJson(List<Measurement> param)
        {
            List<Measurement> measurements = new List<Measurement>();

            //Cross-section
            foreach(Measurement measurement in param)
            {
                if(measurement.AreaGeometries.Count > 0 || measurement.LengthGeometries.Count > 0 || measurement.TextGeometries.Count > 0)
                {
                    if (measurement.AreaGeometries.Count > 0)
                    {
                        foreach (AreaGeometry geometry in measurement.AreaGeometries)
                        {
                            geometry.Path = null;
                        }
                    }
                    measurements.Add(measurement);
                }
            }

            return JsonConvert.SerializeObject(measurements, Formatting.Indented);
        }

        private string ConvertLongitudeToJson()
        {
            //Longitude
            Measurement longitudeMeasurement = new Measurement();
            longitudeMeasurement.LengthGeometries = LModeLengthGeometries;
            longitudeMeasurement.TextGeometries = LModeTextGeometries;

            return JsonConvert.SerializeObject(longitudeMeasurement, Formatting.Indented);
        }

        private void ToggleLongitude(bool isLumenProfile)
        {
            ReviewStatus.IsLumenProfile = isLumenProfile;
            
            if(ReviewStatus.IsAngioOn)
                IndicatorCrossSection.IsVisible = isLumenProfile ? Visibility.Collapsed : Visibility.Visible;
            else
                IndicatorCrossSection.IsVisible = (!isLumenProfile && ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault) ? Visibility.Visible : Visibility.Collapsed;
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

            SetCrossSectionBackground(RaySession.Review, (ReviewStatus.IsAngioOn) ? Constants.CardBackgroundColor : Constants.BackgroundColor);

            if (ReviewStatus.IsAngioOn)
            {
                RightSideBarExpand = Constants.RightSideBarExpandAngioSize;
                ReviewStatus.IsMeasurementOn = false;
                ReviewStatus.IsCalciumOn = true;
                if(ReviewStatus.IsLumenProfile)
                    IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                else
                    IndicatorCrossSection.IsVisible = Visibility.Visible;
            }
            else
            {
                RightSideBarExpand = Constants.RightSideBarExpandDefaultSize;
                if (ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault)
                {
                    ReviewStatus.IsCalciumOn = true;
                    IndicatorCrossSection.IsVisible = Visibility.Visible;
                }
                else
                {
                    ReviewStatus.IsCalciumOn = false;
                    IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                }                    
            }

            (ToggleMeasurementCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void ZoomIn()
        {
            _log.Debug("ZoomIn");

            if (ReviewStatus.Zoom.ZoomIn())
            {
                if(ReviewStatus.IsMeasurementOn)
                    MeasurementCommand = Constants.MeasureZoomIn;
                IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                ReviewStatus.IsCalciumOn = false;
            }
        }

        private void ZoomOut()
        {
            _log.Debug("ZoomOut");

            if (ReviewStatus.Zoom.ZoomOut() && ReviewStatus.IsMeasurementOn)
                MeasurementCommand = Constants.MeasureZoomOut;

            if(ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault)
            {
                ReviewStatus.IsCalciumOn = true;

                if(!ReviewStatus.IsLumenProfile)
                    IndicatorCrossSection.IsVisible = Visibility.Visible;
            }
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (DrawCrossSectionImage())
            {
                DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Review];
                if (!IndicatorLongitude.IsCaptured) updateNavigator(imageInfo.Current, imageInfo.Total);

                FrameNumber = imageInfo.Current;
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
            DrawLumenProfileImage();
        }

        private void threadFuncWaitLumenDetection()
        {
            runWaitLumenDetection = true;
            imglumenProfile = null;

            while (runWaitLumenDetection && !DeviceStatus.IsLumenDetected)
            {
                Thread.Sleep((int) Constants.UpdateLumenProfileInterval);
            }
            runWaitLumenDetection = false;

            for (int curFrame = 0; curFrame < LumenContours.Count; curFrame++)
            {
                int num = RayGetNumOfLumenContourPoints(curFrame);
                if (num > 0)
                {
                    IntPtr contour = RayGetLumenContour(curFrame);
                    if (contour == IntPtr.Zero) continue;

                    Mat matContour = CommonUtil.ByteMemoryToCvMat(contour, 1, num, 2);

                    LumenContours[curFrame].MlContour.Points = new List<Point>();
                    for(int row = 0; row < matContour.Rows; row++)
                    {
                        Vec2i point = matContour.At<Vec2i>(0, row);
                        LumenContours[curFrame].MlContour.Points.Add(new Point(point.Item0, point.Item1));
                    }
                    ContourMeasurement contourMeasurement = new ContourMeasurement();
                    contourMeasurement.Measure(LumenContours[curFrame].MlContour, (int) Constants.OCTImageSize, (int) Constants.OCTImageSize);
                    if (LumenContours[curFrame].MlContour.Valid)
                    {
                        contourMeasurement.CalculateDiameter(LumenContours[curFrame].MlContour);
                    }
                    LumenContours[curFrame].CopyMlToLumenContour();
                }
            }
            imglumenProfile = CommonUtil.MakeLumenProfileImage(LumenContours);

            if (ReviewStatus.IsContourStentOn)
                LumenContourCommand = Constants.LumenContourDraw;
            else
                LumenContourCommand = Constants.LumenContourCurrentInit;

            PatientCase.StrLumenContour = JsonConvert.SerializeObject(LumenContours, Formatting.Indented);

            DeviceStatus.IsLumenLoaded = true;
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
                MoveToFrame(RaySession.Review, (int)curPosition);
            }
        }
    }
}
