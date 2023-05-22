using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using System;
using System.Collections.Generic;

namespace RaywattApp.Common.Annotation.Util
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

        private void calculateArea(Contour contour, Point[] cvPoints)
        {
            contour.Area = Cv2.ContourArea(cvPoints);
        }

        public bool CalculateDiameter(Contour contour)
        {
            bool isValid = false;
            Point ptFrom = new Point(contour.CenterOfMass.X - contourBounds.X, contour.CenterOfMass.Y - contourBounds.Y);

            double minDiameter = double.MaxValue;
            double maxDiameter = double.MinValue;

            Rect rectROI = new Rect((int)contourBounds.X, (int)contourBounds.Y, (int)contourBounds.Width, (int)contourBounds.Height);
            Mat imageRoi = contourImage[rectROI];

            double sumDiameter = 0;
            int numOfDiameter = 0;
            for (int degree = 0; degree < 180; degree++)
            {
                Point point1 = getIntersectionPoint(contour, imageRoi, ptFrom, degree);
                if (point1.X < 0 || point1.Y < 0) continue;

                Point point2 = getIntersectionPoint(contour, imageRoi, ptFrom, degree + 180);
                if (point2.X < 0 || point2.Y < 0) continue;

                double diameter = Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));

                DiameterInfo diameterInfo = new DiameterInfo();
                diameterInfo.point1 = new System.Windows.Point(point1.X, point1.Y);
                diameterInfo.point2 = new System.Windows.Point(point2.X, point2.Y);
                diameterInfo.value = diameter;
                isValid = true;

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

            contour.MeanDiameter = (numOfDiameter == 0) ? 0 : sumDiameter / numOfDiameter;

            return isValid;
        }
        private Point getIntersectionPoint(Contour contour, Mat imageRoi, OpenCvSharp.Point ptFrom, int degree)
        {
            double lineLength = Math.Sqrt(Math.Pow(contourBounds.Right - contourBounds.Left, 2) + Math.Pow(contourBounds.Bottom - contourBounds.Top, 2));
            Scalar color = new Scalar(0x00, 0x00, 0x00);

            Mat imageMask = imageRoi.Clone();

            double xDirection = Math.Cos(degree * Math.PI / 180.0f);
            double yDirection = Math.Sin(degree * Math.PI / 180.0f);

            Point ptTo = new OpenCvSharp.Point(ptFrom.X + lineLength * xDirection, ptFrom.Y + lineLength * yDirection);

            Cv2.Line(imageMask, ptFrom, ptTo, color, 1, LineTypes.Link4);

            Mat imageSub = imageRoi - imageMask;

            Point point = new OpenCvSharp.Point(-1, -1);
            List<Point> pointsIntersection = findNonZero(pointsAll, imageSub);

            if (pointsIntersection.Count < 1)
            {
                return point;
            }

            point = pointsIntersection[0];
            double minDistance = Math.Sqrt(Math.Pow(ptFrom.X - (point.X - contourBounds.X), 2) + Math.Pow(ptFrom.Y - (point.Y - contourBounds.Y), 2));
            for (int i = 1; i < pointsIntersection.Count; i++)
            {
                Point nextPoint = pointsIntersection[i];
                double distance = Math.Sqrt(Math.Pow(ptFrom.X - (nextPoint.X - contourBounds.X), 2) + Math.Pow(ptFrom.Y - (nextPoint.Y - contourBounds.Y), 2));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    point = nextPoint;
                }
            }

            return point;
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

        private List<Point> findNonZero(List<Point> points, Mat image)
        {
            List<Point> nonZeroPoints = new List<Point>();

            foreach (var point in points)
            {
                if (image.At<byte>(point.Y - contourBounds.Y, point.X - contourBounds.X) != 0x00) nonZeroPoints.Add(point);
            }

            return nonZeroPoints;
        }
    }
}
