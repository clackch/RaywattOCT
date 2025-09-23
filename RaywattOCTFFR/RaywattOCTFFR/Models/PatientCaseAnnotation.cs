using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattOCTFFR.Models
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

        [ObservableProperty]
        private string? _lumenSidebranch;

        [ObservableProperty]
        private string? _lumenStent;

        [ObservableProperty]
        private string? _lumenGuidewire;

        [ObservableProperty]
        private string? _ffrPlaque;

        [ObservableProperty]
        private string? _coRegistration;
    }
}
