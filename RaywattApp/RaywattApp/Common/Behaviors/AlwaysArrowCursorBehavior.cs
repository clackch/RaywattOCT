using System.Windows;
using System.Windows.Input;

namespace RaywattApp.Common.Behaviors
{
    public static class AlwaysArrowCursorBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(AlwaysArrowCursorBehavior),
                new PropertyMetadata(false, OnEnabledChanged));

        public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);
        public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement el)
            {
                if ((bool)e.NewValue)
                {
                    if (Application.Current.TryFindResource("arrow") is Cursor customCursor)
                        Mouse.OverrideCursor = customCursor;
                }
                else
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }
    }
}
