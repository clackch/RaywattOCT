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
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using RaywattApp.Views.Dialog;
using System.IO;
using System.Windows.Media;
using RaywattApp.Common.Angio;
using System.Xml;

namespace RaywattApp.ViewModels
{
    public partial class ReviewViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

        private readonly AngioManager _angioManager;

        private CallbackFunctionForDetection cbLumenContour;
        public CallbackFunctionForDetection CBLumenContour => (this.cbLumenContour) ?? (this.cbLumenContour = new CallbackFunctionForDetection(OnRecvLumenContour));

        private bool isLumenContourSave = false;

        private bool isLumenDetectedFrontDone = false;

        private bool isLumenLoadedInit = false;

        private bool isLumenProfileInit = false;

        private double originSectionProximalX;

        private double originSectionDistalX;

        private string originProcedure;

        private double convertedFoV;

        private double degree;
        public double Degree
        {
            get { return degree; }
            set
            {
                degree = value;
                OnPropertyChanged(nameof(Degree));
                RaySetProperty(Property.LongitudeDegree, degree);
            }
        }

        private List<Mat> AngioFrames;

        [ObservableProperty]
        private BitmapSource _calciumIndicator;

        [ObservableProperty]
        private BitmapSource _calciumIndicatorAngio;

        [ObservableProperty]
        private double _maxCalciumDegree = -1;

        [ObservableProperty]
        private double _totalAngle;

        [ObservableProperty]
        private double _maxThickness;

        [ObservableProperty]
        private Indicator _indicatorCrossSection;

        [ObservableProperty]
        private Indicator _indicatorCrossSectionAngio;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private Section _section;

        [ObservableProperty]
        private ImageSource _currentAngioImage;

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

        private int _currentAngioFrameNumber;
        public int CurrentAngioFrameNumber { get { return _currentAngioFrameNumber; } set { _currentAngioFrameNumber = value; OnPropertyChanged(nameof(CurrentAngioFrameNumber)); } }

        private string _measurementCommand;
        public string MeasurementCommand { get { return _measurementCommand; } set { _measurementCommand = value; OnPropertyChanged(nameof(MeasurementCommand)); } }

        private bool _measurementCommandOff;
        public bool MeasurementCommandOff { get { return _measurementCommandOff; } set { _measurementCommandOff = value; OnPropertyChanged(nameof(MeasurementCommandOff)); } }

        private List<Measurement> measurements;
        public List<Measurement> Measurements { get { return measurements; } set { measurements = value; OnPropertyChanged(nameof(Measurements)); } }

        private ObservableCollection<LengthGeometry> _lModeLengthGeometries;
        public ObservableCollection<LengthGeometry> LModeLengthGeometries { get { return _lModeLengthGeometries; } set { _lModeLengthGeometries = value; OnPropertyChanged(nameof(LModeLengthGeometries)); } }

        private List<TextGeometry> _lModeTextGeometries;
        public List<TextGeometry> LModeTextGeometries { get { return _lModeTextGeometries; } set { _lModeTextGeometries = value; OnPropertyChanged(nameof(LModeTextGeometries)); } }

        [ObservableProperty]
        private LumenContour _currentLumenContour;

        private List<LumenContour> _lumenContours;
        public List<LumenContour> LumenContours { get { return _lumenContours; } set { _lumenContours = value; OnPropertyChanged(nameof(LumenContours)); } }

        private string _lumenContourCommand;
        public string LumenContourCommand { get { return _lumenContourCommand; } set { _lumenContourCommand = value; OnPropertyChanged(nameof(LumenContourCommand)); } }

        [ObservableProperty]
        private CoRegistration _currentCoRegistration;

        private List<CoRegistration> _coRegistrations;
        public List<CoRegistration> CoRegistrations { get { return _coRegistrations; } set { _coRegistrations = value; OnPropertyChanged(nameof(CoRegistrations)); } }

        private List<Point> _coRegMarkerPoints;

        public List<Point> CoRegMarkerPoints { get { return _coRegMarkerPoints; } set { _coRegMarkerPoints = value; OnPropertyChanged(nameof(CoRegMarkerPoints)); } }

        [ObservableProperty]
        private List<LumenSidebranch> _lumenSidebranches;

        [ObservableProperty]
        private bool _isDrawLumenSideBranch = false;

        [ObservableProperty]
        private List<LumenStent> _lumenStents;

        [ObservableProperty]
        private LumenStent _currentLumenStent;

        [ObservableProperty]
        private List<LumenGuidewire> _lumenGuidewires;

        [ObservableProperty]
        private List<double> _guideWireRadiusList;

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
            set
            {
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
            set
            {
                _contrast = value;
                OnPropertyChanged(nameof(Contrast));
                RaySetProperty(Property.Contrast, value);
                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
            }
        }

