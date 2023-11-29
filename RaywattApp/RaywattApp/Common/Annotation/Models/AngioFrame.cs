using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.IO;
using System.Windows.Media;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class AngioFrame : ObservableObject
    {
        [ObservableProperty]
        public int angioFrameNumber;

        [ObservableProperty]
        public List<Point> _trackPoints;
    }
}
