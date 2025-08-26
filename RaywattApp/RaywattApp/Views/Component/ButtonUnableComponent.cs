using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Component
{
    public class ButtonUnableComponent : Button
    {
        public static readonly DependencyProperty IsSaveRawDataDoneProperty = DependencyProperty.Register("IsSaveRawDataDone", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsSaveRawDataDone { get => (bool)GetValue(IsSaveRawDataDoneProperty); set => SetValue(IsSaveRawDataDoneProperty, value); }

        public static readonly DependencyProperty IsLumenLoadedProperty = DependencyProperty.Register("IsLumenLoaded", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsLumenLoaded { get => (bool)GetValue(IsLumenLoadedProperty); set => SetValue(IsLumenLoadedProperty, value); }

        public static readonly DependencyProperty IsLumenSavedProperty = DependencyProperty.Register("IsLumenSaved", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsLumenSaved { get => (bool)GetValue(IsLumenSavedProperty); set => SetValue(IsLumenSavedProperty, value); }

        public static readonly DependencyProperty IsOCTImagingDoneProperty = DependencyProperty.Register("IsOCTImagingDone", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsOCTImagingDone { get => (bool)GetValue(IsOCTImagingDoneProperty); set => SetValue(IsOCTImagingDoneProperty, value); }

        public static readonly DependencyProperty IsImageProcessingDoneProperty = DependencyProperty.Register("IsImageProcessingDone", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsImageProcessingDone { get => (bool)GetValue(IsImageProcessingDoneProperty); set => SetValue(IsImageProcessingDoneProperty, value); }
    }
}
