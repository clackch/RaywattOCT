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
        private bool _isCalciumOnAngioCs = true;

        [ObservableProperty]
        private bool _isSheathOnAngioCs = true;

        [ObservableProperty]
        private bool _isPlay = true;

        [ObservableProperty]
        private bool _isImageProcessingDone = false;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        [ObservableProperty]
        private Zoom _zoomAngioCs = new Zoom(Constants.CrossSectionAngio);

        [ObservableProperty]
        private Zoom _zoomAngio = new Zoom(Constants.AngioSize);

        [ObservableProperty]
        private Zoom _zoomFfr = new Zoom();

        [ObservableProperty]
        private int _angioFrameNumber = -1;

        [ObservableProperty]
        private bool _isNoPullback = false;

        //3D


        //Compare
        [ObservableProperty]
        private PatientCase _selectedPatientCase;

        //FFR
    }
}
