using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class FfrFeature : ObservableObject
    {
        [ObservableProperty]
        private string _model;

        [ObservableProperty]
        private double _percentAreaStenosis;

        [ObservableProperty]
        private double _minimalLumenArea;

        [ObservableProperty]
        private double _distalLumenArea;

        [ObservableProperty]
        private double _lesionLength;

        [ObservableProperty]
        private double _plaqueArea;

        [ObservableProperty]
        private double _proximalLumenArea;

        [ObservableProperty]
        public int _minimalLumenFrameNumber;
    }
}
