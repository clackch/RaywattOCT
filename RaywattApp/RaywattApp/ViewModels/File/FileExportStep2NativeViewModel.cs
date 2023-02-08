using log4net;
using RaywattApp.Common.File;
using RaywattApp.Models;
using System.Collections.Generic;
using System;
using System.Windows.Navigation;
using RaywattApp.Common.Dialog;
using RaywattApp.Views.Dialog;
using RaywattApp.Common.Bases;
using Newtonsoft.Json;
using RaywattApp.Services;
using RaywattApp.Common.Util;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2NativeViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2NativeViewModel));

        private readonly SqlManager _sqlManager;

        public FileExportStep2NativeViewModel(SqlManager sqlManager, IDialogService dialogService) : base(dialogService)
        {
            _log.Debug("FileExportStep2NativeViewModel");

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

            if (FileExport.DiskType == null)
                DiskType = Constants.FileDiskCd;
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
            IList<PatientCase> patientCases = _sqlManager.SelectPatientCaseByList(sqlParameters);

            foreach (PatientCase patientCase in patientCases)
            {
                ExportSize += CommonUtil.GetFileSize(patientCase.Image);
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
                parameter["title"] = _l10n["Information"];
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
            string exportPrefix = Constants.FileNamePrefix + DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = exportPrefix;
            string filePath = FileExport.ExternalDrivePath + "\\" + fileName + "." + Constants.FileExtension;
            Dictionary<string, string> exportfiles = new Dictionary<string, string>();

            int cnt = 1;
            while (System.IO.File.Exists(filePath))
            {
                fileName = exportPrefix + "(" + cnt + ")";
                filePath = FileExport.ExternalDrivePath + "\\" + fileName + "." + Constants.FileExtension;
                cnt++;
            }

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["ids"] = FileExport.PatientList;

            FileFormat fileFormat = new FileFormat();
            fileFormat.Size = 0;
            fileFormat.PatientList = _sqlManager.SelectPatientByList(sqlParameters);

            sqlParameters.Clear();
            sqlParameters["ids"] = FileExport.SelectedItem;
            IList<PatientCase> patientCases = _sqlManager.SelectPatientCaseByList(sqlParameters);

            foreach (Patient patient in fileFormat.PatientList)
            {
                patient.PatientCaseList = new List<PatientCase>();

                foreach (PatientCase patientCase in patientCases)
                {
                    if (patient.Id == patientCase.PatientId)
                    {
                        patientCase.ImageSize = CommonUtil.GetFileSize(patientCase.Image);
                        fileFormat.Size += patientCase.ImageSize;

                        string imageFileName = CommonUtil.GetFileName(patientCase.Image);
                        string imageFilePath = FileExport.ExternalDrivePath + "\\" + imageFileName;
                        exportfiles.Add(patientCase.Image, imageFilePath);
                        patientCase.Image = imageFileName;

                        patient.PatientCaseList.Add(patientCase);
                    }
                }
            }
            
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["File Export"];
            parameter["fileExport"] = FileExport;
            parameter["files"] = exportfiles;
            parameter["filePath"] = filePath;
            parameter["contents"] = JsonConvert.SerializeObject(fileFormat, Formatting.Indented);
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter);

            if(FileExport.RemoveWhenComplete)
                RemoveData(exportfiles);

            Close();
            
        }

        private void RemoveData(Dictionary<string, string> exportfiles)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            int cnt = 0, totalCnt = FileExport.SelectedItem.Count;

            foreach (string id in FileExport.SelectedItem)
            {
                sqlParameters.Clear();
                sqlParameters["id"] = id;
                cnt += _sqlManager.DeletePatientCase(sqlParameters);
            }

            if(totalCnt == cnt)
            {
                foreach (var file in exportfiles)
                {
                    System.IO.File.Delete(file.Key);
                }
            }
            else
            {
                _log.Error("Delete Error - Total : " + totalCnt + " Deleted Cnt : " + cnt);
            }
        }
    }
}
