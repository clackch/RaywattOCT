using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace RaywattApp.Common.Annotation.Models
{
    public struct DiameterInfo
    {
        public Point point1, point2;
        public double diameter;
    }
    public class AreaGeometry
    {
        private List<Point> points;
        public List<Point> Points { get { return points; } set { points = value; } }

        private int group;
        public int Group { get { return group; } set { group = value; } }

        private bool isClosed;
        public bool IsClosed { get { return isClosed; } set { isClosed = value; } }

        private double area;
        public double Area { get { return area; } set { area = value; } }

        private List<OpenCvSharp.Point> pointsAll;
        public List<OpenCvSharp.Point> PointsAll { get { return pointsAll; } set { pointsAll = value; } }

        private Point centerOfMass;
        public Point CenterOfMass { get { return centerOfMass; } set { centerOfMass = value; } }

        private DiameterInfo minDiameter;
        public DiameterInfo MinDiameter { get { return minDiameter; } set { minDiameter = value; } }

        private DiameterInfo maxDiameter;
        public DiameterInfo MaxDiameter { get { return maxDiameter; } set { maxDiameter = value; } }

        private double meanDiameter;
        public double MeanDiameter { get { return meanDiameter; } set { meanDiameter = value; } }

        private bool validDiameter;
        public bool ValidDiameter { get { return validDiameter; } set { validDiameter = value; } }

        private Path path;
        public Path Path { get { return path; } set { path = value; } }
    }
}
