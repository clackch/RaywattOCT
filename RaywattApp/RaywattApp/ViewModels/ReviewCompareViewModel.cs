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

        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

        private Mat imglumenProfileCompare;

        private int indicatorLockOffset;

        private Mat imglumenProfileExtra;

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
        private List<LumenContour> _postLumenContour = new List<LumenContour>();

        [ObservableProperty]
        private LumenContour _currentPreLumenContour = new LumenContour();

        private string _lumenContourCommand;
        public string LumenContourCommand { get { return _lumenContourCommand; } set { _lumenContourCommand = value; OnPropertyChanged(nameof(LumenContourCommand)); } }

        [ObservableProperty]
        private BitmapSource _lumenProfileImageCompare;

        [ObservableProperty]
        private int _frameNumberCompare = -1;

        [ObservableProperty]
        private Zoom _zoom = new Zoom(Constants.CrossSectionCompareSize / Constants.OCTImageSize);

        [ObservableProperty]
        private BitmapSource _lumenProfileImageExtra;

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
            IndicatorLongitude.IsVisible = Visibility.Collapsed;

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
                ShowLumenProfile();

                if (ReviewStatus.SelectedPatientCase != null)
                {
                    SetCrossSectionBackground(RaySession.Compare, Constants.CompareBackgroundColor);

                    if(ReviewStatus.SelectedPatientCase.LumenContour == null)
                    {
                        Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile());
                        threadMakeLumenProfile.Start();
                    }
                    else
                    {
                        ShowLumenProfileCompare();
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
                    return CommonUtil.JsonToLumenContours(patientCaseAnnotations[0].LumenContour);
                }
            }

            return null;
        }

        private void ThreadMakeLumenProfile()
        {
            ReviewStatus.SelectedPatientCase.LumenContour = GetLumenContours(ReviewStatus.SelectedPatientCase.Id);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowLumenProfileCompare();
            });

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
            sqlParameters["section_proximal"] = PatientCase.SectionProximal;
            sqlParameters["section_distal"] = PatientCase.SectionDistal;

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
                RayStartCompare(ReviewStatus.SelectedPatientCase.ImageFullPath);
                DeviceStatus.IsOCTImagingCompareDone = false;
                Thread.Sleep(500);

                GetImageInfo(RaySession.Compare);
                MoveToFrame(RaySession.Compare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current);

                if (ReviewStatus.SelectedPatientCase.LumenContour == null)
                {
                    Thread threadMakeLumenProfile = new Thread(() => ThreadMakeLumenProfile());
                    threadMakeLumenProfile.Start();
                }
                else
                {
                    ShowLumenProfileCompare();

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
            PostLumenContour = PatientCase.LumenContour;
            int frameProximal = PatientCase.SectionProximal;
            int frameDistal = PatientCase.SectionDistal;
            Section.SetMsaMinExp(PatientCase.LumenContour, frameProximal, frameDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeCompareWidth, PatientCase.PullbackLength);

            //Test
            List<int> sidebranchs = new List<int>() { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420 };
            List<int> appositionFrames = new List<int>() { 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370 };
            imglumenProfile = CommonUtil.MakeLumenProfileImage(PostLumenContour, frameProximal, frameDistal, sidebranchs, true, appositionFrames);

            //Test
            List<int> colorFrames = CommonUtil.GetExpansionList(PostLumenContour, frameProximal, frameDistal, Section.RefArea, PatientCase.ExpansionThreshold);
            imglumenProfileExtra = CommonUtil.MakeLumenProfileImageExtra(ReviewStatus.NumberOfFrames, colorFrames, false);
            LumenProfileImageExtra = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileExtra);

            Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeCompareWidth, Constants.SectionIndicatorCenterWidth);
            Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeCompareWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;
            Section.Msa.IsVisible = Visibility.Visible;
            Section.MinExp.IsVisible = Visibility.Visible;
            Section.MsaValue.IsVisible = Visibility.Visible;
            Section.MinExpValue.IsVisible = Visibility.Visible;
        }

        private void ShowLumenProfileCompare()
        {
            PreLumenContour = ReviewStatus.SelectedPatientCase.LumenContour;
            int frameProximalCompare = ReviewStatus.SelectedPatientCase.SectionProximal;
            int frameDistalCompare = ReviewStatus.SelectedPatientCase.SectionDistal;
            SectionCompare.SetMlaMld(ReviewStatus.SelectedPatientCase.LumenContour, frameProximalCompare, frameDistalCompare, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, Constants.LongitudeCompareWidth, ReviewStatus.SelectedPatientCase.PullbackLength);

            //Test
            List<int> sidebranchs = new List<int>() { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420 };
            imglumenProfileCompare = CommonUtil.MakeLumenProfileImage(PreLumenContour, frameProximalCompare, frameDistalCompare, sidebranchs, false, null);
            LumenProfileImageCompare = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileCompare);

            //Test
            List<int> colorFrames = new List<int>() { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370 };
            imglumenProfileExtraCompare = CommonUtil.MakeLumenProfileImageExtra(DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, colorFrames, true);
            LumenProfileImageExtraCompare = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileExtraCompare);

            SectionCompare.Proximal.X = CommonUtil.GetPositionFromFrame(ReviewStatus.SelectedPatientCase.SectionProximal, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, Constants.LongitudeCompareWidth, Constants.SectionIndicatorCenterWidth);            
            SectionCompare.Distal.X = CommonUtil.GetPositionFromFrame(ReviewStatus.SelectedPatientCase.SectionDistal, DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total, Constants.LongitudeCompareWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

            IndicatorCompareLongitude.IsVisible = Visibility.Visible;
            SectionCompare.Proximal.IsVisible = Visibility.Visible;
            SectionCompare.Distal.IsVisible = Visibility.Visible;
            SectionCompare.Mla.IsVisible = Visibility.Visible;
            SectionCompare.Mld.IsVisible = Visibility.Visible;
            SectionCompare.MlaValue.IsVisible = Visibility.Visible;
            SectionCompare.MldValue.IsVisible = Visibility.Visible;
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
    }
}
