using System.Collections.Generic;
using RaywattApp.Common.Annotation.Models;

namespace RaywattApp.Models
{
    public class FFRFeatureParameter
    {
        private string? _vesselName;
        private string? _proximalLumenArea;
        private string? _distalLumenArea;
        private string? _lesionLength;
        private string? _minimalLumenArea;
        private string? _plaqueArea;
        private string? _percentAreaStenosis;

        private double _fFResult;

        public double FFResult { get => _fFResult; set => _fFResult = value; }
        
        private bool _isSkip = false;
        public bool IsSkip { get => _isSkip; set => _isSkip = value; }

        public void SetFFRFeatureParameter(string vesselName, string proximalLumenArea, string distalLumenArea, string lesionLength, string minimalLumenArea, string plaqueArea, string percentAreaStenosis)
        {
            _vesselName = vesselName;
            _proximalLumenArea = proximalLumenArea;
            _distalLumenArea = distalLumenArea;
            _lesionLength = lesionLength;
            _minimalLumenArea = minimalLumenArea;
            _plaqueArea = plaqueArea;
            _percentAreaStenosis = percentAreaStenosis;
        }

        public bool IsSame( string vesselName,
                            string proximalLumenArea,
                            string distalLumenArea,
                            string lesionLength,
                            string minimalLumenArea,
                            string plaqueArea,
                            string percentAreaStenosis)
        {
            return _vesselName == vesselName &&
                   _proximalLumenArea == proximalLumenArea &&
                   _distalLumenArea == distalLumenArea &&
                   _lesionLength == lesionLength &&
                   _minimalLumenArea == minimalLumenArea &&
                   _plaqueArea == plaqueArea &&
                   _percentAreaStenosis == percentAreaStenosis;
        }

        public void Clear()
        {
            _vesselName = string.Empty;
            _proximalLumenArea = string.Empty;
            _distalLumenArea = string.Empty;
            _lesionLength = string.Empty;
            _minimalLumenArea = string.Empty;
            _plaqueArea = string.Empty;
            _percentAreaStenosis = string.Empty;
        }

    }
}
