using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Bases;
using System.Windows;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class SettingDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingDialogViewModel));

        [ObservableProperty]
        private string _currentMenu;

        [ObservableProperty]
        private string _popupNavigationSourceSettingDatabasePage;

        [ObservableProperty]
        private Visibility _visibilitySettingDatabasePage = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSourceSettingLogPage;

        [ObservableProperty]
        private Visibility _visibilitySettingLogPage = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSourceSettingTermsConditionsPage;

        [ObservableProperty]
        private Visibility _visibilitySettingTermsConditionsPage = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSourceSettingMaintenancePage;

        [ObservableProperty]
        private Visibility _visibilitySettingMaintenancePage = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSourceSettingAboutPage;

        [ObservableProperty]
        private Visibility _visibilitySettingAboutPage = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSourceSettingDicomPage;

        [ObservableProperty]
        private Visibility _visibilitySettingDicomPage = Visibility.Collapsed;

        private ICommand _popupNavigateCommand;

        public ICommand PopupNavigateCommand
        {
            get { return this._popupNavigateCommand ?? (this._popupNavigateCommand = new RelayCommand<string>(OnPopupNavigate)); }
        }

        private ICommand _okayCommand;
        public ICommand OkayCommand
        {
            get { return this._okayCommand ?? (this._okayCommand = new RelayCommand<IDialogWindow>(Okay)); }
        }

        private ICommand _applyCommand;
        public ICommand ApplyCommand
        {
            get { return this._applyCommand ?? (this._applyCommand = new RelayCommand(Apply)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand<IDialogWindow>(Cancel)); }
        }

        public SettingDialogViewModel()
        {
            PopupNavigationSourceSettingDatabasePage = Constants.SettingDatabasePage;

            WeakReferenceMessenger.Default.Register<PopupNavigationMessage>(this, OnPopupNavigationMessage);

            PopupNavigationSourceSettingDatabasePage = Constants.SettingDatabasePage;
            PopupNavigationSourceSettingLogPage = Constants.SettingLogPage;
            PopupNavigationSourceSettingTermsConditionsPage = Constants.SettingTermsConditionsPage;
            PopupNavigationSourceSettingMaintenancePage = Constants.SettingMaintenancePage;
            PopupNavigationSourceSettingAboutPage = Constants.SettingAboutPage;
            PopupNavigationSourceSettingDicomPage = Constants.SettingDicomPage;

            VisibilitySettingDatabasePage = Visibility.Visible;
            CurrentMenu = Constants.SettingDatabasePage;
        }

        private void OnPopupNavigationMessage(object recipient, PopupNavigationMessage message)
        {
            _log.Debug("OnPopupNavigationMessage : " + message.Value);

            //Apply/Save 시, 페이지에 저장할 기능(or 내용)이 있으면 아래 추가
            string pageUri = message.Value;
            PopupNavigationSourceSettingTermsConditionsPage = pageUri;
        }

        private void OnPopupNavigate(string pageUri)
        {
            _log.Debug("OnPopupNavigate : " + pageUri);

            VisibilitySettingDatabasePage = Visibility.Collapsed;
            VisibilitySettingLogPage = Visibility.Collapsed;
            VisibilitySettingTermsConditionsPage = Visibility.Collapsed;
            VisibilitySettingMaintenancePage = Visibility.Collapsed;
            VisibilitySettingAboutPage = Visibility.Collapsed;
            VisibilitySettingDicomPage = Visibility.Collapsed;

            switch (pageUri)
            {
                case Constants.SettingDatabasePage:
                    VisibilitySettingDatabasePage = Visibility.Visible;
                    break;
                case Constants.SettingLogPage:
                    VisibilitySettingLogPage = Visibility.Visible;
                    break;
                case Constants.SettingTermsConditionsPage:
                    VisibilitySettingTermsConditionsPage = Visibility.Visible;
                    break;
                case Constants.SettingMaintenancePage:
                    VisibilitySettingMaintenancePage = Visibility.Visible;
                    break;
                case Constants.SettingAboutPage:
                    VisibilitySettingAboutPage = Visibility.Visible;
                    break;
                case Constants.SettingDicomPage:
                    VisibilitySettingDicomPage = Visibility.Visible;
                    break;
                default:
                    break;
            }

            CurrentMenu = pageUri;
        }

        private void Apply()
        {
            _log.Debug("Apply");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Refresh"));
        }

        private void Okay(IDialogWindow dialog)
        {
            _log.Debug("Okay");

            Apply();

            Cancel(dialog);
        }

        private void Cancel(IDialogWindow dialog)
        {
            _log.Debug("Cancel");

            if (dialog != null)
            {
                dialog.DialogResult = true;
            }

            //OnNavigating 호출을 위해, 다른 페이지 입력 - Popup이 닫힐 때, 각 페이지별로 처리해야하는 부분이 있는 경우 아래 추가
            PopupNavigationSourceSettingLogPage = Constants.SettingAboutPage;//[SettingLogPage] FileExportStep2Base의 Timer 종료를 위해 추가
            PopupNavigationSourceSettingMaintenancePage = Constants.SettingAboutPage;//[SettingMaintenancePage] RJ의 BLDC Step Motor 원위치를 위해 추가

            WeakReferenceMessenger.Default.Unregister<PopupNavigationMessage>(this);
        }
    }
}
