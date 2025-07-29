using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class PasswordChangeDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordChangeDialogViewModel));

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        private string _loginPassword = string.Empty;
        private string _loginID = string.Empty;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand<IDialogWindow>(OnCancel)); }
        }

        private readonly IPasswordService _passwordService;

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand<IDialogWindow>(OnOk, CanOk)); }
        }

        public PasswordChangeDialogViewModel(IPasswordService passwordService)
        {
            _log.Info("PasswordChangeDialogViewModel");

            _passwordService = passwordService;
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

        private void OnCancel(IDialogWindow dialog)
        {
            _log.Info("OnCancel");

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.No;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private void OnOk(IDialogWindow dialog)
        {
            _log.Info("OnOk");

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

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            CloseDialogWithResult(dialog, dialogResults);
        }

        private bool CanOk(IDialogWindow? obj)
        {
            return !string.IsNullOrEmpty(CurrentPassword) &&
                   !string.IsNullOrEmpty(NewPassword) &&
                   !string.IsNullOrEmpty(ConfirmPassword);
        }

        private void InputPasswordClear()
        {
            _log.Info("InputPasswordClear");

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool ExecuteChangePassword()
        {
            _log.Info("ExecuteChangePassword");

            _loginID = ViewModelBase.DeviceStatus.LoginID;

            _loginPassword = _passwordService.GetPasswordByUserId(_loginID);
            if (_loginPassword == string.Empty)
            {
                _passwordService.ShowAlert(_l10n["Information"], "Current password not found. Please contact support.");
                return false;
            }

            if (!_passwordService.IsSamePassword(_loginPassword, CurrentPassword, "[Current Password]")) return false;

            if (!_passwordService.IsNotSamePassword(CurrentPassword, NewPassword, "[Current/New Password]")) return false;

            if (!_passwordService.IsSamePassword(NewPassword, ConfirmPassword, "[New/Confirm Password]")) return false;

            if (_passwordService.GetPasswordValidationError(ConfirmPassword) is { } message)
            {
                _passwordService.ShowAlert(_l10n["Information"], message);
                return false;
            }

            _passwordService.UpdatePasswordReset(_loginID, ConfirmPassword, _loginPassword);

            _passwordService.ShowAlert(_l10n["Information"], "Password changed successfully");

            return true;
        }
    }
}
