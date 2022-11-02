using log4net;
using RaywattApp.Common.Dialog;
using System.Collections.Generic;
using System;
using RaywattApp.Models;

namespace RaywattApp.ViewModels.Dialog
{
    public class EditOctInfoDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EditOctInfoDialogViewModel));

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Message = data["other"].ToString();
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["other"] = Message.Trim();

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
