using MathNet.Numerics.Interpolation;
using System.Collections.Generic;
using System.Linq;
using Point = System.Windows.Point;

namespace RaywattOCTFFR.Common.Angio
{
    public class SplineCurve
    {
        public static List<Point> GetSplinePoints(List<Point> points, int segments)
        {
            int distanceLimit = 30; // 점 간격

            List<Point> sampledPoints = new List<Point>();
            List<Point> curvePoints = new List<Point>();
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

            //PathFinding된 점이 1개 뿐이라면, 경로를 찾지 못했으므로 빈 List 반환
            if (x.Length <= 2 || y.Length <= 2)
            {
                return curvePoints;
            }

            var splineX = CubicSpline.InterpolateNatural(t, x);
            var splineY = CubicSpline.InterpolateNatural(t, y);

            for (int i = 0; i <= segments; i++)
            {
                double ti = i / (double)segments * (t.Last() - t.First());
                curvePoints.Add(new Point((float)splineX.Interpolate(ti), (float)splineY.Interpolate(ti)));
            }

            return curvePoints;
        }
    }
}
