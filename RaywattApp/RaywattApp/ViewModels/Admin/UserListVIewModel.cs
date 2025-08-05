using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using System.Linq;
using RaywattApp.Views.Dialog;
using System;

namespace RaywattApp.ViewModels.Admin
{
    public partial class UserListVIewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserListVIewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private IList<User> _users;

        [ObservableProperty]
        private Institute _institute = new();

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
            get { return this._editUserCommand ?? (this._editUserCommand = new RelayCommand<User>(EditUser)); }
        }

        public UserListVIewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("UserListVIewModel");

            Constants.CurrentPage = Constants.UserListPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Users = _sqlManager.SelectUserList();

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Institute";
            IList<Configuration> institute = _sqlManager.SelectConfiguration(sqlParameters);
            if (institute != null && institute.Count > 0)
            {
                string tempName = institute.FirstOrDefault(x => x.Key == "Info").Value;
                string tempComment = institute.FirstOrDefault(x => x.Key == "Info").Buffer;
                Institute.Name = tempName == null ? "" : tempName;
                Institute.Comment = tempComment == null ? "" : tempComment;
            }
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

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["name"] = Institute.Name;
            parameter["comment"] = Institute.Comment;
            var result = _dialogService.OpenDialog(new EditInstituteDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                Institute.Name = data["name"].ToString();
                Institute.Comment = data["comment"].ToString();
            }
        }

        private void ChangeAdminPassword()
        {
            _log.Debug("ChangeAdminPassword");

            _dialogService.OpenDialog(new PasswordChangeDialogControl(), null, Constants.ApplicationWidth, Constants.ApplicationHeight);
        }

        private void AddUser()
        {
            _log.Debug("AddUser");

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserNewPage));
        }

        private void EditUser(User user)
        {
            _log.Debug("EditUser");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["user"] = user;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserEditPage) { Parameter = parameter });
        }
    }
}
