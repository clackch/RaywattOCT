using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattOCT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class Review3dViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Review3dViewModel));

        [ObservableProperty]
        private bool _isCutViewOn;

        [ObservableProperty]
        private bool _isIndicatorOn;

        [ObservableProperty]
        private bool _isPtoD;

        [ObservableProperty]
        private bool _isSideBranchView;

        [ObservableProperty]
        private bool _isTissueOn;

        [ObservableProperty]
        private bool _isLumenOn;

        [ObservableProperty]
        private bool _isStentOn;

        [ObservableProperty]
        private bool _isGuidewireOneOn;

        [ObservableProperty]
        private bool _isGuidewireTwoOn;

        private double degree;
        public double Degree
        {
            get { return degree; }
            set 
            { 
                degree = value; 
                OnPropertyChanged(nameof(Degree)); 
                RaySetProperty(Property.LongitudeDegree, degree);

                CameraDegree = degree + 90;
            }
        }

        [ObservableProperty]
        private double _cameraDegree;

        [ObservableProperty]
        private Indicator _indicatorCrossSection;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private bool _isPaused;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

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

        private double _lModeIndicatorX;
        public double LModeIndicatorX
        {
            get { return _lModeIndicatorX; }
            set { _lModeIndicatorX = value; OnPropertyChanged(nameof(LModeIndicatorX)); setCurrentFrame(value); }
        }

        public Review3dViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("Review3dViewModel");

            Constants.CurrentPage = Constants.Review3dPage;

            IndicatorCrossSection = new Indicator();
            IndicatorCrossSection.IsVisible = Visibility.Visible;
            IndicatorCrossSection.IsCrossSection = true;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Collapsed;

            IsCutViewOn = true;
            IsIndicatorOn = true;
            IsPtoD = true;
            IsSideBranchView = false;

            IsTissueOn = true;
            IsLumenOn = false;
            IsStentOn = false;
            IsGuidewireOneOn = true;
            IsGuidewireTwoOn = false;
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
                ReviewStatus.CurrentPage = Constants.Review3dPage;

                RaySetSession(RaySession.Review);
                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);
                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);

                Degree = PatientCase.IndicatorDegree;

                GetImageInfo(RaySession.Review);
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
                }
            }
        }

        private void updateNavigator(int curFrame, int totalFrame)
        {
            if (FrameNumber == curFrame)
                return;

            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= Constants.Longitude3dWidth;
            IndicatorLongitude.X = curPosition - Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.CenterX = curPosition;
        }

        private void RotateIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.IsCaptured)
            {
                Point crossSectionCenter;                
                crossSectionCenter = indicator.Coordinate;

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
                    indicator.IndicatorDiff = Math.Round((Math.Atan2(pointYDiff, pointXDiff) * 180 / Math.PI),1) - Degree;
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
                Degree = Math.Round((Math.Atan2(pointY, pointX) * 180 / Math.PI),1) - indicator.IndicatorDiff;
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
                    indicator.X = 0 - Constants.LongitudeIndicatorWidth / 2;
                    indicator.CenterX = 0;
                    setCurrentFrame(0);
                }
                else if (indicatorCenterX > Constants.Longitude3dWidth)
                {
                    indicator.X = Constants.Longitude3dWidth - Constants.LongitudeIndicatorWidth / 2;
                    indicator.CenterX = Constants.Longitude3dWidth;
                    setCurrentFrame(Constants.Longitude3dWidth);
                }
                else
                {
                    indicator.X = indicatorX;
                    indicator.CenterX = indicatorCenterX;
                    setCurrentFrame(indicatorCenterX);
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
                    IndicatorCrossSection.Coordinate.X = point.X + (frameworkElement.ActualWidth / 2);
                    IndicatorCrossSection.Coordinate.Y = point.Y + (frameworkElement.ActualHeight / 2);
                }
                else if (frameworkElement.Name.Equals("lMode"))
                {
                    IndicatorLongitude.Coordinate = point;
                }
            }
        }

        private void setCurrentFrame(double navigatorPosition)
        {
            double curPosition = navigatorPosition / Constants.Longitude3dWidth;

            if (longitudeFrameInfo != null)
            {
                curPosition *= (longitudeFrameInfo.totalFrame - 1);
                curPosition = Math.Round(curPosition);
                MoveToFrame(RaySession.Review, (int)curPosition);
            }
        }
    }
}
