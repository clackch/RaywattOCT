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

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Dictionary<string, string> _pullbackList;

        [ObservableProperty]
        private string _pbLength;

        [ObservableProperty]
        private string _pbSpeed;

        [ObservableProperty]
        private string _pbTime;

        private string _selectedPullbackType;
        public string SelectedPullbackType
        {
            get { return _selectedPullbackType; }
            set { _selectedPullbackType = value; SetPullback(); }
        }

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
            PullbackList = CodeDefinition.Codes["PBTY"];

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);

            sqlParameters.Clear();
            sqlParameters["classification"] = "Present";
            IList<Configuration> presents = _sqlManager.SelectConfiguration(sqlParameters);
            if (presents != null && presents.Count > 0)
            {
                Brightness = int.Parse(presents.FirstOrDefault(x => x.Key == "brightness").Value);
                Contrast = int.Parse(presents.FirstOrDefault(x => x.Key == "contrast").Value);
            }
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
                {
                    PatientCase = (PatientCase)data["patientCase"];
                    SelectedPullbackType = PatientCase.PullbackType;
                    Brightness = PatientCase.Brightness;
                    Contrast = PatientCase.Contrast;
                }
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
            if (!_angioManager.isLiveView &&DeviceStatus.IsAngioConnected)
            {
                _angioManager.SendCommandPacket(CommandType.FGStarted);
                _angioManager.isLiveView = true;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();

            //Send Stop Command
            if (_angioManager.isLiveView && DeviceStatus.IsAngioConnected)
            {
                _angioManager.SendCommandPacket(CommandType.FGStopped);
                _angioManager.isLiveView= false;
            }
        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            DeviceStatus.IsLiveView = (RayGetProperty(Property.MotorOnOff) != 0);

            if (PatientCase.Procedure == null)
                PatientCase.Procedure = "$001";
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

            if (String.IsNullOrEmpty(PatientCase.PullbackType))
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Select Pullback"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                return;
            }

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

        private void SetPullback()
        {
            if (SelectedPullbackType == null) return;

            PatientCase.PullbackType = SelectedPullbackType;
            PatientCase.PullbackLength = SelectedPullbackType.IndexOf("LO") > 0 ? Constants.PullbackLengthLong : Constants.PullbackLengthShort;

            Code pullback = pullbackTypes.FirstOrDefault(x => x.Key == SelectedPullbackType);

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
    }
}
