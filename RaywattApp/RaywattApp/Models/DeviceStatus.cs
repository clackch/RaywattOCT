using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class DeviceStatus : ObservableObject
    {
        [ObservableProperty]
        private bool _isInitialized = false;

        [ObservableProperty]
        private bool _isLiveView = false;

        [ObservableProperty]
        private bool _canExecuteCalibration = true;

        [ObservableProperty]
        private bool _isAngioConnected = false;

        [ObservableProperty]
        private string? _catheterStatus;

        public DeviceStatus()
        {
        }
    }
}
