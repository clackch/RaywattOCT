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
        private string _popupNavigationSource;

        [ObservableProperty]
        private Visibility _visibility = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSource2;

        [ObservableProperty]
        private Visibility _visibility2 = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSource3;

        [ObservableProperty]
        private Visibility _visibility3 = Visibility.Collapsed;

        [ObservableProperty]
        private string _popupNavigationSource4;

        [ObservableProperty]
        private Visibility _visibility4 = Visibility.Collapsed;

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
            PopupNavigationSource = Constants.SettingDatabasePage;

            WeakReferenceMessenger.Default.Register<PopupNavigationMessage>(this, OnPopupNavigationMessage);

            PopupNavigationSource = Constants.SettingDatabasePage;
            PopupNavigationSource2 = Constants.SettingLogPage;
            PopupNavigationSource3 = Constants.SettingTermsConditionsPage;
            PopupNavigationSource4 = Constants.SettingAboutPage;

            Visibility = Visibility.Visible;
            CurrentMenu = Constants.SettingDatabasePage;
        }

        private void OnPopupNavigationMessage(object recipient, PopupNavigationMessage message)
        {
            _log.Debug("OnPopupNavigationMessage : " + message.Value);

            //저장 기능이 있는 페이지만 추가
            string pageUri = message.Value;
            PopupNavigationSource3 = pageUri;//SettingTermsConditionsPage
        }

        private void OnPopupNavigate(string pageUri)
        {
            _log.Debug("OnPopupNavigate : " + pageUri);

            Visibility = Visibility.Collapsed;
            Visibility2 = Visibility.Collapsed;
            Visibility3 = Visibility.Collapsed;
            Visibility4 = Visibility.Collapsed;

            switch (pageUri)
            {
                case Constants.SettingDatabasePage:
                    Visibility = Visibility.Visible;
                    break;
                case Constants.SettingLogPage:
                    Visibility2 = Visibility.Visible;
                    break;
                case Constants.SettingTermsConditionsPage:
                    Visibility3 = Visibility.Visible;
                    break;
                case Constants.SettingAboutPage:
                    Visibility4 = Visibility.Visible;
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

            WeakReferenceMessenger.Default.Unregister<PopupNavigationMessage>(this);
        }
    }
}
