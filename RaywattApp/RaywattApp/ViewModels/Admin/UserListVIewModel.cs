using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Admin
{
    public partial class UserListVIewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserListVIewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private IList<User> _users;

        private ICommand _editInstituteCommand;
        public ICommand EditInstituteCommand
        {
            get { return this._editInstituteCommand ?? (this._editInstituteCommand = new RelayCommand(EditInstitute)); }
        }

        private ICommand _changeAdminPasswordCommand;
        public ICommand ChangeAdminPasswordCommand
        {
            get { return this._changeAdminPasswordCommand ?? (this._changeAdminPasswordCommand = new RelayCommand(ChangeAdminPassword)); }
        }

        private ICommand _addUserCommand;
        public ICommand AddUserCommand
        {
            get { return this._addUserCommand ?? (this._addUserCommand = new RelayCommand(AddUser)); }
        }

        private ICommand _editUserCommand;
        public ICommand EditUserCommand
        {
            get { return this._editUserCommand ?? (this._editUserCommand = new RelayCommand(EditUser)); }
        }

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

        private void EditInstitute()
        {
            _log.Debug("EditInstitute");
        }

        private void ChangeAdminPassword()
        {
            _log.Debug("ChangeAdminPassword");
        }

        private void AddUser()
        {
            _log.Debug("AddUser");
        }

        private void EditUser()
        {
            _log.Debug("EditUser");
        }
    }
}
