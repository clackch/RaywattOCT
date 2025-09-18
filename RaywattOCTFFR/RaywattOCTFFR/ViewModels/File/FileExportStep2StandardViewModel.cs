using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.File;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattOCTFFR.ViewModels.File
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

        private ICommand _cmdFormatChanged;
        public ICommand CmdFormatChanged
        {
            get { return _cmdFormatChanged ?? (this._cmdFormatChanged = new RelayCommand(GetExportSize)); }
        }

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

            ExportSize = 0;

            double frameSize;
            if (FileExport.Material == Constants.ExportMaterialPullback)
            {
                foreach (PatientCase patientCase in PatientCases)
                {                    
                    int numOfFrames = patientCase.NumOfFrames;
                    if (FileExport.Pullback == Constants.ExportPullbackAVI)
                    {
                        ExportSize += CommonUtil.GetVideoSize(10, 12, numOfFrames);//10fps, 12Mbps
                    }
                    else
                    {
                        frameSize = GetFrameSize();
                        ExportSize += frameSize * numOfFrames;
                    }
                }
            }
            else
            {
                frameSize = GetFrameSize();
                int numOfFrames = (FileExport.Material == Constants.ExportMaterialBookmarked) ? FileExport.BookmarkedFrames.Count : 1;
                double compression = (FileExport.StillFrame == Constants.ExportStillFrameJPEG) ? Constants.ExportJpegCompression : 1;
                ExportSize = frameSize * numOfFrames * compression;
            }

            UpdateFileSize(ExportSize);
        }

        private double GetFrameSize()
        {
            double frameSize = 0;
            double height = Constants.ExportHeight;
            double width = Constants.ExportLongitudeWidth;

            if (FileExport.MeasureAuto || FileExport.MeasureManual)
                width = Constants.ExportWidth;
            else if (!FileExport.AngioView && !FileExport.Longitude && !FileExport.MeasureAuto && !FileExport.MeasureManual)
                width = Constants.ExportCrossSectionBig;

            if(FileExport.StillFrame == Constants.ExportStillFrameTIFF || FileExport.Pullback == Constants.ExportPullbackTIFF)
                frameSize = height * width;
            else
                frameSize = height * width * 3.0;

            return frameSize;
        }

        protected override void FileSave()
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["fileExport"] = FileExport;
            parameter["patientCases"] = PatientCases;
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);
            Close();
        }
    }
}
