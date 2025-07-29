using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Models;
using System.Collections.Generic;
using System.Windows.Navigation;
using System;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Views.Dialog;

namespace RaywattApp.ViewModels.Admin
{
    public partial class UserEditViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserEditViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private User _user = new();

        [ObservableProperty]
        private string _temporaryPassword;

        private string userId;

        private ICommand _resetPasswordCommand;
        public ICommand ResetPasswordCommand
        {
            get { return this._resetPasswordCommand ?? (this._resetPasswordCommand = new RelayCommand(ResetPassword)); }
        }

        private ICommand _deleteUserCommand;
        public ICommand DeleteUserCommand
        {
            get { return this._deleteUserCommand ?? (this._deleteUserCommand = new RelayCommand(DeleteUser)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return this._saveCommand ?? (this._saveCommand = new RelayCommand(Save, CanSave)); }
        }

        public UserEditViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("UserEditViewModel");

            Constants.CurrentPage = Constants.UserEditPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;            
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                User = (User)data["user"];
                this.userId = User.Id;

                User.PropertyChanged += User_PropertyChanged;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void ResetPassword()
        {
            _log.Debug("ResetPassword");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = this.userId;
            sqlParameters["password"] = TemporaryPassword = CommonUtil.GenerateRandomPassword();

            int nRows = _sqlManager.ResetPasswordUser(sqlParameters);

            if (nRows == 0)
            {
                _log.Error("Update Error");
            }
            else
            {
                Dictionary<string, object> popupParameter = new Dictionary<string, object>();
                popupParameter["title"] = _l10n["Information"];
                popupParameter["message"] = _l10n["Password initialized"] + "\n\n" + "Password : " + TemporaryPassword;
                var popupResult = _dialogService.OpenDialog(new AlertDialogControl(), popupParameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
            }
        }

        private void DeleteUser()
        {
            _log.Debug("DeleteUser");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Confirm deletion of selected user"];
            var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = this.userId;

                int nRows = _sqlManager.DeleteUser(sqlParameters);

                if (nRows == 0)
                {
                    _log.Error("Delete Error");
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
                }
            }
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
        }

        private void User_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _log.Debug("User_PropertyChanged");

            (SaveCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(User.Id);
        }

        private void Save()
        {
            _log.Debug("Save");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = User.Id = User.Id.Trim();

            if (!this.userId.Equals(User.Id))
            {
                int nCnt = _sqlManager.CountUser(sqlParameters);

                if (nCnt > 0)
                {
                    User.ValidateId = _l10n["ID is duplicated"];

                    return;
                }
            }

            sqlParameters.Clear();
            sqlParameters["id"] = this.userId;
            sqlParameters["newId"] = User.Id;
            sqlParameters["comment"] = User.Comment;

            int nRows = _sqlManager.UpdateUser(sqlParameters);

            if (nRows == 0)
            {
                _log.Error("Update Error");
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
            }
        }
    }
}
