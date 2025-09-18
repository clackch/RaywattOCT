using log4net;
using RaywattOCTFFR.Common.Dialog;
using System;
using System.Collections.Generic;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class PowerOffDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PowerOffDialogViewModel));

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Message = data["message"].ToString();
        }
    }
}
