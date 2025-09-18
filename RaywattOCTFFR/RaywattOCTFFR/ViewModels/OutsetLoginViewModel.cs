using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Enums;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattOCTFFR.ViewModels
{

    public partial class OutsetLoginViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoginViewModel));

        private readonly SqlManager _sqlManager;
        private readonly IPasswordService _passwordService;

        private IDialogService _dialogService;

        private User? _user;

        [ObservableProperty]
        private TextValidator _id = new TextValidator();

        [ObservableProperty]
        private string _password = string.Empty;

        private ICommand _loginCommand;
        public ICommand LoginCommand
        {
            get { return this._loginCommand ?? (this._loginCommand = new RelayCommand(Login, CanLogin)); }
        }

        private ICommand _exitCommand;
        public ICommand ExitCommand
        {
            get { return this._exitCommand ?? (this._exitCommand = new RelayCommand(Exit)); }
        }

        public OutsetLoginViewModel(SqlManager sqlManager, IDialogService dialogService, IPasswordService passwordService)
        {
            _log.Debug("OutsetLoginViewModel");

            Constants.CurrentPage = Constants.OutsetLoginPage;

            Id.PropertyChanged += OnIdPropertyChanged;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _passwordService = passwordService;
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is NavigationEventArgs navArgs && navArgs.ExtraData is Dictionary<string, object> data)
            {
                if (data.TryGetValue("login_step", out var stepObj) && stepObj is LoginStep step)
                {
                    _user = data.GetValueOrDefault("user") as User ?? new User();
                    ExecuteLoginStep(step);
                }
            }
        }

        private void Login()
        {
            _log.Debug("Login");
            ExecuteLoginStep(LoginStep.AttemptLogin);
        }

        private void ExecuteLoginStep(LoginStep step)
        {
            _log.Debug($"ExecuteLoginStep: {step}");

            switch (step)
            {
                case LoginStep.AttemptLogin:
                    AttemptLogin();
                    ClearTextBox();
                    break;
                case LoginStep.CheckInitialPasswordReset:
                    if (!HandleInitialPasswordReset()) ExecuteLoginStep(LoginStep.CheckPasswordExpiry);
                    break;
                case LoginStep.CheckPasswordExpiry:
                    if (!CheckPasswordExpiry()) ExecuteLoginStep(LoginStep.CheckTermsAgreement);
                    break;
                case LoginStep.CheckTermsAgreement:
                    if (EnsureTermsAgreement()) ExecuteLoginStep(LoginStep.FinalizeLogin);
                    break;
                case LoginStep.FinalizeLogin:
                    FinalizeLogin();
                    break;
            }
        }

        private void AttemptLogin()
        {
            _log.Debug("AttemptLogin");

            PasswordService.CurrentPasswordRetryCount++;
            if (!_passwordService.CheckLoginWithRetryCount(Id.Text, Password))
            {
                return;
            }

            DeviceStatus.LoginID = Id.Text;

            _user = _passwordService.GetAccount(DeviceStatus.LoginID);
            
            IPasswordService.ResetPasswordCount();

            ExecuteLoginStep(LoginStep.CheckInitialPasswordReset);
        }

        private bool HandleInitialPasswordReset()
        {
            _log.Debug("HandleInitialPasswordReset");

            _user = _passwordService.GetAccount(DeviceStatus.LoginID);

            if (_user?.PasswordReset == true)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.InitialPasswordSetupPage)
                {
                    Parameter = new Dictionary<string, object> { ["user"] = _user }
                });
                return true;
            }
            return false;
        }

        private bool CheckPasswordExpiry()
        {
            _log.Debug("CheckPasswordExpiry");

            _user = _passwordService.GetAccount(DeviceStatus.LoginID);

            if ((DateTime.Now - _user.PasswordChangedAt).TotalDays > _passwordService.PasswordExpiryDays)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PasswordExpiryCheckPage)
                {
                    Parameter = new Dictionary<string, object> { ["user"] = _user }
                });
                return true;
            }
            return false;
        }

        private bool EnsureTermsAgreement()
        {
            _log.Debug("EnsureTermsAgreement");

            _user = _passwordService.GetAccount(DeviceStatus.LoginID);

            if (_user?.TermsAgreedAt > DateTime.MinValue) return true;

            var parameter = new Dictionary<string, object> { ["tnC"] = _user! };
            var result = _dialogService.OpenDialog(new TermsConditionsControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result?.DialogAnswer == DialogResults.Answer.No)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                return false;
            }

            _sqlManager.UpdateTermsAgreedDateUser(new Dictionary<string, object> { ["id"] = _user!.Id });
            return true;
        }

        private void FinalizeLogin()
        {
            _log.Debug("FinalizeLogin");

            _user = _passwordService.GetAccount(DeviceStatus.LoginID);

            if (_user!.Admin == true)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage)
                {
                    Parameter = new Dictionary<string, object>
                    {
                        ["id"] = _user!.Id,
                        ["password"] = _user!.Password
                    }
                });
            }
        }

        private void ClearTextBox()
        {
            _log.Debug("ClearTextBox");

            Id.Text = string.Empty;
            Password = string.Empty;
        }

        partial void OnPasswordChanged(string value)
        {
            (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void OnIdPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextValidator.Text))
            {
                (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrEmpty(Id.Text) && !string.IsNullOrEmpty(Password);
        }

        private void Exit()
        {
            _log.Debug("Exit");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Power Off"];
            parameter["message"] = _l10n["Are you sure you want to shut down the power?"];
            var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                CommonUtil.Exit(DeviceStatus, null, true, true);
            }
        }
    }
}
