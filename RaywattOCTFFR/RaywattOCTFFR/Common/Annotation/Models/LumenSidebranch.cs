using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public partial class LumenSidebranch : ObservableObject
    {
        [ObservableProperty]
        private List<List<Point>>? points;

        [ObservableProperty]
        private bool isCalcOverlapping = false;

        [ObservableProperty]
        private bool isOverlapping = false;
    }
}
