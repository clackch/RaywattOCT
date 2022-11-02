using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class EditCaseInfoDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EditCaseInfoDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private Dictionary<string, string> _physicianComboBox;

        public EditCaseInfoDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            PatientCase = new PatientCase();
            PhysicianComboBox = new Dictionary<string, string>();

            IList<Physician> physicianList = _sqlManager.SelectPhysicianList();

            foreach (Physician physician in physicianList)
            {
                PhysicianComboBox[physician.Name] = physician.Name;
            }

        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            PatientCase.PhysicianName = data["physicianName"].ToString();
            PatientCase.AccessionNumber = data["accessionNumber"].ToString();
            PatientCase.Comment = data["comment"].ToString();
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["physicianName"] = PatientCase.PhysicianName.Trim();
            parameter["accessionNumber"] = PatientCase.AccessionNumber.Trim();
            parameter["comment"] = PatientCase.Comment.Trim();

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
