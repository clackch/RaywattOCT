using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointF = System.Drawing.PointF;

namespace LiveWire
{
    public class SplineCurve
    {
        public List<PointF> GetSplinePoints(List<PointF> points, int segments)
        {
            int distanceLimit = 30; // 점 간격

            List<PointF> sampledPoints = new List<PointF>();
            List<PointF> curvePoints = new List<PointF>();
            for (int i = 0; i < points.Count; i += distanceLimit)
            {
                sampledPoints.Add(points[i]);
            }

            if (sampledPoints.Last() != points.Last())
            {
                sampledPoints.Add(points.Last());
            }

            // t -> 시퀀스 데이터 생성 후 double타입 변환 -> 점들의 가이드 역할
            double[] t = Enumerable.Range(0, sampledPoints.Count).Select(i => (double)i).ToArray();

            // 각 점을 double타입으로 변환
            double[] x = sampledPoints.Select(p => (double)p.X).ToArray();
            double[] y = sampledPoints.Select(p => (double)p.Y).ToArray();

            var splineX = CubicSpline.InterpolateNatural(t, x);
            var splineY = CubicSpline.InterpolateNatural(t, y);

            for (int i = 0; i <= segments; i++)
            {
                double ti = i / (double)segments * (t.Last() - t.First());
                curvePoints.Add(new PointF((float)splineX.Interpolate(ti), (float)splineY.Interpolate(ti)));
            }

            return curvePoints;
        }
    }
}
