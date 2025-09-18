using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class NewPatientDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(NewPatientDialogViewModel));

        [ObservableProperty]
        private bool _isManual = true;

        protected override void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("AnswerYes");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["isManual"] = IsManual;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
