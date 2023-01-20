using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;

namespace RaywattApp.Models
{
    public partial class DeviceStatus : ObservableObject
    {
        [ObservableProperty]
        private bool _isInitialized = false;

        [ObservableProperty]
        private string? _viewMode = Constants.ViewModeStandBy;

        [ObservableProperty]
        private bool _canExecuteCalibration = true;

        public DeviceStatus()
        {
        }
    }
}
