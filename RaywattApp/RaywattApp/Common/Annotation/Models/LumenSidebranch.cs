using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenSidebranch : ObservableObject
    {
        [ObservableProperty]
        private List<List<Point>>? points;
    }
}
