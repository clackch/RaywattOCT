using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

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

        [ObservableProperty]
        private double _pointCompareLongitudeX;

        [ObservableProperty]
        private int displayFrameNumberCompare;

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
                    }
                }
                else
                {
                    GetPatientCase(false);
                }

                //Test -> Degree 설정해줘야, 화면에 들어온 뒤에 Longitude 동작
                RaySetProperty(Property.LongitudeDegree, PatientCase.IndicatorDegree);

                SetCrossSectionBackground(0, Constants.BackgroundColor);
                SetCrossSectionBackground(1, Constants.CompareBackgroundColor);
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
            sqlParameters["measurements"] = PatientCase.Measurements;
            sqlParameters["bookmarks"] = PatientCase.Bookmarks;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
                _log.Error("Update Error");
        }
        
        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (DrawCrossSectionImage())
            {
                {
                    if (!IndicatorLongitude.IsCaptured) updateNavigator(crossSectionFrameInfo[0].curFrame, crossSectionFrameInfo[0].totalFrame, false);

                    FrameNumber = crossSectionFrameInfo[0].curFrame;
                }
            }
            if (DrawCrossSectionForCompare())
            {
                {
                    if (!IndicatorCompareLongitude.IsCaptured) updateNavigator(crossSectionFrameInfo[1].curFrame, crossSectionFrameInfo[1].totalFrame, true);

                    DisplayFrameNumberCompare = crossSectionFrameInfo[1].curFrame + 1;
                }
            }

            //TODO - Lumen Profile로 변경 필요 (Indicator 때문에 Test로 L-mode 사용)
            if (DrawLongitudeImage())
            {
                // when generating longitude image is completed
                if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame)
                {
                    IndicatorLongitude.IsVisible = Visibility.Visible;
                    IndicatorCompareLongitude.IsVisible = Visibility.Visible;
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
                if (ReviewStatus.SelectedPatientCase.Id == patientCase.Id) return;
            }

            ReviewStatus.SelectedPatientCase = patientCase;
            DisplayPatientCase = patientCase;
            ExpandLeftUpMenu = false;

            if (ReviewStatus.SelectedPatientCase != null) {
                RayStartCompare(ReviewStatus.SelectedPatientCase.ImageFullPath);                
            }
        }

        private void GetPatientCase(bool isNullPatientCase)
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

                DisplayPatientCase = ReviewStatus.SelectedPatientCase;
            }
        }

        private void updateNavigator(int curFrame, int totalFrame, bool isCompare)
        {
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
                double x = indicator.IsCompare ? PointCompareLongitudeX - longitudeCompareCoordinate.X : PointLongitudeX - longitudeCoordinate.X;                

                if (x >= 0 && x < Constants.LongitudeCompareWidth)
                {
                    indicator.X = x - Constants.LongitudeIndicatorWidth / 2;
                    setCurrentFrame(x, indicator.IsCompare);
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

        private void setCurrentFrame(double navigatorPosition, bool isCompare)
        {
            double curPosition = navigatorPosition / Constants.LongitudeCompareWidth;

            if (isCompare)
            {

            }
            else
            {
                if (longitudeFrameInfo != null)
                {
                    curPosition *= (longitudeFrameInfo.totalFrame - 1);
                    curPosition = Math.Round(curPosition);
                    RayMoveToFrame((int)curPosition);
                }
            }
        }

    }
}
