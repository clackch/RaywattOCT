using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingPasswordChangeViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingAboutViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        string _loginID = string.Empty;

        private ICommand _passwordChangeCommand;
        public ICommand PasswordChangeCommand
        {
            get { return this._passwordChangeCommand ?? (this._passwordChangeCommand = new RelayCommand(PasswordChange)); }
        }

        public SettingPasswordChangeViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _loginID = DeviceStatus.LoginID ?? string.Empty;
        }

        private void PasswordChange()
        {
            var result = _dialogService.OpenDialog(new PasswordChangeDialogControl(), null, Constants.ApplicationWidth, Constants.ApplicationHeight);
        }
    }
}
