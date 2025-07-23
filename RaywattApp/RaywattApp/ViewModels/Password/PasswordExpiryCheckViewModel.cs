using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.Password
{
    partial class PasswordExpiryCheckViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordExpiryCheckViewModel));

        [ObservableProperty]
        private string _oldPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        private string _loginId = string.Empty;
        private string _loginPassword = string.Empty;

        private ICommand _changeLaterCommand;
        public ICommand ChangeLaterCommand
        {
            get { return this._changeLaterCommand ?? (this._changeLaterCommand = new RelayCommand<IDialogWindow>(OnChangeLater)); }
        }

        private readonly IDialogService _dialogService;
        private readonly IDatabaseService _databaseService;
        private readonly PasswordService _passwordService;

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand<IDialogWindow>(OnOk)); }
        }

        public PasswordExpiryCheckViewModel(IDialogService dialogService, IDatabaseService databaseService, PasswordService passwordService)
        {
            _dialogService = dialogService;
            _databaseService = databaseService;
            _passwordService = passwordService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is NavigationEventArgs navArgs && navArgs.ExtraData is Dictionary<string, object> data)
            {
                _loginId = data["id"] as string ?? string.Empty;
                _loginPassword = data["password"] as string ?? string.Empty;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void OnChangeLater(IDialogWindow dialog)
        {
            UpdatePasswordChangedAt();

            var parameter = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = _loginPassword
            };

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
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

            var parameter = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = ConfirmPassword
            };

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
        }

        private void InputPasswordClear()
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private bool ExecuteChangePassword()
        {
            if (_loginPassword == string.Empty) return false;

            if (!_passwordService.IsSamePassword(_loginPassword, OldPassword, "[Old Password]")) return false;

            if (!_passwordService.IsNotSamePassword(OldPassword, NewPassword, "[Old/New Password]")) return false;

            if (!_passwordService.IsSamePassword(NewPassword, ConfirmPassword, "[New/Confirm Password]")) return false;

            if (_passwordService.GetPasswordValidationError(ConfirmPassword) is { } message)
            {
                _passwordService.ShowAlert(_l10n["Information"], message);
                return false;
            }

            UpdatePasswordReset();

            _passwordService.ShowAlert(_l10n["Information"], "Password changed successfully");

            return true;
        }
        private bool UpdatePasswordReset()
        {
            var commandText = SqlQuery.GetQuery("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = ConfirmPassword,
                ["before_password"] = _loginPassword,
                ["reset"] = false
            };

            _databaseService.UpdateData(commandText, parameters);

            return true;
        }

        private bool UpdatePasswordChangedAt()
        {
            var commandText = SqlQuery.GetQuery("UpdatePasswordChangedAt");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = _loginPassword,
                ["password_changed_at"] = DateTime.Now.AddDays(90)
            };

            _databaseService.UpdateData(commandText, parameters);

            return true;
        }
    }
}
