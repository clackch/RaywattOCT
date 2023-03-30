using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Bases;
using RaywattApp.Views.Dialog;
using System.Windows;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class SettingDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingDialogViewModel));

        [ObservableProperty]
        private string _popupNavigationSource;

        [ObservableProperty]
        private object _popupNavigationParameter;

        private ICommand _popupNavigateCommand;

        public ICommand PopupNavigateCommand
        {
            get { return this._popupNavigateCommand ?? (this._popupNavigateCommand = new RelayCommand<string>(OnPopupNavigate)); }
        }

        private ICommand _refreshCommand;
        public ICommand RefreshCommand
        {
            get { return this._refreshCommand ?? (this._refreshCommand = new RelayCommand(Refresh)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        public SettingDialogViewModel()
        {
            PopupNavigationSource = Constants.SettingAcquisitionPage;

            WeakReferenceMessenger.Default.Register<PopupNavigationMessage>(this, OnPopupNavigationMessage);
        }

        private void OnPopupNavigationMessage(object recipient, PopupNavigationMessage message)
        {
            _log.Debug("OnPopupNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            PopupNavigationParameter = message.Parameter;
            PopupNavigationSource = pageUri;
        }

        private void OnPopupNavigate(string pageUri)
        {
            _log.Debug("OnPopupNavigate : " + pageUri);

            PopupNavigationSource = pageUri;
        }

        private void Refresh()
        {
            _log.Debug("Refresh");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Refresh"));
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            CloseDialog();
        }

        protected void CloseDialog()
        {
            foreach (var winCollection in Application.Current.Windows)
            {
                if (winCollection.GetType() == typeof(DialogWindow))
                {
                    if ("settingDialogControl".Equals(((winCollection as DialogWindow).Content as SettingDialogControl).Name))
                    {
                        var dialog = (DialogWindow)winCollection;
                        dialog.DialogResult = true;
                        break;
                    }
                }
            }
        }
    }
}
