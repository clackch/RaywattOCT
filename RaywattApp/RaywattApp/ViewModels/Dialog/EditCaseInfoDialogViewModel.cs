using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class EditCaseInfoDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EditCaseInfoDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private IList<Physician> _physicianList;

        [ObservableProperty]
        private Dictionary<string, string> _vesselList;

        [ObservableProperty]
        private Dictionary<string, string> _procedureList;

        [ObservableProperty]
        private KeyValuePair<string, string> _currentVessel;

        [ObservableProperty]
        private KeyValuePair<string, string> _currentProcedure;

        [ObservableProperty]
        private Physician _currentPhysician;

        public EditCaseInfoDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            PatientCase = new PatientCase();

            PhysicianList = _sqlManager.SelectPhysicianList();
            Physician notSelected = new Physician();
            notSelected.Name = _l10n[Constants.NotSelected];
            PhysicianList.Insert(0, notSelected);
            
            VesselList = CodeDefinition.Codes["VESS"];
            ProcedureList = CodeDefinition.Codes["PROC"];
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            PatientCase.Vessel = data["vessel"].ToString();
            PatientCase.Procedure = data["procedure"].ToString();
            PatientCase.PhysicianName = data["physicianName"].ToString();
            PatientCase.AccessionNumber = data["accessionNumber"].ToString();
            PatientCase.Comment = data["comment"].ToString();

            if (CodeDefinition.Codes["VESS"].ContainsKey(PatientCase.Vessel))
                CurrentVessel = new KeyValuePair<string, string>(PatientCase.Vessel, CodeDefinition.Codes["VESS"][PatientCase.Vessel]);
            if (CodeDefinition.Codes["PROC"].ContainsKey(PatientCase.Procedure))
                CurrentProcedure = new KeyValuePair<string, string>(PatientCase.Procedure, CodeDefinition.Codes["PROC"][PatientCase.Procedure]);
            CurrentPhysician = PhysicianList.FirstOrDefault(x => x.Name == PatientCase.PhysicianName);
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["vessel"] = CurrentVessel.Key;
            parameter["procedure"] = CurrentProcedure.Key;
            parameter["physicianName"] = CurrentPhysician != null ? CurrentPhysician.Name : PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber.Trim();
            parameter["comment"] = PatientCase.Comment.Trim();

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
        
    }
}
