using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using RaywattApp.Views.Dialog;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileFolderBrowseDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileFolderBrowseDialogViewModel));

        private IDialogService _dialogService;

        [ObservableProperty]
        private FileExport _fileExport;

        [ObservableProperty]
        private DirectoryItem _selectedDir;

        private DirectoryProvider directoryProvider;

        private ObservableCollection<Item> _dirItems;
        public ObservableCollection<Item> DirItems
        {
            get { return _dirItems; }
            set
            {
                _dirItems = value;
                OnPropertyChanged(nameof(DirItems));
            }
        }

        private bool isSelectedPathChanged;

        private ICommand _folderActionCommand;
        public ICommand FolderActionCommand
        {
            get { return this._folderActionCommand ?? (this._folderActionCommand = new RelayCommand<string>(FolderAction)); }
        }

        public FileFolderBrowseDialogViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            FileExport = new();
            directoryProvider = new();

            isSelectedPathChanged = false;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            FileExport.ExternalDrive = data["externalDrive"].ToString();
            FileExport.ExternalDrivePath = data["externalDrivePath"].ToString();
            directoryProvider.GetDirectory(FileExport.ExternalDrive);
            DirItems = directoryProvider.DirItems;
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();

            if (SelectedDir != null)
                parameter["externalDrivePath"] = SelectedDir.Path;
            else
                parameter["externalDrivePath"] = "";

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }

        protected override void AnswerNo(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();

            parameter["isSelectedPathChanged"] = isSelectedPathChanged;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.No;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private void FolderAction(string action)
        {
            _log.Debug("FolderAction");

            if (SelectedDir == null)
                return;

            if (action == Constants.FolderActionRename)
            {
                if (DirItems[0].Path == SelectedDir.Path)
                    return;

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["folderAction"] = Constants.FolderActionRename;
                parameter["selectedDir"] = SelectedDir;

                var result = _dialogService.OpenDialog(new FileFolderActionDialogControl(), parameter, Constants.FileFolderBrowseDialogWidth, Constants.FileFolderBrowseDialogHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes && result.DialogReturn != null)
                {
                    if(!String.IsNullOrEmpty(FileExport.ExternalDrivePath))
                        isSelectedPathChanged = CheckSelectedPathChanged();

                    Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                    directoryProvider.RenameDirectory(SelectedDir.Path, SelectedDir.Name, data["createRenameFolderName"].ToString());
                }
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["folderAction"] = Constants.FolderActionCreate;
                parameter["selectedDir"] = SelectedDir;

                var result = _dialogService.OpenDialog(new FileFolderActionDialogControl(), parameter, Constants.FileFolderBrowseDialogWidth, Constants.FileFolderBrowseDialogHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                {
                    Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                    directoryProvider.AddDirectory(SelectedDir.Path, data["createRenameFolderName"].ToString());
                }
            }
        }

        private bool CheckSelectedPathChanged()
        {
            if (FileExport.ExternalDrivePath.Contains(SelectedDir.Path))
            {
                return true;
            }

            return false;
        }
    }
}
