using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.IO;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class AngioPath : ObservableObject
    {
        [ObservableProperty]
        List<Point> guidePoints;

        [ObservableProperty]
        Point followPoint;
    }
}
