using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class Contour : ObservableObject
    {
        [ObservableProperty]
        protected List<Point>? points;

        [ObservableProperty]
        protected double area;

        [ObservableProperty]
        protected Point centerOfMass;

        [ObservableProperty]
        protected DiameterInfo? minDiameter;

        [ObservableProperty]
        protected DiameterInfo? maxDiameter;

        [ObservableProperty]
        protected double meanDiameter;
    }
}
