using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Component
{
    public class ButtonUnableComponent : Button
    {
        public static DependencyProperty IsLumenLoadedProperty = DependencyProperty.Register("IsLumenLoaded", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsLumenLoaded { get => (bool)GetValue(IsLumenLoadedProperty); set => SetValue(IsLumenLoadedProperty, value); }

        public static DependencyProperty IsLumenSavedProperty = DependencyProperty.Register("IsLumenSaved", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsLumenSaved { get => (bool)GetValue(IsLumenSavedProperty); set => SetValue(IsLumenSavedProperty, value); }

        public static DependencyProperty IsOCTImagingDoneProperty = DependencyProperty.Register("IsOCTImagingDone", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsOCTImagingDone { get => (bool)GetValue(IsOCTImagingDoneProperty); set => SetValue(IsOCTImagingDoneProperty, value); }
    }
}
