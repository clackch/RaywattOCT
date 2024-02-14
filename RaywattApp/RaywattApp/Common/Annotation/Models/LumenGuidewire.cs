using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenGuidewire : ObservableObject
    {
        [ObservableProperty]
        protected List<Point>? points;
    }
}
