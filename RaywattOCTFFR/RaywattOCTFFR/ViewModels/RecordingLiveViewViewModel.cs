using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Angio;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;
using System.IO;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.ViewModels
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

        private bool isMoveCalibration;

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

        [ObservableProperty]
        private bool _isOctExpanded = true;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        [ObservableProperty]
        private Zoom _zoomSmall = new Zoom(Constants.SmallCrossSectionSize);

        [ObservableProperty]
        private bool _isExpandButtonVisible = false;

        private int _brightness;
        public int Brightness
        {
            get { return _brightness; }
            set 
            { 
                _brightness = value; 
                OnPropertyChanged(nameof(Brightness));
                RayError result = (RayError)RaySetProperty(Property.Brightness, value);
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }
            }
        }

        private int _contrast;
        public int Contrast
        {
            get { return _contrast; }
            set 
            { 
                _contrast = value; 
                OnPropertyChanged(nameof(Contrast));
                RayError result = (RayError)RaySetProperty(Property.Contrast, value);
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }
            }
        }

        private string _colormap;
        public string Colormap
        {
            get { return _colormap; }
            set
            {
                _colormap = value;
                CommonUtil.SetColormap(_colormap);
                OnPropertyChanged(nameof(Colormap));                
            }
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

        private double _fieldOfView;
        public double FieldOfView
        {
            get { return _fieldOfView; }
            set
            {
                _fieldOfView = value;
                OnPropertyChanged(nameof(FieldOfView));

                PatientCase.FieldOfView = value;
                Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);
                ZoomSmall.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);
            }
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

        private ICommand _screenExpandCommand;
        public ICommand ScreenExpandCommand
        {
            get { return _screenExpandCommand ?? (this._screenExpandCommand = new RelayCommand(ScreenExpand)); }
        }

        public RecordingLiveViewViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager)
        {
            _log.Debug("RecordingLiveViewViewModel");

            Constants.CurrentPage = Constants.RecordingLiveViewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            _angioManager = angioManager;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);
            _angioManager.OnAngioAvailabilityChanged = UpdateAngioAvailabilityUI;
        }

        private void UpdateAngioAvailabilityUI()
        {
            bool isAngioConnected = DeviceStatus.IsAngioConnected;
            var cathRoom = DeviceStatus.SelectedCathRoom;
            bool isCathRoomSelected = cathRoom != null && cathRoom.Name != "Not Selected";

            if (isAngioConnected && isCathRoomSelected)
            {
                IsExpandButtonVisible = true;
            }
            else
            {
                _angioManager.ImgAngio = AngioManager.ShowNoSignal();
                IsExpandButtonVisible = false;
            }
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");
            UpdateAngioAvailabilityUI();

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];
                PatientCase = (PatientCase)data["patientCase"];
                Brightness = PatientCase.Brightness;
                Contrast = PatientCase.Contrast;
                FieldOfView = PatientCase.FieldOfView;
                Colormap = PatientCase.Colormap;

                if (PatientCase.ImageFullPath != null && PatientCase.Image != null)
                {
                    string path = PatientCase.ImageFullPath.Substring(0, PatientCase.ImageFullPath.Length - 42);
                    string fileName = PatientCase.Image.Substring(0, PatientCase.Image.Length - 3) + "*";
                    string[] files = Directory.GetFiles(path, fileName);

                    foreach (string file in files)
                    {
                        System.IO.File.Delete(file);
                    }
                }

                RayError result = (RayError)RayShowCalibrationGuide(true);
                if (result != RayError.OK)
                {
                    _log.Error("RayShowCalibrationGuide Error");
                }

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();

                _log.Debug($"Before DeviceStatus.IsLiveView = (RayGetProperty(Property.MotorOnOff) != 0) :{DeviceStatus.IsLiveView}");
                DeviceStatus.IsLiveView = (RayGetProperty(Property.MotorOnOff) != 0);
                _log.Debug($"After DeviceStatus.IsLiveView = (RayGetProperty(Property.MotorOnOff) != 0) :{DeviceStatus.IsLiveView}");

                Code pullback = pullbackTypes.FirstOrDefault(x => x.Key == PatientCase.PullbackType);
                if (pullback != null)
                {
                    string[] temp = pullback.Buffer1.Split("|");
                    PbLength = temp[0];
                    PbSpeed = temp[1];
                    PbTime = temp[2];
                }
                result = (RayError)RaySetProperty(Property.PullbackDistance, Double.Parse(PbLength));
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }
                result = (RayError)RaySetProperty(Property.PullbackSpeed, Double.Parse(PbSpeed));
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }

                if (!ViewModelBase.DeviceStatus.IsAngioInitialized && DeviceStatus.IsAngioConnected && (DeviceStatus.SelectedCathRoom == null || DeviceStatus.SelectedCathRoom.Id != -1))
                {
                    _angioManager.SelectCathRoom();
                }
            }


            if (ViewModelBase.DeviceStatus.IsAngioInitialized && !_angioManager.ReadyToRecv)
            {
                _angioManager.SendCommandPacket(CommandType.FGStarted);
                _angioManager.ToggleLive(true);
            }
            _angioManager.ReadyToRecv = true;
            _angioManager.ImgAngio = AngioManager.ShowNoSignal();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
            UpdateAngioAvailabilityUI();

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();

            if (!this.isStartRecording && _angioManager.ReadyToRecv && DeviceStatus.IsAngioConnected)
            {
                _angioManager.SendCommandPacket(CommandType.FGStopped);
                _angioManager.ReadyToRecv = false;
                _angioManager.ToggleLive(false);
            }

            if(!this.isStartRecording && !this.isMoveCalibration)
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

            leaveToPage(Constants.RecordingPresetPage);
        }

        private void ChangeViewMode()
        {
            _log.Debug("ChangeViewMode : " + DeviceStatus.IsLiveView);

            RayError result;

            if (DeviceStatus.IsLiveView)
            {
                result = (RayError)RayStartLiveView();
                if (result != RayError.OK)
                {
                    _log.Error("RayStartLiveView Error");
                }
            }
            else
            {
                result = (RayError)RayStopLiveView();
                if (result != RayError.OK)
                {
                    _log.Error("RayStopLiveView Error");
                }
            }
        }

        private void Calibration()
        {
            _log.Debug("Calibration");

            this.isMoveCalibration = true;

            DeviceStatus.IsLiveView = true;
            ChangeViewMode();

            leaveToPage(Constants.RecordingCalibrationPage);
        }

        private void StartRecording()
        {
            _log.Debug("StartRecording");

            this.isStartRecording = true;

            if (!DeviceStatus.IsLiveView)
            {
                RayError result = (RayError)RayStartLiveView();
                if (result != RayError.OK)
                {
                    _log.Error("RayStartLiveView Error");
                }
            }

            leaveToPage(Constants.RecordingPage);
        }

        private void ScreenExpand()
        {
            _log.Debug("ScreenExpand : " + IsOctExpanded);

            IsOctExpanded = !IsOctExpanded;
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (!DeviceStatus.IsAngioConnected)
            {
                IsOctExpanded = true;
            }
            DrawCrossSectionImage();
            DrawAngioImage();
        }

        private void leaveToPage(string viewPage)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            PatientCase.Brightness = Brightness;
            PatientCase.Contrast = Contrast;
            PatientCase.FieldOfView = FieldOfView;
            PatientCase.Colormap = Colormap;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(viewPage) { Parameter = parameter });
        }

        private void DrawAngioImage()
        {
            AngioImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(_angioManager.ImgAngio);
        }

        private void ManualZoomIn(bool zoomIn)
        {
            _log.Debug("ManualZoomIn : " + ((zoomIn) ? "IN" : "OUT"));

            RayError result = (RayError)RayManualCalibration(zoomIn);
            if (result != RayError.OK && result != RayError.DeviceBusy)
            {
                _log.Error("RayManualCalibration Error : " + result);
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
    }
}
