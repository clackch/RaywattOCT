using RaywattApp.Common.Dialog;
using System.Collections.Generic;
using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Models;
using log4net;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FilePasswordDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FilePasswordDialogViewModel));

        [ObservableProperty]
        private FileExport _fileExport;

        [ObservableProperty]
        private Visibility _isErrorMessage;

        [ObservableProperty]
        private Visibility _isInfoMessage;

        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; IsErrorMessage = Visibility.Collapsed; IsInfoMessage = Visibility.Collapsed; }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; IsErrorMessage = Visibility.Collapsed; IsInfoMessage = Visibility.Collapsed; }
        }

        public FilePasswordDialogViewModel()
        {
            FileExport = new();
            IsErrorMessage = Visibility.Collapsed;
            IsInfoMessage = Visibility.Collapsed;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            FileExport.PasswordProtected = (bool)data["passwordProtected"];
            Password = data["password"].ToString();
            ConfirmPassword = data["confirmPassword"].ToString();
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            if (FileExport.PasswordProtected)
            {
                if (!Password.Trim().Equals(ConfirmPassword.Trim()))
                {
                    IsErrorMessage = Visibility.Visible;
                    return;
                }
                if(Password.Trim().Length == 0)
                {
                    IsInfoMessage = Visibility.Visible;
                    return;
                }                
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["passwordProtected"] = FileExport.PasswordProtected;
            parameter["password"] = Password.Trim();
            parameter["confirmPassword"] = ConfirmPassword.Trim();

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
