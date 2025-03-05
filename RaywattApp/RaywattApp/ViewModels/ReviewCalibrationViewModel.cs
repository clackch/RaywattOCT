using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
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

        private IDialogService _dialogService;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        private int zOffset;

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

        private ICommand _revertCalibrateCommand;
        public ICommand RevertCalibrateCommand
        {
            get { return this._revertCalibrateCommand ?? (this._revertCalibrateCommand = new RelayCommand(RevertCalibrate)); }
        }

        public ReviewCalibrationViewModel(IDialogService dialogService)
        {
            _log.Debug("ReviewCalibrationViewModel");

            Constants.CurrentPage = Constants.RecordingCalibrationPage;

            _dialogService = dialogService;
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


            int sign = zoomIn ? 1 : -1;

            this.zOffset += sign;

            if (PatientCase.ZOffset + this.zOffset > Constants.ZOffsetLimit || PatientCase.ZOffset + this.zOffset < -1 * Constants.ZOffsetLimit)
            {
                this.zOffset += sign * -1;
                return;
            }

            RaySetProperty(Property.ZOffset, PatientCase.ZOffset + this.zOffset);
            MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
        }

        private void Reset()
        {
            _log.Debug("Reset");

            this.zOffset = 0;
            RaySetProperty(Property.ZOffset, PatientCase.ZOffset + this.zOffset);
            MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
        }

        private void RevertCalibrate()
        {
            _log.Debug("RevertCalibrate");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            DialogResults? result = null;

            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Confirm reversion to original calibration"];
            result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                if(PatientCase.ZOffset != 0)
                {
                    PatientCase.ZOffset = 0;
                    RaySetProperty(Property.ZOffset, PatientCase.ZOffset);
                    RestartReview();
                }
                else
                {
                    RaySetProperty(Property.ZOffset, PatientCase.ZOffset);
                }
                
                GoToPreviousPage();
            }
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            Reset();

            GoToPreviousPage();
        }

        private void Ok()
        {
            _log.Debug("Ok");

            if(this.zOffset != 0)
            {
                PatientCase.ZOffset += this.zOffset;
                RestartReview();
            }

            GoToPreviousPage();
        }

        private void RestartReview()
        {
            _log.Debug("RestartReview");

            RayRestartReview();

            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
            ReviewStatus.IsRestartLumenDetection = true;
            ReviewStatus.IsLumenEdited = true;
        }

        private void GoToPreviousPage()
        {
            _log.Debug("GoToPreviousPage");

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
