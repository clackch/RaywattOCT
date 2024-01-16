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

namespace RaywattApp.ViewModels
{
    public partial class ReviewViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

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

        public ReviewViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewViewModel");

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

                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);

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

        private void SetAnnotation()
        {
            string tempCrossSection = "[]", tempLongitude = "", tempBookmark = "[]";

            if (PatientCase.Bookmark != null && PatientCase.CrossSection != null && PatientCase.Longitude != null && PatientCase.LumenContour != null)//From Related Review Pages
            {
                //Cross-Section
                tempCrossSection = PatientCase.CrossSection;

                //Longitude
                tempLongitude = PatientCase.Longitude;

                //Bookmark
                tempBookmark = PatientCase.Bookmark;

                //Lumen Contour
                LumenContours = PatientCase.LumenContour;
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
                }
                else//From Recording
                {
                    this.isLumenContourSave = true;
                    InitializeLumenContour();
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
        }

        private void ThreadMakeLumenProfile(string lumenContour)
        {
            LumenContours = CommonUtil.JsonToLumenContours(lumenContour);

            //TODO - Calcium/Sidebranch 추가를 위한 테스트 코드 (추후 삭제 필요)
            if (false)
                GetMlData();

            if (ReviewStatus.IsContourStentOn)
                LumenContourCommand = Constants.LumenContourDraw;
            else
                LumenContourCommand = Constants.LumenContourCurrentInit;

            DeviceStatus.IsLumenLoaded = true;
        }

        private void InitializeLumenContour()
        {
            LumenContours = new List<LumenContour>();

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
                    LumenContourProcess(curFrame);
                }
            }

            LumenContourProcess(frame);

            if (ReviewStatus.NumberOfFrames - 1 == frame)
            {
                //TODO - ML detection에서 Calcium/Sidebranch 가져오도록 개발되면 삭제 필요
                GetMlData();

                if (ReviewStatus.IsContourStentOn)
                    LumenContourCommand = Constants.LumenContourDraw;
                else
                    LumenContourCommand = Constants.LumenContourCurrentInit;

                PatientCase.StrLumenContour = CommonUtil.LumenContoursToJson(LumenContours);

                DeviceStatus.IsLumenSaved = true;

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

        private void LumenContourProcess(int frameInfo)
        {
            int num = RayGetNumOfLumenContourPoints(frameInfo);
            if (num > 0)
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
        }

        //TODO - Calcium/Sidebranch 테스트 데이터 만드는 함수 (추후 삭제 필요)
        private void GetMlData()
        {
            Random random = new Random();
            List<int> samples = new List<int>();
            double firstSize = 0, secondSize = 0, thirdSize = 0;
            int range = 0, i = 0, sbSize = 0, sbIdx = 0;

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
                lumenContour.Calcium.MaxThickness = Math.Round(lumenContour.Calcium.TotalAngle / 200.0, 2);

                int idx = firstSize > secondSize ? firstSize > thirdSize ? 0 : 2 : secondSize > thirdSize ? 1 : 2;
                double size = firstSize > secondSize ? firstSize > thirdSize ? firstSize : thirdSize : secondSize > thirdSize ? secondSize : thirdSize;
                lumenContour.Calcium.MaxThicknessDegree = samples[idx] + size / 2;
                if (firstSize + secondSize + thirdSize == 0)
                    lumenContour.Calcium.MaxThicknessDegree = -1;

                //Sidebranch
                if (sidebranchIdx.Contains(sbIdx) && sbSize == 0)
                {
                    sbSize = random.Next(0, 50);
                }

                if (sbSize > 0)
                {
                    lumenContour.HasSidebranch = true;
                    sbSize--;
                }
                else
                {
                    lumenContour.HasSidebranch = false;
                }

                sbIdx++;
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

        private void ZoomIn()
        {
            _log.Debug("ZoomIn");

            if (ReviewStatus.Zoom.ZoomIn())
            {
                if (ReviewStatus.IsMeasurementOn)
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

            if (ReviewStatus.Zoom.ScaleX == Constants.ZoomScaleDefault)
            {
                ReviewStatus.IsCalciumOn = true;

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
            parameter["physicianName"] = PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;
            var result = _dialogService.OpenDialog(new EditCaseInfoDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                PatientCase.Vessel = data["vessel"].ToString();
                PatientCase.Procedure = data["procedure"].ToString();
                PatientCase.PhysicianName = data["physicianName"].ToString();
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
            sqlParameters["procedure"] = PatientCase.Procedure;
            sqlParameters["angio_yn"] = PatientCase.AngioYn;
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
                PatientCase.LumenContour = LumenContours;

                sqlParameters.Clear();
                sqlParameters["id"] = PatientCase.Id;
                PatientCase.CrossSection = ConvertMeasurementsToJson(Measurements);
                sqlParameters["cross_section"] = PatientCase.CrossSection;
                PatientCase.Longitude = ConvertLongitudeToJson();
                sqlParameters["longitude"] = PatientCase.Longitude;
                PatientCase.Bookmark = JsonConvert.SerializeObject(Bookmarks, Formatting.Indented);
                sqlParameters["bookmark"] = PatientCase.Bookmark;

                if (this.isLumenContourSave)
                {
                    sqlParameters["lumen_contour"] = PatientCase.StrLumenContour;
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
            //Test
            List<int> appositionFrames = new List<int>() { 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370 };
            imglumenProfile = CommonUtil.MakeLumenProfileImageOneByOne(imglumenProfile, LumenContours, frameProximal, frameDistal, CommonUtil.IsPostCase(PatientCase.Procedure), appositionFrames, totalFrame);
            DrawLumenProfileImage();

            List<int> colorFrames = new List<int>();
            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                colorFrames = CommonUtil.GetCalciumList(LumenContours, PatientCase.CalciumThreshold);
            }
            else if (CommonUtil.IsPostCase(PatientCase.Procedure))
            {
                colorFrames = CommonUtil.GetExpansionList(LumenContours, frameProximal, frameDistal, Section.RefArea, PatientCase.ExpansionThreshold);
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
                if (Section.SetMsaMinExp(LumenContours, frameProximal, frameDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength, PatientCase.ImageResolution))
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
    }
}
