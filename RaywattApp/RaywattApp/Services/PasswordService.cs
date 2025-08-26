using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Localization;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace RaywattApp.Services
{
    public class PasswordService : IPasswordService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordService));

        private readonly IDialogService _dialogService;
        private readonly SqlManager _sqlManager;

        private readonly DynamicResource _l10n;
        private int _passwordExpiryDays = 90;
        private int _maxPasswordRetryCount = 5;
        private TimeSpan _passwordRetryLockDuration = TimeSpan.FromSeconds(30);

        private static int _currentPasswordRetryCount;

        public static int CurrentPasswordRetryCount
        {
            get => _currentPasswordRetryCount;
            set => _currentPasswordRetryCount = value;
        }

        public int PasswordExpiryDays
        {
            get => _passwordExpiryDays;
        }


        public static void ResetPasswordCount()
        {
            _log.Debug("ResetPasswordCount");

            PasswordService._currentPasswordRetryCount = 0;
        }

        public PasswordService(IDialogService dialogService, SqlManager sqlManager)
        {
            _log.Debug("PasswordService");

            _l10n = (DynamicResource)App.Current.Resources["L10N"];

            _dialogService = dialogService;
            _sqlManager = sqlManager;

            GetPasswordParameter();
        }

        public bool IsPasswordConfirmed(string beforePassword, string inputPassword, string message1, string message2)
        {
            _log.Debug("IsPasswordConfirmed");

            if (!beforePassword.Equals(inputPassword, StringComparison.Ordinal))
            {
                ShowAlert(_l10n["Information"], string.Format(CultureInfo.CurrentCulture, "{0} password and {1} password do not match.\r\nRe-enter passwords.", message1, message2));
                return false;
            }

            return true;
        }

        public bool IsPasswordCorrect(string beforePassword, string inputPassword, string message)
        {
            _log.Debug("IsPasswordCorrect");

            if (!beforePassword.Equals(inputPassword, StringComparison.Ordinal))
            {
                ShowAlert(_l10n["Information"], string.Format(CultureInfo.CurrentCulture, "{0} password is incorrect.", message));
                return false;
            }

            return true;
        }

        public bool CheckLoginWithRetryCount(string id, string inputPassword)
        {
            _log.Debug("CheckLoginWithRetryCount " + PasswordService._currentPasswordRetryCount);

            string password = GetAccount(id)?.Password ?? string.Empty;

            if (password != inputPassword || password == string.Empty)
            {
                if (PasswordService._currentPasswordRetryCount >= _maxPasswordRetryCount + 1)
                {
                    ShowTimerAlert(_l10n["Information"], "Login temporarily disabled.\r\nPlease try again in {0}.", false, _passwordRetryLockDuration);
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                    ResetPasswordCount();
                    return false;
                }

                string alertMessage = string.Format( CultureInfo.CurrentCulture,
                                                    "The user ID or password entered is incorrect.\r\nPlease try again. {0}/{1}",
                                                    PasswordService._currentPasswordRetryCount,
                                                    _maxPasswordRetryCount);

                ShowAlert(_l10n["Information"], alertMessage);

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                return false;
            }

            return true;
        }

        public bool IsNotSamePassword(string beforePassword, string inputPassword, string message1, string message2)
        {
            _log.Debug("IsNotSamePassword");

            if (beforePassword.Equals(inputPassword, StringComparison.Ordinal))
            {
                ShowAlert(_l10n["Information"], string.Format(CultureInfo.CurrentCulture, "{0} password and {1} password cannot be the same.", message1, message2));
                return false;
            }

            return true;
        }

        public User? GetAccount(string id)
        {
            _log.Debug($"GetAccount: {id}");

            var parameters = new Dictionary<string, object> { ["id"] = id };

            var userList = _sqlManager.SelectUser(parameters);
            if (userList != null && userList.Count > 0)
            {
                return userList[0];
            }

            var adminList = _sqlManager.SelectAdmin(parameters);
            if (adminList != null && adminList.Count > 0)
            {
                return adminList[0];
            }

            return null;
        }

        public string? GetPasswordValidationError(string password)
        {
            _log.Debug("GetPasswordValidationError");

            if (password.Length < 8)
                return "Password requires at least 8 characters.";

            if (!Regex.IsMatch(password, @"[A-Z]"))
                return "Include at least one uppercase letter in the password.";

            if (!Regex.IsMatch(password, @"\d"))
                return "Include at least one number in the password.";

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_\-+=\[\]{};':""\\|,.<>\/?]"))
                return "Include at least one special character in the password.\r\n";

            return null;
        }

        public string MessagePasswordChangedSuccessfully
        {
            get
            {
               return  "Password changed successfully.";
            }
        }

        public void ShowAlert(string title, string message)
        {
            _log.Debug("ShowAlert");

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

        public void ShowTimerAlert(string title, string message, bool isShowButton, TimeSpan? timeSpan)
        {
            _log.Debug("ShowTimerAlert");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["title"] = title,
                    ["message"] = message,
                    ["wait_seconds"] = timeSpan!,
                    ["show_button"] = isShowButton
                };

                _dialogService.OpenDialog(new AlertTimerDialogControl(), parameters, Constants.ApplicationWidth, Constants.ApplicationHeight);
            });
        }


        public bool UpdatePasswordReset(string id, string password, string beforePassowrd)
        {
            _log.Debug("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = id,
                ["password"] = password,
                ["before_password"] = beforePassowrd,
                ["reset"] = false,
            };

            _sqlManager.UpdatePasswordReset(parameters);

            return true;
        }

        private void GetPasswordParameter()
        {
            _log.Debug("PasswordParameter");

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Password";

            IList<Configuration> passwordParameter = _sqlManager.SelectConfiguration(sqlParameters);

            _passwordExpiryDays = int.TryParse(
                passwordParameter.FirstOrDefault(x => x.Key == "ExpiryDay")?.Value,
                out var day) ? day : _passwordExpiryDays;

            _maxPasswordRetryCount = int.TryParse(
                passwordParameter.FirstOrDefault(x => x.Key == "MaxCount")?.Value,
                out var count) ? count : _maxPasswordRetryCount;

            _passwordRetryLockDuration = TimeSpan.TryParse(
                passwordParameter.FirstOrDefault(x => x.Key == "WaitSecond")?.Value,
                out var second) ? second : _passwordRetryLockDuration;
        }


    }
}
