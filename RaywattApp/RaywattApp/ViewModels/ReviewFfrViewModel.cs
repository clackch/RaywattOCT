using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using RaywattApp.Models;
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

        private DispatcherTimer opacityTimer = new DispatcherTimer();

        private string modelPath = Constants.MlModelFolderPath + "\\oct_ffr\\oct_ffr_feature5.onnx";

        private InferenceSession sess;

        private float vessel;

        [ObservableProperty]
        private double _ffrResult;

        [ObservableProperty]
        private Visibility _visibilityResult;

        [ObservableProperty]
        private double _opacityResult;

        [ObservableProperty]
        private string _vesselType;

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

        private ICommand _ffrPredictCommand;
        public ICommand FfrPredictCommand
        {
            get { return this._ffrPredictCommand ?? (this._ffrPredictCommand = new RelayCommand(FfrPredict)); }
        }

        public ReviewFfrViewModel()
        {
            _log.Debug("ReviewFfrViewModel");

            Constants.CurrentPage = Constants.ReviewFfrPage;

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;

            VisibilityResult = Visibility.Hidden;
            BtnFfrEnabled = true; 

            sess = new InferenceSession(modelPath);

            //Test
            opacityTimer.Interval = TimeSpan.FromMilliseconds(50);
            opacityTimer.Tick += new EventHandler(OpacityTest);
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
                
                SetVessel();

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
        }

        private void FfrPredict()
        {
            _log.Debug("FfrPredict");

            FfrResult = 0;
            OpacityResult = 0.0;
            VisibilityResult = Visibility.Visible;
            BtnFfrEnabled = false;

            CalcFfrResult();

            opacityTimer.Start();
        }

        private void SetVessel()
        {
            /*
                ■ Vessel 구분
                LAD (Left Anterior Descending artery)
                    LAD Prox (LAD 근위부)
                    LAD Mid (LAD 중간부)
                    LAD Distal (LAD 원위부)
                    Diagonal 1 (첫 번째 대각가지)
                    Diagonal 2 (두 번째 대각가지)
                LCX (Left Circumflex artery)
                    Left Main (좌주간부, 좌주관 동맥)
                    LCX Prox (LCX 근위부)
                    LCX OM1 (첫 번째 Obtuse Marginal branch)
                    LCX Mid (LCX 중간부)
                    LCX OM2 (두 번째 Obtuse Marginal branch)
                    LCX Distal (LCX 원위부)
                RCA (Right Coronary artery)
                    RCA Prox (RCA 근위부)
                    RCA Mid (RCA 중간부)
                    RCA Distal (RCA 원위부)
                    PDA (Posterior Descending artery)
                Other
                    Other (기타)
                 */
            if (PatientCase.Vessel == "$006" || PatientCase.Vessel == "$007" || PatientCase.Vessel == "$008" || PatientCase.Vessel == "$009" || PatientCase.Vessel == "$010")
            {
                VesselType = "LAD";
                vessel = 0;
            }
            else if (PatientCase.Vessel == "$005" || PatientCase.Vessel == "$011" || PatientCase.Vessel == "$012" || PatientCase.Vessel == "$013" || PatientCase.Vessel == "$014" || PatientCase.Vessel == "$015")
            {
                VesselType = "LCX";
                vessel = 1;
            }
            else if (PatientCase.Vessel == "$001" || PatientCase.Vessel == "$002" || PatientCase.Vessel == "$003" || PatientCase.Vessel == "$004")
            {
                VesselType = "RCA";
                vessel = 2;
            }
        }

        private void CalcFfrResult()
        {
            /*
            ■ 참고
            PatientCase.FfrFeature.PercentAreaStenosis / PatientCase.FfrFeature.LesionLength / PatientCase.FfrFeature.MinimalLumenArea
            PatientCase.FfrFeature.PlaqueArea / PatientCase.FfrFeature.DistalLumenArea / PatientCase.FfrFeature.ProximalLumenArea
             */

            Tensor<float> input = new DenseTensor<float>(new[] { 1, 6 });
            input[0, 0] = (float)PatientCase.FfrFeature.ProximalLumenArea;
            input[0, 1] = (float)PatientCase.FfrFeature.MinimalLumenArea;
            input[0, 2] = (float)PatientCase.FfrFeature.DistalLumenArea;
            input[0, 3] = (float)PatientCase.FfrFeature.LesionLength;
            input[0, 4] = (float)PatientCase.FfrFeature.PercentAreaStenosis;
            input[0, 5] = vessel;

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
            int frameProximal = PatientCase.SectionProximal;
            int frameDistal = PatientCase.SectionDistal;
            int stentProximal = 0, stentDistal = 0;
            CommonUtil.GetStentProximalDistal(PatientCase.LumenStents, out stentProximal, out stentDistal);
            if (Section.SetMsaMinExp(PatientCase.LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, PatientCase.PullbackLength, PatientCase.ImageResolution))
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

            imglumenProfile = CommonUtil.MakeLumenProfileImage(PatientCase.LumenContours, PatientCase.LumenSidebranches, PatientCase.LumenStents, PatientCase.AppositionThreshold, frameProximal, frameDistal, CommonUtil.IsPostCase(PatientCase.Procedure));
            DrawLumenProfileImage();

            List<int> colorFrames = CommonUtil.GetExpansionList(PatientCase.LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, Section.RefArea, PatientCase.ExpansionThreshold);
            imglumenProfileExtra = CommonUtil.MakeLumenProfileImageExtra(ReviewStatus.NumberOfFrames, colorFrames, false);
            DrawLumenProfileImageExtra();

            Section.Proximal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionProximal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorCenterWidth);
            Section.Distal.X = CommonUtil.GetPositionFromFrame(PatientCase.SectionDistal, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

            MlaX = CommonUtil.GetPositionFromFrame(PatientCase.FfrFeature.MinimalLumenFrameNumber, ReviewStatus.NumberOfFrames, Constants.LongitudeWidth, 4);
        }

        private void OpacityTest(object sender, EventArgs e)
        {
            if(OpacityResult >= 1.0)
            {
                opacityTimer.Stop();
            }

            OpacityResult += 0.02;
        }
    }
}
