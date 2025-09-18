using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using RaywattOCTFFR.Views.Dialog;
using static RaywattOCT.RayCoreWrapper;
using static RaywattOCT.Ray3DWrapper;
using System.Runtime.InteropServices;
using RaywattOCTFFR.Common.Util;
using System.Threading;
using SharpDX;

namespace RaywattOCTFFR.ViewModels
{

    public partial class Review3dViewModel : ReviewViewModelBase, IModelessPatient
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Review3dViewModel));

        private bool _isRendering;
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
                changeCutVisibility(value);
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
                change3DIndicatorVisibility(value);
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
                int ray3DResult = ODSOCT_MoveCameraPosition(0, !value);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_MoveCameraPosition Error");
                }
                ray3DResult = ODSOCT_Render();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_Render Error");
                }
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
                RayError result = (RayError)RaySetProperty(Property.LongitudeDegree, degree);
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }

                CameraDegree = degree + 90;
                int ray3DResult = ODSOCT_RotateAngle((float)CameraDegree);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_RotateAngle Error");
                }
                ray3DResult = ODSOCT_Render();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_Render Error");
                }
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

        [ObservableProperty]
        private Zoom _zoom = new Zoom(Constants.CrossSection3dSize);

        private DispatcherTimer timerShowData = new DispatcherTimer();

        private bool isFirstRendering = true;

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
        private ICommand _zoomIn3DCommand;
        public ICommand ZoomIn3DCommand
        {
            get { return this._zoomIn3DCommand ?? (this._zoomIn3DCommand = new RelayCommand(ZoomIn3D)); }
        }

        private ICommand _zoomOut3DCommand;
        public ICommand ZoomOut3DCommand
        {
            get { return this._zoomOut3DCommand ?? (this._zoomOut3DCommand = new RelayCommand(ZoomOut3D)); }
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

            int ray3DResult = ODSOCT_SetRenderMode(true);
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_SetRenderMode Error");
            }

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];
                ReviewStatus.CurrentPage = Constants.Review3dPage;

                Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);

                RayError result = (RayError)RaySetSession(RaySession.Review);
                if (result != RayError.OK)
                {
                    _log.Error("RaySetSession Error");
                }
                result = (RayError)RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }
                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);

                Degree = PatientCase.IndicatorDegree;

                GetImageInfo(RaySession.Review);
                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);

                // set default values without rendering
                _isCutViewOn = ray3DStatus.CutViewOn;
                _isIndicatorOn = ray3DStatus.IsIndicatorOn;
                change3DIndicatorVisibility(IsIndicatorOn);
                _isPtoD = ray3DStatus.IsPtoD;
                _isSideBranchView = false;
                isFirstRendering = ray3DStatus.IsFirstRendering;
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
            TurnOffAll3DActors();
            int ray3DResult = ODSOCT_HideAllWindows();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_HideAllWindows Error");
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
            sqlParameters["brightness"] = PatientCase.Brightness;
            sqlParameters["contrast"] = PatientCase.Contrast;
            sqlParameters["field_of_view"] = PatientCase.FieldOfView;
            sqlParameters["section_proximal"] = PatientCase.SectionProximal;
            sqlParameters["section_distal"] = PatientCase.SectionDistal;
            sqlParameters["guidewire_radius"] = PatientCase.GuidewireRadius;
            sqlParameters["z_offset"] = PatientCase.ZOffset;

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

        private double zValueForPullbackType()
        {
            double pullbackLength = int.Parse(PatientCase.PullbackLength)/10.0;
            double frameInterval = pullbackLength / PatientCase.NumOfFrames;
            double zValue = (frameInterval / (Constants.ImageResolution * Constants.XYScale3D)); // 1024에서 500으로 xy 데이터를 축소(속도 이슈)했으므로, 길이 보정 : Constants.XYScale3D
            _log.Debug("3D zValue = " + zValue.ToString());
            return zValue;
        }

        private void threadFuncInitialize()
        {
            int ray3DResult;

            if (ReviewStatus.IsLumenEdited)
            {
                int diameter = (int)RayGetProperty(Property.VolumeWidth);
                int depth = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;
                double zVal = zValueForPullbackType();
                IntPtr buffer = Marshal.AllocHGlobal(diameter * diameter * depth);
                CommonUtil.ContoursToMemory(PatientCase.LumenContours,
                    new OpenCvSharp.Size(Constants.OCTImageSize, Constants.OCTImageSize),
                    buffer,
                    new OpenCvSharp.Size(diameter, diameter));
                
                if (CommonUtil.IsPostCase(PatientCase.Procedure))
                {
                    ray3DResult = ODSOCT_InputData(Ray3DObject.Tissue, RayGetVolumeData(System.IntPtr.Zero), diameter, diameter, depth, 1, 1, zVal);
                    if (ray3DResult == 0)
                    {
                        _log.Error("ODSOCT_InputData Error");
                    }

                    if (CommonUtil.IsVTIFileSave)
                    {
                        _log.Debug("VTIFileSave");
                        ray3DResult = ODSOCT_Export3DVTIFile(RayGetVolumeData(System.IntPtr.Zero), "test");
                        if (ray3DResult == 0)
                        {
                            _log.Error("ODSOCT_Export3DVTIFile Error");
                        }
                    }
                }
                else
                {
                    ray3DResult = ODSOCT_InputData(Ray3DObject.Tissue, RayGetVolumeData(buffer), diameter, diameter, depth, 1, 1, zVal);
                    if (ray3DResult == 0)
                    {
                        _log.Error("ODSOCT_InputData Error");
                    }
                    if (CommonUtil.IsVTIFileSave)
                    {
                        _log.Debug("VTIFileSave");
                        ray3DResult = ODSOCT_Export3DVTIFile(RayGetVolumeData(buffer), "test");
                        if (ray3DResult == 0)
                        {
                            _log.Error("ODSOCT_Export3DVTIFile Error");
                        }
                    }
                }

                ray3DResult = ODSOCT_InputSurfaceParameter(Ray3DObject.Lumen, 10, 50, ".\\data\\lumen_tex.jpg");
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_InputSurfaceParameter Error");
                }
                ray3DResult = ODSOCT_InputData(Ray3DObject.Lumen, buffer, diameter, diameter, depth, 1, 1, zVal);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_InputData Error");
                }

                buffer = Marshal.AllocHGlobal(diameter * diameter * depth);
                CommonUtil.GuideWireToMemory(PatientCase.LumenGuidewires,
                new OpenCvSharp.Size(Constants.OCTImageSize, Constants.OCTImageSize),
                buffer,
                new OpenCvSharp.Size(diameter, diameter), PatientCase.GuidewireRadius);
                ray3DResult = ODSOCT_InputSurfaceParameter(Ray3DObject.GuideWire, 10, 15, ".\\data\\guidewire_tex.jpg");
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_InputSurfaceParameter Error");
                }
                ray3DResult = ODSOCT_InputData(Ray3DObject.GuideWire, buffer, diameter, diameter, depth, 1, 1, zVal);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_InputData Error");
                }

                buffer = Marshal.AllocHGlobal(diameter * diameter * depth);
                CommonUtil.StentsToMemory(PatientCase.LumenStents,
                    new OpenCvSharp.Size(Constants.OCTImageSize, Constants.OCTImageSize),
                    buffer,
                    new OpenCvSharp.Size(diameter, diameter));
                ray3DResult = ODSOCT_InputSurfaceParameter(Ray3DObject.Stent, 10, 15, ".\\data\\stent_tex.jpg");
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_InputSurfaceParameter Error");
                }
                ray3DResult = ODSOCT_InputData(Ray3DObject.Stent, buffer, diameter, diameter, depth, 1, 1, zVal);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_InputData Error");
                }

                ray3DResult = ODSOCT_ProcessingDatas();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_ProcessingDatas Error");
                }
                if (isFirstRendering)
                {
                    bugTestFunc();
                    ray3DStatus.IsFirstRendering = false;
                }

                Marshal.FreeHGlobal(buffer);
                ReviewStatus.IsLumenEdited = false;
            }

            ray3DResult = ODSOCT_UpdateColorTable((int)RayGetProperty(Property.Colormap));
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_UpdateColorTable Error");
            }

            timerShowData.Interval = TimeSpan.FromMilliseconds(MinWaitingDelay);
            timerShowData.Tick += new EventHandler(timerFuncShowData);
            timerShowData.Start();
        }

        private void timerFuncShowData(object sender, EventArgs e)
        { 
            if (timerShowData.IsEnabled)
                timerShowData.Stop();

            int ray3DResult = ODSOCT_RotateAngle((float)CameraDegree);
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_RotateAngle Error");
            }
            ray3DResult = ODSOCT_MoveToFrame(DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_MoveToFrame Error");
            }
            for (Ray3DObject obj = Ray3DObject.Tissue; obj < Ray3DObject.Count; obj++)
            {
                ray3DStatus.ShowObject(obj, ray3DStatus.ObjectVisibility[(int)obj]);
            }
            ray3DResult = ODSOCT_ShowAllWindows();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_ShowAllWindows Error");
            }

            IsCutViewOn = ray3DStatus.CutViewOn;
            ray3DResult = ODSOCT_Render();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_Render Error");
            }

            if (!IsRendering)
            {
                IsRendering = true;
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

        private static void changeCutVisibility(bool isCutView)
        {
            int ray3DResult = ODSOCT_CutViewOn(isCutView);
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_CutViewOn Error");
            }
            ray3DResult = ODSOCT_Render();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_Render Error");
            }
        }

        private static void change3DIndicatorVisibility(bool show)
        {
            Ray3DStatus.ShowIndicator(show);
            int ray3DResult = ODSOCT_Render();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_Render Error");
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

                double indicatorX = indicator.PointLongitudeX - indicator.Coordinate.X - Constants.LongitudeIndicatorWidth / 2;
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

        private void TouchMoveIndicator(object param)
        {
            MouseEventArgs e = (MouseEventArgs)param;
            var position = e.GetPosition((IInputElement)e.Source);

            IndicatorLongitude.X = position.X - Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.CenterX = IndicatorLongitude.X + Constants.LongitudeIndicatorWidth / 2;
            setCurrentFrame(IndicatorLongitude.CenterX);
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

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patientCase"] = PatientCase;

            viewMenuWindow = _dialogService.OpenChildWindow(new Review3dViewMenuControl(), this, parameter, Constants.SideBarExpandSize, Constants.LeftSideBarExpand3dSize, 0, Constants.ViewMenu3dY);
        }
        private void ExpandLeftPatientMenu()
        {
            if (!IsRendering) return;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;

            patientMenuWindow = _dialogService.OpenChildWindow(new Review3dPatientMenuControl(), this, parameter, Constants.SideBarExpandSize, Constants.LeftSideBarExpand3dSize, 0, Constants.PatientMenu3dY);
        }

        private void ZoomIn3D()
        {
            if (!IsRendering) return;
            _log.Debug("zoomFactor = " + ray3DStatus.ZoomFactor);
            if (ray3DStatus.ZoomFactor < Constants.Zoom3DScaleMax)
            {
                int ray3DResult = ODSOCT_CutViewZoom(1);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_CutViewZoom Error");
                }
                ray3DStatus.ZoomFactor += 1;
                ray3DResult = ODSOCT_Render();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_Render Error");
                }
            }
        }

        private void ZoomOut3D() {
            if (!IsRendering) return;
            _log.Debug("zoomFactor = " + ray3DStatus.ZoomFactor);
            if (ray3DStatus.ZoomFactor > - Constants.Zoom3DScaleMax)
            {
                int ray3DResult = ODSOCT_CutViewZoom(-1);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_CutViewZoom Error");
                }
                ray3DStatus.ZoomFactor -= 1;
                ray3DResult = ODSOCT_Render();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_Render Error");
                }
            }
        }


        public void SetResult(object result)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)result;

            if (data.TryGetValue("reviewStatus", out var reviewStatusObj) && reviewStatusObj is ReviewStatus reviewStatus)
            {
                ReviewStatus = reviewStatus;
            }

            if (data.TryGetValue("patient", out var patientObj) && patientObj is Patient patient)
            {
                Patient = patient;
            }

            if (data.TryGetValue("patientCase", out var patientCaseObj) && patientCaseObj is PatientCase patientCase)
            {
                PatientCase = patientCase;
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
                int ray3DResult = ODSOCT_MoveToFrame((int)curPosition);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_MoveToFrame Error");
                }
                ray3DResult = ODSOCT_Render();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_Render Error");
                }
            }
        }

        private static void TurnOffAll3DActors()
        {
            change3DIndicatorVisibility(false);
            for (Ray3DObject obj = Ray3DObject.Tissue; obj < Ray3DObject.Count; obj++)
            {
                if (ray3DStatus.ObjectVisibility[(int)obj] != Ray3DObjectMode.Hide)
                    ray3DStatus.ShowObject(obj, Ray3DObjectMode.Hide, true);
            }
            int ray3DResult = ODSOCT_Render();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_Render Error");
            }
        }

        private void bugTestFunc()
        {
            int diameter = (int)RayGetProperty(Property.VolumeWidth);
            int depth = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;
            double zVal = zValueForPullbackType();
            IntPtr buffer = Marshal.AllocHGlobal(diameter * diameter * depth);

            CommonUtil.ContoursToMemory(PatientCase.LumenContours,
                new OpenCvSharp.Size(Constants.OCTImageSize, Constants.OCTImageSize),
                buffer,
                new OpenCvSharp.Size(diameter, diameter));

            int ray3DResult;

            if (CommonUtil.IsPostCase(PatientCase.Procedure))
            {
                ray3DResult = ODSOCT_InputData(Ray3DObject.Tissue, RayGetVolumeData(System.IntPtr.Zero), diameter, diameter, depth, 1, 1, zVal);
            }
            else
            {
                ray3DResult = ODSOCT_InputData(Ray3DObject.Tissue, RayGetVolumeData(buffer), diameter, diameter, depth, 1, 1, zVal);
            }

            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_InputData Error");
            }
        }
    }
}
