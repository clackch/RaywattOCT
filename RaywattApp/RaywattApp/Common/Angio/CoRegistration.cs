using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace RaywattApp.Common.Angio
{
    public partial class CoRegistration : ObservableObject
    {
        [ObservableProperty]
        private List<Point> _trackPoint;

        [ObservableProperty]
        private List<List<Point>> _line;
    }
}
