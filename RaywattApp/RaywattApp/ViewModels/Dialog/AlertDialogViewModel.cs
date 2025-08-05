using RaywattApp.Common.Dialog;
using System.Collections.Generic;
using System;

namespace RaywattApp.ViewModels.Dialog
{
    public class AlertDialogViewModel : DialogViewModelBase
    {
        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Message = data["message"].ToString();

            if (data.TryGetValue("error", out var errorObj) && errorObj is bool error)
                IsError = error;
            else
                IsError = false;
        }
    }
}
