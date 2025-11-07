using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using static RaywattOCT.RayCoreWrapper;
using System.Threading.Tasks;

namespace RaywattOCTFFR.ViewModels
{
    /// <summary>
    /// 메인 뷰모델 클래스
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainViewModel));

        private readonly SqlManager _sqlManager;
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
        private bool _isAutoPullbackTest = false;

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

        // to avoid garbage collection
        private CallbackFunction cbFunction;
        public CallbackFunction CBFunction => (this.cbFunction) ?? (this.cbFunction = new CallbackFunction(OnMsgCallback));

        /// <summary>
        /// 생성자
        /// </summary>
        public MainViewModel(SqlManager sqlManager, IDialogService dialogService, IdleMonitorService idleMonitorService)
        {
            _log.Debug("MainViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _idleMonitorService = idleMonitorService;

            // Code 정의
            CodeDefinition codeDefinition = new CodeDefinition(_sqlManager);
            codeDefinition.GetCode();

            //시작 페이지 설정
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
            reviewPages.Add(Constants.ReviewFfrSettingPage);
            reviewPages.Add(Constants.ReviewFfrPage);
            reviewPages.Add(Constants.ReviewPresetPage);
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
                }
            }

            DeviceStatus.PowerOffMsg = _l10n["Shutting down"];
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

            if ((reviewPages.Contains(Constants.CurrentPage) && !reviewPages.Contains(pageUri)))//Review 화면에서 나가는 경우, RayEndReview 호출
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
                if (result.DialogAnswer == DialogResults.Answer.Extra)
                {
                    DeviceStatus.PowerOffMsg = _l10n["Logging out"];
                }
                CommonUtil.Exit(DeviceStatus, result.DialogAnswer == DialogResults.Answer.Yes ? true : false, this.isAdmin);
            }
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
        }

        protected static void handleProgress(RayCallbackRequest request, int progress, int param) { }

        protected void handleError(RayCallbackRequest request, RayError error, int param)
        {
            _log.Debug("error: " + error.ToString());

            Task.Run(() =>
            {
                switch (error)
                {
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
                    break;
                case RayWorkItem.OCTImaging:
                    DeviceStatus.IsOCTImagingDone = true;
                    break;
                case RayWorkItem.GenerateCutView:
                    break;
                case RayWorkItem.DetectLumen:
                    DeviceStatus.IsLumenDetected = true;
                    break;
                case RayWorkItem.GenerateVolume:
                    break;
                default:
                    break;
            }
        }
    }
}
