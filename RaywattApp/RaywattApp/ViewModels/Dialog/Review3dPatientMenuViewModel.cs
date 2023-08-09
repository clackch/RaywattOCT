using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class Review3dPatientMenuViewModel : ModelessViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Review3dPatientMenuViewModel));

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        private ICommand _editCaseCommand;
        public ICommand EditCaseCommand
        {
            get { return this._editCaseCommand ?? (this._editCaseCommand = new RelayCommand(EditCase)); }
        }

        public Review3dPatientMenuViewModel()
        { }

        public override void SetParameter(IModelessPatient parent, object parameter)
        {
            Parent = parent;
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Patient = (Patient)data["patient"];
            PatientCase = (PatientCase)data["patientCase"];
        }

        protected override void CloseWindow(Window window)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["patient"] = Patient;
            result["patientCase"] = PatientCase;
            Parent.SetResult(result);

            window.Close();
        }

        private void EditCase()
        {
            _log.Debug("EditCase");
            IDialogService dialogService = new DialogService();

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["vessel"] = PatientCase.Vessel;
            parameter["procedure"] = PatientCase.Procedure;
            parameter["physicianName"] = PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;
            var result = dialogService.OpenDialog(new EditCaseInfoDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                PatientCase.Vessel = data["vessel"].ToString();
                PatientCase.Procedure = data["procedure"].ToString();
                PatientCase.PhysicianName = data["physicianName"].ToString();
                PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                PatientCase.Comment = data["comment"].ToString();
            }
        }
    }
}
