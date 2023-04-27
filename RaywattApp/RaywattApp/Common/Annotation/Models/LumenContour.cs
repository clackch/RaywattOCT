using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenContour : ObservableObject
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
        private List<Point>? points;

        [ObservableProperty]
        private List<OpenCvSharp.Point>? pointsAll;

        [ObservableProperty]
        private double area;

        public double DisplayArea { get { return area / Constants.ScaleArea; } }

        [ObservableProperty]
        private Point centerOfMass;

        [ObservableProperty]
        private DiameterInfo? minDiameter;

        [ObservableProperty]
        private DiameterInfo? maxDiameter;

        [ObservableProperty]
        private double meanDiameter;

        public double DisplayMeanDiameter { get { return meanDiameter / Constants.ScaleLength; } }
    }
}
