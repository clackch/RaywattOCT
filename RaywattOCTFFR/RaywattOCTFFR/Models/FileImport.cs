using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattOCTFFR.Models
{
    public partial class FileImport : ObservableObject
    {
        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private Patient? _patient;

        [ObservableProperty]
        private PatientCase? _patientCase;
    }
}
