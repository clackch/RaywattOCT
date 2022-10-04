using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
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

        /// <summary>
        /// Busy 목록
        /// </summary>
        private IList<BusyMessage> _busys = new List<BusyMessage>();

        private bool _isBusy;
        /// <summary>
        /// IsBusy
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        private string _navigationSource;
        /// <summary>
        /// 네비게이션 소스
        /// </summary>
        public string NavigationSource
        {
            get { return _navigationSource; }
            set { SetProperty(ref _navigationSource, value); }
        }

        private string _popupNavigationSource;

        public string PopupNavigationSource
        {
            get { return _popupNavigationSource; }
            set { SetProperty(ref _popupNavigationSource, value); }
        }

        private bool _showLayerPopup;
        /// <summary>
        /// 레이어 팝업 출력여부
        /// </summary>
        public bool ShowLayerPopup
        {
            get { return _showLayerPopup; }
            set { SetProperty(ref _showLayerPopup, value); }
        }

        private string _controlName;
        /// <summary>
        /// 레이어 팝업 내부 컨트롤 이름
        /// </summary>
        public string ControlName
        {
            get { return _controlName; }
            set { SetProperty(ref _controlName, value); }
        }

        private bool _showViewLayerPopup;
        /// <summary>
        /// 레이어 팝업 출력여부
        /// </summary>
        public bool ShowViewLayerPopup
        {
            get { return _showViewLayerPopup; }
            set { SetProperty(ref _showViewLayerPopup, value); }
        }

        private string _viewControlName;
        /// <summary>
        /// 레이어 팝업 내부 컨트롤 이름
        /// </summary>
        public string ViewControlName
        {
            get { return _viewControlName; }
            set { SetProperty(ref _viewControlName, value); }
        }

        [ObservableProperty]
        private object _navigationParameter;

        [ObservableProperty]
        private object _popupNavigationParameter;

        [ObservableProperty]
        private string _messagePopupLevel;

        [ObservableProperty]
        private string _messagePopupContent;

        [ObservableProperty]
        private string _filePopupType;

        [ObservableProperty]
        private Patient _patient; 

        [ObservableProperty]
        private Visibility _isShowPatient = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _isShowPatientEdit = Visibility.Collapsed;

        private ICommand _navigateCommand;

        public ICommand NavigateCommand
        {
            get { return this._navigateCommand ?? (this._navigateCommand = new RelayCommand<string>(OnNavigate)); }
        }

        private ICommand _popupNavigateCommand;

        public ICommand PopupNavigateCommand
        {
            get { return this._popupNavigateCommand ?? (this._popupNavigateCommand = new RelayCommand<string>(OnPopupNavigate)); }
        }

        private ICommand _settingCommand;
        public ICommand SettingCommand
        {
            get { return this._settingCommand ?? (this._settingCommand = new RelayCommand(Setting)); }
        }

        private ICommand _patiendEditCommand;
        public ICommand PatientEditCommand
        {
            get { return this._patiendEditCommand ?? (this._patiendEditCommand = new RelayCommand(EditPatientInfo)); }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public MainViewModel()
        {
            _log.Debug("MainViewModel");

            //시작 페이지 설정
            NavigationSource = "Views/PatientListPage.xaml";

            //네비게이션 메시지 수신 등록
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, OnNavigationMessage);
            WeakReferenceMessenger.Default.Register<PopupNavigationMessage>(this, OnPopupNavigationMessage);

            //BusyMessage 수신 등록
            WeakReferenceMessenger.Default.Register<BusyMessage>(this, OnBusyMessage);

            //PopupMessage 수신 등록
            WeakReferenceMessenger.Default.Register<PopupMessage>(this, OnLayerPopupMessage);

            //PatientMessage 수신 등록
            WeakReferenceMessenger.Default.Register<PatientMessage>(this, OnPatientMessage);

            Patient = new Patient();
        }

        private void OnNavigate(string pageUri)
        {
            _log.Debug("OnNavigate : " + pageUri);

            NavigationSource = pageUri;
        }

        private void OnPopupNavigate(string pageUri)
        {
            _log.Debug("OnPopupNavigate : " + pageUri);

            PopupNavigationSource = pageUri;
        }

        /// <summary>
        /// 네비게이션 메시지 수신 처리
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="message"></param>
        private void OnNavigationMessage(object recipient, NavigationMessage message)
        {
            _log.Debug("OnNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            ShowPatientInfo(pageUri);
            //순서 중요 - NavigationParameter 먼저 입력 후, NavigationSource 입력 필요
            NavigationParameter = message.Parameter;
            NavigationSource = pageUri;
        }

        private void OnPopupNavigationMessage(object recipient, PopupNavigationMessage message)
        {
            _log.Debug("OnNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            PopupNavigationParameter = message.Parameter;
            PopupNavigationSource = pageUri;
        }

        /// <summary>
        /// 비지 메시지 수신 처리
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="message"></param>
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

        private void OnLayerPopupMessage(object recipient, PopupMessage message)
        {
            _log.Debug("OnLayerPopupMessage : " + message.Type + "/" + message.Value + "/" + message.ControlName);

            switch (message.Type)
            {
                case (int)CommonDefinition.PopupType.Message:

                    ShowLayerPopup = message.Value;
                    ControlName = message.ControlName;

                    //Popup Level
                    switch (message.Level)
                    {
                        case (int)CommonDefinition.PopupLevel.Info:
                            MessagePopupLevel = _l10n["Information"];
                            break;
                        case (int)CommonDefinition.PopupLevel.Warn:
                            MessagePopupLevel = _l10n["Warning"];
                            break;
                        case (int)CommonDefinition.PopupLevel.Error:
                            MessagePopupLevel = _l10n["Error"];
                            break;
                        default:
                            break;
                    }

                    //Popup Message
                    if (message.Parameter != null)
                        MessagePopupContent = message.Parameter.ToString();

                    break;
                case (int)CommonDefinition.PopupType.Setting:

                    ShowViewLayerPopup = message.Value;
                    ViewControlName = message.ControlName;

                    break;
                case (int)CommonDefinition.PopupType.File:

                    ShowViewLayerPopup = message.Value;
                    ViewControlName = message.ControlName;
                    PopupNavigationParameter = message.Parameter;

                    if (message.FileType == (int)CommonDefinition.FileType.Import)
                    {
                        FilePopupType = _l10n["Import"];
                        PopupNavigationSource = "Views/File/FileImportPage.xaml";
                    }
                    else
                    {
                        FilePopupType = _l10n["Export"];
                        PopupNavigationSource = "Views/File/FileExportStep1Page.xaml";                        
                    }

                    break;
                default:
                    break;
            }
        }

        private void OnPatientMessage(object recipient, PatientMessage message)
        {
            _log.Debug("OnPatientMessage : " + message.Id);

            Patient.Id = message.Id;
            Patient.Lastname =  message.Lastname;
            Patient.Firstname = message.Firstname;
            Patient.Birthdate = message.Birthdate;
            Patient.Gender = message.Gender;
        }

        private void Setting()
        {
            _log.Debug("Setting");

            PopupNavigationSource = "Views/Setting/SettingAcquisitionPage.xaml";
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "SettingPopupControl", Type = (int)CommonDefinition.PopupType.Setting });
        }

        private void ShowPatientInfo(string pageUri)
        {
            _log.Debug("ShowPatientInfo : " + pageUri);

            if (pageUri.IndexOf("PatientDetailPage") > 0)
            {
                IsShowPatient = Visibility.Visible;
                IsShowPatientEdit = Visibility.Visible;
            }
            else if (pageUri.IndexOf("RecordingPage") > 0)
            {
                IsShowPatient = Visibility.Visible;
                IsShowPatientEdit = Visibility.Collapsed;
            }
            else
            {
                IsShowPatient = Visibility.Collapsed;
            }
        }

        private void EditPatientInfo()
        {
            _log.Debug("EditPatientInfo");
        }
    }
}
