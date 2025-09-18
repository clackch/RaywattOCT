using System.Windows;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Component
{
    public class MenuUnableComponent : StackPanel
    {
        public static readonly DependencyProperty IsSaveRawDataDoneProperty = DependencyProperty.Register("IsSaveRawDataDone", typeof(bool), typeof(MenuUnableComponent));
        public bool IsSaveRawDataDone { get => (bool)GetValue(IsSaveRawDataDoneProperty); set => SetValue(IsSaveRawDataDoneProperty, value); }

        public static readonly DependencyProperty IsLumenLoadedProperty = DependencyProperty.Register("IsLumenLoaded", typeof(bool), typeof(MenuUnableComponent));
        public bool IsLumenLoaded { get => (bool)GetValue(IsLumenLoadedProperty); set => SetValue(IsLumenLoadedProperty, value); }

        public static readonly DependencyProperty IsLumenSavedProperty = DependencyProperty.Register("IsLumenSaved", typeof(bool), typeof(MenuUnableComponent));
        public bool IsLumenSaved { get => (bool)GetValue(IsLumenSavedProperty); set => SetValue(IsLumenSavedProperty, value); }

        public static readonly DependencyProperty IsOCTImagingDoneProperty = DependencyProperty.Register("IsOCTImagingDone", typeof(bool), typeof(MenuUnableComponent));
        public bool IsOCTImagingDone { get => (bool)GetValue(IsOCTImagingDoneProperty); set => SetValue(IsOCTImagingDoneProperty, value); }

        public static readonly DependencyProperty IsFfrCalculatedProperty = DependencyProperty.Register("IsFfrCalculated", typeof(bool), typeof(MenuUnableComponent));
        public bool IsFfrCalculated { get => (bool)GetValue(IsFfrCalculatedProperty); set => SetValue(IsFfrCalculatedProperty, value); }
    }
}
