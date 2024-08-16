using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using static RaywattOCT.RayCoreWrapper;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using System.Windows;
using RaywattApp.Common.Annotation.Models;
using System.Collections.ObjectModel;
using RaywattApp.Services;
using System.Linq;
using Newtonsoft.Json;

namespace RaywattApp.ViewModels
{
    public partial class ReviewFfrSettingViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewFfrSettingViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private string _ffrStep = Constants.FfrStep1;

        [ObservableProperty]
        private IList<Code> _vesselList;

        [ObservableProperty]
        private Code _currentVessel;

        [ObservableProperty]
        private Section _section;

        [ObservableProperty]
        private LumenContour _currentLumenContour = new LumenContour();

        [ObservableProperty]
        private LumenStent _currentLumenStent = new LumenStent();

        [ObservableProperty]
        private List<Measurement> _plaqueAreaList;

        [ObservableProperty]
        private string _measurementCommand;

        [ObservableProperty]
        private bool _isEditOn;

        [ObservableProperty]
        private bool _isDrawOn;

        [ObservableProperty]
        private bool _isVesselTypeValid;

        [ObservableProperty]
        private FfrFeature _ffrFeature;

        [ObservableProperty]
        private bool _isNextEnabled;

        [ObservableProperty]
        private bool _isMlaEnabled;

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

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next)); }
        }

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand
        {
            get { return this._confirmCommand ?? (this._confirmCommand = new RelayCommand(Confirm)); }
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get { return this._deleteCommand ?? (this._deleteCommand = new RelayCommand(Delete)); }
        }

        private ICommand _selectionChangedCommand;
        public ICommand SelectionChangedCommand
        {
            get { return this._selectionChangedCommand ?? (this._selectionChangedCommand = new RelayCommand<Code>(SelectionChanged)); }
        }

        private ICommand _cmdMoveIndicator;
        public ICommand CmdMoveIndicator
        {
            get { return this._cmdMoveIndicator ?? (this._cmdMoveIndicator = new RelayCommand<object>(MoveIndicator)); }
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

        public ReviewFfrSettingViewModel(SqlManager sqlManager)
        {
            _log.Debug("ReviewFfrSettingViewModel");

            Constants.CurrentPage = Constants.ReviewFfrSettingPage;

            Section = new Section();
            Section.Proximal.IsEnabled = true;
            Section.Distal.IsEnabled = true;

            _sqlManager = sqlManager;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "VESS";
            VesselList = _sqlManager.SelectCode(sqlParameters);
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
                ReviewStatus.CurrentPage = Constants.ReviewFfrSettingPage;
                if (PatientCase.FfrFeature == null)
                    FfrFeature = new FfrFeature();
                else
                    FfrFeature = PatientCase.FfrFeature;
                FfrFeature.PlaqueArea = 0;
                FfrFeature.PercentAreaStenosis = 0;
                FfrFeature.IsPlaqueAreaValid = false;

                ReviewStatus.ZoomFfr.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);

                if (CodeDefinition.Codes["VESS"].ContainsKey(PatientCase.Vessel))
                    CurrentVessel = VesselList.FirstOrDefault(x => x.Key == PatientCase.Vessel);

                CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault);

                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);
                SetCrossSectionBackground(RaySession.Review, Constants.CardBackgroundColor);
                                
                ShowLumenProfile();
                DrawCrossSection(GetMlaFrameNumber());

                CheckVesselType(CurrentVessel.Key);

                //PlaqueAreaList
                if (FfrFeature.PlaqueAreaList == null)
                {
                    Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
                    sqlParameters["id"] = PatientCase.Id;
                    IList<StringModel> ffrPlaques = _sqlManager.SelectPatientCaseFfrPlaque(sqlParameters);

                    if (String.IsNullOrEmpty(ffrPlaques[0].ReturnString))
                        PlaqueAreaList = new List<Measurement>();
                    else
                        PlaqueAreaList = JsonConvert.DeserializeObject<List<Measurement>>(ffrPlaques[0].ReturnString);
                    
                    for (int i = 0; i < ReviewStatus.NumberOfFrames; i++)
                    {
                        Measurement measurement = new Measurement();
                        measurement.FrameNumber = i;
                        measurement.AreaGeometries = new ObservableCollection<AreaGeometry>();
                        measurement.LengthGeometries = new ObservableCollection<LengthGeometry>();
                        measurement.TextGeometries = new List<TextGeometry>();
                        PlaqueAreaList.Add(measurement);
                    }
                    PlaqueAreaList = PlaqueAreaList.DistinctBy(x => x.FrameNumber).OrderBy(x => x.FrameNumber).ToList();
                }
                else
                {
                    PlaqueAreaList = FfrFeature.PlaqueAreaList;
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
        }

        private void ZoomIn()
        {
            _log.Debug("ZoomIn");

            if(ReviewStatus.ZoomFfr.ZoomIn() && (Constants.FfrStep4.Equals(FfrStep) || Constants.FfrStep5.Equals(FfrStep)))
                MeasurementCommand = Constants.MeasureZoomIn;
        }

        private void ZoomOut()
        {
            _log.Debug("ZoomOut");

            if(ReviewStatus.ZoomFfr.ZoomOut() && (Constants.FfrStep4.Equals(FfrStep) || Constants.FfrStep5.Equals(FfrStep)))
                MeasurementCommand = Constants.MeasureZoomOut;
        }

        private void Back()
        {
            _log.Debug("Back");

            switch (FfrStep)
            {
                case Constants.FfrStep2:
                    DrawCrossSection(GetMlaFrameNumber());

                    FfrStep = Constants.FfrStep1;
                    break;
                case Constants.FfrStep3:
                    DrawCrossSection(PatientCase.SectionProximal);

                    FfrStep = Constants.FfrStep2;
                    break;
                case Constants.FfrStep4:
                    DrawCrossSection(PatientCase.SectionDistal);
                    MeasurementCommand = Constants.MeasureDeleteAll;
                    IsDrawOn = false;

                    FfrStep = Constants.FfrStep3;
                    break;
                case Constants.FfrStep5:
                    IsEditOn = true;
                    IsDrawOn = true;
                    if (PlaqueAreaList[FrameNumber].AreaGeometries.Count == 0)
                        MeasurementCommand = Constants.MeasureAddArea + "|" + true;
                    else
                        MeasurementCommand = Constants.MeasureReDraw;
                    MeasurementCommand = null;

                    FfrStep = Constants.FfrStep4;
                    break;
                default:
                    break;
            }
        }

        private void Next()
        {
            _log.Debug("Next");

            switch (FfrStep)
            {
                case Constants.FfrStep1:
                    PatientCase.Vessel = CurrentVessel.Key;
                    DrawCrossSection(PatientCase.SectionProximal);

                    FfrStep = Constants.FfrStep2;
                    break;
                case Constants.FfrStep2:
                    PatientCase.SectionProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, Constants.SectionIndicatorMoveCenterWidth);
                    DrawCrossSection(PatientCase.SectionDistal);
                    MeasurementCommand = Constants.MeasureAddArea + "|" + false;

                    FfrStep = Constants.FfrStep3;
                    break;
                case Constants.FfrStep3:
                    PatientCase.SectionDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);
                    DrawCrossSection(GetMlaFrameNumber());
                    FfrFeature.MinimalLumenArea = GetMla();
                    IsEditOn = true;
                    IsDrawOn = true;
                    if (PlaqueAreaList[FrameNumber].AreaGeometries.Count == 0)
                        MeasurementCommand = Constants.MeasureAddArea + "|" + true;
                    else
                        MeasurementCommand = Constants.MeasureReDraw;
                    MeasurementCommand = null;

                    FfrStep = Constants.FfrStep4;
                    break;
                case Constants.FfrStep4:
                    if (PatientCase.FfrFeature == null)
                        PatientCase.FfrFeature = FfrFeature;
                    PatientCase.FfrFeature.PlaqueAreaList = PlaqueAreaList;
                    IsEditOn = false;
                    IsDrawOn = true;
                    MeasurementCommand = Constants.MeasureReDraw;
                    MeasurementCommand = null;

                    FfrStep = Constants.FfrStep5;
                    break;
                default:
                    break;
            }
        }

        private void DrawCrossSection(int frameNumber)
        {
            FrameNumber = frameNumber;

            MoveToFrame(RaySession.Review, FrameNumber);
            DrawCrossSectionImage();
        }

        private int GetMlaFrameNumber()
        {
            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                return Section.MlaValue.NValue;
            }
            else
            {
                return Section.MsaValue.NValue;
            }
        }

        private double GetMla()
        {
            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                return Section.MlaValue.DValue;
            }
            else
            {
                return Section.MsaValue.DValue;
            }
        }

        private void Confirm()
        {
            _log.Debug("Confirm");            

            double scaleArea = Constants.ImageResolution * Constants.ImageResolution;

            PatientCase.FfrFeature = FfrFeature;
            PatientCase.FfrFeature.VesselType = CurrentVessel.Buffer1;
            PatientCase.FfrFeature.ActualVesselType = CurrentVessel.Buffer2;
            PatientCase.FfrFeature.ProximalLumenArea = Section.Proximal.DValue;
            PatientCase.FfrFeature.DistalLumenArea = Section.Distal.DValue;
            PatientCase.FfrFeature.LesionLength = Section.LesionLength.DValue;
            PatientCase.FfrFeature.MinimalLumenFrameNumber = GetMlaFrameNumber();
            PatientCase.FfrFeature.MinimalLumenArea = GetMla();
            PatientCase.FfrFeature.PlaqueArea = FfrFeature.PlaqueArea;
            PatientCase.FfrFeature.PercentAreaStenosis = FfrFeature.PercentAreaStenosis;
            PatientCase.FfrFeature.PlaqueAreaList = PlaqueAreaList;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["ffr_plaque"] = ConvertMeasurementsToJson(PlaqueAreaList);
            int nRows = _sqlManager.UpdatePatientCaseFfrPlaque(sqlParameters);
            
            if(nRows == 0)
            {
                _log.Error("Update Error");
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                parameter["patientCase"] = PatientCase;
                parameter["prevStatus"] = PrevStatus;
                parameter["reviewStatus"] = ReviewStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewFfrPage) { Parameter = parameter });
            }
        }

        private string ConvertMeasurementsToJson(List<Measurement> param)
        {
            List<Measurement> measurements = new List<Measurement>();

            //Cross-section
            foreach (Measurement measurement in param)
            {
                if (measurement.AreaGeometries.Count > 0)
                {
                    foreach (AreaGeometry geometry in measurement.AreaGeometries)
                    {
                        geometry.Path = null;
                    }
                    measurements.Add(measurement);
                }
            }

            return JsonConvert.SerializeObject(measurements, Newtonsoft.Json.Formatting.Indented);
        }


        private void Delete()
        {
            _log.Debug("Delete");

            MeasurementCommand = Constants.MeasureDeleteAll;
            MeasurementCommand = Constants.MeasureAddArea + "|" + true;
        }

        private void SelectionChanged(Code currentVessel)
        {
            CheckVesselType(currentVessel.Key);
        }

        private void CheckVesselType(string key)
        {
            if ("$000".Equals(key) || "$001".Equals(key) || "$OTH".Equals(key))
            {
                IsVesselTypeValid = false;
                IsNextEnabled = false;
            }
            else
            {
                IsVesselTypeValid = true;
                IsNextEnabled = true;
            }                
        }

        private void ShowLumenProfile()
        {
            _log.Debug("ShowLumenProfile");

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (Section.SetMlaMld(PatientCase.LumenContours, PatientCase.SectionProximal, PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, PatientCase.PullbackLength, Constants.ImageResolution))
                {
                    Section.Proximal.IsVisible = Visibility.Visible;
                    Section.Distal.IsVisible = Visibility.Visible;
                }
                else
                {
                    Section.Proximal.IsVisible = Visibility.Collapsed;
                    Section.Distal.IsVisible = Visibility.Collapsed;
                }
            }
            else
            {
                int stentProximal = 0, stentDistal = 0;
                CommonUtil.GetStentProximalDistal(PatientCase.LumenStents, out stentProximal, out stentDistal);

                if (Section.SetMsaMinExp(PatientCase.LumenContours, PatientCase.SectionProximal, PatientCase.SectionDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, PatientCase.PullbackLength, Constants.ImageResolution))
                {
                    Section.Proximal.IsVisible = Visibility.Visible;
                    Section.Distal.IsVisible = Visibility.Visible;
                }
                else
                {
                    Section.Proximal.IsVisible = Visibility.Collapsed;
                    Section.Distal.IsVisible = Visibility.Collapsed;
                }
            }

            imglumenProfile = CommonUtil.MakeLumenProfileImage(PatientCase.LumenContours, PatientCase.LumenSidebranches, PatientCase.LumenStents, PatientCase.AppositionThreshold, PatientCase.SectionProximal, PatientCase.SectionDistal, CommonUtil.IsPostCase(PatientCase.Procedure));
            DrawLumenProfileImage();

            Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, Constants.SectionIndicatorMoveCenterWidth);
            Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

            RegenerateLumenProfile();
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

                double sectionIndicatorCenter = Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth;

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

                if (indicatorX < -Constants.SectionIndicatorMoveCenterWidth)
                {
                    indicator.X = -Constants.SectionIndicatorMoveCenterWidth;
                }
                else if (indicatorX > Constants.LongitudeFfrWidth - sectionIndicatorCenter)
                {
                    indicator.X = Constants.LongitudeFfrWidth - sectionIndicatorCenter;
                }
                else
                {
                    indicator.X = indicatorX;
                }

                RegenerateLumenProfile();
            }
        }

        private void RegenerateLumenProfile()
        {
            int frameProximal = CommonUtil.GetFrameFromPosition(Section.Proximal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, Constants.SectionIndicatorMoveCenterWidth);            
            int frameDistal = CommonUtil.GetFrameFromPosition(Section.Distal.X, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, Constants.SectionIndicatorMoveWidth - Constants.SectionIndicatorMoveCenterWidth);

            imglumenProfile = null;
            imglumenProfile = CommonUtil.MakeLumenProfileImageOneByOne(imglumenProfile, PatientCase.LumenContours, PatientCase.LumenSidebranches, PatientCase.LumenStents, PatientCase.AppositionThreshold, frameProximal, frameDistal, CommonUtil.IsPostCase(PatientCase.Procedure), ReviewStatus.NumberOfFrames - 1);
            DrawLumenProfileImage();

            Section.VisibleMlaMld(false);
            Section.VislbleMsaMinExp(false);

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (Section.SetMlaMld(PatientCase.LumenContours, frameProximal, frameDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, PatientCase.PullbackLength, Constants.ImageResolution))
                {
                    Section.VisibleMlaMld(true);
                    IsMlaEnabled = true;
                }
                else
                {
                    Section.VisibleMlaMld(false);
                    IsMlaEnabled = false;
                }
                    
            }
            else
            {
                int stentProximal = 0, stentDistal = 0;
                CommonUtil.GetStentProximalDistal(PatientCase.LumenStents, out stentProximal, out stentDistal);

                if (Section.SetMsaMinExp(PatientCase.LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeFfrWidth, PatientCase.PullbackLength, Constants.ImageResolution))
                {
                    Section.VislbleMsaMinExp(true);
                    IsMlaEnabled = true;
                }
                else
                {
                    Section.VislbleMsaMinExp(false);
                    IsMlaEnabled = false;
                }                    
            }

            if (FfrStep.Equals(Constants.FfrStep2))
            {
                DrawCrossSection(frameProximal);
            }
            else if (FfrStep.Equals(Constants.FfrStep3))
            {
                DrawCrossSection(frameDistal);
            }
        }

        public void Window_ManipulationStarting(ManipulationStartingEventArgs e)
        {
            _log.Debug("Manipulation Starting");
            e.Handled = true;
        }

        public void Window_ManipulationDelta(ManipulationDeltaEventArgs e)
        {
            ReviewStatus.ZoomFfr.Window_ManipulationDelta(e);
        }

        public void Window_ManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _log.Debug("Manipulation Completed");
            e.Handled = true;
        }
    }
}
