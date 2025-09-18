using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Views.Dialog;
using System.Windows.Input;

namespace RaywattOCTFFR.ViewModels.Setting
{
    public partial class SettingPasswordChangeViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingPasswordChangeViewModel));

        private IDialogService _dialogService;

        [ObservableProperty]
        string _loginID = string.Empty;

        private ICommand _passwordChangeCommand;
        public ICommand PasswordChangeCommand
        {
            get { return this._passwordChangeCommand ?? (this._passwordChangeCommand = new RelayCommand(PasswordChange)); }
        }

        public SettingPasswordChangeViewModel(IDialogService dialogService)
        {
            _log.Debug("SettingPasswordChangeViewModel");

            _dialogService = dialogService;
            _loginID = DeviceStatus.LoginID ?? string.Empty;
        }

        private void PasswordChange()
        {
            _log.Debug("PasswordChange");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var result = _dialogService.OpenDialog(new PasswordChangeDialogControl(), null, Constants.ApplicationWidth, Constants.ApplicationHeight);
            });
        }
    }
}
