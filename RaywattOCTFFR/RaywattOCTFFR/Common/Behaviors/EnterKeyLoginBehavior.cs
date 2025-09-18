using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RaywattOCTFFR.Common.Behaviors
{
    public static class EnterKeyLoginBehavior
    {
        public static readonly DependencyProperty EnableLoginBehaviorProperty =
            DependencyProperty.RegisterAttached(
                "EnableLoginBehavior",
                typeof(bool),
                typeof(EnterKeyLoginBehavior),
                new PropertyMetadata(false, OnEnableLoginBehaviorChanged));

        public static void SetEnableLoginBehavior(DependencyObject element, bool value)
        {
            element.SetValue(EnableLoginBehaviorProperty, value);
        }

        public static bool GetEnableLoginBehavior(DependencyObject element)
        {
            return (bool)element.GetValue(EnableLoginBehaviorProperty);
        }

        private static void OnEnableLoginBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                    element.PreviewKeyDown += OnPreviewKeyDown;
                else
                    element.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var container = sender as FrameworkElement;
            if (container == null) return;

            var dataContext = container.DataContext;

            var loginCommandProp = dataContext?.GetType().GetProperty("LoginCommand");
            var loginCommand = loginCommandProp?.GetValue(dataContext) as ICommand;

            if (loginCommand?.CanExecute(null) == true)
            {
                loginCommand.Execute(null);
            }

            var window = Window.GetWindow(container);
            if (window == null) return;

            var focusDummy = FindDummyFocusable(window);
            if (focusDummy != null)
            {
                focusDummy.Focus();
                Keyboard.Focus(focusDummy);
            }

            e.Handled = true;
        }

        private static FrameworkElement FindDummyFocusable(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Name == "FocusDummy")
                {
                    return fe;
                }

                var result = FindDummyFocusable(child);
                if (result != null) return result;
            }

            return null;
        }
    }
}
