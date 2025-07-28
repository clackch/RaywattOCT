using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Component
{
    public class ButtonUnableComponent : Button
    {
        public static DependencyProperty IsFirstProperty = DependencyProperty.Register("IsFirst", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsFirst { get => (bool)GetValue(IsFirstProperty); set => SetValue(IsFirstProperty, value); }

        public static DependencyProperty IsSecondProperty = DependencyProperty.Register("IsSecond", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsSecond { get => (bool)GetValue(IsSecondProperty); set => SetValue(IsSecondProperty, value); }

        public static DependencyProperty IsThirdProperty = DependencyProperty.Register("IsThird", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsThird { get => (bool)GetValue(IsThirdProperty); set => SetValue(IsThirdProperty, value); }

        public static DependencyProperty IsFourthProperty = DependencyProperty.Register("IsFourth", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsFourth { get => (bool)GetValue(IsFourthProperty); set => SetValue(IsFourthProperty, value); }

        public static DependencyProperty IsFifthProperty = DependencyProperty.Register("IsFifth", typeof(bool), typeof(ButtonUnableComponent));
        public bool IsFifth { get => (bool)GetValue(IsFifthProperty); set => SetValue(IsFifthProperty, value); }
    }
}
