using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.File;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System;
using System.Windows.Navigation;
using RaywattApp.Common.Util;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2DicomViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2DicomViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private int _maxFrameWidth;

        [ObservableProperty]
        private int _minFrameWidth;

        [ObservableProperty]
        private int _frameTickFrequency;

        public FileExportStep2DicomViewModel(SqlManager sqlManager, IDialogService dialogService) : base(dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                FileExport = (FileExport)extraData;
                SetCondition();
            }
        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            if (FileExport.Measurements == null)
                FileExport.Measurements = Constants.ExportMeasurementShowAll;

            if (FileExport.DiskType == null)
                DiskType = Constants.FileDiskExternal;
            else
                DiskType = FileExport.DiskType;

            if (FileExport.ExternalDrivePath == null)
                FileExport.ExternalDrivePath = "";

            GetExportSize();
            GetDrive();
        }

        private void GetExportSize()
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["ids"] = FileExport.SelectedItem;
            PatientCases = _sqlManager.SelectPatientCaseByList(sqlParameters);

            const double frameSize = Constants.ApplicationWidth * Constants.ApplicationHeight * 3.0;

            if (FileExport.Material == Constants.ExportMaterialPullback)
            {
                foreach (PatientCase patientCase in PatientCases)
                {
                    if (patientCase.PullbackType == Constants.PullbackTypeLong)
                    {
                        ExportSize += frameSize * Constants.PullbackLongFrameCnt;
                    }
                    else
                    {
                        ExportSize += frameSize * Constants.PullbackShortFrameCnt;
                    }
                }
            }
            else if(FileExport.Material == Constants.ExportMaterialBookmarked)
            {
                ExportSize = frameSize * FileExport.BookmarkedFrames.Count;
            }
            else
            {
                ExportSize = frameSize;
            }

            ExportSize = CommonUtil.ByteToGB(ExportSize);
        }

        protected override void Export()
        {
            _log.Debug("Export");

            //TO-DO : CD 일 경우, Path 부분 추가
            if (String.IsNullOrEmpty(FileExport.ExternalDrivePath))
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["message"] = _l10n["Path is required"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
            }
            else
            {
                FileSave();
            }
        }

        private void FileSave()
        {
            if (FileExport.PatientInfoAnonymize)
            {
                foreach (PatientCase patientCase in PatientCases)
                {
                    patientCase.ImageFullPath = patientCase.ImageFullPath;
                    patientCase.IsAnonymize = true;

                    string alternateId = CommonUtil.GetRandomText(9);
                    patientCase.Id = alternateId + "_" + patientCase.Id.Split("_")[1];
                    patientCase.PatientId = alternateId;
                    patientCase.PatientName = Constants.ExportAnonymous;
                    patientCase.Birthdate = new DateTime(1900, 1, 1);                    
                }
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["File Export"];
            parameter["fileExport"] = FileExport;
            parameter["patientCases"] = PatientCases;
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter);

            Close();
        }
    }
}
