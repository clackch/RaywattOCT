using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenContour : Contour
    {
        [ObservableProperty]
        private List<Point>? mlPoints;

        [ObservableProperty]
        private double mlArea;

        [ObservableProperty]
        private DiameterInfo? mlMinDiameter;

        [ObservableProperty]
        private DiameterInfo? mlMaxDiameter;

        [ObservableProperty]
        private double mlMeanDiameter;

        [ObservableProperty]
        private List<OpenCvSharp.Point>? pointsAll;

        public double DisplayArea { get { return area / Constants.ScaleArea; } }

        public double DisplayMeanDiameter { get { return meanDiameter / Constants.ScaleLength; } }
    }
}
