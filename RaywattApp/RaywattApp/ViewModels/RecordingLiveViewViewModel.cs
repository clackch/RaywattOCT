using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;
using RaywattApp.Common.Angio;

namespace RaywattApp.ViewModels
{
    public partial class RecordingLiveViewViewModel : OCTViewModelBase
    {
        private DispatcherTimer timerLiveAngioImage = new DispatcherTimer(DispatcherPriority.Background);

        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingLiveViewViewModel));

        private readonly SqlManager? _sqlManager;

        private IDialogService? _dialogService;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Dictionary<string, string> _procedureList;

        private readonly TcpClientSingleton _tcpClientSingleton;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

        private int _brightness;
        public int Brightness
        {
            get { return _brightness; }
            set { _brightness = value; OnPropertyChanged(nameof(Brightness)); RaySetProperty(Property.Brightness, value); }
        }

        private int _contrast;
        public int Contrast
        {
            get { return _contrast; }
            set { _contrast = value; OnPropertyChanged(nameof(Contrast)); RaySetProperty(Property.Contrast, value); }
        }

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

        private bool _isRecording = false;
        public RecordingLiveViewViewModel(SqlManager sqlManager, IDialogService dialogService, TcpClientSingleton tcpClientSingleton)
        {
            _log.Debug("RecordingLiveViewViewModel");

            Constants.CurrentPage = Constants.RecordingLiveViewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            ProcedureList = CodeDefinition.Codes["PROC"];

            _tcpClientSingleton = tcpClientSingleton;
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

                if (data.ContainsKey("patientCase"))
                    PatientCase = (PatientCase)data["patientCase"];
                else
                {
                    PatientCase = new PatientCase();
                    PatientCase.PatientId = Patient.Id;
                }

                RayShowCalibrationGuide(true);

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();

                SetCondition();
            }

            // Send Start Command
            _tcpClientSingleton.Instance.GetStream().Write(_tcpClientSingleton.startCommand, 0, _tcpClientSingleton.startCommand.Length);

            timerLiveAngioImage.Interval = TimeSpan.Zero; // TimeSpan.Zero;
            timerLiveAngioImage.Tick += new EventHandler(timerFuncLiveAngioImage);
            timerLiveAngioImage.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();

            if (AngioClient.threadOnLiveAngioImage)
                if (_isRecording)
                {
                    //Send Stop Command
                    _tcpClientSingleton.Instance.GetStream().Write(_tcpClientSingleton.stopCommand, 0, _tcpClientSingleton.stopCommand.Length);

                    timerLiveAngioImage.Stop();
                    _isRecording = false;
                }
        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            DeviceStatus.IsLiveView = (RayGetProperty(Property.MotorOnOff) != 0);

            if (PatientCase.PullbackType == null)
                PatientCase.PullbackType = Constants.PullbackTypeShort;

            Brightness = (int) RayGetProperty(Property.Brightness);
            Contrast = (int) RayGetProperty(Property.Contrast);
        }

        private void Back()
        {
            _log.Debug("Back");

            RayStopLiveView();
            leaveToPage(Constants.PatientDetailPage);
        }

        private void ChangeViewMode()
        {
            _log.Debug("ChangeViewMode : " + DeviceStatus.IsLiveView);

            if (DeviceStatus.IsLiveView)
            {
                RayStartLiveView();
            }
            else
            {
                RayStopLiveView();
            }
        }

        private void Calibration()
        {
            _log.Debug("Calibration");

            DeviceStatus.IsLiveView = true;
            ChangeViewMode();

            leaveToPage(Constants.RecordingCalibrationPage);
        }

        private void StartRecording()
        {
            _log.Debug("StartRecording");
            _isRecording = true;

            if (String.IsNullOrEmpty(PatientCase.Procedure))
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Select Procedure"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    
                return;
            }

            leaveToPage(Constants.RecordingPage);
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DrawCrossSectionImage();
        }

        private void leaveToPage(string viewPage)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            PatientCase.Brightness = Brightness;
            PatientCase.Contrast = Contrast;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(viewPage) { Parameter = parameter });
        }

        private void timerFuncLiveAngioImage(object sender, EventArgs e)
        {
            DrawAngioImage();
        }

        protected bool DrawAngioImage()
        {
            AngioImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(_tcpClientSingleton.imgAngio);

            return true;
        }

    }
}
 