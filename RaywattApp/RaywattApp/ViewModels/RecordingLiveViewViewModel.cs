using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using OpenCvSharp;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

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

        private bool isStartRecording = false;

        private bool isMoveCalibration = false;

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
            set { _brightness = value; OnPropertyChanged(nameof(Brightness)); RaySetProperty(Property.Brightness, value); }
        }

        private int _contrast;
        public int Contrast
        {
            get { return _contrast; }
            set { _contrast = value; OnPropertyChanged(nameof(Contrast)); RaySetProperty(Property.Contrast, value); }
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

        private WriteableBitmap? _cachedBitmap;
        private int _cachedStride;

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
                _angioManager.ImgAngio = _angioManager.ShowNoSignal();
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

                RayShowCalibrationGuide(true);

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
                RaySetProperty(Property.PullbackDistance, Double.Parse(PbLength));
                RaySetProperty(Property.PullbackSpeed, Double.Parse(PbSpeed));

                if (!ViewModelBase._deviceStatus.IsAngioInitialized && DeviceStatus.IsAngioConnected && (DeviceStatus.SelectedCathRoom == null || DeviceStatus.SelectedCathRoom.Id != -1))
                {
                    _angioManager.SelectCathRoom();
                }
            }


            if (ViewModelBase._deviceStatus.IsAngioInitialized && !_angioManager.ReadyToRecv)
            {
                _angioManager.SendCommandPacket(CommandType.FGStarted);
                _angioManager.ToggleLive(true);
            }
            _angioManager.ReadyToRecv = true;
            _angioManager.ImgAngio = _angioManager.ShowNoSignal();
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

            if (!this.isStartRecording && !this.isMoveCalibration)
                RayStopLiveView();
        }

        private void Back()
        {
            _log.Debug("Back");

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
                RayStartLiveView();
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
            var sw = Stopwatch.StartNew();

            if (!DeviceStatus.IsAngioConnected)
            {
                IsOctExpanded = true;
            }

            sw.Restart();
            DrawCrossSectionImage();
            Console.WriteLine($"[Timer] DrawCrossSectionImage(): {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            DrawAngioImage();
            Console.WriteLine($"[Timer] DrawAngioImage(): {sw.ElapsedMilliseconds}ms");
        }


        private void leaveToPage(string viewPage)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            PatientCase.Brightness = Brightness;
            PatientCase.Contrast = Contrast;
            PatientCase.FieldOfView = FieldOfView;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(viewPage) { Parameter = parameter });
        }

        private void EnsureWriteableBitmap(Mat mat)
        {
            PixelFormat format = PixelFormats.Bgr24;
            int bytesPerPixel = (mat.Channels() * mat.ElemSize1());
            int stride = mat.Width * bytesPerPixel;

            if (_cachedBitmap == null || _cachedBitmap.PixelWidth != mat.Width || _cachedBitmap.PixelHeight != mat.Height)
            {
                _cachedStride = stride;
                _cachedBitmap = new WriteableBitmap(mat.Width, mat.Height, 96, 96, format, null);
            }
        }

        private void UpdateAngioImage(Mat mat)
        {
            EnsureWriteableBitmap(mat);

            _cachedBitmap!.WritePixels(new Int32Rect(0, 0, mat.Width, mat.Height), mat.Data, _cachedStride * mat.Height, _cachedStride);

            AngioImage = _cachedBitmap;
        }

        private void DrawAngioImage()
        {
            //AngioImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(_angioManager.ImgAngio);
            UpdateAngioImage(_angioManager.ImgAngio);
        }
    }
}
