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
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2NativeViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2NativeViewModel));

        private readonly SqlManager _sqlManager;

        private ICommand _alternateCommand;
        public ICommand AlternateCommand
        {
            get { return this._alternateCommand ?? (this._alternateCommand = new RelayCommand(AlternatePatientId)); }
        }

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

            foreach (PatientCase patientCase in PatientCases)
            {
                ExportSize += CommonUtil.GetFileSize(patientCase.ImageFullPath);
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
            string dbFilePath = FileExport.ExternalDrivePath + "\\" + exportPrefix + "." + Constants.FileExtension;
            List<string> exportfiles = new List<string>();

            int cnt = 1;
            while (System.IO.File.Exists(dbFilePath))
            {
                dbFilePath = FileExport.ExternalDrivePath + "\\" + exportPrefix + "(" + cnt + ")" + "." + Constants.FileExtension;
                cnt++;
            }

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["ids"] = FileExport.PatientList;

            FileFormat fileFormat = new FileFormat();
            fileFormat.Size = 0;
            fileFormat.PatientList = _sqlManager.SelectPatientByList(sqlParameters);

            string alternateId = "";
            string originId = "";

            foreach (Patient patient in fileFormat.PatientList)
            {
                patient.PatientCaseList = new List<PatientCase>();
                originId = patient.Id;

                if (FileExport.PatientInfoAnonymize)
                {
                    if (FileExport.AlternatePatientId == null || !FileExport.AlternatePatientId.ContainsKey(patient.Id) || String.IsNullOrEmpty(FileExport.AlternatePatientId[patient.Id].Trim()))
                    {
                        alternateId = CommonUtil.GetRandomText(9);
                        patient.Id = alternateId;
                    }
                    else
                    {
                        patient.Id = FileExport.AlternatePatientId[patient.Id].Trim();
                    }
                    patient.Lastname = Constants.ExportAnonymous;
                    patient.Firstname = Constants.ExportAnonymous;
                    patient.Birthdate = new DateTime(1900, 1, 1);
                }

                foreach (PatientCase patientCase in PatientCases)
                {
                    if (originId == patientCase.PatientId)
                    {
                        patientCase.ImageSize = CommonUtil.GetFileSize(patientCase.ImageFullPath);
                        fileFormat.Size += patientCase.ImageSize;
                        exportfiles.Add(patientCase.ImageFullPath);

                        if (FileExport.PatientInfoAnonymize)
                        {
                            if (!String.IsNullOrEmpty(alternateId))
                            {
                                patientCase.Id = alternateId + "_" + patientCase.Id.Split("_")[1];
                                patientCase.PatientId = alternateId;
                            }
                            else
                            {
                                patientCase.Id = FileExport.AlternatePatientId[originId].Trim() + "_" + patientCase.Id.Split("_")[1];
                                patientCase.PatientId = FileExport.AlternatePatientId[originId].Trim();
                            }
                            patientCase.PatientName = Constants.ExportAnonymous;
                        }

                        patient.PatientCaseList.Add(patientCase);
                    }
                }
            }
            
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["File Export"];
            parameter["fileExport"] = FileExport;
            parameter["patientCases"] = PatientCases;
            parameter["dbFilePath"] = dbFilePath;
            parameter["contents"] = JsonConvert.SerializeObject(fileFormat, Formatting.Indented);
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter);

            if(FileExport.RemoveWhenComplete)
                RemoveData(exportfiles);

            Close();
            
        }

        private void RemoveData(List<string> exportfiles)
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
                    System.IO.File.Delete(file);
                }
            }
            else
            {
                _log.Error("Delete Error - Total : " + totalCnt + " Deleted Cnt : " + cnt);
            }
        }

        private void AlternatePatientId()
        {
            _log.Debug("AlternatePatientId");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            if (FileExport.AlternatePatientId == null)
                FileExport.AlternatePatientId = new Dictionary<string, string>();
            parameter["patientList"] = FileExport.PatientList;
            parameter["alternatePatientId"] = FileExport.AlternatePatientId;

            var result = _dialogService.OpenDialog(new FileAlternateIdDialogControl(), parameter);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                FileExport.AlternatePatientId.Clear();
                foreach (KeyValuePair<string, string> item in (Dictionary<string, string>)data["alternatePatientId"])
                {
                    FileExport.AlternatePatientId.Add(item.Key, item.Value);
                }
            }
        }
    }
}
