using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class PasswordChangeDialogViewModel : DialogViewModelBase
    {
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
            get { return this._okCommand ?? (this._okCommand = new RelayCommand<IDialogWindow>(OnOk)); }
        }

        public PasswordChangeDialogViewModel(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        private void OnCancel(IDialogWindow dialog)
        {
            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.No;

            CloseDialogWithResult(dialog, dialogResults);
        }
        private void OnOk(IDialogWindow dialog)
        {
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

        private void InputPasswordClear()
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool ExecuteChangePassword()
        {
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

            _passwordService.UpdatePasswordReset(_curruntID, ConfirmPassword, _currentPassword );

            _passwordService.ShowAlert(_l10n["Information"], "Password changed successfully");

            return true;
        }
    }
}
