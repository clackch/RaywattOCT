using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
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

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        public ICommand CancelCommand => new RelayCommand(OnCancel);
        public ICommand ConfrmCommand => new RelayCommand(OnConfirm);

        public InitialPasswordSetupViewModel(IPasswordService passwordService)
        {
            _log.Debug("InitialPasswordSetupViewModel");

            _passwordService = passwordService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is NavigationEventArgs navArgs && navArgs.ExtraData is Dictionary<string, object> data)
            {
                _loginId = data["id"] as string ?? string.Empty;
                _loginPassword = data["password"] as string ?? string.Empty;

                if(string.IsNullOrEmpty(_loginId) || string.IsNullOrEmpty(_loginPassword))
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

        private void OnConfirm()
        {
            _log.Debug("OnConfirm");

            if (!_passwordService.IsSamePassword(Password, ConfirmPassword))
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

            _passwordService.UpdatePasswordReset(_loginId, ConfirmPassword, _loginPassword, false);

            var parameter = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = ConfirmPassword
            };

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
        }

        private void ClearPasswords()
        {
            _log.Debug("ClearPasswords");

            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }
}
