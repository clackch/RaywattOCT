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
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class RecordingCalibrationViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingCalibrationViewModel));

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        private bool isMoveLiveView;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

        private ICommand _cmdBack;
        public ICommand CmdBack
        {
            get { return _cmdBack ?? (this._cmdBack = new RelayCommand(Back)); }
        }

        private ICommand _cmdManualZoomIn;
        public ICommand CmdManualZoomIn
        { 
            get { return _cmdManualZoomIn ?? (this._cmdManualZoomIn = new RelayCommand<bool>(ManualZoomIn)); }
        }

        private ICommand _cmdAutoCalibration;
        public ICommand CmdAutoCalibration
        { 
            get { return _cmdAutoCalibration ?? (this._cmdAutoCalibration = new RelayCommand(AutoCalibration)); }
        }

        public RecordingCalibrationViewModel()
        {
            _log.Debug("RecordingCalibrationViewModel");

            Constants.CurrentPage = Constants.RecordingCalibrationPage;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];
                PatientCase = (PatientCase)data["patientCase"];

                Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();

            if (!this.isMoveLiveView)
            {
                RayError result = (RayError)RayStopLiveView();
                if (result != RayError.OK)
                {
                    _log.Error("RayStopLiveView Error");
                }
            }
        }

        private void Back()
        {
            _log.Debug("Back");

            this.isMoveLiveView = true;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingLiveViewPage) { Parameter = parameter });
        }

        private void ManualZoomIn(bool zoomIn)
        {
            _log.Debug("ManualZoomIn : " + ((zoomIn) ? "IN" : "OUT"));

            RayError result = (RayError)RayManualCalibration(zoomIn);
            if (result != RayError.OK)
            {
                _log.Error("RayManualCalibration Error");
            }
        }

        private void AutoCalibration() 
        {
            _log.Debug("AutoCalibration");

            RayError result = (RayError)RayAutoCalibration();
            if (result != RayError.OK)
            {
                _log.Error("RayAutoCalibration Error");
            }
            DeviceStatus.CanExecuteCalibration = false;
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DrawCrossSectionImage();
        }
    }
}
