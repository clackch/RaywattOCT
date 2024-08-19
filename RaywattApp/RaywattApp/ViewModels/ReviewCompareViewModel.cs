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
using static RaywattOCT.RayCoreWrapper;
using Point = System.Windows.Point;

namespace RaywattApp.ViewModels
{
    public partial class ReviewCompareViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewCompareViewModel));

        private Mat imglumenProfileCompare;

        private int indicatorLockOffset;

        private Mat imglumenProfileExtraCompare;

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
        private Section _section;

        [ObservableProperty]
        private Section _sectionCompare;

        [ObservableProperty]
        private int displayFrameNumberCompare;

        [ObservableProperty]
        private List<LumenContour> _preLumenContour = new List<LumenContour>();

        [ObservableProperty]
        private List<LumenSidebranch> _preLumenSidebranches = new List<LumenSidebranch>();

        [ObservableProperty]
        private List<LumenStent> _preLumenStents = new List<LumenStent>();

        [ObservableProperty]
        private List<LumenGuidewire> _preLumenGuidewires = new List<LumenGuidewire>();

        [ObservableProperty]
        private List<LumenContour> _postLumenContour = new List<LumenContour>();

        [ObservableProperty]
        private List<LumenSidebranch> _postLumenSidebranches = new List<LumenSidebranch>();

        [ObservableProperty]
        private List<LumenStent> _postLumenStents = new List<LumenStent>();

        [ObservableProperty]
        private List<LumenGuidewire> _postLumenGuidewires = new List<LumenGuidewire>();

        [ObservableProperty]
        private LumenContour _currentPreLumenContour = new LumenContour();

        private string _lumenContourCommand;
        public string LumenContourCommand { get { return _lumenContourCommand; } set { _lumenContourCommand = value; OnPropertyChanged(nameof(LumenContourCommand)); } }

        [ObservableProperty]
        private BitmapSource _lumenProfileImageCompare;

        [ObservableProperty]
        private int _frameNumberCompare = -1;

        [ObservableProperty]
        private Zoom _zoom = new Zoom(Constants.CrossSectionCompareSize);

        [ObservableProperty]
        private BitmapSource _lumenProfileImageExtraCompare;

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
            IndicatorLongitude.IsVisible = Visibility.Visible;

            IndicatorCompareLongitude = new Indicator();
            IndicatorCompareLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorCompareLongitude.IsVisible = Visibility.Collapsed;
            IndicatorCompareLongitude.IsCompare = true;

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;
            SectionCompare = new Section();

            ExpandLeftUpMenu = false;
            ExpandLeftDownMenu = false;

            IsIndicatorLockOn = false;

            DeviceStatus.IsOCTImagingCompareDone = false;

            Zoom = new Zoom(Constants.CrossSectionCompareSize);
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

                Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);

                if (ReviewStatus.SelectedPatientCase == null)
                {
                    GetPatientCase(true);

                    if (ReviewStatus.SelectedPatientCase != null)
                    {
                        RayStartCompare(ReviewStatus.SelectedPatientCase.ImageFullPath, ReviewStatus.SelectedPatientCase.ImageResolution);
                        Thread.Sleep(500);
                    }
                    else
                    {
                        DeviceStatus.IsOCTImagingCompareDone = true;
                    }
                }
                else
                {
                    GetPatientCase(false, ReviewStatus.SelectedPatientCase.LumenContours, ReviewStatus.SelectedPatientCase.LumenSidebranches, ReviewStatus.SelectedPatientCase.LumenStents, ReviewStatus.SelectedPatientCase.LumenGuidewires);
                    DeviceStatus.IsOCTImagingCompareDone = true;
                }

                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);
                ShowLumenProfile();

                if (ReviewStatus.SelectedPatientCase != null)
                {
                    SetCrossSectionBackground(RaySession.Compare, Constants.CompareBackgroundColor);

                    if(ReviewStatus.SelectedPatientCase.LumenContours == null)
                    {
                        GetAnnotation();
                    }
                    else
                    {
                        ShowLumenProfileCompare();
                    }
                }
            }

            GetImageInfo(RaySession.Review);
            MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
            GetImageInfo(RaySession.Compare);
            MoveToFrame(RaySession.Compare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current);
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
        }

        private void GetAnnotation()
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = ReviewStatus.SelectedPatientCase.Id;

            IList<PatientCaseAnnotation> patientCaseAnnotations = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);

            if (patientCaseAnnotations != null && patientCaseAnnotations.Count == 1)
            {
                ReviewStatus.SelectedPatientCase.LumenSidebranches = JsonConvert.DeserializeObject<List<LumenSidebranch>>(patientCaseAnnotations[0].LumenSidebranch);
                ReviewStatus.SelectedPatientCase.LumenStents = JsonConvert.DeserializeObject<List<LumenStent>>(patientCaseAnnotations[0].LumenStent);
                ReviewStatus.SelectedPatientCase.LumenGuidewires = JsonConvert.DeserializeObject<List<LumenGuidewire>>(patientCaseAnnotations[0].LumenGuidewire);
                Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile(patientCaseAnnotations[0].LumenContour));
                threadMakeLumenProfile.Start();
            }
        }

        private void ThreadMakeLumenProfile(string lumenContour)
        {
            ReviewStatus.SelectedPatientCase.LumenContours = CommonUtil.JsonToLumenContours(lumenContour);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowLumenProfileCompare();
            });

            LumenContourCommand = Constants.LumenContourDraw;
        }

        protected override void UpdateCrossSectionImage()
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
                HideLumenProfileCompare();

                DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current = 0;
                RayStartCompare(ReviewStatus.SelectedPatientCase.ImageFullPath, ReviewStatus.SelectedPatientCase.ImageResolution);
                DeviceStatus.IsOCTImagingCompareDone = false;
                Thread.Sleep(500);

                GetImageInfo(RaySession.Compare);
                MoveToFrame(RaySession.Compare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current);

                if (ReviewStatus.SelectedPatientCase.LumenContours == null)
                {
                    GetAnnotation();
                }
                else
                {
                    ShowLumenProfileCompare();

                    LumenContourCommand = Constants.LumenContourDraw;
                }

                IsIndicatorLockOn = false;
            }
        }

        private void GetPatientCase(bool isNullPatientCase, List<LumenContour> lumenContours = null
            , List<LumenSidebranch> lumenSidebranches = null, List<LumenStent> lumenStents = null, List<LumenGuidewire> lumenGuidewires = null)
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

                if (lumenContours != null)
                    ReviewStatus.SelectedPatientCase.LumenContours = lumenContours;
                if (lumenSidebranches != null)
                    ReviewStatus.SelectedPatientCase.LumenSidebranches = lumenSidebranches;
                if(lumenStents != null)
                    ReviewStatus.SelectedPatientCase.LumenStents = lumenStents;
                if(lumenGuidewires != null)
                    ReviewStatus.SelectedPatientCase.LumenGuidewires = lumenGuidewires;
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
                    indicator.IndicatorDiff = indicator.PointLongitudeX - indicator.Coordinate.X - indicator.X;
                    indicator.IsLongitudeMove = false;
                }

                double indicatorX = indicator.PointLongitudeX - indicator.Coordinate.X - indicator.IndicatorDiff;
                double indicatorCenterX = indicatorX + Constants.LongitudeIndicatorWidth / 2;

                if (indicatorCenterX < 0)
                {
                    setCurrentFrame(indicator, 0);
                }
                else if (indicatorCenterX > Constants.LongitudeCompareWidth)
                {
                    setCurrentFrame(indicator, Constants.LongitudeCompareWidth);
                }
                else
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
                    IndicatorLongitude.Coordinate = point;
                }
                else if (frameworkElement.Name.Equals("longitudeCompare"))
                {
                    IndicatorCompareLongitude.Coordinate = point;
                }
            }
        }

        private void IndicatorLock()
        {
            if (ReviewStatus.SelectedPatientCase == null)
            {
                IsIndicatorLockOn = false;
                return;
            }                

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

                    if (syncInfo == null) 
                        return;
                    if (syncPosition < 0 || syncPosition >= syncInfo.Total)
                    {
                        if(syncPosition < 0)
                            syncPosition = 0;
                        else
                            syncPosition = syncInfo.Total - 1;

                        curPosition = (indicator.IsCompare) ? (int)syncPosition + diff : (int)syncPosition - diff;
                        navigatorPosition = curPosition * Constants.LongitudeCompareWidth / (frameInfo.Total - 1);
                    }

                    MoveToFrame(syncSession, syncPosition);
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

        private void ShowLumenProfile()
        {
            PostLumenContour = PatientCase.LumenContours;
            PostLumenSidebranches = PatientCase.LumenSidebranches;
            PostLumenStents = PatientCase.LumenStents;
            PostLumenGuidewires = PatientCase.LumenGuidewires;
            int frameProximal = PatientCase.SectionProximal;
            int frameDistal = PatientCase.SectionDistal;
            int stentProximal = 0, stentDistal = 0;
            CommonUtil.GetStentProximalDistal(PatientCase.LumenStents, out stentProximal, out stentDistal);
            if (Section.SetMsaMinExp(PatientCase.LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeCompareWidth, PatientCase.PullbackLength, Constants.ImageResolution))
            {
                Section.VislbleMsaMinExp(true);
                Section.Proximal.IsVisible = Visibility.Visible;
                Section.Distal.IsVisible = Visibility.Visible;
            }
            else
            {
                Section.VislbleMsaMinExp(false);
                Section.Proximal.IsVisible = Visibility.Collapsed;
                Section.Distal.IsVisible = Visibility.Collapsed;
            }

            imglumenProfile = CommonUtil.MakeLumenProfileImage(PostLumenContour, PostLumenSidebranches, PostLumenStents, PatientCase.AppositionThreshold, frameProximal, frameDistal, true);
            DrawLumenProfileImage();

            List<int> colorFrames = CommonUtil.GetExpansionList(PostLumenContour, frameProximal, frameDistal, stentProximal, stentDistal, Section.RefArea, PatientCase.ExpansionThreshold);
            imglumenProfileExtra = CommonUtil.MakeLumenProfileImageExtra(ReviewStatus.NumberOfFrames, colorFrames, false);
            DrawLumenProfileImageExtra();

            Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeCompareWidth, Constants.SectionIndicatorCenterWidth);
            Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeCompareWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);
        }

        private void ShowLumenProfileCompare()
        {
            PreLumenContour = ReviewStatus.SelectedPatientCase.LumenContours;
            PreLumenSidebranches = ReviewStatus.SelectedPatientCase.LumenSidebranches;
            PreLumenStents = ReviewStatus.SelectedPatientCase.LumenStents;
            PreLumenGuidewires = ReviewStatus.SelectedPatientCase.LumenGuidewires;
            int frameProximalCompare = ReviewStatus.SelectedPatientCase.SectionProximal;
            int frameDistalCompare = ReviewStatus.SelectedPatientCase.SectionDistal;
            if(SectionCompare.SetMlaMld(ReviewStatus.SelectedPatientCase.LumenContours, frameProximalCompare, frameDistalCompare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, Constants.LongitudeCompareWidth, ReviewStatus.SelectedPatientCase.PullbackLength, Constants.ImageResolution))
            {
                SectionCompare.VisibleMlaMld(true);
                SectionCompare.Proximal.IsVisible = Visibility.Visible;
                SectionCompare.Distal.IsVisible = Visibility.Visible;
            }
            else
            {
                SectionCompare.VisibleMlaMld(false);
                SectionCompare.Proximal.IsVisible = Visibility.Collapsed;
                SectionCompare.Distal.IsVisible = Visibility.Collapsed;
            }

            imglumenProfileCompare = CommonUtil.MakeLumenProfileImage(PreLumenContour, PreLumenSidebranches, PreLumenStents, ReviewStatus.SelectedPatientCase.AppositionThreshold, frameProximalCompare, frameDistalCompare, false);
            DrawLumenProfileImageCompare();

            List<int> colorFrames = CommonUtil.GetCalciumList(ReviewStatus.SelectedPatientCase.LumenContours, ReviewStatus.SelectedPatientCase.CalciumThreshold);
            imglumenProfileExtraCompare = CommonUtil.MakeLumenProfileImageExtra(DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, colorFrames, true);
            DrawLumenProfileImageExtraCompare();

            SectionCompare.Proximal.X = CommonUtil.GetPositionFromFrame(ReviewStatus.SelectedPatientCase.SectionProximal, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, Constants.LongitudeCompareWidth, Constants.SectionIndicatorCenterWidth);            
            SectionCompare.Distal.X = CommonUtil.GetPositionFromFrame(ReviewStatus.SelectedPatientCase.SectionDistal, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, Constants.LongitudeCompareWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

            IndicatorCompareLongitude.IsVisible = Visibility.Visible;
        }

        private void HideLumenProfileCompare()
        {
            IndicatorCompareLongitude.IsVisible = Visibility.Collapsed;
            SectionCompare.Proximal.IsVisible = Visibility.Collapsed;
            SectionCompare.Distal.IsVisible = Visibility.Collapsed;
            SectionCompare.Mla.IsVisible = Visibility.Visible;
            SectionCompare.Mld.IsVisible = Visibility.Visible;
            SectionCompare.MlaValue.IsVisible = Visibility.Collapsed;
            SectionCompare.MldValue.IsVisible = Visibility.Collapsed;
        }

        private bool DrawLumenProfileImageCompare()
        {
            if (imglumenProfileCompare == null) return false;

            LumenProfileImageCompare = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileCompare);
            return true;
        }
        private bool DrawLumenProfileImageExtraCompare()
        {
            if (imglumenProfileExtraCompare == null) return false;

            LumenProfileImageExtraCompare = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileExtraCompare);
            return true;
        }
    }
}
