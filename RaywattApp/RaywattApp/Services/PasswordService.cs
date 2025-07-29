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
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace RaywattApp.Services
{
    public class PasswordService : IPasswordService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordService));

        private readonly IDialogService _dialogService;
        private readonly IDatabaseService _databaseService;
        private readonly SqlManager _sqlManager;

        protected readonly DynamicResource _l10n;
        private int _passwordExpiryDays = 90;
        private int _maxPasswordRetryCount = 3;
        private TimeSpan PasswordRetryLockDuration = TimeSpan.FromSeconds(30);

        static private int _currentPasswordRetryCount = 0;

        public int PasswordExpiryDays
        {
            get => _passwordExpiryDays;
        }

        public void ResetPasswordCount()
        {
            _log.Debug("ResetPasswordCount");
            _currentPasswordRetryCount = 0;
        }

        public PasswordService(IDialogService dialogService, IDatabaseService databaseService, SqlManager sqlManager)
        {
            _log.Debug("PasswordService");

            _l10n = (DynamicResource)App.Current.Resources["L10N"];

            _dialogService = dialogService;
            _databaseService = databaseService;
            _sqlManager = sqlManager;

            GetPasswordParameter();
        }

        public bool IsSamePassword(string beforePassword, string inputPassword, string message = "")
        {
            _log.Debug("IsSamePassword");

            if (!beforePassword.Equals(inputPassword))
            {
                ShowAlert(_l10n["Information"], $"The password is incorrect.\r\n{message}");
                return false;
            }

            return true;
        }

        public bool CheckLoginWithRetryCount(string id, string inputPassword)
        {
            _currentPasswordRetryCount++;

            _log.Debug("CheckLoginWithRetryCount " + _currentPasswordRetryCount);

            string password = GetPasswordByUserId(id);

            if (password != inputPassword || password == string.Empty)
            {
                if (_currentPasswordRetryCount >= _maxPasswordRetryCount)
                {
                    ShowTimerAlert("Login Failed", $"Too many incorrect password attempts.\r\n\r\n{_maxPasswordRetryCount} times", false, PasswordRetryLockDuration);
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                    ResetPasswordCount();
                    return false;
                }

                ShowAlert(_l10n["Information"], "Invalid ID or Password\r\nPlease try again\r\n\r\nAttempt: " + _currentPasswordRetryCount + "/" + _maxPasswordRetryCount);
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                return false;
            }

            return true;
        }

        public bool IsNotSamePassword(string beforePassword, string inputPassword, string message = "")
        {
            _log.Debug("IsNotSamePassword");

            if (beforePassword.Equals(inputPassword))
            {
                ShowAlert(_l10n["Information"], $"The password is correct.\r\n{message}");
                return false;
            }

            return true;
        }

        public string GetPasswordByUserId(string id)
        {
            _log.Debug("GetPasswordByUserId");

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["id"] = id;

            var commandText = SqlQuery.GetQuery("SelectUserById");
            var userData = _databaseService.GetDatas<User>(commandText, sqlParameters);
            string password = userData.Count > 0 ? userData[0].Password : string.Empty;

            return password;
        }

        public string? GetPasswordValidationError(string password)
        {
            _log.Debug("GetPasswordValidationError");

            if (string.IsNullOrWhiteSpace(password) || password == string.Empty)
                return "Please enter a password.";

            if (password.Contains(" "))
                return "Spaces are not allowed in the password.";

            if (password.Length < 8)
                return "Password should be at least 8 characters.";

            if (!Regex.IsMatch(password, @"[A-Z]"))
                return "Please include at least one uppercase letter.";

            if (!Regex.IsMatch(password, @"\d"))
                return "Please include at least one number.";

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_\-+=\[\]{};':""\\|,.<>\/?]"))
                return "Please include at least one special character.";

            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9!@#$%^&*()_\-+=\[\]{};':""\\|,.<>\/?]+$"))
                return "Only English letters, numbers, and common symbols are allowed.";

            return null;
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


        public bool UpdatePasswordReset(string id, string password, string before_passowrd)
        {
            _log.Debug("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = id,
                ["password"] = password,
                ["before_password"] = before_passowrd,
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

            _passwordExpiryDays = int.Parse(passwordParameter.FirstOrDefault(x => x.Key == "ExpiryDay").Value);
            _maxPasswordRetryCount = int.Parse(passwordParameter.FirstOrDefault(x => x.Key == "MaxCount").Value);
            int waitSeconds = int.Parse(passwordParameter.FirstOrDefault(x => x.Key == "WaitSecond").Value);
            PasswordRetryLockDuration = TimeSpan.FromSeconds(waitSeconds);
        }
    }
}
