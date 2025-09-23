using System.Collections.Generic;
using System.Windows;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public class AngleGeometry
    {
        private Point angleFirstPoint;
        public Point AngleFirstPoint { get { return angleFirstPoint; } set { angleFirstPoint = value; } }

        private Point angleSecondPoint;
        public Point AngleSecondPoint { get { return angleSecondPoint; } set { angleSecondPoint = value; } }

        private Point angleThirdPoint;
        public Point AngleThirdPoint { get { return angleThirdPoint; } set { angleThirdPoint = value; } }

        private Point arcPoint1;
        public Point ArcPoint1 { get { return arcPoint1; } set { arcPoint1 = value; } }

        private Point arcPoint2;
        public Point ArcPoint2 { get { return arcPoint2; } set { arcPoint2 = value; } }

        private double angle;
        public double Angle { get { return angle; } set { angle = value; } }

        private int angleGroup;
        public int AngleGroup { get { return angleGroup; } set { angleGroup = value; } }

    }
}
