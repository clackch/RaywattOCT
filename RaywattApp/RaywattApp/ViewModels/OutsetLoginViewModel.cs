using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
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

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is not NavigationEventArgs navArgs || navArgs.ExtraData is not Dictionary<string, object> data)
                return;

            if (data.TryGetValue("login_step", out object? stepObj) && stepObj is int step)
            {
                _user = data["user"] as User ?? new User();
                LoginStep(step);
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Login()
        {
            _log.Debug("Login");

            LoginStep(1);
        }

        private void LoginStep(int nStep)
        {
            switch (nStep)
            {
                case 1:
                    // login 시도
                    if (!_passwordService.CheckLoginWithRetryCount(Id.Text, Password))
                    {
                        ClearTextBox();
                        return;
                    }

                    var user = GetUserById(Id.Text);

                    DeviceStatus.LoginID = user!.Id;
                    _passwordService.ResetPasswordCount();
                    _user = user;

                    LoginStep(2);

                    break;

                case 2:
                    // 첫 사용자 비밀번호 수정

                    if (HandleInitialPasswordReset(_user))
                    {
                        return;
                    }
                    else
                    {
                        LoginStep(3);
                    }

                    break;


                case 3:
                    // 비밀 번호 주기
                    if (CheckPasswordExpiry(_user))
                    {
                        return;
                    }
                    else
                    {
                        LoginStep(4);
                    }

                    break;

                case 4:
                    // 약관 동의
                    if (!EnsureTermsAgreement(_user))
                    {
                        return;
                    }
                    else
                    {
                        LoginStep(5);
                    }

                    break;
                case 5:
                    // 로그인 성공 후 페이지 이동
                    if (_user.Admin)
                    {
                        WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
                        return;
                    }
                    else
                    {
                        Dictionary<string, object> parameter = new Dictionary<string, object>();
                        parameter["id"] = Id.Text;
                        parameter["password"] = Password;

                        WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage) { Parameter = parameter });
                    }

                    break;

                default:
                    break;
            }

        }

        private bool HandleInitialPasswordReset(User user)
        {
            if (user.PasswordReset)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["user"] = user;

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.InitialPasswordSetupPage) { Parameter = parameter });
                return true;
            }
            return false;
        }

        private bool CheckPasswordExpiry(User user)
        {
            if ((DateTime.Now - user.PasswordChangedAt).TotalDays > _passwordService.PasswordExpiryDays)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["user"] = user;

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PasswordExpiryCheckPage) { Parameter = parameter });
                return true;
            }
            return false;
        }
        private bool EnsureTermsAgreement(User user)
        {
            if (user.TermsAgreedAt > DateTime.MinValue) return true;

            var parameter = new Dictionary<string, object> { ["tnC"] = user };
            var result = _dialogService.OpenDialog(
                new TermsConditionsControl(), parameter,
                Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result?.DialogAnswer == DialogResults.Answer.No)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                return false;
            }

            _sqlManager.UpdateTermsAgreedDateUser(new Dictionary<string, object>
            {
                ["id"] = user.Id,
            });

            return true;
        }

        private void ClearTextBox()
        {
            Id.Text = "";
            Password = "";
        }

        private User? GetUserById(string id)
        {
            var sqlParams = new Dictionary<string, object>
            {
                ["id"] = id,
            };

            var users = _sqlManager.SelectUserById(sqlParams);
            return users?.Count > 0 ? users[0] : null;
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
