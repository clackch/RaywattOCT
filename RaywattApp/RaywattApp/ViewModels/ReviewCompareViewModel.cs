using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using Newtonsoft.Json;
using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;
using Point = System.Windows.Point;

namespace RaywattApp.ViewModels
{
    public partial class ReviewCompareViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewCompareViewModel));

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private Point longitudeCoordinate = new Point();
        private Point longitudeCompareCoordinate = new Point();

        [ObservableProperty]
        private bool _isIndicatorLockOn;

        [ObservableProperty]
        private IList<PatientCase> _patientCases;

        [ObservableProperty]
        private PatientCase _displayPatientCase;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private Indicator _indicatorCompareLongitude;

        [ObservableProperty]
        private double _pointLongitudeX;

        private double indicatorDiffX = 0;

        [ObservableProperty]
        private double _pointCompareLongitudeX;

        [ObservableProperty]
        private int displayFrameNumberCompare;

        [ObservableProperty]
        private List<LumenContour> _preLumenContour = new List<LumenContour>();

        [ObservableProperty]
        private List<LumenContour> _postLumenContour = new List<LumenContour>();

        [ObservableProperty]
        private LumenContour _currentPreLumenContour = new LumenContour();

        private string _lumenContourCommand;
        public string LumenContourCommand { get { return _lumenContourCommand; } set { _lumenContourCommand = value; OnPropertyChanged(nameof(LumenContourCommand)); } }

        [ObservableProperty]
        private BitmapSource _lumenProfileImageCompare;
        protected Mat imglumenProfileCompare;

        [ObservableProperty]
        private int _frameNumberCompare = -1;

        [ObservableProperty]
        private bool _isLumenLoaded = true;

        [ObservableProperty]
        private Zoom _zoom = new Zoom(Constants.CrossSectionCompareSize / Constants.OCTImageSize);

        private int indicatorLockOffset;

        private ICommand _caseSelectCancelCommand;
        public ICommand CaseSelectCancelCommand
        {
            get { return this._caseSelectCancelCommand ?? (this._caseSelectCancelCommand = new RelayCommand(CaseSelectCancel)); }
        }

        private ICommand _caseSelectOkCommand;
        public ICommand CaseSelectOkCommand
        {
            get { return this._caseSelectOkCommand ?? (this._caseSelectOkCommand = new RelayCommand<PatientCase>(CaseSelectOk)); }
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

        private ICommand _cmdIndicatorLock;
        public ICommand CmdIndicatorLock
        { 
            get { return this._cmdIndicatorLock ?? (this._cmdIndicatorLock = new RelayCommand(IndicatorLock)); }
        }

        public ReviewCompareViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewCompareViewModel");

            Constants.CurrentPage = Constants.ReviewComparePage;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Collapsed;

            IndicatorCompareLongitude = new Indicator();
            IndicatorCompareLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorCompareLongitude.IsVisible = Visibility.Collapsed;
            IndicatorCompareLongitude.IsCompare = true;

            ExpandLeftUpMenu = false;
            ExpandLeftDownMenu = false;

            IsIndicatorLockOn = false;
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
                ReviewStatus.CurrentPage = Constants.ReviewComparePage;

                if (ReviewStatus.SelectedPatientCase == null)
                {
                    GetPatientCase(true);

                    if (ReviewStatus.SelectedPatientCase != null)
                    {
                        RayStartCompare(ReviewStatus.SelectedPatientCase.ImageFullPath);
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    GetPatientCase(false, ReviewStatus.SelectedPatientCase.LumenContour);
                }

                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);
                PostLumenContour = PatientCase.LumenContour;
                imglumenProfile = CommonUtil.MakeLumenProfileImage(PostLumenContour);

                if (ReviewStatus.SelectedPatientCase != null)
                {
                    SetCrossSectionBackground(RaySession.Compare, Constants.CompareBackgroundColor);

                    if(ReviewStatus.SelectedPatientCase.LumenContour == null)
                    {
                        IsLumenLoaded = false;

                        Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile());
                        threadMakeLumenProfile.Start();
                    }
                    else
                    {
                        PreLumenContour = ReviewStatus.SelectedPatientCase.LumenContour;
                        imglumenProfileCompare = CommonUtil.MakeLumenProfileImage(PreLumenContour);
                    }
                }

                if (DrawLumenProfileImage())
                {
                    IndicatorLongitude.IsVisible = Visibility.Visible;
                }
            }

            GetImageInfo(RaySession.Review);
            MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
            GetImageInfo(RaySession.Compare);
            MoveToFrame(RaySession.Compare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current);

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

        private List<LumenContour> GetLumenContours(string patientCaseId)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = patientCaseId;
            
            IList<PatientCaseAnnotation> patientCaseAnnotations = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);
            
            if (patientCaseAnnotations != null && patientCaseAnnotations.Count == 1)
            {
                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].LumenContour))
                {
                    return JsonConvert.DeserializeObject<List<LumenContour>>(patientCaseAnnotations[0].LumenContour);
                }
            }

            return null;
        }

        private void ThreadMakeLumenProfile()
        {
            ReviewStatus.SelectedPatientCase.LumenContour = GetLumenContours(ReviewStatus.SelectedPatientCase.Id);
            PreLumenContour = ReviewStatus.SelectedPatientCase.LumenContour;
            imglumenProfileCompare = CommonUtil.MakeLumenProfileImage(PreLumenContour);

            IsLumenLoaded = true;

            LumenContourCommand = Constants.LumenContourDraw;
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
            sqlParameters["indicator_degree"] = PatientCase.IndicatorDegree;
            sqlParameters["preset_name"] = PatientCase.PresetName;
            sqlParameters["calcium_threshold"] = PatientCase.CalciumThreshold;
            sqlParameters["expansion_calculation"] = PatientCase.ExpansionCalculation;
            sqlParameters["expansion_threshold"] = PatientCase.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = PatientCase.AppositionThreshold;
            sqlParameters["brightness"] = PatientCase.Brightness;
            sqlParameters["contrast"] = PatientCase.Contrast;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
                _log.Error("Update Error");
        }
        
        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (DrawCrossSectionImage())
            {
                {
                    DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Review];
                    if (!IndicatorLongitude.IsCaptured) updateNavigator(imageInfo.Current, imageInfo.Total, false);

                    FrameNumber = imageInfo.Current;
                }
            }

            if (DrawCrossSectionForCompare())
            {
                {
                    DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Compare];
                    if (!IndicatorCompareLongitude.IsCaptured) updateNavigator(imageInfo.Current, imageInfo.Total, true);

                    FrameNumberCompare = imageInfo.Current;
                    DisplayFrameNumberCompare = FrameNumberCompare + 1;
                }
            }

            if (imglumenProfileCompare != null)
            {
                LumenProfileImageCompare = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileCompare);
                IndicatorCompareLongitude.IsVisible = Visibility.Visible;
            }
            else
            {
                LumenProfileImageCompare = null;
                IndicatorCompareLongitude.IsVisible = Visibility.Collapsed;
            }
        }

        private void CaseSelectCancel()
        {
            _log.Debug("CaseSelectCancel");

            DisplayPatientCase = ReviewStatus.SelectedPatientCase;
            ExpandLeftUpMenu = false;
        }

        private void CaseSelectOk(PatientCase patientCase)
        {
            _log.Debug("CaseSelectOk");

            // avoid duplication
            if (ReviewStatus.SelectedPatientCase != null && patientCase != null)
            {
                if (ReviewStatus.SelectedPatientCase.Id == patientCase.Id)
                {
                    ExpandLeftUpMenu = false;
                    return;
                }
            }

            ReviewStatus.SelectedPatientCase = patientCase;
            DisplayPatientCase = patientCase;
            ExpandLeftUpMenu = false;

            if (ReviewStatus.SelectedPatientCase != null) {

                LumenContourCommand = Constants.LumenContourClear;

                DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current = 0;
                RayStartCompare(ReviewStatus.SelectedPatientCase.ImageFullPath);
                Thread.Sleep(500);

                GetImageInfo(RaySession.Compare);
                MoveToFrame(RaySession.Compare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current);

                if (ReviewStatus.SelectedPatientCase.LumenContour == null)
                {
                    IsLumenLoaded = false;

                    Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile());
                    threadMakeLumenProfile.Start();
                }
                else
                {
                    PreLumenContour = ReviewStatus.SelectedPatientCase.LumenContour;
                    imglumenProfileCompare = CommonUtil.MakeLumenProfileImage(PreLumenContour);

                    LumenContourCommand = Constants.LumenContourDraw;
                }

                IsIndicatorLockOn = false;
            }
        }

        private void GetPatientCase(bool isNullPatientCase, List<LumenContour> lumenContour = null)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;
            sqlParameters["procedure"] = "$001";//Pre-PCI

            PatientCases = _sqlManager.SelectPatientCaseList(sqlParameters);
            if(PatientCases != null && PatientCases.Count > 0)
            {
                if(isNullPatientCase)
                {
                    ReviewStatus.SelectedPatientCase = PatientCases[0];
                }
                else
                {
                    foreach (PatientCase patientCase in PatientCases)
                    {
                        if (patientCase.Id == ReviewStatus.SelectedPatientCase.Id)
                        {
                            ReviewStatus.SelectedPatientCase = patientCase;
                            break;
                        }
                    }
                }

                if (lumenContour != null)
                    ReviewStatus.SelectedPatientCase.LumenContour = lumenContour;
                DisplayPatientCase = ReviewStatus.SelectedPatientCase;
            }
        }

        private void updateNavigator(int curFrame, int totalFrame, bool isCompare)
        {
            if (isCompare)
            {
                if (FrameNumberCompare == curFrame)
                    return;
            }
            else
            {
                if (FrameNumber == curFrame)
                    return;
            }

            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= Constants.LongitudeCompareWidth;

            if (isCompare)
            {
                IndicatorCompareLongitude.X = curPosition - Constants.LongitudeIndicatorWidth / 2;
                IndicatorCompareLongitude.CenterX = curPosition;
            }
            else
            {
                IndicatorLongitude.X = curPosition - Constants.LongitudeIndicatorWidth / 2;
                IndicatorLongitude.CenterX = curPosition;
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

                if (indicator.IsLongitudeMove)
                {
                    indicatorDiffX = indicator.IsCompare ? PointCompareLongitudeX - longitudeCompareCoordinate.X : PointLongitudeX - longitudeCoordinate.X;
                    indicatorDiffX = indicatorDiffX - indicator.X;
                    indicator.IsLongitudeMove = false;
                }

                double x = indicator.IsCompare ? PointCompareLongitudeX - longitudeCompareCoordinate.X : PointLongitudeX - longitudeCoordinate.X;
                double indicatorX = x - indicatorDiffX;
                double indicatorCenterX = indicatorX + Constants.LongitudeIndicatorWidth / 2;

                if (indicatorCenterX >= 0 && indicatorCenterX < Constants.LongitudeCompareWidth)
                {
                    setCurrentFrame(indicator, indicatorCenterX);
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

                if (frameworkElement.Name.Equals("longitude"))
                {
                    longitudeCoordinate = point;
                }
                else if (frameworkElement.Name.Equals("longitudeCompare"))
                {
                    longitudeCompareCoordinate = point;
                }
            }
        }

        private void IndicatorLock()
        {
            indicatorLockOffset = FrameNumberCompare - FrameNumber;
        }

        private void setCurrentFrame(Indicator indicator, double navigatorPosition)
        {
            double curPosition = navigatorPosition / Constants.LongitudeCompareWidth;
            RaySession session = (indicator.IsCompare) ? RaySession.Compare : RaySession.Review;
            DeviceStatus.ReviewImageInfo frameInfo = DeviceStatus.ReviewImageInfos[(int)session];

            if (frameInfo != null)
            {             
                curPosition *= (frameInfo.Total - 1);
                curPosition = Math.Round(curPosition);

                if (IsIndicatorLockOn)
                {
                    RaySession syncSession = (indicator.IsCompare) ? RaySession.Review : RaySession.Compare;
                    int diff = indicatorLockOffset;
                    int syncPosition = (indicator.IsCompare) ? (int)curPosition - diff : (int)curPosition + diff;
                    DeviceStatus.ReviewImageInfo syncInfo = (indicator.IsCompare) ? DeviceStatus.ReviewImageInfos[(int)RaySession.Review] : DeviceStatus.ReviewImageInfos[(int)RaySession.Compare];

                    if (syncInfo == null) return;
                    if (syncPosition < 0 || syncPosition >= syncInfo.Total) return;

                    MoveToFrame(syncSession, syncPosition);
                    _log.Debug("diff : " + diff);

                    if (indicator.IsCompare)
                    {
                        FrameNumber = (int)syncPosition;
                    }
                    else
                    {
                        FrameNumberCompare = (int)syncPosition;
                    }
                }

                MoveToFrame(session, (int)curPosition);

                indicator.X = navigatorPosition - Constants.LongitudeIndicatorWidth / 2;

                if (indicator.IsCompare)
                {
                    FrameNumberCompare = (int)curPosition;
                }
                else {
                    FrameNumber = (int)curPosition;
                }
            }
        }

    }
}
