using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class PasswordChangeDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordChangeDialogViewModel));

        [ObservableProperty]
        private string _oldPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        private string _currentPassword = string.Empty;
        private string _curruntID = string.Empty;

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

        partial void OnOldPasswordChanged(string value)
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

            if (OldPassword == string.Empty | NewPassword == string.Empty | ConfirmPassword == string.Empty)
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
            return !string.IsNullOrEmpty(OldPassword) &&
                   !string.IsNullOrEmpty(NewPassword) &&
                   !string.IsNullOrEmpty(ConfirmPassword);
        }

        private void InputPasswordClear()
        {
            _log.Info("InputPasswordClear");

            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool ExecuteChangePassword()
        {
            _log.Info("ExecuteChangePassword");

            _curruntID = ViewModelBase.DeviceStatus.LoginID;

            _currentPassword = _passwordService.GetPasswordByUserId(_curruntID);
            if (_currentPassword == string.Empty) return false;

            if (!_passwordService.IsSamePassword(_currentPassword, OldPassword, "[Old Password]")) return false;

            if (!_passwordService.IsNotSamePassword(OldPassword, NewPassword, "[Old/New Password]")) return false;

            if (!_passwordService.IsSamePassword(NewPassword, ConfirmPassword, "[New/Confirm Password]")) return false;

            if (_passwordService.GetPasswordValidationError(ConfirmPassword) is { } message)
            {
                _passwordService.ShowAlert(_l10n["Information"], message);
                return false;
            }

            _passwordService.UpdatePasswordReset(_curruntID, ConfirmPassword, _currentPassword, false);

            _passwordService.ShowAlert(_l10n["Information"], "Password changed successfully");

            return true;
        }
    }
}
