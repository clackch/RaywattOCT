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
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;
using RaywattApp.Common.Angio;

namespace RaywattApp.ViewModels
{
    public partial class RecordingLiveViewViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingLiveViewViewModel));

        private readonly SqlManager? _sqlManager;

        private IDialogService? _dialogService;
               
        private readonly AngioManager _angioManager;

        private IList<Code> pullbackTypes;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer(DispatcherPriority.Render);

        private bool isStartRecording;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private string _pbLength;

        [ObservableProperty]
        private string _pbSpeed;

        [ObservableProperty]
        private string _pbTime;

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

        public RecordingLiveViewViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager)
        {
            _log.Debug("RecordingLiveViewViewModel");

            Constants.CurrentPage = Constants.RecordingLiveViewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            _angioManager = angioManager;

            isStartRecording = false;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);
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
                Brightness = PatientCase.Brightness;
                Contrast = PatientCase.Contrast;                               

                RayShowCalibrationGuide(true);

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();

                DeviceStatus.IsLiveView = (RayGetProperty(Property.MotorOnOff) != 0);

                Code pullback = pullbackTypes.FirstOrDefault(x => x.Key == PatientCase.PullbackType);
                if (pullback != null)
                {
                    string[] temp = pullback.Buffer1.Split("|");
                    PbLength = temp[0];
                    PbSpeed = temp[1];
                    PbTime = temp[2];
                }
                RaySetProperty(Property.PullbackDistance, Double.Parse(PbLength));
                RaySetProperty(Property.PullbackSpeed, Double.Parse(PbSpeed));
            }

            SelectCathRoom(); // 이전으로 돌아오는 경우 제외

            // Send Start Command
            if (!_angioManager.readyToRecv && DeviceStatus.IsAngioConnected)
            {
                _angioManager.SendCommandPacket(CommandType.FGStarted);
            }
            _angioManager.readyToRecv = true;
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();

            if (!isStartRecording && _angioManager.readyToRecv && DeviceStatus.IsAngioConnected)
            {
                _angioManager.SendCommandPacket(CommandType.FGStopped);
                _angioManager.readyToRecv = false;
            }
        }

        private void Back()
        {
            _log.Debug("Back");
            
            _angioManager.SendCommandPacket(CommandType.FGStopped);
            _angioManager.readyToRecv = false;

            RayStopLiveView();
            leaveToPage(Constants.RecordingPresetPage);
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

            isStartRecording = true;
            
            if (!DeviceStatus.IsLiveView)
            {
                RayStartLiveView();
            }

            leaveToPage(Constants.RecordingPage);
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
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            PatientCase.Brightness = Brightness;
            PatientCase.Contrast = Contrast;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(viewPage) { Parameter = parameter });
        }

        private bool DrawAngioImage()
        {
            AngioImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(_angioManager.ImgAngio);

            return true;
        }

        private void SelectCathRoom()
        {
            _log.Debug("SelectCathRoom");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedCathRoomId"] = DeviceStatus.SelectedCathRoom == null ? 0 : DeviceStatus.SelectedCathRoom.Id;

            var result = _dialogService.OpenDialog(new CathRoomDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                DeviceStatus.SelectedCathRoom = (CathRoom)data["selectedCathRoom"];
            }
        }
    }
}
