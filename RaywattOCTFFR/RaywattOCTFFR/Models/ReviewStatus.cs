using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattOCTFFR.Models
{
    public partial class ReviewStatus : ObservableObject
    {
        //Common
        [ObservableProperty]
        private string _currentPage;

        //2D
        [ObservableProperty]
        private bool _isLumenProfile = true;

        [ObservableProperty]
        private bool _isContourStentOn = false;

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
        private bool _isNoPullback = false;

        [ObservableProperty]
        private bool _isLumenEdited = true;

        [ObservableProperty]
        private bool _isMeasureInit = false;

        [ObservableProperty]
        private bool _isRestartLumenDetection = false;

        //FFR
        [ObservableProperty]
        private Zoom _zoomFfr = new Zoom();
    }
}
