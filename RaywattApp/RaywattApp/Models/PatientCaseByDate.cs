using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class PatientCaseByDate : ObservableObject
    {
        [ObservableProperty]
        private string key;

        [ObservableProperty]
        private string value;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private IList<PatientCase> patientCaseList;
    }
}
