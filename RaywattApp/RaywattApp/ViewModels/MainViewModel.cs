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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string _navigationSource;

        [ObservableProperty]
        private object _navigationParameter;

        [ObservableProperty]
        private Patient _patient;

        private List<int> currReviewPages;

        private List<string> nextReviewPages;

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
            RaywattOCT.RayCoreWrapper.RayConnectDevices();

            Directory.CreateDirectory(Constants.DataRootPath);

            currReviewPages = new List<int>();
            currReviewPages.Add((int)CommonDefinition.PageList.ReviewPage);
            currReviewPages.Add((int)CommonDefinition.PageList.Review3dPage);
            currReviewPages.Add((int)CommonDefinition.PageList.ReviewComparePage);
            currReviewPages.Add((int)CommonDefinition.PageList.ReviewFfrPage);
            currReviewPages.Add((int)CommonDefinition.PageList.ReviewPresetPage);

            nextReviewPages = new List<string>();
            nextReviewPages.Add("Views/ReviewPage.xaml");
            nextReviewPages.Add("Views/Review3dPage.xaml");
            nextReviewPages.Add("Views/ReviewComparePage.xaml");
            nextReviewPages.Add("Views/ReviewFfrPage.xaml");
            nextReviewPages.Add("Views/ReviewPresetPage.xaml");
        }

        private void OnNavigationMessage(object recipient, NavigationMessage message)
        {
            _log.Debug("OnNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            //순서 중요 - NavigationParameter 먼저 입력 후, NavigationSource 입력 필요
            NavigationParameter = message.Parameter;
            NavigationSource = pageUri;

            //Review 화면에서 나가는 경우, RayEndReivew 호출
            if (currReviewPages.Contains(CommonDefinition.CurrentPage))
            {
                if (!nextReviewPages.Contains(pageUri))
                    RayEndReview();
            }
            //Recording(Confirm) 화면에서 나가는 경우, RayEndReivew 호출
            if (CommonDefinition.CurrentPage == (int)CommonDefinition.PageList.RecordingPage)
            {
                if (!pageUri.Equals("Views/ReviewPresetPage.xaml"))
                    RayEndReview();
            }
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

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml"));

            Application.Current.MainWindow.Close();
        }
    }
}
