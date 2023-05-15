using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class ReviewStatus : ObservableObject
    {
        //Common
        [ObservableProperty]
        private string _currentPage;

        //2D
        [ObservableProperty]
        private bool _isAngioOn = false;

        [ObservableProperty]
        private bool _isLumenProfile = true;

        [ObservableProperty]
        private bool _isContourStentOn = true;

        [ObservableProperty]
        private bool _isMeasurementOn = false;

        [ObservableProperty]
        private int _numberOfFrames = 0;

        //3D


        //Compare
        [ObservableProperty]
        private PatientCase _selectedPatientCase;

        //FFR
    }
}
