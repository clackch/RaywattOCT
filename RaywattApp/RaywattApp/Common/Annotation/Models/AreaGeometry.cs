using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace RaywattApp.Common.Annotation.Models
{
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
    }
}
