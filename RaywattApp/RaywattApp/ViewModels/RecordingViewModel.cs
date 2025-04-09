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
using RaywattApp.Common.Angio;
using System.Threading;

namespace RaywattApp.ViewModels
{
    public partial class RecordingViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingViewModel));

        private readonly SqlManager _sqlManager;
        private readonly AngioManager _angioManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private bool _isStep1;

        [ObservableProperty]
        private bool _isReady;

        [ObservableProperty]
        private bool _isStart;

        [ObservableProperty]
        private bool _isCancel;

        [ObservableProperty]
        private int _startTime;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        private Thread threadWaitPullbackDone;
        private bool runWaitPullbackDone;

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer readyTimer = new DispatcherTimer();
        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

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

        public RecordingViewModel(SqlManager sqlManager, AngioManager angioManager)
        {
            _log.Debug("RecordingViewModel");

            Constants.CurrentPage = Constants.RecordingPage;

            _sqlManager = sqlManager;
            _angioManager = angioManager;

            IsStep1 = true;
            IsReady = true;
            IsStart = true;
            IsCancel = true;

            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(StartTimer);

            readyTimer.Interval = TimeSpan.FromMilliseconds(Constants.TransientTime);
            readyTimer.Tick += new EventHandler(ReadyTimer);

            threadWaitPullbackDone = new Thread(new ThreadStart(threadFuncWaitPullbackDone));

            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total = 0;

            // Instant start 방지
            //Thread.Sleep(1000);
            
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

            if (timer.IsEnabled)
                timer.Stop();

            if(readyTimer.IsEnabled)
                readyTimer.Stop();

            _angioManager.ReadyToRecv = true;
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

            IsReady = false;
            IsCancel = false;

            Thread threadReadyPullback = new Thread(() => ThreadReadyPullback());
            threadReadyPullback.Start();
        }

        private void ThreadReadyPullback()
        {
            RayReadyPullback();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                isReadyOn = false;
                (ReadyCommand as RelayCommand).NotifyCanExecuteChanged();
                readyTimer.Start();
            });
        }

        private bool CanReady()
        {
            _log.Debug("CanReady");

            return isReadyOn;
        }

        private void ReadyTimer(object sender, EventArgs e)
        {
            IsStep1 = false;
            IsCancel = true;

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
                IsReady = true;
                IsStart = true;
                IsCancel = true;
                (ReadyCommand as RelayCommand).NotifyCanExecuteChanged();
                timer.Stop();
            }
        }

        private void Start()
        {
            _log.Debug("Start");

            if (timer.IsEnabled)
                timer.Stop();

            IsStart = false;
            IsCancel = false;

            PatientCase.Image = generateFileName("oct");            
            DeviceStatus.IsSaveRawDataDone = false;
            DeviceStatus.IsLumenSaved = false;
            DeviceStatus.IsOCTImagingDone = false;
            DeviceStatus.IsLumenDetected = false;
            DeviceStatus.IsPullbackDone = false;

            RayPullbackScan(PatientCase.ImageFullPath);

            if (DeviceStatus.IsAngioConnected && _angioManager.isChpFileConnected == 1)
            {
                _angioManager.ReadyToRecv = false;
                _angioManager.ReadyToSaveAngioThread(PatientCase);
            }

            threadWaitPullbackDone.Start();            
        }

        private void threadFuncWaitPullbackDone()
        {
            runWaitPullbackDone = true;

            while (runWaitPullbackDone && !DeviceStatus.IsPullbackDone)
            {
                Thread.Sleep((int)Constants.WaitForEventInterval);
            }
            runWaitPullbackDone = false;

            leaveToPage(Constants.RecordingConfirmPage);
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DrawCrossSectionImage();

            if (DeviceStatus.IsAngioConnected)
                DrawAngioImage();
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

        private bool DrawAngioImage()
        {
            AngioImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(_angioManager.ImgAngio);

            return true;
        }
    }
}
