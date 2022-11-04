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
using System.Windows;
using System.Windows.Input;

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
        private string _navigationSource;

        [ObservableProperty]
        private object _navigationParameter;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private Visibility _isShowPatient = Visibility.Collapsed;

        private ICommand _navigateCommand;

        public ICommand NavigateCommand
        {
            get { return this._navigateCommand ?? (this._navigateCommand = new RelayCommand<string>(OnNavigate)); }
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
            NavigationSource = "Views/PatientListPage.xaml";

            //네비게이션 메시지 수신 등록
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, OnNavigationMessage);

            //BusyMessage 수신 등록
            WeakReferenceMessenger.Default.Register<BusyMessage>(this, OnBusyMessage);

            Patient = new Patient();
            
            RaywattOCT.RayCoreWrapper.RayStartSystem();
        }

        private void OnNavigate(string pageUri)
        {
            _log.Debug("OnNavigate : " + pageUri);

            NavigationSource = pageUri;
        }

        private void OnNavigationMessage(object recipient, NavigationMessage message)
        {
            _log.Debug("OnNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            ShowPatientInfo(pageUri, message.Parameter);
            //순서 중요 - NavigationParameter 먼저 입력 후, NavigationSource 입력 필요
            NavigationParameter = message.Parameter;
            NavigationSource = pageUri;
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

        private void ShowPatientInfo(string pageUri, object parameter)
        {
            _log.Debug("ShowPatientInfo : " + pageUri);

            if (pageUri.IndexOf("RecordingPage") > 0 || pageUri.IndexOf("ReviewPage") > 0)
            {
                IsShowPatient = Visibility.Visible;
                Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
                SetPatientInfo((Patient)data["patient"]);
            }
            else
            {
                IsShowPatient = Visibility.Collapsed;
            }
        }

        private void SetPatientInfo(Patient patient)
        {
            _log.Debug("SetPatientInfo");

            if (patient == null)
            {
                _log.Info("Patient is null");
                return;
            }

            CopyPatient(patient, Patient);
        }

        private void CopyPatient(Patient src, Patient dest)
        {
            _log.Debug("CopyPatient");

            dest.Id = src.Id.Trim();
            dest.Lastname = src.Lastname.Trim();
            dest.Firstname = src.Firstname.Trim();
            dest.Birthdate = src.Birthdate;
            dest.Gender = src.Gender;
        }
        private void Home()
        {
            _log.Debug("Home");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml"));
        }

        private void Setting()
        {
            _log.Debug("Setting");

            var result = _dialogService.OpenDialog(new SettingDialogControl());
        }

        private void Exit()
        {
            _log.Debug("Exit");

            Application.Current.MainWindow.Close();
        }
    }
}
