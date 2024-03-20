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
using RaywattApp.Views.Dialog;
using static RaywattOCT.RayCoreWrapper;
using static RaywattOCT.Ray3DWrapper;
using System.Runtime.InteropServices;
using RaywattApp.Common.Util;
using System.Threading;

namespace RaywattApp.ViewModels
{
    public partial class Review3dViewModel : ReviewViewModelBase, IModelessPatient
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Review3dViewModel));

        private bool _isRendering = false;
        public bool IsRendering
        { 
            get { return _isRendering; }
            set 
            { 
                _isRendering = value; 
                OnPropertyChanged(nameof(IsRendering));

                DeviceStatus.IsOCTImagingDone = value;
                IndicatorLongitude.IsEnabled = value;
                IndicatorCrossSection.IsEnabled = value;
            }
        }

        private bool _isCutViewOn;
        public bool IsCutViewOn 
        { 
            get {  return _isCutViewOn; } 
            set 
            {
                _isCutViewOn = value;
                OnPropertyChanged(nameof(IsCutViewOn));

                ray3DStatus.CutViewOn = value;
                for(Ray3DObject obj = Ray3DObject.Tissue; obj < Ray3DObject.Count; obj++)
                {
                    changeCutVisibility(obj, value);
                }
            }
        }

        private bool _isIndicatorOn;
        public bool IsIndicatorOn
        { 
            get { return _isIndicatorOn; }
            set 
            { 
                _isIndicatorOn = value;
                OnPropertyChanged(nameof(IsIndicatorOn));

                ray3DStatus.IsIndicatorOn = value;
            }
        }

        private bool _isPtoD;
        public bool IsPtoD
        { 
            get { return _isPtoD; } 
            set 
            { 
                _isPtoD = value;
                OnPropertyChanged(nameof(IsPtoD));

                ray3DStatus.IsPtoD = value;
                ODSOCT_MoveCameraPosition(0, !value);
            }
        }

        [ObservableProperty]
        private bool _isSideBranchView;

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
                ODSOCT_RotateAngle((float)CameraDegree);
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

        private DispatcherTimer timerShowData = new DispatcherTimer();

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

        private IDialogWindow viewMenuWindow;
        private ICommand _cmdExpandLeftViewMenu;
        public ICommand CmdExpandLeftViewMenu
        { 
            get { return this._cmdExpandLeftViewMenu ?? (this._cmdExpandLeftViewMenu = new RelayCommand(ExpandLeftViewMenu)); }
        }

        private IDialogWindow patientMenuWindow;
        private ICommand _cmdExpandLeftPatientMenu;

        public ICommand CmdExpandLeftPatientMenu
        { 
            get { return this._cmdExpandLeftPatientMenu ?? (this._cmdExpandLeftPatientMenu = new RelayCommand(ExpandLeftPatientMenu)); }
        }

        private Thread threadInitialize;

        public Review3dViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("Review3dViewModel");

            Constants.CurrentPage = Constants.Review3dPage;

            IndicatorCrossSection = new Indicator();
            IndicatorCrossSection.IsVisible = Visibility.Visible;
            IndicatorCrossSection.IsCrossSection = true;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;
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

                // set default values without rendering
                _isCutViewOn = ray3DStatus.CutViewOn;
                _isIndicatorOn = ray3DStatus.IsIndicatorOn;
                _isPtoD = ray3DStatus.IsPtoD;
                _isSideBranchView = false;
            }

            IsRendering = false;

            threadInitialize = new Thread(() => threadFuncInitialize());
            threadInitialize.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
            if (viewMenuWindow != null) viewMenuWindow.Close();
            if (patientMenuWindow != null) patientMenuWindow.Close();

            if (threadInitialize.IsAlive)
                threadInitialize.Join();

            if (timerShowData.IsEnabled)
                timerShowData.Stop();

            Save();
            ODSOCT_HideAllWindows();
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
            sqlParameters["brightness"] = PatientCase.Brightness;
            sqlParameters["contrast"] = PatientCase.Contrast;
            sqlParameters["section_proximal"] = PatientCase.SectionProximal;
            sqlParameters["section_distal"] = PatientCase.SectionDistal;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
                _log.Error("Update Error");
        }

        protected override void UpdateCrossSectionImage()
        {
            if (DrawCrossSectionImage())
            {
                DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Review];
                if (!IndicatorLongitude.IsCaptured) updateNavigator(imageInfo.Current, imageInfo.Total);

                FrameNumber = imageInfo.Current;
            }
        }

        private void threadFuncInitialize()
        {
            int diameter = (int)RayGetProperty(Property.VolumeWidth);
            int depth = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;
            IntPtr buffer = Marshal.AllocHGlobal(diameter * diameter * depth);

            CommonUtil.ContoursToMemory(PatientCase.LumenContours, 
                new OpenCvSharp.Size(Constants.OCTImageSize, Constants.OCTImageSize), 
                buffer, 
                new OpenCvSharp.Size(diameter, diameter));

            ODSOCT_InputData(Ray3DObject.Tissue, RayGetVolumeData(buffer), diameter, diameter, depth, 1, 1, 12.5);

            ODSOCT_InputSurfaceParameter(Ray3DObject.Lumen, 10, 50, ".\\data\\lumen_tex.jpg");
            ODSOCT_InputData(Ray3DObject.Lumen, buffer, diameter, diameter, depth, 1, 1, 12.5);
            ODSOCT_ProcessingDatas();
            Marshal.FreeHGlobal(buffer);

            timerShowData.Interval = TimeSpan.FromMilliseconds(MinWaitingDelay);
            timerShowData.Tick += new EventHandler(timerFuncShowData);
            timerShowData.Start();
        }

        private void timerFuncShowData(object sender, EventArgs e)
        { 
            if (timerShowData.IsEnabled)
                timerShowData.Stop();

            ODSOCT_RotateAngle((float)CameraDegree);
            ODSOCT_MoveToFrame(DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
            ODSOCT_ShowAllWindows();

            for (Ray3DObject obj = Ray3DObject.Tissue; obj < Ray3DObject.Count; obj++)
            {
                ray3DStatus.ShowObject(obj, ray3DStatus.ObjectVisibility[(int)obj]);
            }

            IsRendering = true;
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

        private void changeCutVisibility(Ray3DObject obj, bool isCutView)
        {
            Ray3DObjectMode mode = (isCutView) ? Ray3DObjectMode.Cut : Ray3DObjectMode.Full;
            if (ray3DStatus.IsObjectVisible(obj))
            {
                ray3DStatus.ShowObject(obj, mode);
            }
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

        private void ExpandLeftViewMenu()
        {
            if (!IsRendering) return;

            viewMenuWindow = _dialogService.OpenChildWindow(new Review3dViewMenuControl(), this, null, Constants.SideBarExpandSize, Constants.LeftSideBarExpand3dSize, 0, Constants.ViewMenu3dY);
        }
        private void ExpandLeftPatientMenu()
        {
            if (!IsRendering) return;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;

            patientMenuWindow = _dialogService.OpenChildWindow(new Review3dPatientMenuControl(), this, parameter, Constants.SideBarExpandSize, Constants.LeftSideBarExpand3dSize, 0, Constants.PatientMenu3dY);
        }

        public void SetResult(object result)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)result;

            if (data.ContainsKey("reviewStatus"))
            {
                ReviewStatus = (ReviewStatus)data["reviewStatus"];
            }
            if (data.ContainsKey("patient"))
            {
                Patient = (Patient)data["patient"];
            }
            if (data.ContainsKey("patientCase"))
            {
                PatientCase = (PatientCase)data["patientCase"];
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
                ODSOCT_MoveToFrame((int)curPosition);
            }
        }
    }
}
