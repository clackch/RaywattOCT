using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Component
{
    public class IndicatorImage : Image, SvgComponentBase
    {
        private const string resPath = "/res/indicator/";

        public static DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(IndicatorImage), new PropertyMetadata(null, SvgComponentBase.OnIconPropertyChanged));
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public static DependencyProperty IsCapturedProperty = DependencyProperty.Register("IsCaptured", typeof(bool), typeof(IndicatorImage));
        public bool IsCaptured { get => (bool)GetValue(IsCapturedProperty); set => SetValue(IsCapturedProperty, value); }

        public void InitializeIconPath(string iconName)
        {
            IconDefault = SvgComponentBase.GetPath(resPath, iconName);
            IconOver = SvgComponentBase.GetPath(resPath, iconName, "_hover");
        }

        public static readonly DependencyProperty IconDefaultProperty = DependencyProperty.Register(nameof(IconDefault), typeof(string), typeof(IndicatorImage));
        public static readonly DependencyProperty IconOverProperty = DependencyProperty.Register(nameof(IconOver), typeof(string), typeof(IndicatorImage));

        public string IconDefault { get => (string)GetValue(IconDefaultProperty); set => SetValue(IconDefaultProperty, value); }
        public string IconOver { get => (string)GetValue(IconOverProperty); set => SetValue(IconOverProperty, value); }
    }
}
