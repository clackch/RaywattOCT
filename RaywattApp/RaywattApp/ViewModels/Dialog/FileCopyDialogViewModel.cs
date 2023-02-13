using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileCopyDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileCopyDialogViewModel));

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        List<string> _files;

        [ObservableProperty]
        private string? _dbFilePath;

        [ObservableProperty]
        private string? _contents;

        [ObservableProperty]
        private FileExport _fileExport;

        [ObservableProperty]
        private bool enableDone = false;

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Files = (List<string>)data["files"];

            if (data.ContainsKey("fileExport"))
                FileExport = (FileExport) data["fileExport"];

            if (Files.Count == 0)
            {
                Progress = 100;
                EnableDone = true;
                return;
            }

            if (FileExport != null) {
                if (FileExport.Type == Constants.ExportTypeNative)
                {
                    if (data.ContainsKey("dbFilePath"))
                        DbFilePath = data["dbFilePath"].ToString();
                    if (data.ContainsKey("contents"))
                        Contents = data["contents"].ToString();
                    FileCopyNative();
                }
                else if (FileExport.Type == Constants.ExportTypeDicom)
                {
                    FileSaveDicom();
                }
                else if (FileExport.Type == Constants.ExportTypeStandard) 
                {
                }
            }
        }

        private async void FileCopyNative()
        {
            Dictionary<string, string> fileCopyInfo = new Dictionary<string, string>();
            foreach (string file in Files)
            {
                string fileName = CommonUtil.GetFileName(file);
                string dstFilePath = FileExport.ExternalDrivePath + "\\" + fileName;
                fileCopyInfo.Add(file, dstFilePath);
            }

            await CommonUtil.CopyFiles(fileCopyInfo, prog => Progress = prog);

            if (!String.IsNullOrEmpty(DbFilePath) && !String.IsNullOrEmpty(Contents))
                CommonUtil.Encryptor(DbFilePath, Contents);

            EnableDone = true;
        }

        private async void FileSaveDicom()
        {
            // DICOMDIRInputFolder();
            for (int i=0; i<Files.Count; i++)
            {
                const string dicomPrefix = "IMG";

                List<Mat> convertedImages = new List<Mat>();
                Mat imgLongitude = await CommonUtil.ConvertImage(Files[i], FileExport.BookmarkedFrames, convertedImages);
                Progress = (i + 1) / (double)Files.Count * 100.0;

                List<Mat> exportedImages = new List<Mat>(convertedImages.Count);
                // DicomStart();
                // DicomImageStart(convertedImages.Count);
                for (int j = 0; j < convertedImages.Count; j++)
                {
                    Mat imgExport = CommonUtil.MakeImageForExport(convertedImages[0], imgLongitude, imgLongitude);
                    exportedImages.Add(imgExport);
                    // DicomAddImage(imgExport.Cols, imgExport.Rows, imgExport.Data);
                }
                // DicomImageFinish();

                // SetProperty();
                // SetSequenceProperty();

                string filePath = dicomPrefix + string.Format("{0:0000}", i);
                // DicomSave(filePath);

                // DICOMDIRInputFile(filePath);
            }
            // DICOMDIRWrite();
            EnableDone = true;
        }
    }
}
