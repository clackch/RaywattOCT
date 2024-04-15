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
    }
}
