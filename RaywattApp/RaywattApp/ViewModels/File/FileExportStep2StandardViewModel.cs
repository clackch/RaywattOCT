using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.File;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2StandardViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2StandardViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private int _maxFrameWidth;

        [ObservableProperty]
        private int _minFrameWidth;

        [ObservableProperty]
        private int _frameTickFrequency;

        public FileExportStep2StandardViewModel(SqlManager sqlManager, IDialogService dialogService) : base(dialogService)
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

            if (FileExport.Pullback == null)
                FileExport.Pullback = Constants.ExportPullbackAVI;

            if (FileExport.StillFrame == null)
                FileExport.StillFrame = Constants.ExportStillFrameJPEG;

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

            //계산로직 필요 및 추출 항목에 따른 변경 기능 필요

            ExportSize = CommonUtil.ByteToGB(ExportSize);
        }

        protected override void FileSave()
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["fileExport"] = FileExport;
            parameter["patientCases"] = PatientCases;
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter);
            Close();
        }
    }
}
