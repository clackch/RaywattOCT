using Accord.Statistics.Distributions.Univariate;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Enums;
using System.Collections.Generic;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.Models
{
    public partial class DeviceStatus : ObservableObject
    {
        public class ReviewImageInfo
        {
            public int Width;
            public int Height;
            public int Channels;
            public int Total;
            public int Current;
        };

        [ObservableProperty]
        private string _loginID = string.Empty;

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
        private bool _isAngioConnected = false;

        [ObservableProperty]
        private bool _isAngioInitialized = false;

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
        private bool _isFfrCalculated = true;

        [ObservableProperty]
        private bool _isPaused = true;

        [ObservableProperty]
        private bool _canExit = true;

        [ObservableProperty]
        private string? _catheterStatus;

        [ObservableProperty]
        private CathRoom _selectedCathRoom;

        [ObservableProperty]
        private bool _autoPullbackOnOff;

        [ObservableProperty]
        private bool _autoPullbackModel; //Detection Model - true: Lumen, false: Flush

        [ObservableProperty]
        private bool _autoPullbackIsImageCleared;

        [ObservableProperty]
        private int _autoPullbackTriggerCandidate;

        [ObservableProperty]
        private int _autoPullbackTriggerCount;

        [ObservableProperty]
        private bool _enhancedLUT;

        [ObservableProperty]
        private bool _isCleaningDone = true;

        [ObservableProperty]
        private bool _isExecutedAIFFR = false;

        [ObservableProperty]
        private Dictionary<string, bool> _testMode = new Dictionary<string, bool>();

        [ObservableProperty]
        private ReviewImageInfo[] _reviewImageInfos = new ReviewImageInfo[2];

        [ObservableProperty]
        // TODO: junghw 추후 의사 정보로 default로 변환
        private LongitudeOrientation _longitudeOrientation = LongitudeOrientation.DistalToProximal; // default
        [ObservableProperty]
        private bool _longitudeOrientationChanged = false;

        public DeviceStatus()
        {
            ReviewImageInfos[(int)RaySession.Review] = new ReviewImageInfo();
            ReviewImageInfos[(int)RaySession.Compare] = new ReviewImageInfo();
        }
    }
}
