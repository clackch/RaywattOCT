using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.Password
{
    partial class InitialPasswordSetupViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(InitialPasswordSetupViewModel));

        private readonly IDialogService _dialogService;
        private readonly IDatabaseService _databaseService;

        private string _loginId = string.Empty;
        private string _loginPassword = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        public ICommand CancelCommand => new RelayCommand(OnCancel);
        public ICommand ConfrmCommand => new RelayCommand(OnConfirm);

        public InitialPasswordSetupViewModel(IDialogService dialogService, IDatabaseService databaseService)
        {
            _dialogService = dialogService;
            _databaseService = databaseService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is NavigationEventArgs navArgs && navArgs.ExtraData is Dictionary<string, object> data)
            {
                _loginId = data["id"] as string ?? string.Empty;
                _loginPassword = data["password"] as string ?? string.Empty;

                if(string.IsNullOrEmpty(_loginId) || string.IsNullOrEmpty(_loginPassword))
                {
                    ShowAlert(_l10n["Error"], "Login ID or password is missing.");
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void OnCancel()
        {
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
        }

        private void OnConfirm()
        {
            if (Password != ConfirmPassword)
            {
                ShowAlert(_l10n["Information"], "Passwords do not match.");
                ClearPasswords();
                return;
            }

            string? error = GetPasswordValidationError();
            if (error != null)
            {
                ShowAlert(_l10n["Information"], error);
                ClearPasswords();
                return;
            }

            UpdatePasswordInDatabase();

            var parameter = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = ConfirmPassword
            };

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
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

        private void ClearPasswords()
        {
            Password = string.Empty;
            ConfirmPassword = string.Empty;
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

        private void UpdatePasswordInDatabase()
        {
            var commandText = SqlQuery.GetQuery("UpdatePasswordReset");

            var parameters = new Dictionary<string, object>
            {
                ["id"] = _loginId,
                ["password"] = ConfirmPassword,
                ["before_password"] = _loginPassword,
                ["reset"] = true
            };

            _databaseService.UpdateData(commandText, parameters);
        }
    }
}
