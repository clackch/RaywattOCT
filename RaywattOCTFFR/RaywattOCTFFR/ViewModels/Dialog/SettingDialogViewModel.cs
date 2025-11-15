using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Bases;

namespace RaywattOCTFFR.ViewModels.Dialog
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

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand<IDialogWindow>(Cancel)); }
        }

        public SettingDialogViewModel()
        {
            PopupNavigationSource = Constants.SettingDatabasePage;

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

        private void Cancel(IDialogWindow dialog)
        {
            _log.Debug("Cancel");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage(Constants.SettingAboutPage));

            if (dialog != null)
            {
                dialog.DialogResult = true;
            }
        }
    }
}
