using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;

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
        
        [ObservableProperty]
        private bool _isCalciumOn = true;

        [ObservableProperty]
        private bool _isSheathOn = true;

        [ObservableProperty]
        private bool _isPlay = true;

        [ObservableProperty]
        private bool _isImageProcessingDone = false;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        [ObservableProperty]
        private int _angioFrameNumber = -1;

        [ObservableProperty]
        private bool _isNoPullback = false;

        //3D


        //Compare
        private PatientCase _selectedPatientCase;
        public PatientCase SelectedPatientCase
        { 
            get { return _selectedPatientCase; }
            set { 
                _selectedPatientCase = value;
                OnPropertyChanged(nameof(SelectedPatientCase));
                if (_selectedPatientCase != null)
                {
                    Constants.ImageResolutionCompare = _selectedPatientCase.ImageResolution;
                }
            }
        }

        //FFR
    }
}
