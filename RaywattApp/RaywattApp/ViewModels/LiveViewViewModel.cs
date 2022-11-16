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
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

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

                RayInitialize();
                RaySetProperty(Property.BackgroundColor, 0xFFFFFF);

                syncWithCoreSystem();

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(5);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Back()
        {
            _log.Debug("Back");
            
            RayFinalize();

            leaveToPage("Views/PatientDetailPage.xaml");
        }

        private void ChangeViewMode()
        {
            _log.Debug("ChangeViewMode : " + ViewMode);

            if (Constants.ViewModeLiveView.Equals(ViewMode))
            {
                RayMotorOnOff(true);
            }
            else if (Constants.ViewModeStandBy.Equals(ViewMode))
            {
                RayMotorOnOff(false);
            }
        }

        private void Calibration()
        {
            _log.Debug("Calibration");

            ViewMode = Constants.ViewModeLiveView;
            ChangeViewMode();

            leaveToPage("Views/CalibrationPage.xaml");
        }

        private void StartRecording()
        {
            _log.Debug("StartRecording");

            leaveToPage("Views/RecordingPage.xaml");
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);
            }
        }

        private void leaveToPage(string viewPage)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(viewPage) { Parameter = parameter });
        }

        private void syncWithCoreSystem()
        {
            RayScannerState curState = (RayScannerState)RayGetProperty(Property.CurrentState);
            bool isLiveView = (bool)(RayGetProperty(Property.MotorOnOff) != 0);

            this.IsInitialized = true;// (curState == RayScannerState.LiveView) ? true : false;
            this.ViewMode = (isLiveView) ? Constants.ViewModeLiveView : Constants.ViewModeStandBy;
        }

        protected override void handleError(RayCallbackRequest request, RayError error)
        {
            _log.Debug("handleError : " + ((int)error).ToString());

            switch (error)
            {
            case RayError.InitializeFailed:
                break;
            case RayError.WrongOCTScannerState:
                 break;
            default:
                break;
            }
        }

        protected override void handleProgress(RayCallbackRequest request, int progress)
        {
        }

        protected override void handleState(RayCallbackRequest request, RayScannerState state)
        {
            syncWithCoreSystem();
        }

        protected override void handleWorkDone(RayCallbackRequest request, RayWorkItem work)
        {
        }
    }
}
 