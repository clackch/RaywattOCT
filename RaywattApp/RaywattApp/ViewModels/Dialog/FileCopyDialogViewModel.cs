using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileCopyDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileCopyDialogViewModel));

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        Dictionary<string, string> _files;

        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private string? _contents;

        [ObservableProperty]
        private bool enableDone = false;

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Files = (Dictionary<string,string>)data["files"];

            if (data.ContainsKey("filePath"))
                FilePath = data["filePath"].ToString();
            if (data.ContainsKey("contents"))
                Contents = data["contents"].ToString();

            FileCopy();
        }

        private async void FileCopy()
        {
            if (Files.Count == 0)
                Progress = 100;
            else
                await CommonUtil.CopyFiles(Files, prog => Progress = prog);

            if(!String.IsNullOrEmpty(FilePath) && !String.IsNullOrEmpty(Contents))
                CommonUtil.Encryptor(FilePath, Contents);

            EnableDone = true;
        }
    }
}
