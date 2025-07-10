using RaywattApp.Models;
using System.Collections.Generic;

namespace RaywattApp.Common.Dialog
{
    public interface IDialogService
    {
        DialogResults OpenDialog(object dialog, Dictionary<string, object> parameter = null, double parentWidth = double.NaN, double parentHeight = double.NaN, double left = double.NaN, double top = double.NaN);
        IDialogWindow OpenChildWindow(object dialog, IModelessPatient parent, Dictionary<string, object> parameter = null, double width = double.NaN, double height = double.NaN, double left = double.NaN, double top = double.NaN);
        public void CloseAllDialogs();
        List<IDialogWindow> GetOpenDialogs();
    }
}
