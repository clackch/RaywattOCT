using System.Windows;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Component
{
    public class PercentValueComponent : TextBlock
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(PercentValueComponent));
        public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public static readonly DependencyProperty OverallProperty = DependencyProperty.Register("Overall", typeof(double), typeof(PercentValueComponent));
        public double Overall { get => (double)GetValue(OverallProperty); set => SetValue(OverallProperty, value); }
    }
}
