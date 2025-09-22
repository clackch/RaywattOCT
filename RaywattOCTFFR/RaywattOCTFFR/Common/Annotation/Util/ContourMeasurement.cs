using OpenCvSharp;
using RaywattOCTFFR.Common.Annotation.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RaywattOCTFFR.Common.Annotation.Util
{
    public class ContourMeasurement
    {
        private Mat contourImage;
        private Rect contourBounds;
        List<Point> pointsAll;

        public void Measure(Contour contour, Mat contourImage)
        {
            this.contourImage = contourImage;

            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(contourImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
                return;

            contourBounds = Cv2.BoundingRect(contours[0]);

            calculateArea(contour, contours[0]);
            calculateCenterOfMass(contour, contours[0]);
            findAllPoints();
        }

        public void Measure(Contour contour, int width, int height)
        {
            Point[] cvPoints = new Point[contour.Points.Count];
            for (int i = 0; i < contour.Points.Count; i++)
            {
                cvPoints[i] = new Point(contour.Points[i].X, contour.Points[i].Y);
            }

            contourBounds = Cv2.BoundingRect(cvPoints);
            contourImage = new Mat(height, width, MatType.CV_8UC1);
            contourImage.SetTo(Scalar.Black);

            Point[][] cvContours = new Point[1][];
            cvContours[0] = cvPoints;
            Cv2.DrawContours(contourImage, cvContours, 0, Scalar.White);

            calculateArea(contour, cvPoints);
            calculateCenterOfMass(contour, cvPoints);
            findAllPoints();
        }
        private void validate(Contour contour, Point[][] contours)
        {
            Mat imageFill = contourImage.EmptyClone();
            imageFill.SetTo(Scalar.White);

            Cv2.DrawContours(imageFill, contours, -1, Scalar.Black, -1);

            Mat imageCenter = imageFill.Clone();
            imageCenter.At<byte>((int)contour.CenterOfMass.Y, (int)contour.CenterOfMass.X) = 0xff;

            Mat imageSub = imageCenter - imageFill;

            Mat points = new Mat();
            Cv2.FindNonZero(imageSub, points);

            contour.Valid = (points.Rows == 1);
        }
        private void calculateCenterOfMass(Contour contour, Point[] cvPoints)
        {
            Moments moments = Cv2.Moments(cvPoints);
            System.Windows.Point centerOfMass;
            centerOfMass.X = moments.M10 / moments.M00;
            centerOfMass.Y = moments.M01 / moments.M00;

            contour.CenterOfMass = centerOfMass;

            Point[][] cvContours = new Point[1][];
            cvContours[0] = cvPoints;
            validate(contour, cvContours);
        }

        private static void calculateArea(Contour contour, Point[] cvPoints)
        {
            contour.Area = Cv2.ContourArea(cvPoints);
        }

        public bool CalculateDiameter(Contour contour)
        {
            int[] conflict = new int[360];
            Dictionary<double, Tuple<Point, double>> dictionary = new Dictionary<double, Tuple<Point, double>>();
            foreach (Point point in pointsAll)
            {
                double x = contour.CenterOfMass.X - point.X;
                double y = contour.CenterOfMass.Y - point.Y;
                double degree = Math.Round(Math.Atan2(y, x) * 180 / Math.PI) + 180;
                double distance = Math.Sqrt(Math.Pow(contour.CenterOfMass.X - point.X, 2) + Math.Pow(contour.CenterOfMass.Y - point.Y, 2));

                degree = (degree == 0) ? 360 : degree;

                if (dictionary.TryGetValue(degree, out var existing))
                {
                    conflict[(int)degree - 1]++;
                    if (distance > existing.Item2)
                    {
                        dictionary[degree] = new Tuple<Point, double>(point, distance); // 덮어쓰기
                    }
                }
                else
                {
                    dictionary.Add(degree, new Tuple<Point, double>(point, distance));
                }
            }

            double minDiameter = double.MaxValue;
            double maxDiameter = double.MinValue;

            double sumDiameter = 0;
            int numOfDiameter = 0;
            for (int degree = 1; degree <= 180; degree++)
            {
                if (!dictionary.ContainsKey(degree) || !dictionary.ContainsKey(degree + 180)) continue;

                Tuple<Point, double> point1 = dictionary[degree];
                Tuple<Point, double> point2 = dictionary[degree + 180];

                double diameter = point1.Item2 + point2.Item2;

                DiameterInfo diameterInfo = new DiameterInfo();
                diameterInfo.point1 = new System.Windows.Point(point1.Item1.X, point1.Item1.Y);
                diameterInfo.point2 = new System.Windows.Point(point2.Item1.X, point2.Item1.Y);
                diameterInfo.value = diameter;

                sumDiameter += diameterInfo.value;
                numOfDiameter++;

                if (diameter < minDiameter)
                {
                    minDiameter = diameter;
                    contour.MinDiameter = diameterInfo;
                }

                if (diameter > maxDiameter)
                {
                    maxDiameter = diameter;
                    contour.MaxDiameter = diameterInfo;
                }
            }

            contour.MeanDiameter = sumDiameter / numOfDiameter;

            return true;
        }

        private void findAllPoints()
        {
            pointsAll = new List<Point>();

            Mat points = new Mat();
            Cv2.FindNonZero(contourImage, points);

            for (int i = 0; i < points.Rows; i++)
            {
                Point point = points.At<Point>(i);
                pointsAll.Add(point);
            }
        }
    }
}
