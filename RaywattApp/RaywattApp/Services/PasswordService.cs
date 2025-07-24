using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Localization;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace RaywattApp.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly IDialogService _dialogService;
        private readonly IDatabaseService _databaseService;
        protected readonly DynamicResource _l10n;
        private const int _passwordExpiryDays = 90;

        public int PasswordExpiryDays
        {
            get => _passwordExpiryDays;
        }

        public PasswordService(IDialogService dialogService, IDatabaseService databaseService)
        {
            _l10n = (DynamicResource)App.Current.Resources["L10N"];

            _dialogService = dialogService;
            _databaseService = databaseService;
        }

        public bool IsSamePassword(string beforePassword, string inputPassword, string message = "")
        {
            if (!beforePassword.Equals(inputPassword))
            {
                ShowAlert(_l10n["Information"], $"The password is incorrect.\r\n{message}");
                return false;
            }

            return true;
        }

        public bool IsNotSamePassword(string beforePassword, string inputPassword, string message = "")
        {
            if (beforePassword.Equals(inputPassword))
            {
                ShowAlert(_l10n["Information"], $"The password is correct.\r\n{message}");
                return false;
            }

            return true;
        }

        public string GetPasswordByUserId(string id)
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["id"] = id;

            var commandText = SqlQuery.GetQuery("SelectUserListById");
            var userData = _databaseService.GetDatas<User>(commandText, sqlParameters);
            string password = userData.Count > 0 ? userData[0].Password : string.Empty;

            if (password == string.Empty)
            {
                ShowAlert(_l10n["Information"], "The login ID is not registered.");
                return string.Empty;
            }

            return password;
        }

        public string? GetPasswordValidationError(string password)
        {
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

        public bool UpdatePasswordReset(string id, string password, string before_passowrd)
        {
            var commandText = SqlQuery.GetQuery("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = id,
                ["password"] = password,
                ["before_password"] = before_passowrd,
                ["reset"] = false
            };

            _databaseService.UpdateData(commandText, parameters);

            return true;
        }
    }
}
