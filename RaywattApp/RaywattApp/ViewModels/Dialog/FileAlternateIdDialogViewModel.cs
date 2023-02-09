using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileAlternateIdDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileAlternateIdDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private FileExport _fileExport;

        [ObservableProperty]
        private IList<Patient> _patientList;

        private ICommand _resetCommand;
        public ICommand ResetCommand
        {
            get { return this._resetCommand ?? (this._resetCommand = new RelayCommand(ResetPatientId)); }
        }

        public FileAlternateIdDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;
        }

        public override void SetParameter(object parameter)
        {
            if (FileExport == null)
                FileExport = new FileExport();
            if (FileExport.AlternatePatientId == null)
                FileExport.AlternatePatientId = new Dictionary<string, string>();

            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;

            FileExport.AlternatePatientId.Clear();
            foreach (KeyValuePair<string, string> item in (Dictionary<string, string>)data["alternatePatientId"])
            {
                FileExport.AlternatePatientId.Add(item.Key, item.Value);
            }

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["ids"] = (List<string>)data["patientList"];

            PatientList = _sqlManager.SelectPatientByList(sqlParameters);

            //기존에 입력한 정보가 있는 경우, 설정 (단, 변경이 있으면 입력 안함)
            if (PatientList.Count == FileExport.AlternatePatientId.Count)
            {
                bool isExist = true;

                //기존 patient 목록과 대체 ID 입력의 patient 목록 비교
                foreach (Patient patient in PatientList)
                {
                    isExist = false;
                    foreach (KeyValuePair<string, string> id in FileExport.AlternatePatientId)
                    {
                        if (patient.Id == id.Key)
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist)
                        break;
                }

                //전체 동일할 경우만, 기존 대체 ID 입력
                if (isExist)
                {
                    foreach (Patient patient in PatientList)
                    {
                        foreach (KeyValuePair<string, string> id in FileExport.AlternatePatientId)
                        {
                            if (patient.Id == id.Key)
                                patient.AlternateId = id.Value;
                        }
                    }
                }
            }
        }

        private void ResetPatientId()
        {
            _log.Debug("ResetPatientId");

            foreach (Patient patient in PatientList)
            {
                patient.AlternateId = "";
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            FileExport.AlternatePatientId.Clear();
            foreach (Patient patient in PatientList)
            {
                if (patient.AlternateId == null)
                    patient.AlternateId = "";

                FileExport.AlternatePatientId.Add(patient.Id, patient.AlternateId);
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["alternatePatientId"] = FileExport.AlternatePatientId;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
