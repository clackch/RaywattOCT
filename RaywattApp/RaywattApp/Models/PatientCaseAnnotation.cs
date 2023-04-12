using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class PatientCaseAnnotation : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string? _bookmark;

        [ObservableProperty]
        private string? _longitude;

        [ObservableProperty]
        private string? _crossSection;

        [ObservableProperty]
        private string? _lumenContour;
    }
}
