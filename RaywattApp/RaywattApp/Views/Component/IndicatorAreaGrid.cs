using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Component
{
    public class IndicatorAreaGrid : Grid
    {
        public static DependencyProperty IsCapturedProperty = DependencyProperty.Register("IsCaptured", typeof(bool), typeof(IndicatorAreaGrid));
        public bool IsCaptured { get => (bool)GetValue(IsCapturedProperty); set => SetValue(IsCapturedProperty, value); }
    }
}
