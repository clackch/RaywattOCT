using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using System;
using System.Collections.Generic;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class FileImportDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportDialogViewModel));

        [ObservableProperty]
        private IList<PatientCase>? _patientCaseList;

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            Message = data["message"].ToString();
            PatientCaseList = (IList<PatientCase>)data["patientCaseList"];
        }
    }
}
