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
using System.Collections.Generic;
using System.Windows.Input;
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
                    frameSize = GetFrameSize(patientCase.AngioYn);
                    int numOfFrames = patientCase.NumOfFrames;
                    if (FileExport.Pullback == Constants.ExportPullbackAVI)
                    {
                        ExportSize += frameSize * numOfFrames * 1024;
                    }
                    else
                    {
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

        private double GetFrameSize(bool angioYn = false)
        {
            double frameSize = 0;

            if(FileExport.Material == Constants.ExportMaterialPullback && FileExport.Pullback == Constants.ExportPullbackAVI)
            {
                //Export 한 파일 대상으로 경험적으로 찾은 수치
                if (!FileExport.AngioView && !FileExport.Longitude && !FileExport.MeasureAuto && !FileExport.MeasureManual)//Cross Section Only
                {
                    frameSize = 145;
                }
                else if((FileExport.Longitude || (FileExport.AngioView && angioYn)) && (FileExport.MeasureAuto || FileExport.MeasureManual))//Check All, Longitude + Measure, Angio + Measure
                {
                    frameSize = 240;
                }
                else if((FileExport.Longitude || (FileExport.AngioView && angioYn)) && (!FileExport.MeasureAuto && !FileExport.MeasureManual))//Longitude, Angio (Measure X)
                {
                    frameSize = 170;
                }
                else
                {
                    frameSize = 255;
                }
            }
            else
            {
                double height = Constants.ExportHeight;
                double width = Constants.ExportLongitudeWidth;

                if (FileExport.MeasureAuto || FileExport.MeasureManual)
                    width = Constants.ExportWidth;
                else if (!FileExport.AngioView && !FileExport.Longitude && !FileExport.MeasureAuto && !FileExport.MeasureManual)
                    width = Constants.ExportCrossSectionBig;

                frameSize = height * width * 3.0;
            }

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
