using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class FileFormat : ObservableObject
    {
        [ObservableProperty]
        long size;

        [ObservableProperty]
        string annotationFilePath;

        [ObservableProperty]
        IList<Patient> patientList;
    }
}
