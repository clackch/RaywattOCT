using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;

namespace RaywattApp.ViewModels.Admin
{
    public partial class UserListVIewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserListVIewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private IList<User> _users;

        public UserListVIewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("UserListVIewModel");

            Constants.CurrentPage = Constants.UserListPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Users = _sqlManager.SelectUserList();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }
    }
}
