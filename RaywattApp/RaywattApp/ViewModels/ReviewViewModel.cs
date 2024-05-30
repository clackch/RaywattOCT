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
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

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
        private double _prevScale;

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

        private int _angioFrameNumber;
        public int AngioFrameNumber { get { return _angioFrameNumber; } set { _angioFrameNumber = value; OnPropertyChanged(nameof(AngioFrameNumber)); syncAngioFrame(value); } }

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
        private CoRegistration _currentTrackPoint;

        private List<CoRegistration> _angioTrackPoints;
        public List<CoRegistration> AngioTrackPoints { get { return _angioTrackPoints; } set { _angioTrackPoints = value; OnPropertyChanged(nameof(AngioTrackPoints)); } }
        
        [ObservableProperty]
        private List<LumenSidebranch> _lumenSidebranches;

        [ObservableProperty]
        private List<LumenStent> _lumenStents;

        [ObservableProperty]
        private LumenStent _currentLumenStent;

        [ObservableProperty]
        private List<LumenGuidewire> _lumenGuidewires;

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

        private ICommand _toggleContourStentCommand;
        public ICommand ToggleContourStentCommand
        {
            get { return this._toggleContourStentCommand ?? (this._toggleContourStentCommand = new RelayCommand(ToggleContourStent)); }
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
        
        private ICommand _manipulationStartingCommand;
        public ICommand ManipulationStartingCommand
        {
            get { return this._manipulationStartingCommand ?? (this._manipulationStartingCommand = new RelayCommand<object>(Window_ManipulationStarting)); }
        }

        private ICommand _manipulationDeltaCommand;
        public ICommand ManipulationDeltaCommand
        {
            get { return this._manipulationDeltaCommand ?? (this._manipulationDeltaCommand = new RelayCommand<object>(Window_ManipulationDelta)); }
        }

        private ICommand _manipulationCompletedCommand;
        public ICommand ManipulationCompletedCommand
        {
            get { return this._manipulationCompletedCommand ?? (this._manipulationCompletedCommand = new RelayCommand<object>(Window_ManipulationCompleted)); }
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
            AngioTrackPoints = new List<CoRegistration>();
            CurrentTrackPoint = new CoRegistration();

            UpdateCrossSectionImage();
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
                
                ToggleAngio(ReviewStatus.IsAngioOn);
                ToggleLongitude(ReviewStatus.IsLumenProfile);

                SetAngioFrame();

                Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
                Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

                SetLumenProfileValue();

                GetImageInfo(RaySession.Review);

                Degree = PatientCase.IndicatorDegree;
                Brightness = PatientCase.Brightness;
                Contrast = PatientCase.Contrast;
                CrossSectionScale = (1 / PatientCase.ImageResolution) * (Constants.CrossSectionSize / Constants.OCTImageSize);
                CrossSectionAngioScale = (1 / PatientCase.ImageResolution) * (Constants.CrossSectionAngio / Constants.OCTImageSize);
                
                SetAnnotation();
                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);

                AngioFrameNumber = ReviewStatus.AngioFrameNumber;
                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);

                ReviewStatus.IsNoPullback = PatientCase.PullbackType == "TEST" ? true : false;

                if (ReviewStatus.IsPlay)
                    Playback();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
            Save();

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

            if (PatientCase.AngioFrame.CoRegistration.Count == 0)
            {
                ReadTrackPoints();
            }
            else
            {
                AngioTrackPoints = PatientCase.AngioFrame.CoRegistration;
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

            //Cross-Section
            Measurements = JsonConvert.DeserializeObject<List<Measurement>>(tempCrossSection);
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

            //Test
            //InitializeLumenData();
            //DeviceStatus.IsLumenSaved = false;
            //RayStartLumenDetection();
            //this.isLumenContourSave = true;
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
                GetMlData();

                if (ReviewStatus.IsContourStentOn)
                    LumenContourCommand = Constants.LumenContourDraw;
                else
                    LumenContourCommand = Constants.LumenContourCurrentInit;

                PatientCase.StrLumenContour = CommonUtil.LumenContoursToJson(LumenContours);
                PatientCase.StrLumenSidebranch = JsonConvert.SerializeObject(LumenSidebranches, Newtonsoft.Json.Formatting.Indented);
                PatientCase.StrLumenStent = JsonConvert.SerializeObject(LumenStents, Newtonsoft.Json.Formatting.Indented);
                PatientCase.StrLumenGuidewire = JsonConvert.SerializeObject(LumenGuidewires, Newtonsoft.Json.Formatting.Indented);

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
                    if (height > 0)
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

                Mat mat = CommonUtil.ByteMemoryToCvMat(contour, 1, guidewireHeight, 2);

                LumenGuidewires[frameInfo].Points = new List<Point>();
                for (int row = 0; row < mat.Rows; row++)
                {
                    Vec2i point = mat.At<Vec2i>(0, row);
                    LumenGuidewires[frameInfo].Points.Add(new Point(point.Item0, point.Item1));
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
                IndicatorCrossSectionAngio.IsVisible = isLumenProfile ? Visibility.Collapsed : Visibility.Visible;
            else
                IndicatorCrossSection.IsVisible = (!isLumenProfile && ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleAngio(bool isAngioOn)
        {
            ReviewStatus.IsAngioOn = isAngioOn;

            if (ReviewStatus.IsAngioOn)
            {
                ReviewStatus.IsMeasurementOn = false;
                ReviewStatus.IsCalciumOn = true;
                if (ReviewStatus.IsLumenProfile)
                    IndicatorCrossSectionAngio.IsVisible = Visibility.Collapsed;
                else
                    IndicatorCrossSectionAngio.IsVisible = Visibility.Visible;

                MenuExpand(false);
            }
            else
            {
                if (ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault)
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

        private void ToggleContourStent()
        {
            ReviewStatus.IsContourStentOn = !ReviewStatus.IsContourStentOn;
        }

        private void ToggleMeasurement()
        {
            StopPlayback();

            ReviewStatus.IsMeasurementOn = !ReviewStatus.IsMeasurementOn;
        }

        public void Window_ManipulationStarting(object parameter)
        {
            _log.Debug("Manipulation Starting");
            ManipulationStartingEventArgs e = (ManipulationStartingEventArgs)parameter;
            e.ManipulationContainer = Application.Current.MainWindow;
            PrevScale = ReviewStatus.Zoom.ScaleX;
            MeasurementCommand = Constants.MeasureZooming;
            e.Handled = true;
        }

        public void Window_ManipulationDelta(object parameter)
        {
            ManipulationDeltaEventArgs e = (ManipulationDeltaEventArgs)parameter;
            int touchPoints = e.Manipulators.Count();

            if (!ReviewStatus.IsMeasurementOn || (touchPoints > 1 && MeasurementCommand == Constants.MeasureZooming))
            {
                ReviewStatus.Zoom.Window_ManipulationDelta(parameter);

                if (ReviewStatus.Zoom.ScaleX > Constants.ZoomScaleDefault)
                {
                    IndicatorCrossSection.IsVisible = Visibility.Collapsed;
                    ReviewStatus.IsCalciumOn = false;
                    ReviewStatus.IsSheathOn = false;
                }

                if (ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault)
                {
                    ReviewStatus.IsCalciumOn = true;
                    ReviewStatus.IsSheathOn = true;

                    if (!ReviewStatus.IsLumenProfile)
                        IndicatorCrossSection.IsVisible = Visibility.Visible;
                }
            }
        }

        public void Window_ManipulationCompleted(object parameter)
        {
            _log.Debug("Manipulation Completed");
            ManipulationCompletedEventArgs e = (ManipulationCompletedEventArgs)parameter;

            if (ReviewStatus.IsMeasurementOn)
                MeasurementCommand = (PrevScale < ReviewStatus.Zoom.ScaleX) ? Constants.MeasureZoomIn : Constants.MeasureZoomOut;

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

            if (ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault)
            {
                ReviewStatus.IsCalciumOn = true;
                ReviewStatus.IsSheathOn = true;

                if (!ReviewStatus.IsLumenProfile)
                    IndicatorCrossSection.IsVisible = Visibility.Visible;
            }
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
            sqlParameters["angio_yn"] = PatientCase.AngioYn;
            sqlParameters["angio_co_registration"] = PatientCase.AngioCoRegistration;
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
            PatientCase.SectionProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
            sqlParameters["section_proximal"] = PatientCase.SectionProximal;
            PatientCase.SectionDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);
            sqlParameters["section_distal"] = PatientCase.SectionDistal;

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

            SaveFfrValues();
        }

        private void SaveFfrValues()
        {
            if (PatientCase.FfrFeature == null)
                PatientCase.FfrFeature = new FfrFeature();

            double percentAreaStenosis = 0;
            double minimalLumenArea = 0;
            int minimalLumenFrameNumber = 0;

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                percentAreaStenosis = Section.MlaValue.DValue / Section.MeanArea;
                minimalLumenArea = Section.MlaValue.DValue;
                minimalLumenFrameNumber = Section.MlaValue.NValue;
            }
            else
            {
                percentAreaStenosis = Section.MsaValue.DValue / Section.MeanArea;
                minimalLumenArea = Section.MsaValue.DValue;
                minimalLumenFrameNumber = Section.MsaValue.NValue;
            }

            double scaleArea = PatientCase.ImageResolution * PatientCase.ImageResolution;

            PatientCase.FfrFeature.MinimalLumenFrameNumber = minimalLumenFrameNumber;
            PatientCase.FfrFeature.PercentAreaStenosis = Math.Round(percentAreaStenosis * 100, 1);
            PatientCase.FfrFeature.MinimalLumenArea = Math.Round(minimalLumenArea * scaleArea, 2);
            PatientCase.FfrFeature.DistalLumenArea = Math.Round(Section.Distal.DValue * scaleArea, 2);
            PatientCase.FfrFeature.LesionLength = Math.Round(Section.LesionLength.DValue, 1);
            PatientCase.FfrFeature.PlaqueArea = 0.0;
            PatientCase.FfrFeature.ProximalLumenArea = Math.Round(Section.Proximal.DValue * scaleArea, 2);
        }

        private string ConvertMeasurementsToJson(List<Measurement> param)
        {
            List<Measurement> measurements = new List<Measurement>();

            //Cross-section
            foreach (Measurement measurement in param)
            {
                if (measurement.AreaGeometries.Count > 0 || measurement.LengthGeometries.Count > 0 || measurement.TextGeometries.Count > 0)
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

            int OctFrameLength = ReviewStatus.NumberOfFrames;
            int angioTotalFrameNum = PatientCase.AngioFrame.AngioFrameNum;
            
            double ratio = (double)angioTotalFrameNum / OctFrameLength * FrameNumber;
            CurrentAngioFrameNumber = (int)ratio;

            if (CurrentAngioFrameNumber < PatientCase.AngioFrame.AngioImage.Count)
            {
                CurrentAngioImage = PatientCase.AngioFrame.AngioImage[CurrentAngioFrameNumber];
            }

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

                DrawSheathIndicator();
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
                }
                else if (!this.isLumenLoadedInit)
                {
                    int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
                    int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);
                    Section.CalcMean(LumenContours, frameProximal, frameDistal);
                    this.isLumenLoadedInit = true;
                }

                DrawLumenProfile(longitudeFrameInfo.curFrame - 1);
            }
        }

        private void DrawLumenProfile(int totalFrame)
        {
            int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
            int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

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
                int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
                int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

                int count = frameDistal - frameProximal + 1;
                double mla = LumenContours.GetRange(frameProximal, count).Min(x => x.Area);
                int mlaIdx = LumenContours.GetRange(frameProximal, count).FindIndex(x => x.Area == mla) + frameProximal;

                int frameDiff = (int)(Constants.PreLesionLengthInitValue * ReviewStatus.NumberOfFrames * 10 / int.Parse(PatientCase.PullbackLength));
                proximalIdx = mlaIdx - frameDiff > 0 ? mlaIdx - frameDiff : 0;
                distalIdx = mlaIdx + frameDiff < ReviewStatus.NumberOfFrames ? mlaIdx + frameDiff : ReviewStatus.NumberOfFrames - 1;
            }
            else
            {
                int stentProximal = 0, stentDistal = 0;
                CommonUtil.GetStentProximalDistal(LumenStents, out stentProximal, out stentDistal);

                int frameDiff = (int)(Constants.PostLesionLengthInitValue * ReviewStatus.NumberOfFrames * 10 / int.Parse(PatientCase.PullbackLength));
                proximalIdx = stentProximal - frameDiff > 0 ? stentProximal - frameDiff : 0;
                distalIdx = stentDistal + frameDiff < ReviewStatus.NumberOfFrames ? stentDistal + frameDiff : ReviewStatus.NumberOfFrames - 1;
            }

            Section.Proximal.X = CommonUtil.GetPositionFromFrame(proximalIdx, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
            Section.Distal.X = CommonUtil.GetPositionFromFrame(distalIdx, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

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
            int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
            int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

            Section.VisibleMlaMld(false);
            Section.VislbleMsaMinExp(false);

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (Section.SetMlaMld(LumenContours, frameProximal, frameDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength, PatientCase.ImageResolution))
                    Section.VisibleMlaMld(true);
                else
                    Section.VisibleMlaMld(false);
            }
            else
            {
                int stentProximal = 0, stentDistal = 0;
                CommonUtil.GetStentProximalDistal(LumenStents, out stentProximal, out stentDistal);

                if (Section.SetMsaMinExp(LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength, PatientCase.ImageResolution))
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
                    double sectionIndicatorCenter = Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth;

                    if (indicator.IsSectionProximal && (indicatorX >= Section.Distal.X - sectionIndicatorCenter))
                    {
                        indicator.X = Section.Distal.X - sectionIndicatorCenter;
                        return;
                    }

                    if (!indicator.IsSectionProximal && (indicatorX <= Section.Proximal.X + sectionIndicatorCenter))
                    {
                        indicator.X = Section.Proximal.X + sectionIndicatorCenter;
                        return;
                    }

                    if (indicatorX < -Constants.SectionIndicatorCenterWidth)
                    {
                        indicator.X = -Constants.SectionIndicatorCenterWidth;
                    }
                    else if (indicatorX > Constants.LongitudeWidth - sectionIndicatorCenter)
                    {
                        indicator.X = Constants.LongitudeWidth - sectionIndicatorCenter;
                    }
                    else
                    {
                        indicator.X = indicatorX;
                    }

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
            if (value < 0) return;

            int OctFrameLength = ReviewStatus.NumberOfFrames;
            double FrameNumber = (double)PatientCase.AngioFrame.AngioImage.Count / OctFrameLength / value;
            FrameNumber = 1 / FrameNumber;

            base.MoveToFrame(RaySession.Review, (int)FrameNumber);
            CurrentAngioImage = PatientCase.AngioFrame.AngioImage[value];
        }

        private void ThreadReadAngioFrames()
        {
            string file = PatientCase.Image;
            string angioFile = file.Substring(0, file.Length - 3) + "angioframes";
            string paramsFile = file.Substring(0, file.Length - 3) + "params";
            
            string directory = Path.Combine(Constants.DataRootPath, PatientCase.PatientId);
            string angioPath = Path.Combine(directory, angioFile);
            string paramsPath = Path.Combine(directory, paramsFile);

            //Read .params
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(paramsPath);

            XmlNode configNode = xmlDoc.SelectSingleNode("/config");
            int angioFrameHeight = int.Parse(configNode.SelectSingleNode("AngioFrameHeight").InnerText);
            int angioFrameWidth = int.Parse(configNode.SelectSingleNode("AngioFrameWidth").InnerText);
            PatientCase.AngioFrame.AngioFrameNum = int.Parse(configNode.SelectSingleNode("AngioFrameNumber").InnerText);
            int channels = int.Parse(configNode.SelectSingleNode("BitsPerPixel").InnerText) / 8;

            float Scale = angioFrameHeight > angioFrameWidth ? (float)Constants.AngioSize / angioFrameHeight : (float)Constants.AngioSize / angioFrameWidth;

            int newHeight, newWidth;
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

            //Recording -> Review
            if (_angioManager.AngioSaveBuffer.Count != 0)
            {
                foreach (byte[] data in _angioManager.AngioSaveBuffer)
                {
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

                _angioManager.AngioSaveFrameNum = 0;
                _angioManager.AngioSaveBuffer.Clear();
                return;
            }

            //PatientCaseList -> Review
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

            IList<StringModel> coRegistrationTrackPoint = _sqlManager.SelectCoRegistrationTrackPoint(sqlParameters);
            if (coRegistrationTrackPoint == null || coRegistrationTrackPoint.Count == 0 || coRegistrationTrackPoint[0].ReturnString == null) return;

            List<CoRegistration> coRegistrations = JsonConvert.DeserializeObject<List<CoRegistration>>(coRegistrationTrackPoint[0].ReturnString);

            foreach (CoRegistration coReg in coRegistrations)
            {
                PatientCase.AngioFrame.CoRegistration.Add(coReg);
            }

            AngioTrackPoints = PatientCase.AngioFrame.CoRegistration;
        }

        private void ImageProcessing(List<Mat> frames)
        {
            Mat prevEqualImg = null, currEqualImg;
            int frameNum = 0;
            foreach (var frame in frames)
            {
                Mat blurredImage = new Mat();
                Cv2.Blur(frame, blurredImage, new OpenCvSharp.Size(7, 7));

                // HE 영역 분할 처리
                Mat equalizedImage = new Mat();
                var clahe = Cv2.CreateCLAHE(clipLimit: 10, new OpenCvSharp.Size(9, 9));
                clahe.Apply(blurredImage, equalizedImage);

                currEqualImg = equalizedImage.Clone();
                if (prevEqualImg != null)
                {
                    CalculateMotionVector(prevEqualImg, currEqualImg); // Constants.AngioSize Square 
                }
                prevEqualImg = equalizedImage.Clone();

                // thresholdImage 초기화
                Mat thresholdImage = new Mat(equalizedImage.Size(), equalizedImage.Type(), Scalar.All(255));

                // 이진화 (픽셀 값이 100 미만인 경우 -> 255, 그 외에는 그대로 둠)
                for (int y = 0; y < equalizedImage.Rows; y++)
                {
                    for (int x = 0; x < equalizedImage.Cols; x++)
                    {
                        byte pixelValue = equalizedImage.At<byte>(y, x);
                        if (pixelValue > 0)
                        {
                            thresholdImage.Set<byte>(y, x, pixelValue < 100 ? (byte)255 : (byte)0);
                        }
                    }
                }

                // 이미지 변형(분할 : Segmentation) 처리
                Mat morphedImage = new Mat();
                var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
                Cv2.MorphologyEx(thresholdImage, morphedImage, MorphTypes.Open, kernel, iterations: 2);

                // 변형 처리 반복 -> 스켈레톤(골격화)
                Mat skeleton = new Mat();
                skeleton = Skeletonize(morphedImage);

                byte[] imageData = new byte[frame.Rows * frame.Cols * frame.ElemSize()];
                Marshal.Copy(skeleton.Data, imageData, 0, imageData.Length);

                PatientCase.AngioFrame.DijkstraHeap.Add(new DijkstraHeap(imageData, frame.Rows, frame.Cols));
            }
        }
        private void CalculateMotionVector(Mat prevFrame, Mat nextFrame)
        {
            Mat flow = new Mat();
            Cv2.CalcOpticalFlowFarneback(prevFrame, nextFrame, flow, 0.5, 5, 21, 7, 5, 1.1, 0);

            //curr, next: 이전 영상과 현재 영상. 그레이스케일 영상.
            //flow: (출력)계산된 옵티컬플로우.np.ndarray.shape = (h, w, 2(for x, y vector)), dtype = np.float32.
            //pyr_scale: 피라미드 영상을 만들 때 축소 비율. (e.g.) 0.5 ~0.7, 클수록 계산량감소, 오차확률 상승
            //levels: 피라미드 영상 개수. (e.g.) 3
            //winsize: 평균 윈도우 크기. (e.g.) 15 ~21
            //iterations: 각 피라미드 레벨에서 알고리즘 반복 횟수. (e.g.) 3 다다익선(tradeOff -> 계산량)
            //poly_n: 다항식 확장을 위한 이웃 픽셀 크기. 보통 5 또는 7.
            //poly_sigma: 가우시안 표준편차. 보통 poly_n = 5-> 1.1, poly_n = 7-> 1.5.
            //flags: 0, cv2.OPTFLOW_USE_INITIAL_FLOW, cv2.OPTFLOW_FARNEBACK_GAUSSIAN.

            PatientCase.AngioFrame.MotionVector.Add(flow);
        }

        private Mat Skeletonize(Mat img)
        {
            Mat skel = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Mat temp = new Mat();
            Mat eroded = new Mat();
            int i = 0;

            var element = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(3, 3));

            bool done;
            do
            {
                i++;
                Cv2.MorphologyEx(img, eroded, MorphTypes.Erode, element); // 침식(Erode)
                Cv2.MorphologyEx(eroded, temp, MorphTypes.Dilate, element); // 팽창(Dilate)
                Cv2.Subtract(img, temp, temp);
                Cv2.BitwiseOr(skel, temp, skel);
                eroded.CopyTo(img);
                if (i == 100) break; // 검은 화면의 경우 무한반복 탈출

                done = (Cv2.CountNonZero(img) == 0);
            } while (!done);

            return skel;
        }
        #endregion
    }
}
