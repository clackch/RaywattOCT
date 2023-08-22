using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class ReviewFfrViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewFfrViewModel));

        [ObservableProperty]
        private double _ffrProgress;

        [ObservableProperty]
        private double _ffrResult;

        [ObservableProperty]
        private Visibility _visibilityResult;

        [ObservableProperty]
        private double _opacityResult;

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer opacityTimer = new DispatcherTimer();

        private ICommand _ffrPredictCommand;
        public ICommand FfrPredictCommand
        {
            get { return this._ffrPredictCommand ?? (this._ffrPredictCommand = new RelayCommand(FfrPredict)); }
        }

        public ReviewFfrViewModel()
        {
            _log.Debug("ReviewFfrViewModel");

            Constants.CurrentPage = Constants.ReviewFfrPage;

            VisibilityResult = Visibility.Hidden;

            //Test
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += new EventHandler(ProgressTest);
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

                SetCrossSectionBackground(RaySession.Review, Constants.BackgroundColor);
                int currentFrameNumber = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current;
                MoveToFrame(RaySession.Review, PatientCase.FfrFeature.MinimalLumenFrameNumber);
                DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = currentFrameNumber;
                DrawCrossSectionImage();
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

            FfrProgress = 0;
            FfrResult = 0;
            OpacityResult = 0.0;
            VisibilityResult = Visibility.Hidden;

            //Test
            timer.Start();
        }

        private void CalcFfrResult()
        {
            //고정값 나오도록 처리 (인증용) - 변경 필요
            double res = 0;
            double sum = PatientCase.FfrFeature.PercentAreaStenosis + PatientCase.FfrFeature.LesionLength + PatientCase.FfrFeature.MinimalLumenArea
                + PatientCase.FfrFeature.PlaqueArea + PatientCase.FfrFeature.DistalLumenArea + PatientCase.FfrFeature.ProximalLumenArea;

            int temp = (int)sum % 3;
            if (temp == 0)
                res = 0.7;
            else if (temp == 1)
                res = 0.8;
            else
                res = 0.9;

            int temp2 = (int)sum % 10;

            res += temp2 * 0.01;

            FfrResult = Math.Round(res, 2);
        }

        private void ProgressTest(object sender, EventArgs e)
        {
            if (FfrProgress == 100)
            {
                timer.Stop();

                CalcFfrResult();
                VisibilityResult = Visibility.Visible;

                opacityTimer.Start();
            }

            FfrProgress += 1;
        }

        private void OpacityTest(object sender, EventArgs e)
        {
            if(OpacityResult >= 1.0)
            {
                opacityTimer.Stop();
            }

            OpacityResult += 0.05;
        }
    }
}
