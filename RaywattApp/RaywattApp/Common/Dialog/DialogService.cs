using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Dialog
{
    public class DialogService : IDialogService
    {
        public DialogResults OpenDialog(object dialog, Dictionary<string, object> parameter, double left, double top)
        {
            var dialogFE = dialog as FrameworkElement;
            var dialogDataContext = dialogFE.DataContext as DialogViewModelBase;
            Window mainWindow = Application.Current.MainWindow;

            IDialogWindow window = new DialogWindow();
            window.Content = dialog;
            window.DataContext = dialogDataContext;
            
            if(double.NaN.Equals(left))
            {
                window.Left = mainWindow.Left + (mainWindow.Width - dialogFE.Width) / 2;
            }
            else
            {
                window.Left = left;
            }

            if(double.NaN.Equals(top))
            {
                window.Top = mainWindow.Top + (mainWindow.Height - dialogFE.Height) / 2;
            }
            else
            {
                window.Top = top; 
            }

            if (parameter != null)
                dialogDataContext.SetParameter(parameter);

            window.ShowDialog();

            return dialogDataContext.DialogResult;
        }
    }
}
