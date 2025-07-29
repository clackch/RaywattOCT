using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.Password
{
    partial class PasswordExpiryCheckViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordExpiryCheckViewModel));

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        private string _loginId = string.Empty;
        private string _loginPassword = string.Empty;
        private User _user;

        private ICommand _changeLaterCommand;
        public ICommand ChangeLaterCommand
        {
            get { return this._changeLaterCommand ?? (this._changeLaterCommand = new RelayCommand<IDialogWindow>(OnChangeLater)); }
        }

        private readonly IPasswordService _passwordService;

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand<IDialogWindow>(OnOk, CanOk)); }
        }

        public PasswordExpiryCheckViewModel(IPasswordService passwordService)
        {
            _log.Debug("PasswordExpiryCheckViewModel");

            _passwordService = passwordService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is NavigationEventArgs navArgs && navArgs.ExtraData is Dictionary<string, object> data)
            {
                _user = data["user"] as User ?? new User();
                _loginPassword = _user.Password;
                _loginId = _user.Id;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        partial void OnConfirmPasswordChanged(string value)
        {
            (OkCommand as RelayCommand<IDialogWindow>)?.NotifyCanExecuteChanged();
        }

        partial void OnNewPasswordChanged(string value)
        {
            (OkCommand as RelayCommand<IDialogWindow>)?.NotifyCanExecuteChanged();
        }

        partial void OnCurrentPasswordChanged(string value)
        {
            (OkCommand as RelayCommand<IDialogWindow>)?.NotifyCanExecuteChanged();
        }

        private void OnChangeLater(IDialogWindow dialog)
        {
            _log.Debug("OnChangeLater");

            _passwordService.UpdatePasswordReset(_loginId, _loginPassword, _loginPassword, false);

            var parameter = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = _loginPassword
            };

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
        }
        private void OnOk(IDialogWindow dialog)
        {
            _log.Debug("OnOk");

            if (CurrentPassword == string.Empty | NewPassword == string.Empty | ConfirmPassword == string.Empty)
            {
                InputPasswordClear();
                _passwordService.ShowAlert(_l10n["Information"], "Password entry is required");
                return;
            }

            if (!ExecuteChangePassword())
            {
                InputPasswordClear();
                return;
            }

            var parameter = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = ConfirmPassword
            };

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
        }

        private bool CanOk(IDialogWindow? obj)
        {
            return !string.IsNullOrEmpty(CurrentPassword) &&
                   !string.IsNullOrEmpty(NewPassword) &&
                   !string.IsNullOrEmpty(ConfirmPassword);
        }

        private void InputPasswordClear()
        {
            _log.Debug("InputPasswordClear");

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool ExecuteChangePassword()
        {
            _log.Debug("ExecuteChangePassword");

            if (_loginPassword == string.Empty) return false;

            if (!_passwordService.IsSamePassword(_loginPassword, CurrentPassword, "[Current Password]")) return false;

            if (!_passwordService.IsNotSamePassword(CurrentPassword, NewPassword, "[Current/New Password]")) return false;

            if (!_passwordService.IsSamePassword(NewPassword, ConfirmPassword, "[New/Confirm Password]")) return false;

            if (_passwordService.GetPasswordValidationError(ConfirmPassword) is { } message)
            {
                _passwordService.ShowAlert(_l10n["Information"], message);
                return false;
            }

            _passwordService.UpdatePasswordReset(_loginId, ConfirmPassword, _loginPassword, false);

            _passwordService.ShowAlert(_l10n["Information"], "Password changed successfully");

            return true;
        }
    }
}
