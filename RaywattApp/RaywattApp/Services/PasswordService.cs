using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Localization;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace RaywattApp.Services
{
    public class PasswordService : IPasswordService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PasswordService));

        private readonly IDialogService _dialogService;
        private readonly IDatabaseService _databaseService;
        protected readonly DynamicResource _l10n;
        private const int _passwordExpiryDays = 90;
        private readonly int _maxPasswordRetryCount = 5;
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

        public PasswordService(IDialogService dialogService, IDatabaseService databaseService)
        {
            _log.Debug("PasswordService");

            _l10n = (DynamicResource)App.Current.Resources["L10N"];

            _dialogService = dialogService;
            _databaseService = databaseService;
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

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["id"] = id;
            sqlParameters["admin"] = false;

            string password = GetPasswordByUserId(id);

            if(password != inputPassword || password == string.Empty)
            {
                if (_currentPasswordRetryCount >= _maxPasswordRetryCount)
                {
                    ShowAlert(_l10n["Information"], $"You have entered the wrong password {_maxPasswordRetryCount} times.");
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                    ResetPasswordCount();
                    return false;
                }

                ShowAlert(_l10n["Information"], "Please verify your ID and password and try again\r\n" + _currentPasswordRetryCount);
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
            sqlParameters["admin"] = false;
            
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

        public void ShowAlert(string title, string message, string timer = "")
        {
            _log.Debug("ShowAlert");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["title"] = title,
                    ["message"] = message
                };

                if(timer != "")
                {
                    parameters.Add("timer", timer);
                }

                _dialogService.OpenDialog(new AlertDialogControl(), parameters, Constants.ApplicationWidth, Constants.ApplicationHeight);
            });
        }

        public bool UpdatePasswordReset(string id, string password, string before_passowrd, bool admin)
        {
            _log.Debug("UpdatePasswordReset");

            var commandText = SqlQuery.GetQuery("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = id,
                ["password"] = password,
                ["before_password"] = before_passowrd,
                ["reset"] = false,
                ["admin"] = admin
            };

            _databaseService.UpdateData(commandText, parameters);

            return true;
        }
    }
}
