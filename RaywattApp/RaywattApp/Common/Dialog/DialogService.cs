using RaywattApp.Common.Bases;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
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

            window.ShowDialog();

            return dialogDataContext.DialogResult;
        }

        private void GetParentSize(out double width, out double height)
        {
            width = Constants.ApplicationWidth;
            height = Constants.ApplicationHeight;
            
            for(int i = 0; i < Application.Current.Windows.Count-1; i++)
            {
                var win = Application.Current.Windows[i];

                if (win.IsActive && win.IsVisible && win.Width != double.NaN && win.Height != double.NaN)
                {
                    width = win.Width;
                    height = win.Height;
                }
            }
        }
    }
}
