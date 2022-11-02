using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;

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

        public SettingDialogViewModel()
        {
            PopupNavigationSource = "Views/Setting/SettingAcquisitionPage.xaml";

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
    }
}
