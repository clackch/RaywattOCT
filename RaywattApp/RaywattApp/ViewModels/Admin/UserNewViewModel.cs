using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Models;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using System.Collections.Generic;
using System;
using RaywattApp.Common.Util;
using RaywattApp.Views.Dialog;

namespace RaywattApp.ViewModels.Admin
{
    public partial class UserNewViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserNewViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private User _user = new();

        [ObservableProperty]
        private string _temporaryPassword;

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

        public UserNewViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("UserNewViewModel");

            Constants.CurrentPage = Constants.UserNewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            User.PropertyChanged += User_PropertyChanged;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
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

            //Duplication Check
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = User.Id.Trim();

            int nCnt = _sqlManager.CountUser(sqlParameters);

            if (nCnt > 0)
            {
                User.ValidateId = _l10n["ID is duplicated"];

                return;
            }

            //Insert
            sqlParameters.Clear();
            sqlParameters["id"] = User.Id = User.Id.Trim();
            sqlParameters["password"] = TemporaryPassword = CommonUtil.GenerateRandomPassword();
            sqlParameters["comment"] = User.Comment;

            int nRows = _sqlManager.InsertUser(sqlParameters);

            if (nRows == 0)
            {
                _log.Error("Insert Error");
            }
            else
            {
                Dictionary<string, object> popupParameter = new Dictionary<string, object>();
                popupParameter["title"] = _l10n["Information"];
                popupParameter["message"] = _l10n["A new user has been added"] + "\n\n" + "Password : " + TemporaryPassword;
                var popupResult = _dialogService.OpenDialog(new AlertDialogControl(), popupParameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
            }
        }
    }
}
