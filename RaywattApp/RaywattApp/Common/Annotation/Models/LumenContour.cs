using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public class LumenContour : ObservableObject
    {
        private List<Point>? pointsFromMl;
        public List<Point>? PointsFromMl { get { return pointsFromMl; } set { pointsFromMl = value; } }

        private List<Point>? points;
        public List<Point>? Points { get { return points; } set { points = value; } }

        private List<OpenCvSharp.Point>? pointsAll;
        public List<OpenCvSharp.Point>? PointsAll { get { return pointsAll; } set { pointsAll = value; } }

        private double area;
        public double Area { get { return area; } set { area = value; OnPropertyChanged(nameof(Area)); } }

        public double DisplayArea { get { return area / Constants.ScaleArea; } }

        private Point centerOfMass;
        public Point CenterOfMass { get { return centerOfMass; } set { centerOfMass = value; } }

        private DiameterInfo? minDiameter;
        public DiameterInfo? MinDiameter { get { return minDiameter; } set { minDiameter = value; OnPropertyChanged(nameof(MinDiameter)); } }

        private DiameterInfo? maxDiameter;
        public DiameterInfo? MaxDiameter { get { return maxDiameter; } set { maxDiameter = value; OnPropertyChanged(nameof(MaxDiameter)); } }

        private double meanDiameter;
        public double MeanDiameter { get { return meanDiameter; } set { meanDiameter = value; OnPropertyChanged(nameof(MeanDiameter)); } }

        public double DisplayMeanDiameter { get { return meanDiameter / Constants.ScaleLength; } }
    }
}
