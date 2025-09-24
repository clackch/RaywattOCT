using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class AutoPullback : ObservableObject
    {
        [ObservableProperty]
        private bool _onOff;

        [ObservableProperty]
        private bool _isCleared;

        [ObservableProperty]
        private int _triggerTargetCount;

        [ObservableProperty]
        private int _triggerActualCount;
    }
}
