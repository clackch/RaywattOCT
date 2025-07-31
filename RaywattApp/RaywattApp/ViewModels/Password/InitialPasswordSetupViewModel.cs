using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Enums;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.Password
{
    partial class InitialPasswordSetupViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(InitialPasswordSetupViewModel));

        private readonly IPasswordService _passwordService;

        private string _loginId = string.Empty;
        private string _loginPassword = string.Empty;
        private User _user;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        public ICommand CancelCommand => new RelayCommand(OnCancel);

        private ICommand? _okCommand;
        public ICommand OkCommand => _okCommand ??= new RelayCommand(OnOk, CanOk);

        public InitialPasswordSetupViewModel(IPasswordService passwordService)
        {
            Constants.CurrentPage = Constants.InitialPasswordSetupPage;

            _log.Debug("InitialPasswordSetupViewModel");

            _passwordService = passwordService;
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            (OkCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        partial void OnPasswordChanged(string value)
        {
            (OkCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is NavigationEventArgs navArgs && navArgs.ExtraData is Dictionary<string, object> data)
            {
                _user = data["user"] as User ?? new User();
                _loginId = _user.Id;
                _loginPassword = _user.Password;

                if (string.IsNullOrEmpty(_loginId) || string.IsNullOrEmpty(_loginPassword))
                {
                    _passwordService.ShowAlert(_l10n["Error"], "Login ID or password is missing.");
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void OnCancel()
        {
            _log.Debug("OnCancel");

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
        }

        private void OnOk()
        {
            _log.Debug("OnConfirm");

            if (!_passwordService.IsPasswordConfirmed(Password, ConfirmPassword))
            {
                ClearPasswords();
                return;
            }

            if (_passwordService.GetPasswordValidationError(ConfirmPassword) is { } message)
            {
                _passwordService.ShowAlert(_l10n["Information"], message);
                ClearPasswords();
                return;
            }

            _passwordService.UpdatePasswordReset(_loginId, ConfirmPassword, _loginPassword);

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["login_step"] = LoginStep.CheckPasswordExpiry;
            parameter["user"] = _user;

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage) { Parameter  = parameter});
        }

        private bool CanOk()
        {
            return !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(ConfirmPassword);
        }

        private void ClearPasswords()
        {
            _log.Debug("ClearPasswords");

            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }
}
