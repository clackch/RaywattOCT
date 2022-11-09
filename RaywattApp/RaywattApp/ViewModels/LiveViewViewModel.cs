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
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace RaywattApp.ViewModels
{
    public partial class LiveViewViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LiveViewViewModel));

        private readonly SqlManager? _sqlManager;

        private IDialogService? _dialogService;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private string? _viewMode;

        [ObservableProperty]
        private bool? _isInitialized;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _cmdBack;
        public ICommand CmdBack
        {
            get { return _cmdBack ?? (this._cmdBack = new RelayCommand(Back)); }
        }

        private ICommand _cmdChangeViewMode;
        public ICommand CmdChangeViewMode
        { 
            get { return _cmdChangeViewMode ?? (this._cmdChangeViewMode = new RelayCommand(ChangeViewMode)); }
        }

        private ICommand _cmdCalibration;
        public ICommand CmdCalibration
        {
            get { return _cmdCalibration ?? (this._cmdCalibration = new RelayCommand(Calibration)); }
        }

        private ICommand _cmdStartRecording;
        public ICommand CmdStartRecording
        {
            get { return _cmdStartRecording ?? (this._cmdStartRecording = new RelayCommand(StartRecording)); }
        }

        public LiveViewViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("LiveViewViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.LiveViewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            ViewMode = Constants.ViewModeStandBy;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];

                this.IsInitialized = false;

                RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.BackgroundColor, 0xFFFFFF);
                RayCoreWrapper.RayInitialize();

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(5);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            // To-Do : Finalize
        }

        private void Back()
        {
            _log.Debug("Back");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
        }

        private void ChangeViewMode()
        {
            _log.Debug("ChangeViewMode : " + ViewMode);

            if (Constants.ViewModeLiveView.Equals(ViewMode))
            {
                RayCoreWrapper.RayMotorOnOff(true);
            }
            else if (Constants.ViewModeStandBy.Equals(ViewMode))
            {
                RayCoreWrapper.RayMotorOnOff(false);
            }
        }

        private void Calibration()
        {
            _log.Debug("Calibration");

            if (Constants.ViewModeStandBy.Equals(ViewMode))
            {
                // Select LiveView First
            }
            else { 
                // Do Calibration (Manual + Auto)
            }
        }

        private void StartRecording()
        {
            _log.Debug("StartRecording");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/RecordingPage.xaml") { Parameter = parameter });
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);
            }
        }

        protected override void handleError(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayError error)
        {
            _log.Debug("handleError : " + ((int)error).ToString());
            switch (error)
            {
            case RayCoreWrapper.RayError.InitializeFailed:
                break;
            case RayCoreWrapper.RayError.WrongOCTScannerState:
                 break;
            default:
                break;
            }
        }

        protected override void handleProgress(RayCoreWrapper.RayCallbackRequest request, int progress)
        {
        }

        protected override void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState state)
        {
            switch (state) 
            {
            case RayCoreWrapper.RayScannerState.None:
                break;
                case RayCoreWrapper.RayScannerState.Initializing:
                break;
                case RayCoreWrapper.RayScannerState.LiveView:
                    IsInitialized = true;
                break;
                case RayCoreWrapper.RayScannerState.Homing:
                case RayCoreWrapper.RayScannerState.Ready:
                case RayCoreWrapper.RayScannerState.Scanning:
                    break;
            }
        }

        protected override void handleWorkDone(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayWorkItem work)
        {
        }
    }
}
