using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public partial class LumenGuidewire : ObservableObject
    {
        [ObservableProperty]
        private List<Point>? points;
    }
}
