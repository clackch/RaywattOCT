using CommunityToolkit.Mvvm.ComponentModel;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.Models
{
    public partial class DeviceStatus : ObservableObject
    {
        public class ReviewImageInfo
        {
            public int Width = 0;
            public int Height = 0;
            public int Channels = 0;
            public int Total = 0;
            public int Current = 0;
        };

        [ObservableProperty]
        private bool _isInitialized = false;

        [ObservableProperty]
        private bool _isLiveView = false;

        [ObservableProperty]
        private bool _canExecuteCalibration = true;

        [ObservableProperty]
        private bool _isAngioConnected = false;

        [ObservableProperty]
        private bool _isLumenDetected = false;

        [ObservableProperty]
        private bool _isLumenLoaded = true;

        [ObservableProperty]
        private bool _isPaused = true;
        
        [ObservableProperty]
        private bool _isLumenSaved = true;

        [ObservableProperty]
        private string? _catheterStatus;

        [ObservableProperty]
        private ReviewImageInfo[] _reviewImageInfos = new ReviewImageInfo[2];

        public DeviceStatus()
        {
            ReviewImageInfos[(int)RaySession.Review] = new ReviewImageInfo();
            ReviewImageInfos[(int)RaySession.Compare] = new ReviewImageInfo();
        }
    }
}
