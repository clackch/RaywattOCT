using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaywattApp.Common.Annotation.LiveWire
{
    public class BezierCurve
    {
        public List<PointF> GenerateBezierCurve(PointF p0, PointF p1, PointF p2, PointF p3, int segments)
        {
            var curvePoints = new List<PointF>();

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                PointF point = CalculateBezierPoint(t, p0, p1, p2, p3);
                curvePoints.Add(point);
            }

            return curvePoints;
        }

        private PointF CalculateBezierPoint(float t, PointF p0, PointF p1, PointF p2, PointF p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            PointF p = new PointF
                (
                    (uuu * p0.X) + (3 * uu * t * p1.X) + (3 * u * tt * p2.X) + (ttt * p3.X),
                    (uuu * p0.Y) + (3 * uu * t * p1.Y) + (3 * u * tt * p2.Y) + (ttt * p3.Y)
                );

            return p;
        }
    }
}
