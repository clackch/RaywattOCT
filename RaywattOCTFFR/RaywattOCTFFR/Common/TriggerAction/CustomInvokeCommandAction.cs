using Microsoft.Xaml.Behaviors;
using System.Windows.Input;
using System.Windows;

namespace RaywattOCTFFR.Common.TriggerAction
{
    public class CustomInvokeCommandAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(CustomInvokeCommandAction), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(CustomInvokeCommandAction), new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (Command != null && Command.CanExecute(CommandParameter))
            {
                Command.Execute(CommandParameter);
            }

            // 이벤트 핸들링 이후 이벤트 전파 방지
            if (parameter is RoutedEventArgs eventArgs)
            {
                eventArgs.Handled = true;
            }
        }
    }

}
