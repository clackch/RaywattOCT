using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Services;
using System.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Windows.Threading;
using RaywattApp.Common.Util;
using static RaywattOCT.RayCoreWrapper;
using System.Threading;

namespace RaywattApp.ViewModels
{
    public partial class RecordingViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private bool _isStep1;

        [ObservableProperty]
        private int _startTime;

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer readyTimer = new DispatcherTimer();
        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private bool isReadyOn = true;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _readyCommand;
        public ICommand ReadyCommand
        {
            get { return this._readyCommand ?? (this._readyCommand = new RelayCommand(Ready, CanReady)); }
        }

        private ICommand _startCommand;
        public ICommand StartCommand
        {
            get { return this._startCommand ?? (this._startCommand = new RelayCommand(Start)); }
        }

        public RecordingViewModel(SqlManager sqlManager)
        {
            _log.Debug("RecordingViewModel");

            Constants.CurrentPage = Constants.RecordingPage;

            _sqlManager = sqlManager;

            IsStep1 = true;

            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(StartTimer);

            readyTimer.Interval = TimeSpan.FromMilliseconds(Constants.TransientTime);
            readyTimer.Tick += new EventHandler(ReadyTimer);
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

            if (timer.IsEnabled)
                timer.Stop();

            if(readyTimer.IsEnabled)
                readyTimer.Stop();
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            RayStopLiveView();
            leaveToPage(Constants.RecordingLiveViewPage);
        }

        private void Ready()
        {
            _log.Debug("Ready");

            RayReadyPullback();

            isReadyOn = false;
            (ReadyCommand as RelayCommand).NotifyCanExecuteChanged();
            readyTimer.Start();
        }

        private bool CanReady()
        {
            _log.Debug("CanReady");

            return isReadyOn;
        }

        private void ReadyTimer(object sender, EventArgs e)
        {
            IsStep1 = false;

            StartTime = Constants.StartTime;
            timer.Start();

            readyTimer.Stop();
        }

        private void StartTimer(object sender, EventArgs e)
        {
            StartTime--;
            if(StartTime == 0)
            {
                RayStartLiveView();

                IsStep1 = true;
                isReadyOn = true;
                (ReadyCommand as RelayCommand).NotifyCanExecuteChanged();
                timer.Stop();
            }
        }

        private void Start()
        {
            _log.Debug("Start");

            if (timer.IsEnabled)
                timer.Stop();

            PatientCase.Image = generateFileName("oct");
            DeviceStatus.IsLumenDetected = false;
            DeviceStatus.IsLumenLoaded = false;
            DeviceStatus.IsPullbackDone = false;
            RayPullbackScan(PatientCase.ImageFullPath);

            leaveToPage(Constants.RecordingConfirmPage);
        }
        
        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DrawCrossSectionImage();
        }

        private void leaveToPage(string viewPage)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(viewPage) { Parameter = parameter });
        }

        private string generateFileName(string ext)
        {
            string filename = "{" +
                CommonUtil.GetRandomText(8) + "-" +
                CommonUtil.GetRandomText(4) + "-" +
                CommonUtil.GetRandomText(4) + "-" +
                CommonUtil.GetRandomText(4) + "-" +
                CommonUtil.GetRandomText(12) +
                "}." + ext;

            _log.Debug("generateFileName : " + filename);

            return filename;
        }
    }
}
