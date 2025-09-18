using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using System.Windows;

namespace RaywattOCTFFR.Models
{
    public partial class IndicatorBase : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(IndicatorBase));

        [ObservableProperty]
        private Visibility _isVisible;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private double _x = double.NaN;

        [ObservableProperty]
        private double _y = double.NaN;

        [ObservableProperty]
        private bool _isValid = false;

        [ObservableProperty]
        private double _centerX;

        [ObservableProperty]
        private string? _strValue;

        [ObservableProperty]
        private double _dValue;

        [ObservableProperty]
        private int _nValue;
    }
}
