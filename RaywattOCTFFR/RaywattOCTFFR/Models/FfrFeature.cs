using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Common.Annotation.Models;
using System.Collections.Generic;

namespace RaywattOCTFFR.Models
{
    public partial class FfrFeature : ObservableObject
    {
        [ObservableProperty]
        private string _model;

        [ObservableProperty]
        private string _vesselType;

        [ObservableProperty]
        private string _vesselTypeGroup;

        [ObservableProperty]
        private string _actualVesselType;

        [ObservableProperty]
        private double _proximalLumenArea;

        [ObservableProperty]
        private double _distalLumenArea;

        [ObservableProperty]
        private double _lesionLength;

        [ObservableProperty]
        private int _minimalLumenFrameNumber;

        [ObservableProperty]
        private double _minimalLumenArea;

        [ObservableProperty]
        private double _plaqueArea;

        [ObservableProperty]
        private double _percentAreaStenosis;

        [ObservableProperty]
        private bool _isPlaqueAreaValid;

        [ObservableProperty]
        private double _result;

        [ObservableProperty]
        private List<Measurement> _plaqueAreaList;
    }
}
