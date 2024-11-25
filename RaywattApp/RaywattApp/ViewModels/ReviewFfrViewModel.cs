using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class ReviewFfrViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewFfrViewModel));

        private DispatcherTimer ffrCalculatingTimer = new DispatcherTimer();

        private DispatcherTimer opacityFadeOutTimer = new DispatcherTimer();

        private DispatcherTimer opacityFadeInTimer = new DispatcherTimer();

        private string modelPath = Constants.MlModelFolderPath + "\\oct_ffr\\oct_ffr_feature7.onnx";

        private InferenceSession sess;

        [ObservableProperty]
        private double _ffrResult;

        [ObservableProperty]
        private Visibility _visibilityResult;

        [ObservableProperty]
        private double _opacityPopup;

        [ObservableProperty]
        private double _opacityResult;

        [ObservableProperty]
        private int _zIndex;

        [ObservableProperty]
        private BitmapSource _proximalOctImage;

        [ObservableProperty]
        private BitmapSource _distalOctImage;

        [ObservableProperty]
        private Section _section;

        [ObservableProperty]
        private double _mlaX;

        [ObservableProperty]
        private bool _btnFfrEnabled;

        [ObservableProperty]
        private Zoom _zoom = new Zoom(Constants.CrossSectionFfrSize);

        private ICommand _ffrPredictCommand;
        public ICommand FfrPredictCommand
        {
            get { return this._ffrPredictCommand ?? (this._ffrPredictCommand = new RelayCommand(FfrPredict)); }
        }

        private ICommand _ffrSettingCommand;
        public ICommand FfrSettingCommand
        {
            get { return this._ffrSettingCommand ?? (this._ffrSettingCommand = new RelayCommand(FfrSetting)); }
        }

        public ReviewFfrViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewFfrViewModel");

            Constants.CurrentPage = Constants.ReviewFfrPage;

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;

            VisibilityResult = Visibility.Hidden;
            BtnFfrEnabled = true;
            OpacityPopup = 1.0;
            ZIndex = 0;

            sess = new InferenceSession(modelPath);

            ffrCalculatingTimer.Interval = TimeSpan.FromMilliseconds(3000);
            ffrCalculatingTimer.Tick += new EventHandler(FfrCalculatingTimer);
            opacityFadeOutTimer.Interval = TimeSpan.FromMilliseconds(50);
            opacityFadeOutTimer.Tick += new EventHandler(OpacityFadeOutTimer);
            opacityFadeInTimer.Interval = TimeSpan.FromMilliseconds(50);
            opacityFadeInTimer.Tick += new EventHandler(OpacityFadeInTimer);
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
                ReviewStatus.CurrentPage = Constants.ReviewFfrPage;

                Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);

                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);
                int currentFrameNumber = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current;

                MoveToFrame(RaySession.Review, PatientCase.SectionProximal);
                DrawCrossSectionImage();
                ProximalOctImage = CrossSectionImage;

                MoveToFrame(RaySession.Review, PatientCase.SectionDistal);
                DrawCrossSectionImage();
                DistalOctImage = CrossSectionImage;


                MoveToFrame(RaySession.Review, PatientCase.FfrFeature.MinimalLumenFrameNumber);
                DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = currentFrameNumber;
                DrawCrossSectionImage();

                ShowLumenProfile();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            Save();
        }

        private void FfrPredict()
        {
            _log.Debug("FfrPredict");

            FfrResult = 0;
            OpacityResult = 0.0;
            VisibilityResult = Visibility.Visible;
            BtnFfrEnabled = false;

            CalcFfrResult();

            DeviceStatus.IsFfrCalculated = false;
            ZIndex = 1;

            ffrCalculatingTimer.Start();            
        }

        private void FfrSetting()
        {
            _log.Debug("FfrSetting");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewFfrSettingPage) { Parameter = parameter });
        }

        private int GetVessel()
        {
            int vesselType = 0;

            switch (PatientCase.FfrFeature.VesselType)
            {
                case "$002":
                    vesselType = 0;
                    break;
                case "$005":
                    vesselType = 1;
                    break;
                case "$008":
                    vesselType = 2;
                    break;
                default:
                    break;
            }

            return vesselType;
        }

        private void CalcFfrResult()
        {
            _log.Debug("CalcFfrResult");

            Tensor<float> input = new DenseTensor<float>(new[] { 1, 7 });
            input[0, 0] = (float)PatientCase.FfrFeature.ProximalLumenArea;
            input[0, 1] = (float)PatientCase.FfrFeature.MinimalLumenArea;
            input[0, 2] = (float)PatientCase.FfrFeature.DistalLumenArea;
            input[0, 3] = (float)PatientCase.FfrFeature.LesionLength;
            input[0, 4] = (float)PatientCase.FfrFeature.PlaqueArea;
            input[0, 5] = (float)PatientCase.FfrFeature.PercentAreaStenosis;
            input[0, 6] = GetVessel();

            var inputs = new List<NamedOnnxValue>
            {            
                NamedOnnxValue.CreateFromTensor("float_input", input)            
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = sess.Run(inputs))
            {
                FfrResult = Math.Round(results[0].AsEnumerable<float>().ToArray()[0], 2);
            }
        }

        private void ShowLumenProfile()
        {
            _log.Debug("ShowLumenProfile");

            int frameProximal = PatientCase.SectionProximal;
            int frameDistal = PatientCase.SectionDistal;

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (Section.SetMlaMld(PatientCase.LumenContours, frameProximal, frameDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength))
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

                if (Section.SetMsaMinExp(PatientCase.LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength))
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

            imglumenProfile = CommonUtil.MakeLumenProfileImage(PatientCase.LumenContours, PatientCase.LumenSidebranches, PatientCase.LumenStents, PatientCase.AppositionThreshold, frameProximal, frameDistal, CommonUtil.IsPostCase(PatientCase.Procedure));
            DrawLumenProfileImage();

            Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
            Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

            MlaX = CommonUtil.GetPositionFromFrame(PatientCase.FfrFeature.MinimalLumenFrameNumber, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, 4);
        }

        private void FfrCalculatingTimer(object sender, EventArgs e)
        {
            ffrCalculatingTimer.Stop();
            opacityFadeOutTimer.Start();
        }

        private void OpacityFadeOutTimer(object sender, EventArgs e)
        {
            if(OpacityPopup <= 0)
            {
                opacityFadeOutTimer.Stop();
                opacityFadeInTimer.Start();

                ZIndex = 0;
                DeviceStatus.IsFfrCalculated = true;
            }

            OpacityPopup -= 0.05;
        }

        private void OpacityFadeInTimer(object sender, EventArgs e)
        {
            if (OpacityResult >= 1.0)
            {
                opacityFadeInTimer.Stop();
            }

            OpacityResult += 0.05;
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
            sqlParameters["manual_calibration"] = PatientCase.ManualCalibration;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
                _log.Error("Update Error");
        }
    }
}
