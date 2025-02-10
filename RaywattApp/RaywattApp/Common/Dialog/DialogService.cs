using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaywattApp.Common.Dialog
{
    public class DialogService : IDialogService
    {
        public DialogResults OpenDialog(object dialog, Dictionary<string, object> parameter, double parentWidth, double parentHeight, double left, double top)
        {
            var dialogFE = dialog as FrameworkElement;
            var dialogDataContext = dialogFE.DataContext as DialogViewModelBase;
            Window mainWindow = Application.Current.MainWindow;

            dialogDataContext.DialogWidth = parentWidth;
            dialogDataContext.DialogHeight = parentHeight;

            IDialogWindow window = new DialogWindow();
            window.Content = dialog;
            window.DataContext = dialogDataContext;
            
            if(double.NaN.Equals(left))
            {
                window.Left = mainWindow.Left + (mainWindow.Width - parentWidth) / 2;
            }
            else
            {
                window.Left = left;
            }

            if(double.NaN.Equals(top))
            {
                window.Top = mainWindow.Top + (mainWindow.Height - parentHeight) / 2;
            }
            else
            {
                window.Top = top; 
            }

            if (parameter != null)
                dialogDataContext.SetParameter(parameter);
            
            window.Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            window.ShowDialog();

            return dialogDataContext.DialogResult;
        }
        public IDialogWindow OpenChildWindow(object dialog, IModelessPatient parent, Dictionary<string, object> parameter, double width, double height, double left, double top)
        {
            var dialogFE = dialog as FrameworkElement;
            var dialogDataContext = dialogFE.DataContext as ModelessViewModelBase;
            Window mainWindow = Application.Current.MainWindow;

            dialogDataContext.DialogWidth = width;
            dialogDataContext.DialogHeight = height;

            IDialogWindow window = new ChildWindow();
            window.Content = dialog;
            window.DataContext = dialogDataContext;

            if (double.NaN.Equals(left))
            {
                window.Left = mainWindow.Left + (mainWindow.Width - width) / 2;
            }
            else
            {
                window.Left = left;
            }

            if (double.NaN.Equals(top))
            {
                window.Top = mainWindow.Top + (mainWindow.Height - height) / 2;
            }
            else
            {
                window.Top = top;
            }

            if (parameter != null)
                dialogDataContext.SetParameter(parent, parameter);

            window.Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            window.Show();

            return window;
        }
    }
}
