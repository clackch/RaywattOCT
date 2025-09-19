using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using OpenCvSharp;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    /// <summary>
    /// 메인 뷰모델 클래스
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainViewModel));

        private readonly SqlManager _sqlManager;
        private readonly AngioManager _angioManager;
        private readonly IdleMonitorService _idleMonitorService;
        private IDialogService _dialogService;

        [ObservableProperty]
        private bool _isHome;

        [ObservableProperty]
        private bool _isSetting;

        [ObservableProperty]
        private bool _isExit;

        private bool isAdmin;

        private bool isCatheterFailPopupOpened;

        [ObservableProperty]
        private string _navigationSource;

        [ObservableProperty]
        private object _navigationParameter;

        private List<string> reviewPages;

        private List<string> adminPages;

        [ObservableProperty]
        private PrevStatus? _prevStatus;

        [ObservableProperty]
        private Patient? _patient;

        [ObservableProperty]
        private PatientCase? _patientCase;

        [ObservableProperty]
        private double _catheterProgress;

        [ObservableProperty]
        private bool _isImageAnalysisTest = false;

        [ObservableProperty]
        private string _autoPullbackModel;

        [ObservableProperty]
        private bool _autoPullbackOnOff;

        [ObservableProperty]
        private bool _autoPullbackShowLumenGuide;

        private string _autoPullbackLumenThresholdMin;
        public string AutoPullbackLumenThresholdMin
        {
            get { return _autoPullbackLumenThresholdMin; }
            set
            {
                if (value.Length <= 2)
                {
                    if (!CommonUtil.ValidateNumber(value))
                        return;

                    _autoPullbackLumenThresholdMin = value;
                    OnPropertyChanged(nameof(AutoPullbackLumenThresholdMin));
                }
            }
        }

        private string _autoPullbackLumenThresholdMax;
        public string AutoPullbackLumenThresholdMax
        {
            get { return _autoPullbackLumenThresholdMax; }
            set
            {
                if (value.Length <= 2)
                {
                    if (!CommonUtil.ValidateNumber(value))
                        return;

                    _autoPullbackLumenThresholdMax = value;
                    OnPropertyChanged(nameof(AutoPullbackLumenThresholdMax));
                }
            }
        }

        private string _autoPullbackLumenSnrThreshold;
        public string AutoPullbackLumenSnrThreshold
        {
            get { return _autoPullbackLumenSnrThreshold; }
            set
            {
                if (value.Length <= 3)
                {
                    if (value.Contains("-"))
                        return;

                    if (!CommonUtil.ValidateRealNumber(value))
                    {
                        value = "";
                    }

                    _autoPullbackLumenSnrThreshold = value;
                    OnPropertyChanged(nameof(AutoPullbackLumenSnrThreshold));
                }
            }
        }

        private string _autoPullbackTriggerCandidate;
        public string AutoPullbackTriggerCandidate
        {
            get { return _autoPullbackTriggerCandidate; }
            set
            {
                if (value.Length <= 3)
                {
                    if (!CommonUtil.ValidateNumber(value))
                        return;

                    _autoPullbackTriggerCandidate = value;
                    OnPropertyChanged(nameof(AutoPullbackTriggerCandidate));
                }
            }
        }

        private string _autoPullbackTriggerCount; //0은 Pullback 기능 정지, 1부터 임계치 설정 (Flushing 여부 판단은 DeviceStatus.AutoPullbackOnOff)
        public string AutoPullbackTriggerCount
        {
            get { return _autoPullbackTriggerCount; }
            set
            {
                if (value.Length <= 3)
                {
                    if (!CommonUtil.ValidateNumber(value))
                        return;

                    _autoPullbackTriggerCount = value;
                    OnPropertyChanged(nameof(AutoPullbackTriggerCount));
                }
            }
        }

        private ICommand _homeCommand;
        public ICommand HomeCommand
        {
            get { return this._homeCommand ?? (this._homeCommand = new RelayCommand(Home)); }
        }

        private ICommand _settingCommand;
        public ICommand SettingCommand
        {
            get { return this._settingCommand ?? (this._settingCommand = new RelayCommand(Setting)); }
        }

        private ICommand _exitCommand;
        public ICommand ExitCommand
        {
            get { return this._exitCommand ?? (this._exitCommand = new RelayCommand(Exit)); }
        }

        private ICommand _angioIndicatorCommand;
        public ICommand AngioIndicatorCommand
        {
            get { return this._angioIndicatorCommand ?? (this._angioIndicatorCommand = new RelayCommand(_angioManager.SelectCathRoom)); }
        }

        private ICommand _catheterIndicatorCommand;
        public ICommand CatheterIndicatorCommand
        {
            get { return this._catheterIndicatorCommand ?? (this._catheterIndicatorCommand = new RelayCommand(UnloadCatheter)); }
        }

        //Test
        private ICommand _catheterFailTest;
        public ICommand CatheterFailTestCommmand
        {
            get { return this._catheterFailTest ?? (this._catheterFailTest = new RelayCommand(CatheterFailReceiver)); }
        }

        //Test
        private ICommand _catheterUnlockTest;
        public ICommand CatheterUnlockTestCommmand
        {
            get { return this._catheterUnlockTest ?? (this._catheterUnlockTest = new RelayCommand(CatheterUnlockReceiver)); }
        }

        //Test
        private ICommand _catheterConnectTest;
        public ICommand CatheterConnectTestCommmand
        {
            get { return this._catheterConnectTest ?? (this._catheterConnectTest = new RelayCommand(CatheterConnectReceiver)); }
        }

        //Test
        private ICommand _imageAnalysisTest;
        public ICommand ImageAnalysisTestCommmand
        {
            get { return this._imageAnalysisTest ?? (this._imageAnalysisTest = new RelayCommand(ImageAnalysisTest)); }
        }

        //Test
        private ICommand _compensationTest;
        public ICommand CompensationTestCommand
        {
            get { return this._compensationTest ?? (this._compensationTest = new RelayCommand(CompensationTest)); }
        }

        //Test
        private ICommand _compensationWindowTest;
        public ICommand CompensationWindowTestCommand
        {
            get { return this._compensationWindowTest ?? (this._compensationWindowTest = new RelayCommand(CompensationControlWindowTest)); }
        }

        private Thread threadCompensationWindow;
        private bool showCompensationWindow;

        private ICommand _SaveVTIFileTest;
        public ICommand SaveVTIFileTestCommand
        {
            get { return this._SaveVTIFileTest ?? (this._SaveVTIFileTest = new RelayCommand(SaveVTIFileTest)); }
        }

        //Test
        private ICommand _imageAnalysisReload;
        public ICommand ImageAnalysisReloadCommmand
        {
            get { return this._imageAnalysisReload ?? (this._imageAnalysisReload = new RelayCommand(ImageAnalysisReload)); }
        }

        //Test
        private ICommand _imageAnalysisApply;
        public ICommand ImageAnalysisApplyCommmand
        {
            get { return this._imageAnalysisApply ?? (this._imageAnalysisApply = new RelayCommand(ImageAnalysisApply)); }
        }

        private ICommand _modelSwitch;
        public ICommand ModelSwitchCommmand
        {
            get { return this._modelSwitch ?? (this._modelSwitch = new RelayCommand<string>(ModelSwitch)); }
        }

        // to avoid garbage collection
        private CallbackFunction cbFunction;
        public CallbackFunction CBFunction => (this.cbFunction) ?? (this.cbFunction = new CallbackFunction(OnMsgCallback));

        /// <summary>
        /// 생성자
        /// </summary>
        public MainViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager, IdleMonitorService idleMonitorService)
        {
            _log.Debug("MainViewModel");

            _sqlManager = sqlManager;
            _angioManager = angioManager;
            _dialogService = dialogService;
            _idleMonitorService = idleMonitorService;

            // Code 정의
            CodeDefinition codeDefinition = new CodeDefinition(_sqlManager);
            codeDefinition.GetCode();

            //시작 페이지 설정
            if(CommonUtil.IsRV200())
                NavigationSource = Constants.OutsetLoadingPage;
            else
                NavigationSource = Constants.OutsetLoginPage;

            //네비게이션 메시지 수신 등록
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, OnNavigationMessage);

            RayError result = (RayError)RayRegisterCallback(Marshal.GetFunctionPointerForDelegate(CBFunction));
            if (result != RayError.OK)
            {
                _log.Error("RayRegisterCallback Error");
            }

            Directory.CreateDirectory(Constants.DataRootPath);

            adminPages = new List<string>();
            adminPages.Add(Constants.UserListPage);
            adminPages.Add(Constants.UserNewPage);
            adminPages.Add(Constants.UserEditPage);

            reviewPages = new List<string>();
            reviewPages.Add(Constants.ReviewPage);
            reviewPages.Add(Constants.Review3dPage);
            reviewPages.Add(Constants.ReviewComparePage);
            reviewPages.Add(Constants.ReviewFfrSettingPage);
            reviewPages.Add(Constants.ReviewFfrPage);
            reviewPages.Add(Constants.ReviewPresetPage);
            reviewPages.Add(Constants.ReviewAngioCoRegPage);
            reviewPages.Add(Constants.ReviewLumenEditPage);
            reviewPages.Add(Constants.ReviewCalibrationPage);

            IsHome = true;
            IsSetting = true;
            IsExit = true;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "TestMode";
            IList<Configuration> testMode = _sqlManager.SelectConfiguration(sqlParameters);

            foreach(Configuration config in testMode)
            {
                if(String.IsNullOrEmpty(config.Key) || String.IsNullOrEmpty(config.Buffer))
                    continue;

                if (config.Buffer.Contains(Environment.UserName))
                {
                    DeviceStatus.TestMode.Add(config.Key, "Y".Equals(config.Value) ? true : false);

                    if ("RJ".Equals(config.Key))
                    {
                        result = (RayError)RaySetProperty(Property.TestMode, "Y".Equals(config.Value) ? 1.0f : 0.0f);
                        if (result != RayError.OK)
                        {
                            _log.Error("RaySetProperty Error");
                        }
                    }                        

                    if ("Image".Equals(config.Key))
                    {
                        ImageAnalysisReload();
                    }
                }
            }

            DeviceStatus.PowerOffMsg = _l10n["Shutting down"];
            DeviceStatus.CatheterStatus = Constants.CatheterStatusDisconnected;

            //Auto Pullback Initial Setting
            AutoPullbackLumenThresholdMin = "2";
            AutoPullbackLumenThresholdMax = "50";
            AutoPullbackLumenSnrThreshold = "1.0";
            AutoPullbackTriggerCandidate = "3";
            AutoPullbackTriggerCount = "3";
            AutoPullbackModel = "Lumen";
            ImageAnalysisApply();
        }

        private void OnNavigationMessage(object recipient, NavigationMessage message)
        {
            _log.Debug("OnNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            //순서 중요 - NavigationParameter 먼저 입력 후, NavigationSource 입력 필요
            NavigationParameter = message.Parameter;
            NavigationSource = pageUri;

            if (message.Parameter != null)
            {
                Dictionary<string, object> data = (Dictionary<string, object>)message.Parameter;

                if (data.TryGetValue("prevStatus", out var prevStatusObj) && prevStatusObj is PrevStatus prevStatus)
                    PrevStatus = prevStatus;
                else
                    PrevStatus = null;

                if (data.TryGetValue("patient", out var patientObj) && patientObj is Patient patient)
                    Patient = patient;
                else
                    Patient = null;

                if (data.TryGetValue("patientCase", out var patientCaseObj) && patientCaseObj is PatientCase patientCase)
                    PatientCase = patientCase;
                else
                    PatientCase = null;
            }

            if ((reviewPages.Contains(Constants.CurrentPage) && !reviewPages.Contains(pageUri))//Review 화면에서 나가는 경우, RayEndReview 호출
                || (Constants.CurrentPage == Constants.RecordingConfirmPage && !pageUri.Equals(Constants.ReviewPage)))//Recording(Confirm) 화면에서 나가는 경우, RayEndReview 호출
            {
                RayError result = (RayError)RayEndReview();
                if (result != RayError.OK)
                {
                    _log.Error("RayEndReview Error");
                }
            }

            IsHome = false;
            IsSetting = false;
            IsExit = false;
            this.isAdmin = false;

            if (NavigationSource == Constants.PatientListPage || (NavigationSource == "Refresh" && Constants.CurrentPage == Constants.PatientListPage))
                IsHome = true;

            if (adminPages.Contains(NavigationSource))
            {
                IsHome = true;
                IsSetting = true;
                this.isAdmin = true;
            }

            if (Constants.OutsetLoadingPage.Equals(NavigationSource) || Constants.OutsetLoginPage.Equals(NavigationSource) || Constants.InitialPasswordSetupPage.Equals(NavigationSource) || Constants.PasswordExpiryCheckPage.Equals(NavigationSource))
            {
                IsHome = true;
                IsSetting = true;
                IsExit = true;
                this.isAdmin = true;
            }
        }

        private void Home()
        {
            _log.Debug("Home");

            DeviceStatus.IsOCTImagingDone = true;

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
        }

        private void Setting()
        {
            _log.Debug("Setting");
            var result = _dialogService.OpenDialog(new SettingDialogControl(), null, Constants.ApplicationWidth, Constants.ApplicationHeight);
        }

        private void Exit()
        {
            _log.Debug("Exit");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Power Off"];
            parameter["message"] = _l10n["Choose one of the power off options"];
            
            var result = _dialogService.OpenDialog(new PowerOffDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer != DialogResults.Answer.No)
            {
                if (threadCompensationWindow != null)
                {
                    showCompensationWindow = false;
                    threadCompensationWindow.Join();
                }

                if (result.DialogAnswer == DialogResults.Answer.Extra)
                {
                    DeviceStatus.PowerOffMsg = _l10n["Logging out"];
                }
                CommonUtil.Exit(DeviceStatus, _angioManager, result.DialogAnswer == DialogResults.Answer.Yes ? true : false, this.isAdmin);
            }
        }

        private void UnloadCatheter()
        {
            _log.Debug("UnloadCatheter");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            DialogResults? result = null;

            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Confirm unloading of the catheter"];
            result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                CatheterUnlockReceiver();
            }            
        }

        private void LeaveFromRecording()
        {
            List<string> recordingPages = new List<string>();
            recordingPages.Add(Constants.RecordingLiveViewPage);
            recordingPages.Add(Constants.RecordingCalibrationPage);
            recordingPages.Add(Constants.RecordingPage);

            if (recordingPages.Contains(Constants.CurrentPage))
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                parameter["patientCase"] = PatientCase;
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingSetupPage) { Parameter = parameter });
            }
        }

        private void InitCatheterTimer()
        {
            double rotationTime = RayGetProperty(Property.LoadCatheterTime);
            timer.Interval = TimeSpan.FromMilliseconds(rotationTime / (100 / catheterProgressStep));
            timer.Tick += new EventHandler(ProgressLoadTest);
            timerUnload.Interval = TimeSpan.FromMilliseconds(rotationTime / (100 / catheterProgressStep) / 2);
            timerUnload.Tick += new EventHandler(ProgressUnloadTest);
        }

        private void CatheterFailReceiver()
        {
            _log.Debug("CatheterFailReceiver");

            if (this.isCatheterFailPopupOpened)
                return;

            this.isCatheterFailPopupOpened = true;

            DeviceStatus.CatheterStatus = Constants.CatheterStatusFailed;//Fail Receive

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Error"];
                parameter["message"] = _l10n["$MSG007"];
                parameter["error"] = true;
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Undefined)
                {
                    parameter.Clear();
                    parameter["patient"] = Patient;
                    parameter["patientCase"] = PatientCase;
                    parameter["prevStatus"] = PrevStatus;
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingCatheterFailPage) { Parameter = parameter });

                    this.isCatheterFailPopupOpened = false;
                }
            });
        }

        private void CatheterUnlockReceiver() 
        {
            _log.Debug("CatheterUnlockReceiver");

            DeviceStatus.CatheterStatus = Constants.CatheterStatusUnloading;

            LeaveFromRecording();

            RayError result = (RayError)RayUnloadCatheter();
            if (result != RayError.OK)
            {
                _log.Error("RayUnloadCatheter Error");
            }
        }

        private DispatcherTimer timerUnload = new DispatcherTimer();
        private void ProgressUnloadTest(object sender, EventArgs e)
        {
            if(DeviceStatus.CatheterStatus == Constants.CatheterStatusConnected)
            {
                timerUnload.Stop();
            }

            CatheterProgress -= catheterProgressStep;
        }


        private void CatheterConnectReceiver()
        {
            _log.Debug("CatheterConnectReceiver");
            DeviceStatus.CatheterStatus = Constants.CatheterStatusLoading;    // Micro-limit switch on

            RayError result = (RayError)RayLoadCatheter();
            if (result != RayError.OK)
            {
                _log.Error("RayLoadCatheter Error");
            }
        }

        private void ImageAnalysisTest()
        {
            _log.Debug("ImageAnalysisTest");

            IsImageAnalysisTest = !IsImageAnalysisTest;
        }

        private void CompensationTest()
        {
            _log.Debug("CompensationTest");

            bool bImageCompensation = (bool)(RayGetProperty(Property.ImageCompensation) != 0);

            RayError result = (RayError)RaySetProperty(Property.ImageCompensation, bImageCompensation ? 0 : 1);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }
        }

        private void CompensationControlWindowTest()
        {
            _log.Debug("CompensationControlWindowTest");

            if (threadCompensationWindow == null)
            {
                threadCompensationWindow = new Thread(() => ThreadCompensationWindow(this));
                threadCompensationWindow.Start();
            }
            else {
                showCompensationWindow = false;
                threadCompensationWindow.Join();
                threadCompensationWindow = null;
            }
        }

        private static void ThreadCompensationWindow(MainViewModel model)
        {
            _log.Debug("ThreadCompensationWindow");

            model.showCompensationWindow = true;
            RayError result = (RayError)RaySetProperty(Property.ImageCompensationControlWindow, 1);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }

            while (model.showCompensationWindow) 
            {
                Cv2.WaitKey(1);
            }

            result = (RayError)RaySetProperty(Property.ImageCompensationControlWindow, 0);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }

            _log.Debug("ThreadCompensationWindow done.");
        }

        private void SaveVTIFileTest()
        {
            _log.Debug("SaveVTIFileTest");

            CommonUtil.IsVTIFileSave = !CommonUtil.IsVTIFileSave;
            if(CommonUtil.IsVTIFileSave)
                _log.Debug("SaveVTIFileTest True");
            else
            {
                _log.Debug("SaveVTIFileTest False");
            }
        }

        private void ImageAnalysisReload()
        {
            _log.Debug("ImageAnalysisReload");

            AutoPullbackOnOff = DeviceStatus.AutoPullbackOnOff;
            if (DeviceStatus.AutoPullbackModel)
            {
                AutoPullbackModel = "Lumen";
            }
            else
            {
                AutoPullbackModel = "Flush";
            }
            AutoPullbackShowLumenGuide = (RayGetProperty(Property.ShowLumenGuide) == 1.0) ? true : false;
            AutoPullbackLumenThresholdMin = (RayGetProperty(Property.LumenThresholdMin) * 100).ToString();
            AutoPullbackLumenThresholdMax = (RayGetProperty(Property.LumenThresholdMax) * 100).ToString();
            AutoPullbackLumenSnrThreshold = RayGetProperty(Property.LumenSnrThreshold).ToString();
            AutoPullbackTriggerCandidate = DeviceStatus.AutoPullbackTriggerCandidate.ToString();
            AutoPullbackTriggerCount = DeviceStatus.AutoPullbackTriggerCount.ToString();
        }
        
        private void ImageAnalysisApply()
        {
            _log.Debug("ImageAnalysisApply");

            if (String.IsNullOrWhiteSpace(AutoPullbackLumenThresholdMin))
                AutoPullbackLumenThresholdMin = "0";
            if (String.IsNullOrWhiteSpace(AutoPullbackLumenThresholdMax))
                AutoPullbackLumenThresholdMax = "0";
            if (String.IsNullOrWhiteSpace(AutoPullbackLumenSnrThreshold))
                AutoPullbackLumenSnrThreshold = "0";
            if (String.IsNullOrWhiteSpace(AutoPullbackTriggerCandidate))
                AutoPullbackTriggerCandidate = "0";
            if (String.IsNullOrWhiteSpace(AutoPullbackTriggerCount))
                AutoPullbackTriggerCount = "0";

            DeviceStatus.AutoPullbackOnOff = AutoPullbackOnOff;
            if ("Lumen".Equals(AutoPullbackModel))
            {
                RaySetProperty(Property.AutoPullback, AutoPullbackOnOff ? 1.0 : 0.0);
                DeviceStatus.AutoPullbackModel = true;
            }
            else
            {
                RaySetProperty(Property.AutoPullback, 0.0);
                DeviceStatus.AutoPullbackModel = false;
            }
             
            RaySetProperty(Property.ShowLumenGuide, AutoPullbackShowLumenGuide ? 1.0 : 0.0);
            RaySetProperty(Property.LumenThresholdMin, double.Parse(AutoPullbackLumenThresholdMin) / 100);
            RaySetProperty(Property.LumenThresholdMax, double.Parse(AutoPullbackLumenThresholdMax) / 100);
            RaySetProperty(Property.LumenSnrThreshold, double.Parse(AutoPullbackLumenSnrThreshold));
            DeviceStatus.AutoPullbackTriggerCandidate = int.Parse(AutoPullbackTriggerCandidate);
            DeviceStatus.AutoPullbackTriggerCount = int.Parse(AutoPullbackTriggerCount);
        }

        private void ModelSwitch(string model)
        {
            _log.Debug("ModelSwitch : " + model);

            if ("LUMEN".Equals(model))
            {
                AutoPullbackModel = "Lumen";
            }
            else
            {
                AutoPullbackModel = "Flush";
            }
        }

        //Test
        private double catheterProgressStep = 10;
        private DispatcherTimer timer = new DispatcherTimer();
        private void ProgressLoadTest(object sender, EventArgs e)
        {
            if (DeviceStatus.CatheterStatus == Constants.CatheterStatusEnable)
            {
                timer.Stop();
            }

            CatheterProgress += catheterProgressStep;
        }

        private void OnMsgCallback(int request, int response, int param)
        {
            switch ((RayCallbackRequest)request)
            {
                case RayCallbackRequest.State:
                    handleState((RayCallbackRequest)request, (RayScannerState)response, param);
                    break;
                case RayCallbackRequest.ProgressSave:
                    handleProgress((RayCallbackRequest)request, response, param);
                    break;
                case RayCallbackRequest.Event:
                    handleEvent((RayCallbackRequest)request, (RayEvent)response, param);
                    break;
                case RayCallbackRequest.Error:
                    handleError((RayCallbackRequest)request, (RayError)response, param);
                    break;
                case RayCallbackRequest.WorkDone:
                    handleWorkDone((RayCallbackRequest)request, (RayWorkItem)response, param);
                    break;
                default:
                    break;
            }
        }

        private void handleState(RayCallbackRequest request, RayScannerState state, int param)
        {
            _log.Debug("state: " + state.ToString());
            RayScannerState curState = (RayScannerState)RayGetProperty(Property.CurrentState);
            DeviceStatus.IsLiveView = (bool)(RayGetProperty(Property.MotorOnOff) != 0);
        }

        protected static void handleProgress(RayCallbackRequest request, int progress, int param) { }

        protected void handleError(RayCallbackRequest request, RayError error, int param)
        {
            _log.Debug("error: " + error.ToString());

            Task.Run(() =>
            {
                switch (error)
                {
                    case RayError.CatheterNotValid:
                        CatheterFailReceiver();
                        break;
                    case RayError.HomingFailed:
                        DeviceStatus.CatheterStatus = Constants.CatheterStatusFailed;
                        break;
                    case RayError.RotaryJunctionError:
                        DeviceStatus.CatheterStatus = Constants.CatheterStatusFailed;
                        break;
                    default:
                        break;
                }
            });
        }

        protected void handleEvent(RayCallbackRequest request, RayEvent e, int param)
        {
            _log.Debug("event: " + e.ToString());
            switch (e)
            {
                case RayEvent.CatheterConnected:
                    CatheterConnectReceiver();
                    break;
                case RayEvent.CatheterLoading:
                    //Test
                    DeviceStatus.CatheterStatus = Constants.CatheterStatusLoading;
                    CatheterProgress = 0;
                    if(!timer.IsEnabled)
                        timer.Start();
                    break;
                case RayEvent.CatheterUnloading:
                    //Test
                    DeviceStatus.CatheterStatus = Constants.CatheterStatusUnloading;
                    CatheterProgress = 100;
                    if(!timerUnload.IsEnabled)
                        timerUnload.Start();
                    LeaveFromRecording();
                    break;
                default:
                    break;
            }
        }

        protected void handleWorkDone(RayCallbackRequest request, RayWorkItem work, int param)
        {
            _log.Debug("workItem - " + work.ToString());
            switch (work)
            {
                case RayWorkItem.StartService:
                    DeviceStatus.IsServiceStarted = true;
                    InitCatheterTimer();
                    break;
                case RayWorkItem.AutoCalibration:
                    DeviceStatus.CanExecuteCalibration = true;
                    RayError result = (RayError)RayGetAutoCalibResult();
                    if (result == RayError.AutoCalibError)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            Dictionary<string, object> parameter = new Dictionary<string, object>();
                            parameter["title"] = _l10n["Auto calibration"];
                            parameter["message"] = _l10n["Auto calibration might be failed.\nCheck the image please."];
                            var resultOpenDialog = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.AlertDialogWidth, Constants.AlertDialogHeight);
                        });
                        _log.Error("AutoCalibration Error");
                    }else
                    {
                        _log.Debug("AutoCalibration Success");
                    }
                    break;
                case RayWorkItem.LoadCatheter:
                    DeviceStatus.CatheterStatus = Constants.CatheterStatusLoaded;
                    break;
                case RayWorkItem.UnloadCatheter:
                    DeviceStatus.CatheterStatus = Constants.CatheterStatusConnected;
                    break;
                case RayWorkItem.EnableCatheter:
                    DeviceStatus.CatheterStatus = Constants.CatheterStatusEnable;
                    break;
                case RayWorkItem.Recording:
                    if (DeviceStatus.IsAngioConnected)
                    {
                        _angioManager.StopGettingAngioImageThread();
                    }
                    break;
                case RayWorkItem.Pullback:
                    DeviceStatus.IsPullbackDone = true;                                        
                    break;
                case RayWorkItem.OCTImaging:
                    if(param == (int)RaySession.Review)
                        DeviceStatus.IsOCTImagingDone = true;
                    else
                        DeviceStatus.IsOCTImagingCompareDone = true;
                    break;
                case RayWorkItem.GenerateCutView:
                    break;
                case RayWorkItem.DetectLumen:
                    DeviceStatus.IsLumenDetected = true;
                    break;
                case RayWorkItem.GenerateVolume:
                    break;
                case RayWorkItem.SaveRawData:
                    DeviceStatus.IsSaveRawDataDone = true;
                    break;
                case RayWorkItem.CleanRotaryJunction:
                    DeviceStatus.IsCleaningDone = true;
                    break;
                default:
                    break;
            }
        }
    }
}
