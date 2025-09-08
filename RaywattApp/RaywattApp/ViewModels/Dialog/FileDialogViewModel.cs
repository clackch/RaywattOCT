using RaywattApp.Common.Dialog;
using System.Collections.Generic;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Models;
using RaywattApp.Common.Bases;
using log4net;
using RaywattApp.Common.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileDialogViewModel));

        [ObservableProperty]
        private string _popupNavigationSource;

        [ObservableProperty]
        private object _popupNavigationParameter;

        public FileDialogViewModel()
        {
            WeakReferenceMessenger.Default.Register<PopupNavigationMessage>(this, OnPopupNavigationMessage);
        }

        private void OnPopupNavigationMessage(object recipient, PopupNavigationMessage message)
        {
            _log.Debug("OnPopupNavigationMessage : " + message.Value);

            string pageUri = message.Value;
            PopupNavigationParameter = message.Parameter;
            PopupNavigationSource = pageUri;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            string fileType = data["fileType"].ToString();

            if (fileType == Constants.FileTypeExport)
            {
                Title = _l10n["Export"];
                PopupNavigationSource = Constants.FileExportStep1Page;
                FileExport fileExport = null;

                if (data.TryGetValue("fileExport", out var value) && value is FileExport fe)
                    fileExport = fe;

                if (fileExport == null)
                    fileExport = new();

                PopupNavigationParameter = fileExport;
            }
            else
            {
                Title = _l10n["Import"];
                PopupNavigationSource = Constants.FileImportPage;
                FileImport fileImport = new();
                PopupNavigationParameter = fileImport;
            }
        }
    }
}
