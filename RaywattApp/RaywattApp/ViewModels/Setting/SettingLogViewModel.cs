using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.File;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingLogViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingLogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private IList<FileInformation> _logFileList;

        List<FileInformation> selectedLogFileList;

        private ICommand _logExportCommand;
        public ICommand LogExportCommand
        {
            get { return this._logExportCommand ?? (this._logExportCommand = new RelayCommand<object>(LogExport)); }
        }

        public SettingLogViewModel(SqlManager sqlManager, IDialogService dialogService) : base(dialogService)
        {
            _log.Debug("SettingLogExportDialogViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            FileExport = new FileExport();
            FileExport.ExternalDrivePath = "";


            LogFileList = new List<FileInformation>();
            GetLogFileList();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        private void GetLogFileList()
        {
            DirectoryInfo directory = new DirectoryInfo(Constants.LogFolderPath);
            FileInfo[] logFileList = directory.GetFiles(Constants.LogExtension);
            var orderedLogFIleList = logFileList.OrderByDescending(x => x.LastWriteTime);

            foreach(FileInfo logFile in orderedLogFIleList)
            {
                FileInformation fileInfo = new FileInformation();
                fileInfo.Name = logFile.Name;
                fileInfo.Size = logFile.Length;
                fileInfo.DisplaySize = CommonUtil.ByteToMB(logFile.Length) + " MB";
                fileInfo.LastWriteTime = logFile.LastWriteTime;
                fileInfo.CreationTime = logFile.CreationTime;
                LogFileList.Add(fileInfo);
            }
        }

        protected override void FileSave()
        {
            _log.Debug("FileSave");

            Dictionary<string, string> exportFiles = new Dictionary<string, string>();
            foreach(FileInformation fileInfo in selectedLogFileList)
            {
                exportFiles.Add(Constants.LogFolderPath + "\\" + fileInfo.Name, FileExport.ExternalDrivePath + "\\" + fileInfo.Name);
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Log Export"];
            parameter["logExport"] = exportFiles;
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter, Constants.SettingDialogWidth, Constants.SettingDialogHeight);
        }

        private void LogExport(object param)
        {
            IList items = (IList)param;
            List<FileInformation> logFileList = items.Cast<FileInformation>().ToList();

            if (logFileList == null || logFileList.Count == 0)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["No items have been selected"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.SettingDialogWidth, Constants.SettingDialogHeight);
                return;
            }

            UpdateFileSize(logFileList.Select(x => x.Size).Sum());

            selectedLogFileList = logFileList;

            base.Export();
        }
    }
}