        private double _fieldOfView;
        public double FieldOfView
        {
            get { return _fieldOfView; }
            set 
            { 
                _fieldOfView = value; 
                OnPropertyChanged(nameof(FieldOfView));

                this.convertedFoV = Constants.DefaultFoV / value;
                ReviewStatus.Zoom.SetFieldOfView(this.convertedFoV);
                ReviewStatus.ZoomAngioCs.SetFieldOfView(this.convertedFoV);

                CrossSectionScaleIndicator = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault * this.convertedFoV);
                CrossSectionAngioScaleIndicator = (1 / Constants.ImageResolution) * (Constants.ZoomAngioCsScaleDefault * this.convertedFoV);

                ReviewStatus.IsCalciumOn = true;
                ReviewStatus.IsSheathOn = true;
                ReviewStatus.IsCalciumOnAngioCs = true;
                ReviewStatus.IsSheathOnAngioCs = true;
                if (ReviewStatus.IsLumenProfile)
                    IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                else
                    IndicatorCrossSection.IsVisible = Visibility.Visible;
            }
        }

        private ICommand _cmdPlayback;
        public ICommand CmdPlayback
        {
            get { return this._cmdPlayback ?? (this._cmdPlayback = new RelayCommand<object>(Playback)); }
        }

        private ICommand _toggleLongitudeCommand;
        public ICommand ToggleLongitudeCommand
        {
            get { return this._toggleLongitudeCommand ?? (this._toggleLongitudeCommand = new RelayCommand<bool>(ToggleLongitude)); }
        }

        private ICommand _toggleAngioCommand;
        public ICommand ToggleAngioCommand
        {
            get { return this._toggleAngioCommand ?? (this._toggleAngioCommand = new RelayCommand<bool>(ToggleAngio)); }
        }

        private ICommand _toggleMeasurementCommand;
        public ICommand ToggleMeasurementCommand
        {
            get { return this._toggleMeasurementCommand ?? (this._toggleMeasurementCommand = new RelayCommand(ToggleMeasurement)); }
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

        private ICommand _zoomInAngioCsCommand;
        public ICommand ZoomInAngioCsCommand
        {
            get { return this._zoomInAngioCsCommand ?? (this._zoomInAngioCsCommand = new RelayCommand(ZoomInAngioCs)); }
        }

        private ICommand _zoomOutAngioCsCommand;
        public ICommand ZoomOutAngioCsCommand
        {
            get { return this._zoomOutAngioCsCommand ?? (this._zoomOutAngioCsCommand = new RelayCommand(ZoomOutAngioCs)); }
        }

        private ICommand _zoomInAngioCommand;
        public ICommand ZoomInAngioCommand
        {
            get { return this._zoomInAngioCommand ?? (this._zoomInAngioCommand = new RelayCommand(ZoomInAngio)); }
        }

        private ICommand _zoomOutAngioCommand;
        public ICommand ZoomOutAngioCommand
        {
            get { return this._zoomOutAngioCommand ?? (this._zoomOutAngioCommand = new RelayCommand(ZoomOutAngio)); }
        }

        private ICommand _adjustResetCommand;
        public ICommand AdjustResetCommand
        {
            get { return this._adjustResetCommand ?? (this._adjustResetCommand = new RelayCommand(AdjustReset)); }
        }

        private ICommand _coRegistrationCommand;
        public ICommand CoRegistrationCommand
        {
            get { return this._coRegistrationCommand ?? (this._coRegistrationCommand = new RelayCommand(CoRegistration)); }
        }

        private ICommand _editCaseCommand;
        public ICommand EditCaseCommand
        {
            get { return this._editCaseCommand ?? (this._editCaseCommand = new RelayCommand(EditCase)); }
        }

        private ICommand _cmdViewSizeChanged;
        public ICommand CmdViewSizeChanged
        {
            get { return this._cmdViewSizeChanged ?? (this._cmdViewSizeChanged = new RelayCommand<object>(ViewSizeChanged)); }
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

        private ICommand _cmdTouchMoveIndicator;
        public ICommand CmdTouchMoveIndicator
        {
            get { return this._cmdTouchMoveIndicator ?? (this._cmdTouchMoveIndicator = new RelayCommand<object>(TouchMoveIndicator)); }
        }

        private ICommand _manipulationStartingCommand;
        public ICommand ManipulationStartingCommand
        {
            get { return this._manipulationStartingCommand ?? (this._manipulationStartingCommand = new RelayCommand<ManipulationStartingEventArgs>(Window_ManipulationStarting)); }
        }

        private ICommand _manipulationDeltaCommand;
        public ICommand ManipulationDeltaCommand
        {
            get { return this._manipulationDeltaCommand ?? (this._manipulationDeltaCommand = new RelayCommand<ManipulationDeltaEventArgs>(Window_ManipulationDelta)); }
        }

        private ICommand _manipulationCompletedCommand;
        public ICommand ManipulationCompletedCommand
        {
            get { return this._manipulationCompletedCommand ?? (this._manipulationCompletedCommand = new RelayCommand<ManipulationCompletedEventArgs>(Window_ManipulationCompleted)); }
        }

        public ReviewViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewViewModel");

            _angioManager = angioManager;

            Constants.CurrentPage = Constants.ReviewPage;

            IndicatorCrossSection = new Indicator();
            IndicatorCrossSection.IsVisible = Visibility.Collapsed;
            IndicatorCrossSection.IsCrossSection = true;

            IndicatorCrossSectionAngio = new Indicator();
            IndicatorCrossSectionAngio.IsVisible = Visibility.Collapsed;
            IndicatorCrossSectionAngio.IsCrossSection = true;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;
            IndicatorLongitude.IsEnabled = false;

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;

            MenuExpand(true);

            CurrentLumenContour = new LumenContour();
            CurrentLumenStent = new LumenStent();
            AngioFrames = new List<Mat>();
            CoRegistrations = new List<CoRegistration>();
            CoRegMarkerPoints = new List<Point>();
            CurrentCoRegistration = new CoRegistration();

            UpdateCrossSectionImage();

            if(CommonUtil.IsTestMode(DeviceStatus.TestMode, "Sidebranch"))
                IsDrawLumenSideBranch = true;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");

            RayRegisterDetectionCallback(Marshal.GetFunctionPointerForDelegate(CBLumenContour));
            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];
                
                ReviewStatus.CurrentPage = Constants.ReviewPage;

                FieldOfView = PatientCase.FieldOfView;
                ToggleAngio(ReviewStatus.IsAngioOn);
                ToggleLongitude(ReviewStatus.IsLumenProfile);

                SetAngioFrame();

                Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
                Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

                SetLumenProfileValue();

                GetImageInfo(RaySession.Review);

                Degree = PatientCase.IndicatorDegree;
                Brightness = PatientCase.Brightness;
                Contrast = PatientCase.Contrast;                
                CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault);
                CrossSectionAngioScale = (1 / Constants.ImageResolution) * (Constants.ZoomAngioCsScaleDefault);

                PatientCase.MeasureUIElements = new List<List<UIElement>>(ReviewStatus.NumberOfFrames);
                for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
                {
                    PatientCase.MeasureUIElements.Add(new List<UIElement>());
                }

                SetAnnotation();
                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);

                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);

                ReviewStatus.IsNoPullback = PatientCase.PullbackType == "TEST" ? true : false;

                if (ReviewStatus.IsPlay)
                    Playback();

                DrawSheathIndicator(PatientCase.SheathDiameter);
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
            Save();
            FfrValueChangedCheck();

            RayUnregisterDetectionCallback();
        }

        /*
         * Initialize
         */
        #region Initialize

        private void SetAngioFrame()
        {
            if (!PatientCase.AngioYn) return;

            if (PatientCase.AngioFrame == null) PatientCase.AngioFrame = new AngioFrame();
            if (PatientCase.AngioFrame.CoRegistration == null) PatientCase.AngioFrame.CoRegistration = new List<CoRegistration>();


            if (PatientCase.AngioFrame.CoRegistration.Count == 0 && PatientCase.AngioCoRegistration)
            {
                ReadTrackPoints();
            }
            else
            {
                CoRegistrations = PatientCase.AngioFrame.CoRegistration;
            }

            if (PatientCase.AngioFrame.AngioImage.Count == 0)
            {
                PatientCase.AngioFrame.AngioFrameNum = 0;
                Thread threadReadAngioFrames = new Thread(() => ThreadReadAngioFrames());
                threadReadAngioFrames.IsBackground = true;
                threadReadAngioFrames.Start();
            }
        }
        
        private void SetAnnotation()
        {
            string tempCrossSection = "[]", tempLongitude = "", tempBookmark = "[]";

            if (PatientCase.Bookmark != null && PatientCase.CrossSection != null && PatientCase.Longitude != null && PatientCase.LumenContours != null
                && PatientCase.LumenSidebranches != null && PatientCase.LumenStents != null && PatientCase.LumenGuidewires != null)//From Related Review Pages
            {
                //Cross-Section
                tempCrossSection = PatientCase.CrossSection;

                //Longitude
                tempLongitude = PatientCase.Longitude;

                //Bookmark
                tempBookmark = PatientCase.Bookmark;

                //Lumen Contour
                LumenContours = PatientCase.LumenContours;

                //Sidebranch
                LumenSidebranches = PatientCase.LumenSidebranches;

                //Stent
                LumenStents = PatientCase.LumenStents;

                //Guidewire
                LumenGuidewires = PatientCase.LumenGuidewires;

                //Restart Lumen detection when re-calibrated
                if (ReviewStatus.IsRestartLumenDetection)
                {
                    InitializeLumenData();
                    DeviceStatus.IsLumenSaved = false;
                    this.isLumenContourSave = true;
                    ReviewStatus.IsRestartLumenDetection = false;
                    ReviewStatus.IsMeasurementOn = false;
                    ReviewStatus.IsPlay = true;
                }
            }
            else
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = PatientCase.Id;
                IList<PatientCaseAnnotation> patientCaseAnnotations = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);

                if (patientCaseAnnotations.Count == 1)//From Patient Detail
                {
                    //Cross-Section
                    tempCrossSection = patientCaseAnnotations[0].CrossSection;

                    //Longitude
                    tempLongitude = patientCaseAnnotations[0].Longitude;

                    //Bookmark
                    tempBookmark = patientCaseAnnotations[0].Bookmark;

                    //Lumen Contour
                    DeviceStatus.IsLumenLoaded = false;
                    Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile(patientCaseAnnotations[0].LumenContour));
                    threadMakeLumenProfile.Start();

                    //Sidebranch
                    if(!String.IsNullOrEmpty(patientCaseAnnotations[0].LumenSidebranch))
                    {
                        LumenSidebranches = JsonConvert.DeserializeObject<List<LumenSidebranch>>(patientCaseAnnotations[0].LumenSidebranch);
                    }
                    else
                    {
                        LumenSidebranches = new List<LumenSidebranch>();
                        for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
                        {
                            LumenSidebranch lumenSidebranch = new LumenSidebranch();
                            LumenSidebranches.Add(lumenSidebranch);
                        }
                    }

                    //Stent
                    if(!String.IsNullOrEmpty(patientCaseAnnotations[0].LumenStent))
                    {
                        LumenStents = JsonConvert.DeserializeObject<List<LumenStent>>(patientCaseAnnotations[0].LumenStent);
                    }
                    else
                    {
                        LumenStents = new List<LumenStent>();
                        for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
                        {
                            LumenStent lumenStent = new LumenStent();
                            LumenStents.Add(lumenStent);
                        }
                    }                    

                    //Guidewire
                    if(!String.IsNullOrEmpty(patientCaseAnnotations[0].LumenGuidewire))
                    {
                        LumenGuidewires = JsonConvert.DeserializeObject<List<LumenGuidewire>>(patientCaseAnnotations[0].LumenGuidewire);
                        
                    }
                    else
                    {
                        LumenGuidewires= new List<LumenGuidewire>();
                        for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
                        {
                            LumenGuidewire lumenGuidewire = new LumenGuidewire();
                            LumenGuidewires.Add(lumenGuidewire);
                        }
                    }
                }
                else//From Recording
                {
                    this.isLumenContourSave = true;
                    InitializeLumenData();
                    Thread threadLumenDetectionDone = new Thread(() => ThreadLumenDetectionDone());
                    threadLumenDetectionDone.Start();

                }
            }

            //Longitude
            Measurement lModeMeasurement = JsonConvert.DeserializeObject<Measurement>(tempLongitude);
            LModeLengthGeometries = lModeMeasurement == null ? new ObservableCollection<LengthGeometry>() : lModeMeasurement.LengthGeometries;
            LModeTextGeometries = lModeMeasurement == null ? new List<TextGeometry>() : lModeMeasurement.TextGeometries;

            //Bookmark
            Bookmarks = JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(tempBookmark);
            if (Bookmarks == null)
                Bookmarks = new ObservableCollection<Bookmark>();

            //Cross-Section
            Measurements = JsonConvert.DeserializeObject<List<Measurement>>(tempCrossSection);
            if (Measurements == null)
                Measurements = new List<Measurement>();
            for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
            {
                Measurement measurement = new Measurement();
                measurement.FrameNumber = i;
                measurement.AreaGeometries = new ObservableCollection<AreaGeometry>();
                measurement.LengthGeometries = new ObservableCollection<LengthGeometry>();
                measurement.AngleGeometries = new ObservableCollection<AngleGeometry>();
                measurement.TextGeometries = new List<TextGeometry>();
                Measurements.Add(measurement);
            }
            Measurements = Measurements.DistinctBy(x => x.FrameNumber).OrderBy(x => x.FrameNumber).ToList();
        }

        private void AngioImageProcessing()
        {
            ImageProcessing(AngioFrames);

            ReviewStatus.IsImageProcessingDone = true;
        }

        private void ThreadMakeLumenProfile(string lumenContour)
        {
            LumenContours = CommonUtil.JsonToLumenContours(lumenContour);

            //TODO - Calcium 추가를 위한 테스트 코드 (추후 삭제 필요)
            if (false)
                GetMlData();

            if (ReviewStatus.IsContourStentOn)
                LumenContourCommand = Constants.LumenContourDraw;
            else
                LumenContourCommand = Constants.LumenContourCurrentInit;

            DeviceStatus.IsLumenLoaded = true;
        }

        private void InitializeLumenData()
        {
            LumenContours = new List<LumenContour>();
            LumenSidebranches = new List<LumenSidebranch>();
            LumenStents = new List<LumenStent>();
            LumenGuidewires = new List<LumenGuidewire>();
            GuideWireRadiusList = new List<double>();

            for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
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

        private void ThreadLumenDetectionDone()
        {
            while (!DeviceStatus.IsLumenDetected)
            {
                Thread.Sleep(500);
            }

            if (!isLumenDetectedFrontDone)
                OnRecvLumenContour(ReviewStatus.NumberOfFrames - 1);
        }

        private void OnRecvLumenContour(int frame)
        {
            if (LumenContours == null || LumenContours.Count != ReviewStatus.NumberOfFrames || frame <= 0)
                return;

            if (!isLumenDetectedFrontDone)
            {
                isLumenDetectedFrontDone = true;

                for (int curFrame = 0; curFrame < frame; curFrame++)
                {
                    LumenDataProcess(curFrame);
                }
            }

            LumenDataProcess(frame);

            if (ReviewStatus.NumberOfFrames - 1 == frame)
            {
                //TODO - ML detection에서 Calcium 가져오도록 개발되면 삭제 필요
                if (false)
                    GetMlData();

                PatientCase.GuidewireRadius = GetGuidewireAverageRadius();

                if (ReviewStatus.IsContourStentOn)
                    LumenContourCommand = Constants.LumenContourDraw;
                else
                    LumenContourCommand = Constants.LumenContourCurrentInit;

                PatientCase.StrLumenContour = CommonUtil.LumenContoursToJson(LumenContours);
                PatientCase.StrLumenSidebranch = JsonConvert.SerializeObject(LumenSidebranches, Newtonsoft.Json.Formatting.Indented);
                PatientCase.StrLumenStent = JsonConvert.SerializeObject(LumenStents, Newtonsoft.Json.Formatting.Indented);
                PatientCase.StrLumenGuidewire = JsonConvert.SerializeObject(LumenGuidewires, Newtonsoft.Json.Formatting.Indented);
                PatientCase.StrCoRegistration = "";

                DeviceStatus.IsLumenSaved = true;
                SetLumenProfileInit();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateLumenProfile();
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DrawLumenProfile(frame);
                });
            }
        }

        private void LumenDataProcess(int frameInfo)
        {
            //lumen
            int num = RayGetNumOfLumenContourPoints(frameInfo);
            if (num > 2)
            {
                IntPtr contour = RayGetLumenContour(frameInfo);
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
            int sbSize = RayGetNumOfSidebranchContourSize(frameInfo);
            if(sbSize > 0)
            {
                LumenSidebranches[frameInfo].Points = new List<List<Point>>();
                for (int i = 0; i < sbSize; i++)
                {
                    int height = RayGetNumOfSidebranchContourPoints(frameInfo, i);
                    if (height > 2)
                    {
                        IntPtr contour = RayGetSidebranchContour(frameInfo, i);
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
            int stentHeight = RayGetNumOfStentPoints(frameInfo);
            if(stentHeight > 0)
            {
                IntPtr contour = RayGetStentPoints(frameInfo);
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
            int guidewireHeight = RayGetNumOfGuidewirePoints(frameInfo);
            if (guidewireHeight > 0)
            {
                IntPtr contour = RayGetGuidewirePoints(frameInfo);
                if (contour == IntPtr.Zero) return;

                IntPtr radius = RayGetGuidewireRadius(frameInfo);
                if (radius == IntPtr.Zero) return;

                Mat mat = CommonUtil.ByteMemoryToCvMat(contour, 1, guidewireHeight, 2);
                LumenGuidewires[frameInfo].Points = new List<Point>();

                unsafe
                {
                    float* doublePtr = (float*)radius.ToPointer();
                    for (int row = 0; row < mat.Rows; row++)
                    {
                        Vec2i point = mat.At<Vec2i>(0, row);
                        if(point.Item0 < 0 || point.Item0 < 0)
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

        //TODO - Calcium 테스트 데이터 만드는 함수 (추후 삭제 필요)
        private void GetMlData()
        {
            Random random = new Random();
            List<int> samples = new List<int>();
            double firstSize = 0, secondSize = 0, thirdSize = 0;
            int range = 0, i = 0;

            List<int> sidebranchIdx = new List<int>();
            int sbTotal = random.Next(2, 6);

            for (int j = 0; j < sbTotal; j++)
            {
                sidebranchIdx.Add(random.Next(0, LumenContours.Count));
            }
            sidebranchIdx.Sort();

            foreach (LumenContour lumenContour in LumenContours)
            {
                //Calcium
                if (i == range)
                {
                    samples.Clear();

                    if (random.Next(0, 2) % 2 == 0)
                    {
                        samples.Add(random.Next(0, 361));
                        samples.Add(random.Next(0, 361));
                        samples.Add(random.Next(0, 361));
                        samples.Sort();

                        firstSize = random.Next(samples[0], samples[1]) - samples[0];
                        secondSize = random.Next(samples[1], samples[2]) - samples[1];
                        thirdSize = random.Next(samples[2], 361) - samples[2];
                    }
                    else
                    {
                        samples.Add(0);
                        samples.Add(0);
                        samples.Add(0);
                        firstSize = 0;
                        secondSize = 0;
                        thirdSize = 0;
                    }

                    range = random.Next(10, 50);
                    i = 0;
                }
                i++;

                lumenContour.Calcium = new Calcium();
                lumenContour.Calcium.List = new List<Tuple<double, double>>();

                lumenContour.Calcium.List.Add(new Tuple<double, double>(samples[0], firstSize));
                lumenContour.Calcium.List.Add(new Tuple<double, double>(samples[1], secondSize));
                lumenContour.Calcium.List.Add(new Tuple<double, double>(samples[2], thirdSize));

                lumenContour.Calcium.TotalAngle = (int)(firstSize + secondSize + thirdSize);
                lumenContour.Calcium.TotalAngle = 0; // TODO - 동물실험 후 삭제
                lumenContour.Calcium.MaxThickness = Math.Round(lumenContour.Calcium.TotalAngle / 200.0, 2);

                int idx = firstSize > secondSize ? firstSize > thirdSize ? 0 : 2 : secondSize > thirdSize ? 1 : 2;
                double size = firstSize > secondSize ? firstSize > thirdSize ? firstSize : thirdSize : secondSize > thirdSize ? secondSize : thirdSize;
                lumenContour.Calcium.MaxThicknessDegree = samples[idx] + size / 2;
                if (firstSize + secondSize + thirdSize == 0)
                    lumenContour.Calcium.MaxThicknessDegree = -1;
            }
        }

        private double GetGuidewireAverageRadius()
        {
            if(GuideWireRadiusList == null || !(GuideWireRadiusList.Any()))
            {
                return 0.0;
            }

            // 0보다 작은 값들을 제거
            List<double> validRadiusList = GuideWireRadiusList.Where(v => v >= 0 && double.IsFinite(v) && v <= 90).ToList();

            if (validRadiusList.Count == 0)
            {
                _log.Debug("No valid radius values.");
                return 0.0;
            }

            double mean = validRadiusList.Average();
            double stdDev = Math.Sqrt(validRadiusList.Average(v => Math.Pow(v - mean, 2)));

            // 정규화된 값 계산
            List<double> normalizedValues = GuideWireRadiusList.Select(v => (v - mean) / stdDev).ToList();

            // 편차가 ±2 이하인 값들만 선택(95%)하고 ±3 이상인 값들을 제거
            List<double> filteredValues = GuideWireRadiusList.Where((v, index) =>
            {
                double normalizedValue = normalizedValues[index];
                return normalizedValue >= -2 && normalizedValue <= 2;
            }).ToList();

            if(!filteredValues.Any())
            {
                return 0.0;
            }
            
            double filteredAverage = filteredValues.Average();

            _log.Debug("Average Radius Value" + filteredAverage.ToString());

            return filteredAverage;
        }

        #endregion

        /*
        * Button
        */
        #region Button

        private void Playback(object param)
        {
            string action = (string)param;

            if (action.ToLower().Equals("prev"))
            {
                StopPlayback();

                PrevFrame(RaySession.Review);
            }
            else if (action.ToLower().Equals("next"))
            {
                StopPlayback();

                NextFrame(RaySession.Review);
            }
            else if (action.ToLower().Equals("play"))
            {
                Playback();

                if (!IsPaused) {
                    ReviewStatus.IsMeasurementOn = false;
                    ReviewStatus.IsPlay = true;
                }
                else
                {
                    ReviewStatus.IsPlay = false;
                }
                
            }
        }

        private void ToggleLongitude(bool isLumenProfile)
        {
            ReviewStatus.IsLumenProfile = isLumenProfile;

            if (ReviewStatus.IsAngioOn)
                IndicatorCrossSectionAngio.IsVisible = (!isLumenProfile && CommonUtil.GetRoundScale(ReviewStatus.ZoomAngioCs.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomAngioCsScaleDefault * this.convertedFoV)) ? Visibility.Visible : Visibility.Collapsed;
            else
                IndicatorCrossSection.IsVisible = (!isLumenProfile && CommonUtil.GetRoundScale(ReviewStatus.Zoom.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomScaleDefault * this.convertedFoV)) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleAngio(bool isAngioOn)
        {
            ReviewStatus.IsAngioOn = isAngioOn;

            if (ReviewStatus.IsAngioOn)
            {
                ReviewStatus.IsMeasurementOn = false;

                if (CommonUtil.GetRoundScale(ReviewStatus.ZoomAngioCs.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomAngioCsScaleDefault * this.convertedFoV))
                {
                    ReviewStatus.IsCalciumOn = true;
                    if (ReviewStatus.IsLumenProfile)
                        IndicatorCrossSectionAngio.IsVisible = Visibility.Collapsed;
                    else
                        IndicatorCrossSectionAngio.IsVisible = Visibility.Visible;
                }
                else
                {
                    ReviewStatus.IsCalciumOn = false;
                    IndicatorCrossSectionAngio.IsVisible = Visibility.Collapsed;
                }

                MenuExpand(false);
            }
            else
            {
                if (CommonUtil.GetRoundScale(ReviewStatus.Zoom.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomScaleDefault * this.convertedFoV))
                {
                    ReviewStatus.IsCalciumOn = true;
                    if (ReviewStatus.IsLumenProfile)
                        IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                    else
                        IndicatorCrossSection.IsVisible = Visibility.Visible;
                }
                else
                {
                    ReviewStatus.IsCalciumOn = false;
                    IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                }

                MenuExpand(true);
            }

            (ToggleMeasurementCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void MenuExpand(bool isExpand)
        {
            ExpandLeftUpMenu = isExpand;
            ExpandLeftDownMenu = isExpand;
            ExpandRightMenu = isExpand;
        }

        private void ToggleMeasurement()
        {
            StopPlayback();
        }

        public void Window_ManipulationStarting(ManipulationStartingEventArgs e)
        {
            _log.Debug("Manipulation Starting");
            e.Handled = true;
        }

        public void Window_ManipulationDelta(ManipulationDeltaEventArgs e)
        {
            System.Windows.Controls.Grid touchGrid = e.Source as System.Windows.Controls.Grid;
            string touchName = touchGrid.Name;

            if ("TouchCs".Equals(touchName))
            {
                ReviewStatus.Zoom.Window_ManipulationDelta(e);

                if (CommonUtil.GetRoundScale(ReviewStatus.Zoom.ScaleX) > CommonUtil.GetRoundScale(Constants.ZoomScaleDefault * this.convertedFoV))
                {
                    IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                    ReviewStatus.IsCalciumOn = false;
                    ReviewStatus.IsSheathOn = false;
                }

                if (CommonUtil.GetRoundScale(ReviewStatus.Zoom.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomScaleDefault * this.convertedFoV))
                {
                    ReviewStatus.IsCalciumOn = true;
                    ReviewStatus.IsSheathOn = true;

                    if (!ReviewStatus.IsLumenProfile)
                        IndicatorCrossSection.IsVisible = Visibility.Visible;
                }
            }
            else if ("TouchAngio".Equals(touchName))
            {
                ReviewStatus.ZoomAngio.Window_ManipulationDelta(e);
            }
            else if ("TouchAngioCs".Equals(touchName))
            {
                ReviewStatus.ZoomAngioCs.Window_ManipulationDelta(e);

                if (CommonUtil.GetRoundScale(ReviewStatus.ZoomAngioCs.ScaleX) > CommonUtil.GetRoundScale(Constants.ZoomAngioCsScaleDefault * this.convertedFoV))
                {
                    IndicatorCrossSectionAngio.IsVisible = Visibility.Collapsed;
                    ReviewStatus.IsCalciumOnAngioCs = false;
                    ReviewStatus.IsSheathOnAngioCs = false;
                }

                if (CommonUtil.GetRoundScale(ReviewStatus.ZoomAngioCs.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomAngioCsScaleDefault * this.convertedFoV))
                {
                    ReviewStatus.IsCalciumOnAngioCs = true;
                    ReviewStatus.IsSheathOnAngioCs = true;

                    if (!ReviewStatus.IsLumenProfile)
                        IndicatorCrossSectionAngio.IsVisible = Visibility.Visible;
                }
            }
        }

        public void Window_ManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _log.Debug("Manipulation Completed");
            e.Handled = true;
        }

        private void ZoomIn()
        {
            _log.Debug("ZoomIn");

            if (ReviewStatus.Zoom.ZoomIn())
            {
                if (ReviewStatus.IsMeasurementOn)
                    MeasurementCommand = Constants.MeasureZoomIn;
                IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                ReviewStatus.IsCalciumOn = false;
                ReviewStatus.IsSheathOn = false;
            }
        }

        private void ZoomOut()
        {
            _log.Debug("ZoomOut");

            if (ReviewStatus.Zoom.ZoomOut() && ReviewStatus.IsMeasurementOn)
                MeasurementCommand = Constants.MeasureZoomOut;

            if (CommonUtil.GetRoundScale(ReviewStatus.Zoom.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomScaleDefault * this.convertedFoV))
            {
                ReviewStatus.IsCalciumOn = true;
                ReviewStatus.IsSheathOn = true;

                if (!ReviewStatus.IsLumenProfile)
                    IndicatorCrossSection.IsVisible = Visibility.Visible;
            }
        }

        private void ZoomInAngioCs()
        {
            _log.Debug("ZoomInAngioCs");

            if (ReviewStatus.ZoomAngioCs.ZoomIn())
            {
                IndicatorCrossSectionAngio.IsVisible = Visibility.Collapsed;
                ReviewStatus.IsCalciumOnAngioCs = false;
                ReviewStatus.IsSheathOnAngioCs = false;
            }
        }

        private void ZoomOutAngioCs()
        {
            _log.Debug("ZoomOutAngioCs");

            ReviewStatus.ZoomAngioCs.ZoomOut();

            if(CommonUtil.GetRoundScale(ReviewStatus.ZoomAngioCs.ScaleX) == CommonUtil.GetRoundScale(Constants.ZoomAngioCsScaleDefault * this.convertedFoV))
            {
                ReviewStatus.IsCalciumOnAngioCs = true;
                ReviewStatus.IsSheathOnAngioCs = true;

                if (!ReviewStatus.IsLumenProfile)
                    IndicatorCrossSectionAngio.IsVisible = Visibility.Visible;
            }
        }

        private void ZoomInAngio()
        {
            _log.Debug("ZoomInAngio");

            ReviewStatus.ZoomAngio.ZoomIn();
        }

        private void ZoomOutAngio()
        {
            _log.Debug("ZoomOutAngio");

            ReviewStatus.ZoomAngio.ZoomOut();
        }

        private void AdjustReset()
        {
            _log.Debug("AdjustReset");

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Present";
            IList<Configuration> presents = _sqlManager.SelectConfiguration(sqlParameters);
            if (presents != null && presents.Count > 0)
            {
                Brightness = int.Parse(presents.FirstOrDefault(x => x.Key == "brightness").Value);
                Contrast = int.Parse(presents.FirstOrDefault(x => x.Key == "contrast").Value);
                FieldOfView = double.Parse(presents.FirstOrDefault(x => x.Key == "FoV").Value);
            }
        }

        private void CoRegistration()
        {
            _log.Debug("CoRegistration");
            ReviewStatus.AngioFrameNumber = CurrentAngioFrameNumber;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewAngioCoRegPage) { Parameter = parameter });
        }

        private void EditCase()
        {
            _log.Debug("EditCase");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["vessel"] = PatientCase.Vessel;
            parameter["procedure"] = PatientCase.Procedure;
            parameter["location"] = PatientCase.Location;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;
            var result = _dialogService.OpenDialog(new EditCaseInfoDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                PatientCase.Vessel = data["vessel"].ToString();
                PatientCase.Procedure = data["procedure"].ToString();
                PatientCase.Location = data["location"].ToString();
                PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                PatientCase.Comment = data["comment"].ToString();

                if (IsChangedLumenProfileValue())
                {
                    imglumenProfile = null;
                    imglumenProfileExtra = null;
                    MinimalValueChanged();
                    DrawLumenProfile(longitudeFrameInfo.curFrame - 1);
                    SetLumenProfileValue();
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
            sqlParameters["location"] = PatientCase.Location;
            sqlParameters["procedure"] = PatientCase.Procedure;
            PatientCase.IndicatorDegree = Degree;
            sqlParameters["indicator_degree"] = PatientCase.IndicatorDegree;
            sqlParameters["colormap"] = PatientCase.Colormap;
            sqlParameters["calcium_threshold"] = PatientCase.CalciumThreshold;
            sqlParameters["expansion_calculation"] = PatientCase.ExpansionCalculation;
            sqlParameters["expansion_threshold"] = PatientCase.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = PatientCase.AppositionThreshold;
            PatientCase.Brightness = Brightness;
            sqlParameters["brightness"] = PatientCase.Brightness;
            PatientCase.Contrast = Contrast;
            sqlParameters["contrast"] = PatientCase.Contrast;
            sqlParameters["z_offset"] = PatientCase.ZOffset;
            PatientCase.FieldOfView = FieldOfView;
            sqlParameters["field_of_view"] = PatientCase.FieldOfView;
            PatientCase.SectionProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
            sqlParameters["section_proximal"] = PatientCase.SectionProximal;
            PatientCase.SectionDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);
            sqlParameters["section_distal"] = PatientCase.SectionDistal;
            sqlParameters["guidewire_radius"] = PatientCase.GuidewireRadius;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
            {
                _log.Error("Update Error");
            }
            else
            {
                PatientCase.LumenContours = LumenContours;
                PatientCase.LumenSidebranches = LumenSidebranches;
                PatientCase.LumenStents = LumenStents;
                PatientCase.LumenGuidewires = LumenGuidewires;

                sqlParameters.Clear();
                sqlParameters["id"] = PatientCase.Id;
                PatientCase.CrossSection = ConvertMeasurementsToJson(Measurements);
                sqlParameters["cross_section"] = PatientCase.CrossSection;
                PatientCase.Longitude = ConvertLongitudeToJson();
                sqlParameters["longitude"] = PatientCase.Longitude;
                PatientCase.Bookmark = JsonConvert.SerializeObject(Bookmarks, Newtonsoft.Json.Formatting.Indented);
                sqlParameters["bookmark"] = PatientCase.Bookmark;

                if (this.isLumenContourSave)
                {
                    sqlParameters["lumen_contour"] = PatientCase.StrLumenContour;
                    sqlParameters["lumen_sidebranch"] = PatientCase.StrLumenSidebranch;
                    sqlParameters["lumen_stent"] = PatientCase.StrLumenStent;
                    sqlParameters["lumen_guidewire"] = PatientCase.StrLumenGuidewire;
                    sqlParameters["ffr_plaque"] = "";
                    sqlParameters["co_registration"] = PatientCase.StrCoRegistration;
                    nRows = _sqlManager.UpsertPatientCaseAnnotation(sqlParameters);
                }
                else
                {
                    nRows = _sqlManager.UpdatePatientCaseAnnotationWithoutLumenContour(sqlParameters);
                }

                if (nRows == 0)
                {
                    _log.Error("Update Error");
                }
            }
        }

        private string ConvertMeasurementsToJson(List<Measurement> param)
        {
            List<Measurement> measurements = new List<Measurement>();

            //Cross-section
            foreach (Measurement measurement in param)
            {
                if (measurement.AreaGeometries.Count > 0 || measurement.LengthGeometries.Count > 0 || measurement.TextGeometries.Count > 0 || measurement.AngleGeometries.Count > 0) //add by gordon 250718
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

            return JsonConvert.SerializeObject(measurements, Newtonsoft.Json.Formatting.Indented);
        }

        private string ConvertLongitudeToJson()
        {
            //Longitude
            Measurement longitudeMeasurement = new Measurement();
            longitudeMeasurement.LengthGeometries = LModeLengthGeometries;
            longitudeMeasurement.TextGeometries = LModeTextGeometries;

            return JsonConvert.SerializeObject(longitudeMeasurement, Newtonsoft.Json.Formatting.Indented);
        }

        private void FfrValueChangedCheck()
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["id"] = PatientCase.Id;
            IList<StringModel> ffrPlaques = _sqlManager.SelectPatientCaseFfr(sqlParameters);

            if (!String.IsNullOrEmpty(ffrPlaques[0].ReturnString2))
            {
                FfrFeature ffrValue = JsonConvert.DeserializeObject<FfrFeature>(ffrPlaques[0].ReturnString2);
                if (ffrValue == null)
                    return;

                bool isSame = true;

                if (ffrValue.ProximalLumenArea != Section.Proximal.DValue)
                    isSame = false;
                if (ffrValue.MinimalLumenArea != Section.MlaValue.DValue)
                    isSame = false;
                if (ffrValue.DistalLumenArea != Section.Distal.DValue)
                    isSame = false;
                if (ffrValue.LesionLength != Section.LesionLength.DValue)
                    isSame = false;
                if (ffrValue.VesselType != PatientCase.Vessel)
                    isSame = false;

                if (isSame)
                {
                    if (PatientCase.FfrFeature == null)
                        PatientCase.FfrFeature = ffrValue;
                }
                else
                {
                    PatientCase.FfrFeature = null;
                }
            }
        }

        #endregion

        /*
        * Image(Cross-section, Longitude)
        */
        #region Image(Cross-section, Longitude)

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
                    IndicatorCrossSection.Coordinate.X = point.X + (frameworkElement.ActualWidth / 2);
                    IndicatorCrossSection.Coordinate.Y = point.Y + (frameworkElement.ActualHeight / 2);
                }
                else if (frameworkElement.Name.Equals("crossSectionImageSmall"))
                {
                    IndicatorCrossSectionAngio.Coordinate.X = point.X + (frameworkElement.ActualWidth / 2);
                    IndicatorCrossSectionAngio.Coordinate.Y = point.Y + (frameworkElement.ActualHeight / 2);
                }
                else if (frameworkElement.Name.Equals("lumenProfile") || frameworkElement.Name.Equals("lMode"))
                {
                    IndicatorLongitude.Coordinate = point;
                    Section.Proximal.Coordinate = point;
                    Section.Distal.Coordinate = point;
                }
            }
        }
        protected override bool MoveToFrame(RaySession session, int nFrame)
        {
            bool ret = base.MoveToFrame(session, nFrame);

            if (ret == false || PatientCase.AngioYn == false || PatientCase.AngioFrame.AngioImage.Count == 0) return false;
            syncAngioFrame(nFrame);
            return true;
        }
        protected override void UpdateCrossSectionImage()
        {
            if (DrawCrossSectionImage())
            {
                DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Review];                
                if (!IndicatorLongitude.IsCaptured) updateNavigator(imageInfo.Current, imageInfo.Total);

                FrameNumber = imageInfo.Current;

                if (CommonUtil.IsPreCase(PatientCase.Procedure))
                    DrawCalciumIndicator();
            }
        }

        protected override void UpdateLumenProfile()
        {
            if (DeviceStatus.IsLumenLoaded && DeviceStatus.IsLumenSaved && !this.isLumenProfileInit)
            {
                // when generating longitude image is completed
                if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame)
                {
                    MeasurementCommand = Constants.MeasureDrawAll;
                    IndicatorLongitude.IsEnabled = true;
                    Section.Proximal.IsEnabled = true;
                    Section.Distal.IsEnabled = true;
                    MinimalValueChanged();

                    this.isLumenProfileInit = true;
                    ReviewStatus.IsMeasureInit = true;
                }
                else if (!this.isLumenLoadedInit)
                {
                    int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
                    int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);
                    Section.CalcMean(LumenContours, frameProximal, frameDistal);
                    this.isLumenLoadedInit = true;
                }

                DrawLumenProfile(longitudeFrameInfo.curFrame - 1);
            }
        }

        private void DrawLumenProfile(int totalFrame)
        {
            int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
            int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

            imglumenProfile = CommonUtil.MakeLumenProfileImageOneByOne(imglumenProfile, LumenContours, LumenSidebranches, LumenStents, PatientCase.AppositionThreshold, frameProximal, frameDistal, CommonUtil.IsPostCase(PatientCase.Procedure), totalFrame);
            DrawLumenProfileImage();

            List<int> colorFrames = new List<int>();
            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                colorFrames = CommonUtil.GetCalciumList(LumenContours, PatientCase.CalciumThreshold);
            }
            else if (CommonUtil.IsPostCase(PatientCase.Procedure))
            {
                int stentProximal = 0, stentDistal = 0;
                CommonUtil.GetStentProximalDistal(LumenStents, out stentProximal, out stentDistal);

                colorFrames = CommonUtil.GetExpansionList(LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, Section.RefArea, PatientCase.ExpansionThreshold);
            }
            imglumenProfileExtra = CommonUtil.MakeLumenProfileImageExtraOneByOne(imglumenProfileExtra, ReviewStatus.NumberOfFrames, colorFrames, CommonUtil.IsPreCase(PatientCase.Procedure), totalFrame);
            DrawLumenProfileImageExtra();
        }

        private void DrawCalciumIndicator()
        {
            if (LumenContours == null || LumenContours.Count == 0 || FrameNumber < 0 || LumenContours[FrameNumber].Calcium == null)
                return;

            CalciumIndicator = CommonUtil.DrawCalciumIndicator(LumenContours[FrameNumber].Calcium.List, Constants.CalciumIndicatorColor, (int)Constants.CalciumIndicatorSize);
            CalciumIndicatorAngio = CommonUtil.DrawCalciumIndicator(LumenContours[FrameNumber].Calcium.List, Constants.CalciumIndicatorColor, (int)Constants.CalciumIndicatorAngioSize);

            TotalAngle = LumenContours[FrameNumber].Calcium.TotalAngle;
            MaxThickness = LumenContours[FrameNumber].Calcium.MaxThickness;
            MaxCalciumDegree = LumenContours[FrameNumber].Calcium.MaxThicknessDegree;
        }

        private void SetLumenProfileValue()
        {
            this.originSectionProximalX = Section.Proximal.X;
            this.originSectionDistalX = Section.Distal.X;
            this.originProcedure = PatientCase.Procedure;
        }

        private void SetLumenProfileInit()
        {
            imglumenProfile = null;
            imglumenProfileExtra = null;
            int proximalIdx = 0;
            int distalIdx = 0;

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
                int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

                int count = frameDistal - frameProximal + 1;
                var mlaSubset = LumenContours.GetRange(frameProximal, count).Where(x => x.Area > 0);
                if (mlaSubset.Any())
                {
                    double mla = mlaSubset.Min(x => x.Area);
                    int mlaIdx = LumenContours.GetRange(frameProximal, count).FindIndex(x => x.Area == mla) + frameProximal;

                    int frameDiff = (int)(Constants.PreLesionLengthInitValue * ReviewStatus.NumberOfFrames * 10 / int.Parse(PatientCase.PullbackLength));
                    proximalIdx = mlaIdx - frameDiff > 0 ? mlaIdx - frameDiff : 0;
                    distalIdx = mlaIdx + frameDiff < ReviewStatus.NumberOfFrames ? mlaIdx + frameDiff : ReviewStatus.NumberOfFrames - 1;
                }
                else
                {
                    proximalIdx = 0;
                    distalIdx = ReviewStatus.NumberOfFrames - 1;
                }
            }
            else
            {
                int stentProximal = 0, stentDistal = 0;
                if(CommonUtil.GetStentProximalDistal(LumenStents, out stentProximal, out stentDistal))
                {
                    int frameDiff = (int)(Constants.PostLesionLengthInitValue * ReviewStatus.NumberOfFrames * 10 / int.Parse(PatientCase.PullbackLength));
                    proximalIdx = stentProximal - frameDiff > 0 ? stentProximal - frameDiff : 0;
                    distalIdx = stentDistal + frameDiff < ReviewStatus.NumberOfFrames ? stentDistal + frameDiff : ReviewStatus.NumberOfFrames - 1;
                }
                else
                {
                    proximalIdx = 0;
                    distalIdx = ReviewStatus.NumberOfFrames - 1;
                }
            }

            if(proximalIdx >= 0)
                Section.Proximal.X = CommonUtil.GetPositionFromFrame(proximalIdx, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
            if(distalIdx >= 0)
                Section.Distal.X = CommonUtil.GetPositionFromFrame(distalIdx, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

            SetLumenProfileValue();
        }

        private bool IsChangedLumenProfileValue()
        {
            if (this.originSectionProximalX != Section.Proximal.X || this.originSectionDistalX != Section.Distal.X || this.originProcedure != PatientCase.Procedure)
                return true;
            else
                return false;
        }

        private void MinimalValueChanged()
        {
            int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveCenterWidth);
            int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

            Section.VisibleMlaMld(false);
            Section.VislbleMsaMinExp(false);

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (Section.SetMlaMld(LumenContours, frameProximal, frameDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength))
                    Section.VisibleMlaMld(true);
                else
                    Section.VisibleMlaMld(false);
            }
            else
            {
                int stentProximal = 0, stentDistal = 0;
                CommonUtil.GetStentProximalDistal(LumenStents, out stentProximal, out stentDistal);

                if (Section.SetMsaMinExp(LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength))
                    Section.VislbleMsaMinExp(true);
                else
                    Section.VislbleMsaMinExp(false);
            }
        }

        #endregion

        /*
        * Indicator
        */
        #region Indicator

        private void RotateIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.IsCaptured)
            {
                Point crossSectionCenter;
                if (ReviewStatus.IsAngioOn)
                {
                    crossSectionCenter = IndicatorCrossSectionAngio.Coordinate;
                }
                else
                {
                    crossSectionCenter = IndicatorCrossSection.Coordinate;
                }

                indicator.SetDirection(crossSectionCenter, Degree);
                if (!indicator.IsValid) return;

                if (indicator.IsCrossSectionClicked)
                {
                    Point headerSidePointDiff = new Point(indicator.X, indicator.Y);
                    if (indicator.OppositeCaptured)
                    {
                        double xOffset = indicator.X - crossSectionCenter.X;
                        double yOffset = indicator.Y - crossSectionCenter.Y;

                        headerSidePointDiff.X = crossSectionCenter.X - xOffset;
                        headerSidePointDiff.Y = crossSectionCenter.Y - yOffset;
                    }

                    double pointXDiff = crossSectionCenter.X - headerSidePointDiff.X;
                    double pointYDiff = crossSectionCenter.Y - headerSidePointDiff.Y;
                    indicator.IndicatorDiff = Math.Round((Math.Atan2(pointYDiff, pointXDiff) * 180 / Math.PI), 1) - Degree;
                    indicator.IsCrossSectionClicked = false;
                }

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
                Degree = Math.Round((Math.Atan2(pointY, pointX) * 180 / Math.PI), 1) - indicator.IndicatorDiff;
            }
        }

        private void MoveIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.IsCaptured)
            {
                if (indicator.IsLongitudeClicked)
                {
                    StopPlayback();

                    indicator.IsLongitudeClicked = false;
                    return;
                }

                if (indicator.IsLongitudeMove)
                {
                    indicator.IndicatorDiff = indicator.PointLongitudeX - indicator.Coordinate.X - indicator.X;
                    indicator.IsLongitudeMove = false;
                }

                double indicatorX = indicator.PointLongitudeX - indicator.Coordinate.X - indicator.IndicatorDiff;

                if (indicator.IsSectionIndicator)
                {
                    double sectionIndicatorCenter = Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth;

                    if (indicator.IsSectionProximal && (indicatorX >= Section.Distal.X - sectionIndicatorCenter))
                    {
                        indicator.X = Section.Distal.X - sectionIndicatorCenter;
                        setCurrentFrame(indicator.X + Constants.SectionIndicatorMoveCenterWidth);
                        return;
                    }

                    if (!indicator.IsSectionProximal && (indicatorX <= Section.Proximal.X + sectionIndicatorCenter))
                    {
                        indicator.X = Section.Proximal.X + sectionIndicatorCenter;
                        setCurrentFrame(indicator.X + Constants.SectionIndicatorMoveCenterWidth);
                        return;
                    }

                    if (indicatorX < -Constants.SectionIndicatorMoveCenterWidth)
                    {
                        indicator.X = -Constants.SectionIndicatorMoveCenterWidth;
                    }
                    else if (indicatorX > Constants.LongitudeWidth - sectionIndicatorCenter)
                    {
                        indicator.X = Constants.LongitudeWidth - sectionIndicatorCenter;
                    }
                    else
                    {
                        indicator.X = indicatorX;
                    }

                    setCurrentFrame(indicator.X + Constants.SectionIndicatorMoveCenterWidth);

                    if (IsChangedLumenProfileValue())
                    {
                        imglumenProfile = null;
                        imglumenProfileExtra = null;
                        MinimalValueChanged();
                        DrawLumenProfile(longitudeFrameInfo.curFrame - 1);
                        SetLumenProfileValue();
                    }
                }
                else
                {
                    indicatorX = indicatorX + indicator.IndicatorDiff - Constants.LongitudeIndicatorWidth / 2;
                    double indicatorCenterX = indicatorX + Constants.LongitudeIndicatorWidth / 2;

                    if (indicatorCenterX < 0)
                    {
                        indicator.X = 0 - Constants.LongitudeIndicatorWidth / 2;
                        indicator.CenterX = 0;
                        setCurrentFrame(0);
                    }
                    else if (indicatorCenterX > Constants.LongitudeWidth)
                    {
                        indicator.X = Constants.LongitudeWidth - Constants.LongitudeIndicatorWidth / 2;
                        indicator.CenterX = Constants.LongitudeWidth;
                        setCurrentFrame(Constants.LongitudeWidth);
                    }
                    else
                    {
                        indicator.X = indicatorX;
                        indicator.CenterX = indicatorCenterX;
                        setCurrentFrame(indicatorCenterX);
                    }
                }
            }
        }

        private void TouchMoveIndicator(object param)
        {
            StopPlayback();

            MouseEventArgs e = (MouseEventArgs)param;
            var position = e.GetPosition((IInputElement)e.Source);

            IndicatorLongitude.X = position.X - Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.CenterX = IndicatorLongitude.X + Constants.LongitudeIndicatorWidth / 2;
            setCurrentFrame(IndicatorLongitude.CenterX);
        }

        private void updateNavigator(int curFrame, int totalFrame)
        {
            if (FrameNumber == curFrame)
                return;

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

        #endregion

        /*
        * CoRegistration
        */
        #region CoRegistration
        private void syncAngioFrame(int value)
        {
            int OctFrameLength = ReviewStatus.NumberOfFrames;
            int angioTotalFrameNum = PatientCase.AngioFrame.AngioFrameNum;

            double ratio = (double)angioTotalFrameNum / OctFrameLength * value;
            CurrentAngioFrameNumber = (int)ratio;

            if (CurrentAngioFrameNumber < PatientCase.AngioFrame.AngioImage.Count)
            {
                CurrentAngioImage = PatientCase.AngioFrame.AngioImage[CurrentAngioFrameNumber];
            }
        }

        private void ThreadReadAngioFrames()
        {
            try
            {
                int angioFrameHeight = 0;
                int angioFrameWidth = 0;
                int channels = 3;

                string file = PatientCase.Image;
                string angioFile = file.Substring(0, file.Length - 3) + "angioframes";
                string paramsFile = file.Substring(0, file.Length - 3) + "params";

                string directory = Path.Combine(Constants.DataRootPath, PatientCase.PatientId);
                string angioPath = Path.Combine(directory, angioFile);
                string paramsPath = Path.Combine(directory, paramsFile);
                int newHeight, newWidth;
                float Scale;

                // Recording -> Review
                if (_angioManager.fromRecording)
                {
                    _angioManager.fromRecording = false;

                    while (!_angioManager.threadOnSaveFinished)
                    {
                        Thread.Sleep(300);
                    }

                    angioFrameHeight = _angioManager.AngioFrameHeight;
                    angioFrameWidth = _angioManager.AngioFrameWidth;
                    PatientCase.AngioFrame.AngioFrameNum = _angioManager.AngioSaveFrameNum;
                    channels = _angioManager.AngioBitsPerPixel / 8;

                    _log.Debug($"angio : Height = {angioFrameHeight}, Width = {angioFrameWidth}, Channel = {channels}");

                    Scale = angioFrameHeight > angioFrameWidth ? (float)Constants.AngioSize / angioFrameHeight : (float)Constants.AngioSize / angioFrameWidth;
                    
                    if (Scale >= 1.0)
                    {
                        newHeight = (int)(angioFrameHeight / Scale);
                        newWidth = (int)(angioFrameWidth / Scale);
                    }
                    else
                    {
                        newHeight = (int)(angioFrameHeight * Scale);
                        newWidth = (int)(angioFrameWidth * Scale);
                    }

                    for (int i = 0; i < _angioManager.AngioSaveFrameNum; i++)
                    {
                        if (!_angioManager.angioBuffer.TryDequeue(out byte[] data))
                        {
                            _log.Debug("Buffer underrun while reading angioBuffer, currIndex = " + i.ToString());
                            break;
                        }
                        Mat frame = new Mat(angioFrameHeight, angioFrameWidth, MatType.CV_8UC(channels), data);
                        Cv2.Resize(frame, frame, new OpenCvSharp.Size(newWidth, newHeight));

                        switch (channels)
                        {
                            case 3:
                                Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2GRAY);
                                break;

                            case 4:
                                Cv2.CvtColor(frame, frame, ColorConversionCodes.RGBA2GRAY);
                                break;
                        }

                        Mat paddedFrame = new Mat((int)Constants.AngioSize, (int)Constants.AngioSize, MatType.CV_8UC1, Scalar.Black);

                        int top = ((int)Constants.AngioSize - newHeight) / 2;
                        int left = ((int)Constants.AngioSize - newWidth) / 2;
                        OpenCvSharp.Rect roi = new OpenCvSharp.Rect(left, top, newWidth, newHeight);
                        Mat destinationROI = new Mat(paddedFrame, roi);
                        frame.CopyTo(destinationROI);

                        AngioFrames.Add(paddedFrame);
                        PatientCase.AngioFrame.AngioImage.Add(ConvertMatsToImageSource(paddedFrame));
                    }

                    AngioFrames.Reverse();
                    PatientCase.AngioFrame.AngioImage.Reverse();
                    return;
                }
                else // PatientCaseList -> Review
                {
                    //Read .params
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(paramsPath);
                    XmlNode configNode = xmlDoc.SelectSingleNode("/config");
                    angioFrameHeight = int.Parse(configNode.SelectSingleNode("AngioFrameHeight").InnerText);
                    angioFrameWidth = int.Parse(configNode.SelectSingleNode("AngioFrameWidth").InnerText);
                    PatientCase.AngioFrame.AngioFrameNum = int.Parse(configNode.SelectSingleNode("AngioFrameNumber").InnerText);
                    channels = int.Parse(configNode.SelectSingleNode("BitsPerPixel").InnerText) / 8;

                    Scale = angioFrameHeight > angioFrameWidth ? (float)Constants.AngioSize / angioFrameHeight : (float)Constants.AngioSize / angioFrameWidth;

                    if (Scale >= 1.0)
                    {
                        newHeight = (int)(angioFrameHeight / Scale);
                        newWidth = (int)(angioFrameWidth / Scale);
                    }
                    else
                    {
                        newHeight = (int)(angioFrameHeight * Scale);
                        newWidth = (int)(angioFrameWidth * Scale);
                    }

                    using (BinaryReader reader = new BinaryReader(System.IO.File.Open(angioPath, FileMode.Open)))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            byte[] data = reader.ReadBytes(angioFrameWidth * angioFrameHeight * channels);

                            Mat frame = new Mat(angioFrameHeight, angioFrameWidth, MatType.CV_8UC(channels), data);
                            Cv2.Resize(frame, frame, new OpenCvSharp.Size(newWidth, newHeight));

                            switch (channels)
                            {
                                case 3:
                                    Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2GRAY);
                                    break;

                                case 4:
                                    Cv2.CvtColor(frame, frame, ColorConversionCodes.RGBA2GRAY);
                                    break;
                            }

                            Mat paddedFrame = new Mat((int)Constants.AngioSize, (int)Constants.AngioSize, MatType.CV_8UC1, Scalar.Black);

                            int top = ((int)Constants.AngioSize - newHeight) / 2;
                            int left = ((int)Constants.AngioSize - newWidth) / 2;
                            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(left, top, newWidth, newHeight);
                            Mat destinationROI = new Mat(paddedFrame, roi);
                            frame.CopyTo(destinationROI);

                            AngioFrames.Add(paddedFrame);
                            PatientCase.AngioFrame.AngioImage.Add(ConvertMatsToImageSource(paddedFrame));
                        }

                        reader.Close();
                    }

                    AngioImageProcessing();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"[ThreadReadAngioFrames] Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private ImageSource ConvertMatsToImageSource(Mat mat)
        {
            using (var stream = new MemoryStream())
            {
                mat.WriteToStream(stream, "." + Constants.ExportStillFrameBitmap);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }

        }

        private void ReadTrackPoints()
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;

            IList<PatientCaseAnnotation> annotations = _sqlManager.SelectCoRegistration(sqlParameters);

            if(annotations != null && annotations.Count == 1 )
            {
                if (!string.IsNullOrEmpty(annotations[0].CoRegistration))
                {
                    PatientCase.StrCoRegistration = annotations[0].CoRegistration;
                }
                else
                {
                    PatientCase.StrCoRegistration = "";
                }
            }

            List<CoRegistration> coRegistrations = CommonUtil.JsonToCoRegistrations(PatientCase.StrCoRegistration);

            CoRegistrations = PatientCase.AngioFrame.CoRegistration = coRegistrations;
        }

        private void ImageProcessing(List<Mat> frames)
        {
            int frameNum = 0;
            int imageCount = frames.Count;
            List<List<byte>> statusList = new List<List<byte>>(imageCount);
            List<List<Point>> nextPoints = new List<List<Point>>(imageCount);
            List<List<Point>> pastPoints = new List<List<Point>>(imageCount);

            for (int i = 0; i < imageCount; i++)
            {
                statusList.Add(new List<byte>());
                nextPoints.Add(new List<Point>());
                pastPoints.Add(new List<Point>());
            }

            OpticalFlow(frames, ref statusList, ref nextPoints, ref pastPoints);

            for (int i = 0; i < imageCount; i++)
            {
                Mat gradX = new Mat();
                Mat gradY = new Mat();
                Cv2.Sobel(frames[i], gradX, MatType.CV_64F, 1, 0, ksize: 3);
                Cv2.Sobel(frames[i], gradY, MatType.CV_64F, 0, 1, ksize: 3);
                Mat grad = new Mat();
                Cv2.Magnitude(gradX, gradY, grad);
                Mat edge = new Mat();
                grad.ConvertTo(edge, frames[i].Type());
                //Cv2.ImWrite("edge" + (i + 1).ToString() + ".png", edge);

                Cv2.EqualizeHist(frames[i], frames[i]);
                //Cv2.ImWrite("HE" + (i + 1).ToString() + ".png", frames[i]);// Histogram Equalization

                frames[i] = frames[i] - edge/2;
                //Cv2.ImWrite("real" + (i + 1).ToString() + ".png", frames[i]);
            }

            int thresholdOfNow = 10;    // 현재 프레임이 해당 값보다 작으면 혈관, 크면 혈관이 아닌 걸로 판정
            int thresholdOfOther = 0;  // 앞, 뒤 프레임이 해당 값보다 작으면 현재 프레임이 혈관이 아니라고 판정된 상태에도 혈관으로 판정
            int thresholdCut = 30;      // 앞, 뒤 프레임이 해당 값보다 크면 현재 프레임이 혈관이라고 판정된 상태에도 혈관이 아니라고 판정

            for (int i = 0; i < imageCount; i++)
            {
                Mat nowimage = new Mat();
                nowimage.Create(frames[i].Rows, frames[i].Cols, frames[i].Depth(), frames[i].Type());
                frames[i].CopyTo(nowimage);

                if (i == 0)
                {
                    // 첫 번째 프레임: 다음 프레임과만 비교
                    for (int y = 0; y < frames[i].Rows; y++)
                    {
                        for (int x = 0; x < frames[i].Cols; x++)
                        {
                            int final_x, final_y;
                            ThisPixelGoesWhere(x, y, i + 1, statusList, nextPoints, pastPoints, out final_x, out final_y);

                            byte pixVal = nowimage.At<byte>(y, x);
                            if (pixVal < thresholdOfNow)
                            {
                                nowimage.At<byte>(y, x) = 255;
                                // 다음 프레임에 맞춰서 0으로 만드는 부분은 오류가 많아지는 경향이 있음.
                            }
                            else
                            {
                                if (!(x + final_x < 0 || x + final_x >= nowimage.Cols ||
                                      y + final_y < 0 || y + final_y >= nowimage.Rows) &&
                                    frames[i + 1].At<byte>(y + final_y, x + final_x) < thresholdOfOther)
                                {
                                    nowimage.At<byte>(y, x) = 255;
                                }
                                nowimage.At<byte>(y, x) = 0;
                            }
                        }
                    }
                }
                else if (i == imageCount - 1)
                {
                    // 마지막 프레임: 이전 프레임과만 비교
                    for (int y = 0; y < frames[i].Rows; y++)
                    {
                        for (int x = 0; x < frames[i].Cols; x++)
                        {
                            int final_x, final_y;
                            ThisPixelGoesWhere(x, y, i, statusList, nextPoints, pastPoints, out final_x, out final_y);

                            byte pixVal = nowimage.At<byte>(y, x);
                            if (pixVal < thresholdOfNow)
                            {
                                if (!(x - final_x < 0 || x - final_x >= nowimage.Cols ||
                                      y - final_y < 0 || y - final_y >= nowimage.Rows) &&
                                    frames[i - 1].At<byte>(y - final_y, x - final_x) > thresholdCut)
                                {
                                    nowimage.At<byte>(y, x) = 0;
                                }
                                else
                                    nowimage.At<byte>(y, x) = 255;
                            }
                            else
                            {
                                if (!(x - final_x < 0 || x - final_x >= nowimage.Cols ||
                                      y - final_y < 0 || y - final_y >= nowimage.Rows) &&
                                    frames[i - 1].At<byte>(y - final_y, x - final_x) < thresholdOfOther)
                                {
                                    nowimage.At<byte>(y, x) = 255;
                                }
                                nowimage.At<byte>(y, x) = 0;
                            }
                        }
                    }
                }
                else
                {
                    // 중간 프레임: 이전 프레임과 다음 프레임 모두와 비교
                    for (int y = 0; y < frames[i].Rows; y++)
                    {
                        for (int x = 0; x < frames[i].Cols; x++)
                        {
                            int final_x0, final_y0, final_x1, final_y1;
                            ThisPixelGoesWhere(x, y, i, statusList, nextPoints, pastPoints, out final_x0, out final_y0);
                            ThisPixelGoesWhere(x, y, i + 1, statusList, nextPoints, pastPoints, out final_x1, out final_y1);

                            byte pixVal = nowimage.At<byte>(y, x);
                            if (pixVal < thresholdOfNow)
                            {
                                if (!(x - final_x0 < 0 || x - final_x0 >= nowimage.Cols ||
                                      y - final_y0 < 0 || y - final_y0 >= nowimage.Rows) &&
                                    !(x + final_x1 < 0 || x + final_x1 >= nowimage.Cols ||
                                      y + final_y1 < 0 || y + final_y1 >= nowimage.Rows) &&
                                    (frames[i - 1].At<byte>(y - final_y0, x - final_x0) > thresholdCut &&
                                     frames[i + 1].At<byte>(y + final_y1, x + final_x1) > thresholdCut))
                                {
                                    nowimage.At<byte>(y, x) = 0;
                                }
                                else
                                    nowimage.At<byte>(y, x) = 255;
                            }
                            else
                            {
                                if (!(x - final_x0 < 0 || x - final_x0 >= nowimage.Cols ||
                                      y - final_y0 < 0 || y - final_y0 >= nowimage.Rows) &&
                                    !(x + final_x1 < 0 || x + final_x1 >= nowimage.Cols ||
                                      y + final_y1 < 0 || y + final_y1 >= nowimage.Rows) &&
                                    (frames[i - 1].At<byte>(y - final_y0, x - final_x0) < thresholdOfOther &&
                                     frames[i + 1].At<byte>(y + final_y1, x + final_x1) < thresholdOfOther))
                                {
                                    nowimage.At<byte>(y, x) = 255;
                                }
                                nowimage.At<byte>(y, x) = 0;
                            }
                        }
                    }
                }
                Mat morphedImage = new Mat();
                var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));

                Cv2.MorphologyEx(nowimage, morphedImage, MorphTypes.Close, kernel, iterations: 4);
                //Cv2.ImWrite("nowimage" + frameNum.ToString() + ".png", morphedImage);

                Mat skeleton = Skeletonize(morphedImage);

                byte[] imageData = new byte[frames[i].Rows * frames[i].Cols * frames[i].ElemSize()];
                Marshal.Copy(skeleton.Data, imageData, 0, imageData.Length);
                PatientCase.AngioFrame.DijkstraHeap.Add(new DijkstraHeap(imageData, frames[i].Rows, frames[i].Cols));
                frameNum++;

                //Cv2.ImWrite("check" + frameNum.ToString() + ".png", skeleton);
            }
        }

        private void ThisPixelGoesWhere(int x, int y, int index, 
            List<List<byte>> statusList, List<List<Point>> nextPoints, List<List<Point>> pastPoints, 
            out int nextX, out int nextY)
        {
            int mask_r = 40;
            int validCount = 0;                                 
            double total = 0.0;

            for (int j = 0; j < pastPoints[index].Count; j++)
            {
                if (statusList[index][j] != 0 &&
                    Math.Abs(nextPoints[index][j].X - x) <= mask_r &&
                    Math.Abs(nextPoints[index][j].Y - y) <= mask_r)
                {
                    double tempX = nextPoints[index][j].X - pastPoints[index][j].X;
                    double tempY = nextPoints[index][j].Y - pastPoints[index][j].Y;
                    total += 1.0 / Math.Max(Math.Sqrt(Math.Pow(tempX, 2) + Math.Pow(tempY, 2)), 0.05);
                    validCount++;
                }
            }

            if (validCount > 0)
            {
                double temp_x = 0.0, temp_y = 0.0;
                for (int j = 0; j < pastPoints[index].Count; j++)
                {
                    if (statusList[index][j] != 0 &&
                        Math.Abs(nextPoints[index][j].X - x) <= mask_r &&
                        Math.Abs(nextPoints[index][j].Y - y) <= mask_r)
                    {
                        double dist_x = nextPoints[index][j].X - pastPoints[index][j].X;
                        double dist_y = nextPoints[index][j].Y - pastPoints[index][j].Y;
                        double dist = 1.0 / Math.Max(Math.Sqrt(Math.Pow(dist_x, 2) + Math.Pow(dist_y, 2)), 0.05);
                        temp_x += (dist / total) * dist_x;
                        temp_y += (dist / total) * dist_y;
                    }
                }
                nextX = (int)Math.Round(temp_x);
                nextY = (int)Math.Round(temp_y);
            }
            else
            {
                nextX = 0; nextY = 0;
            }
        }

        private void OpticalFlow(List<Mat> frames, ref List<List<Byte>> statusList, ref List<List<Point>> nextPoints, ref List<List<Point>> pastPoints)
        {
            int checkTooFast = 400;

            // 프레임 간 optical flow 계산
            for (int i = 1; i < frames.Count; i++)
            {
                Mat img1 = frames[i - 1].Clone();
                Mat img2 = frames[i].Clone();

                // ORB 특징점 검출 및 기술자 생성
                var detector = ORB.Create();
                KeyPoint[] keypoints1, keypoints2;
                Mat descriptors1 = new Mat(), descriptors2 = new Mat();
                detector.DetectAndCompute(img1, null, out keypoints1, descriptors1);
                detector.DetectAndCompute(img2, null, out keypoints2, descriptors2);

                // BFMatcher (Hamming norm)로 매칭 수행
                BFMatcher matcher = new BFMatcher(NormTypes.Hamming);
                DMatch[] matches = matcher.Match(descriptors1, descriptors2);

                // 매칭 결과를 거리 기준으로 정렬 (거리가 짧을수록 좋은 매칭)
                List<DMatch> matchList = matches.ToList();
                matchList.Sort((a, b) => a.Distance.CompareTo(b.Distance));

                // 상위 20%만 사용
                int numGoodMatches = (int)(matchList.Count * 0.2);
                matchList = matchList.Take(numGoodMatches).ToList();

                // 매칭된 특징점 위치를 points 리스트에 저장
                List<Point> points1 = new List<Point>();

                foreach (var m in matchList)
                {
                    points1.Add(new Point((int)keypoints1[m.QueryIdx].Pt.X, (int)keypoints1[m.QueryIdx].Pt.Y));
                }

                // Lucas–Kanade 방식으로 옵티컬 플로우 계산
                Mat points1Mat = new Mat(points1.Count, 1, MatType.CV_32FC2, points1.ToArray());
                Mat flowPointsMat = new Mat();
                Mat statusMat = new Mat();
                Mat errMat = new Mat();
                Cv2.CalcOpticalFlowPyrLK(img1, img2, points1Mat, flowPointsMat, statusMat, errMat);

                Point[] flowPoints = new Point[flowPointsMat.Rows];
                for (int j = 0; j < flowPointsMat.Rows; j++)
                {
                    flowPoints[j] = new Point(flowPointsMat.At<Vec2f>(j)[0], flowPointsMat.At<Vec2f>(j)[1]);
                }

                byte[] status = new byte[statusMat.Rows];
                for (int j = 0; j < statusMat.Rows; j++)
                {
                    status[j] = statusMat.At<byte>(j, 0);
                }

                float[] err = new float[errMat.Rows];
                for (int j = 0; j < errMat.Rows; j++)
                {
                    err[j] = errMat.At<float>(j, 0);
                }

                List<Point> flowPointsList = new List<Point>(flowPoints);
                List<byte> statusListForFrame = new List<byte>(status);

                // 너무 빠른 이동(체크 값 초과)한 포인트 제거
                for (int j = 0; j < points1.Count; j++)
                {
                    if (status[j] != 0)
                    {
                        double dx = flowPointsList[j].X - points1[j].X;
                        double dy = flowPointsList[j].Y - points1[j].Y;
                        if (dx * dx + dy * dy > checkTooFast)
                        {
                            flowPointsList.RemoveAt(j);
                            points1.RemoveAt(j);
                            statusListForFrame.RemoveAt(j);
                            j--;
                        }
                    }
                }

                statusList[i] = statusListForFrame;
                nextPoints[i] = flowPointsList;
                pastPoints[i] = points1;
            }
        }


        private Mat Skeletonize(Mat img)
        {
            Mat skel = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Mat temp = new Mat();
            Mat eroded = new Mat();
            Mat current = img.Clone();
            int i = 0;

            var element = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(3, 3));

            bool done;
            do
            {
                i++;
                Cv2.MorphologyEx(current, eroded, MorphTypes.Erode, element);
                Cv2.MorphologyEx(eroded, temp, MorphTypes.Dilate, element);
                Cv2.Subtract(current, temp, temp);
                Cv2.BitwiseOr(skel, temp, skel);
                eroded.CopyTo(current);

                if (i == 100) break; // 검은 화면의 경우 무한반복 탈출
                done = (Cv2.CountNonZero(current) == 0);

            } while (!done);

            return skel;
        }

        #endregion
    }
}
