using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class ReviewCalibrationViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewCalibrationViewModel));

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus;

        [ObservableProperty]
        private Zoom _zoom;

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand(Ok)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _resetCommand;
        public ICommand ResetCommand
        {
            get { return this._resetCommand ?? (this._resetCommand = new RelayCommand(Reset)); }
        }

        private ICommand _cmdManualZoomIn;
        public ICommand CmdManualZoomIn
        {
            get { return _cmdManualZoomIn ?? (this._cmdManualZoomIn = new RelayCommand<bool>(ManualZoomIn)); }
        }

        public ReviewCalibrationViewModel()
        {
            _log.Debug("ReviewCalibrationViewModel");

            Constants.CurrentPage = Constants.RecordingCalibrationPage;

            Zoom = new Zoom();
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
                PrevStatus = (PrevStatus)data["prevStatus"];
                PatientCase = (PatientCase)data["patientCase"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];

                Zoom.SetFieldOfView(Constants.DefaultFoV / 5);

                SetCrossSectionBackground(RaySession.Review, Constants.CardBackgroundColor);

                GetImageInfo(RaySession.Review);
                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);

                DrawSheathIndicator();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
        }

        private void ManualZoomIn(bool zoomIn)
        {
            _log.Debug("ManualZoomIn : " + ((zoomIn) ? "IN" : "OUT"));
        }

        private void Reset()
        {
            _log.Debug("Reset");
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            GoToPreviousPage(false);
        }

        private void Ok()
        {
            _log.Debug("Ok");

            GoToPreviousPage(true);
        }

        private void GoToPreviousPage(bool isSave)
        {
            _log.Debug("GoToPreviousPage");

            if (!isSave)
            {
                Reset();
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(ReviewStatus.CurrentPage) { Parameter = parameter });
        }

        protected override void UpdateCrossSectionImage()
        {
            DrawCrossSectionImage();
        }
    }
}
