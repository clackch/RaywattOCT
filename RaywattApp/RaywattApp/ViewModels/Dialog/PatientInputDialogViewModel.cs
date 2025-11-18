using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using System;
using System.Collections.Generic;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class PatientInputDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientInputDialogViewModel));

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private Dictionary<string, string> _genderComboBox;

        [ObservableProperty]
        private string _selectedGender;

        public PatientInputDialogViewModel()
        {
            _log.Info("PatientInputDialogViewModel");

            Patient = new Patient();
            GenderComboBox = CodeDefinition.Codes["GEND"];
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;

            if (data.ContainsKey("patient"))
            {
                Patient existingPatient = (Patient)data["patient"];
                Patient.Id = existingPatient.Id ?? "";
                Patient.Firstname = existingPatient.Firstname ?? "";
                Patient.Lastname = existingPatient.Lastname ?? "";
                Patient.Birthdate = existingPatient.Birthdate;
                Patient.Gender = existingPatient.Gender;
                SelectedGender = existingPatient.Gender;
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            if (string.IsNullOrEmpty(Patient.Id?.Trim()))
            {
                Patient.ValidateId = _l10n["Enter ID"].ToString();
                return;
            }

            if (string.IsNullOrEmpty(Patient.Lastname?.Trim()))
            {
                return;
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["id"] = Patient.Id.Trim();
            parameter["firstname"] = Patient.Firstname?.Trim() ?? "";
            parameter["lastname"] = Patient.Lastname.Trim();
            parameter["birthdate"] = Patient.Birthdate;
            parameter["gender"] = SelectedGender ?? "";

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
