using System.Collections.Generic;
using System.Windows.Input;
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

namespace RaywattApp.ViewModels
{
    public partial class OutsetLoginViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoginViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private TextValidator _id = new TextValidator();

        [ObservableProperty]
        private TextValidator _password = new TextValidator();

        private ICommand _loginCommand;
        public ICommand LoginCommand
        {
            get { return this._loginCommand ?? (this._loginCommand = new RelayCommand(Login)); }
        }

        private ICommand _exitCommand;
        public ICommand ExitCommand
        {
            get { return this._exitCommand ?? (this._exitCommand = new RelayCommand(Exit)); }
        }

        public OutsetLoginViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("OutsetLoginViewModel");

            Constants.CurrentPage = Constants.OutsetLoginPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Login()
        {
            _log.Debug("Login");

            if("Admin".Equals(Id.Text))
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
            else
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoadingPage));
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
