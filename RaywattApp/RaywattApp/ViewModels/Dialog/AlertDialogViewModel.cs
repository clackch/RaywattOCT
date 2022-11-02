using RaywattApp.Common.Dialog;
using System.Collections.Generic;
using System;
using log4net;

namespace RaywattApp.ViewModels.Dialog
{
    public class AlertDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AlertDialogViewModel));

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Message = data["message"].ToString();
        }
    }
}
