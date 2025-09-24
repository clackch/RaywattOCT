using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public partial class LumenStent : ObservableObject
    {
        [ObservableProperty]
        private List<Point>? points;

        [ObservableProperty]
        private List<double>? appositionLength;

        [ObservableProperty]
        private bool isStent;
    }
}
