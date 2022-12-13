using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public class LengthGeometry
    {
        private Point firstPoint;
        public Point FirstPoint { get { return firstPoint; } set { firstPoint = value; } }

        private Point secondPoint;
        public Point SecondPoint { get { return secondPoint; } set { secondPoint = value; } }

        private int group;
        public int Group { get { return group; } set { group = value; } }

        private double length;
        public double Length { get { return length; } set { length = value; } }
    }
}
