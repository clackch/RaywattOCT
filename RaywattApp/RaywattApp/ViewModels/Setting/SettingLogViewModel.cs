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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingLogViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingLogViewModel));

        private readonly SqlManager _sqlManager;

        List<FileInformation> selectedLogFileList;

        [ObservableProperty]
        private ObservableCollection<FileInformation> _logFileList;

        [ObservableProperty]
        private string _searchYear;

        [ObservableProperty]
        private string _searchMonth;

        private ICommand _logExportCommand;
        public ICommand LogExportCommand
        {
            get { return this._logExportCommand ?? (this._logExportCommand = new RelayCommand<object>(LogExport)); }
        }

        private ICommand _searchParameterCommand;
        public ICommand SearchParameterCommand
        {
            get { return this._searchParameterCommand ?? (this._searchParameterCommand = new RelayCommand<string>(SetSearchParameter)); }
        }

        public SettingLogViewModel(SqlManager sqlManager, IDialogService dialogService) : base(dialogService)
        {
            _log.Debug("SettingLogExportDialogViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            FileExport = new FileExport();
            FileExport.ExternalDrivePath = "";

            DateTime dateTimeNow = DateTime.Now;

            SearchYear = dateTimeNow.Year.ToString();
            SearchMonth = MonthZeroPadding(dateTimeNow.Month.ToString());

            LogFileList = new ObservableCollection<FileInformation>();
            GetLogFileList();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
        }

        private void GetLogFileList()
        {
            DirectoryInfo directory = new DirectoryInfo(Constants.LogFolderPath);
            string fileNameFilter = "*_" + SearchYear + "-" + SearchMonth + "-" + Constants.LogExtension;
            FileInfo[] logFileList = directory.GetFiles(fileNameFilter);
            var orderedLogFIleList = logFileList.OrderByDescending(x => x.LastWriteTime);

            LogFileList.Clear();

            foreach (FileInfo logFile in orderedLogFIleList)
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

        private void SetSearchParameter(string type)
        {
            int num;

            switch (type)
            {
                case "YP"://Year Previous
                    num = int.Parse(SearchYear);
                    if (num > 1)
                        SearchYear = (num - 1).ToString();
                    break;
                case "YN"://Year Next
                    num = int.Parse(SearchYear);
                    SearchYear = (num + 1).ToString();
                    break;
                case "MP"://Month Previous
                    num = int.Parse(SearchMonth);
                    if(num > 1)
                    {
                        SearchMonth = MonthZeroPadding((num - 1).ToString());
                    }
                    else
                    {
                        SetSearchParameter("YP");
                        SearchMonth = "12";
                    }
                    break;
                case "MN"://Month Next
                    num = int.Parse(SearchMonth);
                    if(num < 12)
                    {
                        SearchMonth = MonthZeroPadding((num + 1).ToString());
                    }
                    else
                    {
                        SetSearchParameter("YN");
                        SearchMonth = "01";
                    }                        
                    break;
                default:
                    break;
            }

            GetLogFileList();
        }

        private static string MonthZeroPadding(string month)
        {
            string result = month;

            if (month.Length == 1)
                result = "0" + month;

            return result;
        }
    }
}
