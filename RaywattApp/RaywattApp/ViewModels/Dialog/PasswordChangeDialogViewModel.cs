using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
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

        private string _getPassword = string.Empty;
        private string _getID = string.Empty;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand<IDialogWindow>(OnCancel)); }
        }

        private readonly IDialogService _dialogService;
        private readonly IDatabaseService _databaseService;

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand<IDialogWindow>(OnOk)); }
        }

        public PasswordChangeDialogViewModel(IDialogService dialogService, IDatabaseService databaseService)
        {
            _dialogService = dialogService;
            _databaseService = databaseService;
        }

        private void OnCancel(IDialogWindow dialog)
        {
            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.No;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private void OnOk(IDialogWindow dialog)
        {
            if (!ExecuteChangePassword())
            {
                OldPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                return;
            }

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            CloseDialogWithResult(dialog, dialogResults);
        }

        private bool ExecuteChangePassword()
        {
            _getID = ViewModelBase.DeviceStatus.LoginID;
            if (!GetPasswordByUserId(_getID)) return false;
            if (!IsSamePassword(_getPassword, OldPassword)) return false;
            if (!IsNewPasswordSameAsOld(OldPassword, NewPassword)) return false;
            if (!IsSamePassword(NewPassword, ConfirmPassword)) return false;
            if (!IsValidationPassword()) return false;
            if (!UpdatePasswordReset()) return false;

            ShowAlert(_l10n["Information"], "Password changed successfully");

            return true;
        }

        private bool IsNewPasswordSameAsOld(string oldPwd, string newPwd)
        {
            if (oldPwd.Equals(newPwd))
            {
                ShowAlert(_l10n["Information"], "Passwords do match.");
                return false;
            }

            return true;
        }
        private bool IsValidationPassword()
        {
            string? error = GetPasswordValidationError();

            if (error != null)
            {
                ShowAlert(_l10n["Information"], error);
                return false;
            }

            return true;
        }

        private bool IsSamePassword(string beforePassword, string inputPassword)
        {
            if (!beforePassword.Equals(inputPassword))
            {
                ShowAlert("info", "Incorrect current password.\r\nPlease try again.");
                return false;
            }

            return true;
        }

        private bool GetPasswordByUserId(string id)
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["id"] = id;

            var commandText = SqlQuery.GetQuery("SelectUserListById");
            var userData = _databaseService.GetDatas<User>(commandText, sqlParameters);
            _getPassword = userData.Count > 0 ? userData[0].Password : string.Empty;

            if (_getPassword == string.Empty)
            {
                ShowAlert("info", "Login ID is not set.");
                return false;
            }

            return true;
        }

        private void ShowAlert(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["title"] = title,
                    ["message"] = message
                };

                _dialogService.OpenDialog(new AlertDialogControl(), parameters, Constants.ApplicationWidth, Constants.ApplicationHeight);
            });
        }

        private string? GetPasswordValidationError()
        {
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
                return "Please enter a password.";

            if (ConfirmPassword.Contains(" "))
                return "Password cannot contain spaces.";

            if (ConfirmPassword.Length < 8)
                return "Password must be at least 8 characters long.";

            if (!Regex.IsMatch(ConfirmPassword, @"[A-Z]"))
                return "Password must include at least one uppercase letter.";

            if (!Regex.IsMatch(ConfirmPassword, @"\d"))
                return "Password must include at least one number.";

            if (!Regex.IsMatch(ConfirmPassword, @"[!@#$%^&*()_\-+=\[\]{};':""\\|,.<>\/?]"))
                return "Password must include at least one special character.";

            if (!Regex.IsMatch(ConfirmPassword, @"^[a-zA-Z0-9!@#$%^&*()_\-+=\[\]{};':""\\|,.<>\/?]+$"))
                return "Password can only contain English letters, numbers, and special characters.";

            return null;
        }

        private bool UpdatePasswordReset()
        {
            var commandText = SqlQuery.GetQuery("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = _getID,
                ["password"] = ConfirmPassword,
                ["before_password"] = _getPassword,
                ["reset"] = true
            };

            _databaseService.UpdateData(commandText, parameters);

            return true;
        }

    }
}
