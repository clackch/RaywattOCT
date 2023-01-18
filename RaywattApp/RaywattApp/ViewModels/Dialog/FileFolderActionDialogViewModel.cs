using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using System.Collections.Generic;
using System;
using System.Windows;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using log4net;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileFolderActionDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileFolderActionDialogViewModel));

        [ObservableProperty]
        private DirectoryItem _selectedDir;

        [ObservableProperty]
        private Visibility _isErrorMessage;

        private string folderAction;

        private string _createRenameFolderName;
        public string CreateRenameFolderName
        {
            get { return _createRenameFolderName; }
            set { _createRenameFolderName = value; IsErrorMessage = Visibility.Collapsed; OnPropertyChanged(nameof(CreateRenameFolderName)); }
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            folderAction = data["folderAction"].ToString(); ;
            SelectedDir = (DirectoryItem)data["selectedDir"];

            if (folderAction == Constants.FolderActionRename)
            {
                Title = _l10n["Rename Folder"];
                CreateRenameFolderName = SelectedDir.Name;
            }
            else
            {
                Title = _l10n["Create New Folder"];
                CreateRenameFolderName = "";
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            DirectoryProvider directoryProvider = new DirectoryProvider();

            if (folderAction == Constants.FolderActionRename)
            {
                if (SelectedDir.Name.Equals(CreateRenameFolderName.Trim()))
                {
                    base.AnswerYes(dialog);
                    return;
                }

                bool result = directoryProvider.DuplicateCheckRename(SelectedDir.Path, SelectedDir.Name, CreateRenameFolderName.Trim());
                if (!result)
                {
                    IsErrorMessage = Visibility.Visible;
                    return;
                }
            }
            else
            {
                bool result = directoryProvider.DuplicateCheck(SelectedDir.Path, CreateRenameFolderName.Trim());
                if (!result)
                {
                    IsErrorMessage = Visibility.Visible;
                    return;
                }
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["createRenameFolderName"] = CreateRenameFolderName.Trim();

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);

        }
    }
}
