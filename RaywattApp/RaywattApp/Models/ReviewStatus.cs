using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class ReviewStatus : ObservableValidator
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

        //3D


        //Compare
        [ObservableProperty]
        private PatientCase _selectedPatientCase;

        //FFR
    }
}
