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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
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

        private IDialogService _dialogService;

        private IList<BusyMessage> _busys = new List<BusyMessage>();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private Visibility _isHome;

        [ObservableProperty]
        private string _navigationSource;

        [ObservableProperty]
        private object _navigationParameter;

        private List<string> reviewPages;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

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

        //Test
        private ICommand _catheterFailTest;
        public ICommand CatheterFailTestCommmand
        {
            get { return this._catheterFailTest ?? (this._catheterFailTest = new RelayCommand(CatheterFailReceiver)); }
        }

        // to avoid garbage collection
        private CallbackFunction cbFunction;
        public CallbackFunction CBFunction => (this.cbFunction) ?? (this.cbFunction = new CallbackFunction(OnMsgCallback));

        /// <summary>
        /// 생성자
        /// </summary>
        public MainViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("MainViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            // Code 정의
            CodeDefinition codeDefinition = new CodeDefinition(_sqlManager);
            codeDefinition.GetCode();

            //시작 페이지 설정
            NavigationSource = Constants.PatientListPage;

            //네비게이션 메시지 수신 등록
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, OnNavigationMessage);

            //BusyMessage 수신 등록
            WeakReferenceMessenger.Default.Register<BusyMessage>(this, OnBusyMessage);

            RayRegisterCallback(Marshal.GetFunctionPointerForDelegate(CBFunction));
            RayStartSystem();
            RayConnectDevices();

            Directory.CreateDirectory(Constants.DataRootPath);

            reviewPages = new List<string>();
            reviewPages.Add(Constants.ReviewPage);
            reviewPages.Add(Constants.Review3dPage);
            reviewPages.Add(Constants.ReviewComparePage);
            reviewPages.Add(Constants.ReviewFfrPage);
            reviewPages.Add(Constants.ReviewPresetPage);
            reviewPages.Add(Constants.ReviewAngioCoRegPage);

            IsHome = Visibility.Hidden;
        }

        private void OnNavigationMessage(object recipient, NavigationMessage message)
        {
            _log.Debug("OnNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            //순서 중요 - NavigationParameter 먼저 입력 후, NavigationSource 입력 필요
            NavigationParameter = message.Parameter;
            NavigationSource = pageUri;

            if(message.Parameter != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)message.Parameter;
                if(data.ContainsKey("prevStatus"))
                    PrevStatus = (PrevStatus)data["prevStatus"];
                if (data.ContainsKey("patient"))
                    Patient = (Patient)data["patient"];
                if (data.ContainsKey("patientCase"))
                    PatientCase = (PatientCase)data["patientCase"];
            }

            //Review 화면에서 나가는 경우, RayEndReivew 호출
            if (reviewPages.Contains(Constants.CurrentPage))
            {
                if (!reviewPages.Contains(pageUri))
                    RayEndReview();
            }
            //Recording(Confirm) 화면에서 나가는 경우, RayEndReivew 호출
            if (Constants.CurrentPage == Constants.RecordingPage)
            {
                if (!pageUri.Equals(Constants.ReviewPresetPage))
                    RayEndReview();
            }

            if (NavigationSource == Constants.PatientListPage || Constants.CurrentPage == Constants.PatientListPage)
                IsHome = Visibility.Hidden;
            else
                IsHome = Visibility.Visible;
        }

        private void OnBusyMessage(object recipient, BusyMessage message)
        {
            _log.Debug("OnBusyMessage : " + message.Value);

            if (message.Value)
            {
                var existBusy = _busys.FirstOrDefault(b => b.BusyId == message.BusyId);
                if (existBusy != null)
                {
                    //이미 추가된 녀석이기 때문에 추가하지 않음
                    return;
                }
                _busys.Add(message);
            }
            else
            {
                var existBusy = _busys.FirstOrDefault(b => b.BusyId == message.BusyId);
                if (existBusy == null)
                {
                    //없기 때문에 나감
                    return;
                }
                _busys.Remove(existBusy);
            }
            //_busys에 아이템이 있으면 true, 없으면 false
            IsBusy = _busys.Any();
        }

        private void Home()
        {
            _log.Debug("Home");

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

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));

            Application.Current.MainWindow.Close();
        }

        private void CatheterFailReceiver()
        {
            _log.Debug("CatheterFailReceiver");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["The imaging catheter has failed. It must be removed from the patient before continuing."];
            parameter["error"] = true;
            var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Undefined)
            {
                DeviceStatus.CatheterStatus = Constants.CatheterStatusRemove;
                parameter.Clear();
                parameter["patient"] = Patient;
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingCatheterFailPage) { Parameter = parameter });
            }

        }

        private void OnMsgCallback(int request, int response)
        {
            switch ((RayCallbackRequest)request)
            {
                case RayCallbackRequest.State:
                    handleState((RayCallbackRequest)request, (RayScannerState)response);
                    break;
                case RayCallbackRequest.Progress:
                    handleProgress((RayCallbackRequest)request, response);
                    break;
                case RayCallbackRequest.Error:
                    handleError((RayCallbackRequest)request, (RayError)response);
                    break;
                case RayCallbackRequest.WorkDone:
                    handleWorkDone((RayCallbackRequest)request, (RayWorkItem)response);
                    break;
                default:
                    break;
            }
        }

        private void handleState(RayCallbackRequest request, RayScannerState state)
        {
            RayScannerState curState = (RayScannerState)RayGetProperty(Property.CurrentState);
            DeviceStatus.IsInitialized = (curState == RayScannerState.Default) ? true : false;
            DeviceStatus.IsLiveView = (bool)(RayGetProperty(Property.MotorOnOff) != 0);
            DeviceStatus.IsAngioConnected = true;
        }
        protected void handleProgress(RayCallbackRequest request, int progress) { }
        protected void handleError(RayCallbackRequest request, RayError error) { }
        protected void handleWorkDone(RayCallbackRequest request, RayWorkItem work)
        {
            if (work == RayWorkItem.AutoCalibration)
            {
                DeviceStatus.CanExecuteCalibration = true;
            }
        }
    }
}
