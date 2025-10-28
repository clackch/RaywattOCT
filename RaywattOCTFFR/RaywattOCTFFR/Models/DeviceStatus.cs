using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using static RaywattOCT.RayCoreFFRWrapper;

namespace RaywattOCTFFR.Models
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
        private bool _isServiceStarted = false;

        [ObservableProperty]
        private bool _isLumenDetected = false;

        [ObservableProperty]
        private bool _isSaveRawDataDone = true; //파일 import 시, 시간 소요되면 사용 예정

        [ObservableProperty]
        private bool _isLumenLoaded = true;

        [ObservableProperty]
        private bool _isLumenSaved = true;

        [ObservableProperty]
        private bool _isOCTImagingDone = true;

        [ObservableProperty]
        private bool _isFfrCalculated = true;

        [ObservableProperty]
        private bool _isPaused = true;

        [ObservableProperty]
        private bool _canExit = true;

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
