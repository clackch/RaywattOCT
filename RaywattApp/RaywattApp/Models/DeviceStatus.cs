using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
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
        private bool _isPowerOff = false;

        [ObservableProperty]
        private string _powerOffMsg;

        [ObservableProperty]
        private bool _isDeviceConnected = false;

        [ObservableProperty]
        private bool _isServiceStarted = false;

        [ObservableProperty]
        private bool _isLiveView = false;

        [ObservableProperty]
        private bool _canExecuteCalibration = true;

        [ObservableProperty]
        private bool _isAngioConnected = true;

        [ObservableProperty]
        private bool _isPullbackDone = false;

        [ObservableProperty]
        private bool _isLumenDetected = false;

        [ObservableProperty]
        private bool _isSaveRawDataDone = true;

        [ObservableProperty]
        private bool _isLumenLoaded = true;

        [ObservableProperty]
        private bool _isLumenSaved = true;

        [ObservableProperty]
        private bool _isOCTImagingDone = true;

        [ObservableProperty]
        private bool _isOCTImagingCompareDone = true;

        [ObservableProperty]
        private bool _isPaused = true;

        [ObservableProperty]
        private bool _canExit = true;

        [ObservableProperty]
        private string? _catheterStatus;

        [ObservableProperty]
        private CathRoom _selectedCathRoom;

        [ObservableProperty]
        private Dictionary<string, bool> _testMode = new Dictionary<string, bool>();

        [ObservableProperty]
        private ReviewImageInfo[] _reviewImageInfos = new ReviewImageInfo[2];

        public DeviceStatus()
        {
            ReviewImageInfos[(int)RaySession.Review] = new ReviewImageInfo();
            ReviewImageInfos[(int)RaySession.Compare] = new ReviewImageInfo();
        }
    }
}
