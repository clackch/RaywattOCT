using System.Collections.Generic;
using System.Drawing;
using Point = System.Windows.Point;

namespace RaywattApp.Common.Angio
{
    public class BezierCurve
    {
        public List<Point> GenerateBezierCurve(Point p0, Point p1, Point p2, Point p3, int segments)
        {
            var curvePoints = new List<Point>();

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Point point = CalculateBezierPoint(t, p0, p1, p2, p3);
                curvePoints.Add(point);
            }

            return curvePoints;
        }

        private Point CalculateBezierPoint(float t, Point p0, Point p1, Point p2, Point p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Point p = new Point
                (
                    uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X,
                    uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y
                );

            return p;
        }
    }
}
