using log4net;
using RaywattApp.Common.Dialog;
using System;
using System.Collections.Generic;

namespace RaywattApp.ViewModels.Dialog
{
    public class ConfirmDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ConfirmDialogViewModel));

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Message = data["message"].ToString();
        }
    }
}
