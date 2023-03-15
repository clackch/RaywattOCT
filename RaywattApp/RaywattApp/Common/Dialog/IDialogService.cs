using RaywattApp.Models;
using System.Collections.Generic;

namespace RaywattApp.Common.Dialog
{
    public interface IDialogService
    {
        DialogResults OpenDialog(object dialog, Dictionary<string, object> parameter = null, double parentWidth = double.NaN, double parentHeight = double.NaN, double left = double.NaN, double top = double.NaN);
    }
}
