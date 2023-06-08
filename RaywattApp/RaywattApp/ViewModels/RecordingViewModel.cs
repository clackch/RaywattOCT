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
        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _readyCommand;
        public ICommand ReadyCommand
        {
            get { return this._readyCommand ?? (this._readyCommand = new RelayCommand(Ready)); }
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
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            leaveToPage(Constants.RecordingLiveViewPage);
        }

        private void Ready()
        {
            _log.Debug("Ready");

            //400 rps


            IsStep1 = false;

            Thread.Sleep(Constants.TransientTime);

            StartTime = Constants.StartTime;
            timer.Start();
        }

        private void StartTimer(object sender, EventArgs e)
        {
            StartTime--;
            if(StartTime == 0)
            {
                //50 rps


                IsStep1 = true;
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
