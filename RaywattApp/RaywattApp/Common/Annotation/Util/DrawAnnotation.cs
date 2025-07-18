using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace RaywattApp.Common.Annotation.Util
{
    public class DrawAnnotation
    {
        public static void DrawMeasurements(OpenCvSharp.Mat image, int frameNumber, Size originSize, List<Measurement>? Measurements)
        {
            if (Measurements == null) return;

            foreach (Measurement measurement in Measurements)
            {
                if (frameNumber == measurement.FrameNumber)
                {
                    DrawMeasurement(image, originSize, measurement);
                    break;
                }
            }
        }

        public static void DrawMeasurement(OpenCvSharp.Mat image, Size originSize, Measurement? measurement)
        {
            if(measurement == null) return;

            double xScale = image.Width / originSize.Width;
            double yScale = image.Height / originSize.Height;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();

            if (measurement.AreaGeometries != null)
            {
                foreach (AreaGeometry area in measurement.AreaGeometries)
                {
                    DrawAreaGeometry(context, area, xScale, yScale);
                }
            }

            if (measurement.LengthGeometries != null)
            {
                foreach (LengthGeometry length in measurement.LengthGeometries)
                {
                    DrawLengthGeometry(context, length, xScale, yScale);
                }
            }

            // add by gordon 250718
            if (measurement.AngleGeometries != null)
            {
                foreach (AngleGeometry Angle in measurement.AngleGeometries)
                {
                    DrawAngleGeometry(context, Angle, xScale, yScale);
                }
            }
            // add by gordon 250718


            if (measurement.TextGeometries != null)
            {
                foreach (TextGeometry text in measurement.TextGeometries)
                {
                    DrawTextGeometry(context, text, xScale, yScale);
                }
            }

            context.Close();
            CommonUtil.RenderVisualToMat(visual, image);
        }

        public static void DrawAreaGeometry(DrawingContext context, AreaGeometry area, double xScale, double yScale)
        {
            Brush brush = Constants.AnnotationBrushes[area.Group % Constants.AnnotationBrushes.Length];

            List<Point> scaledPoints = new List<Point>();
            for (int i = 0; i < area.Points.Count; i++)
            {
                scaledPoints.Add(CommonUtil.GetScaledPoint(area.Points[i], xScale, yScale));
            }

            PathGeometry? path = CommonUtil.GetBezierCurve(scaledPoints, true);
            context.DrawGeometry(null, new Pen(brush, 1.0), path);
            if (area.Valid)
            {
                Pen penMin = new Pen(brush, 1.0);
                penMin.DashStyle = Constants.AnnotationDashSmall;
                context.DrawLine(penMin, CommonUtil.GetScaledPoint(area.MinDiameter.point1, xScale, yScale), CommonUtil.GetScaledPoint(area.MinDiameter.point2, xScale, yScale));

                Pen penMax = new Pen(brush, 1.0);
                penMax.DashStyle = Constants.AnnotationDashLarge;
                context.DrawLine(penMax, CommonUtil.GetScaledPoint(area.MaxDiameter.point1, xScale, yScale), CommonUtil.GetScaledPoint(area.MaxDiameter.point2, xScale, yScale));

                DrawText(context, area.CenterOfMass, GetLabelText(area.Group, area.Area), xScale, yScale);
            }
        }
        public static void DrawLengthGeometry(DrawingContext context, LengthGeometry length, double xScale, double yScale)
        {
            Brush brush = Constants.AnnotationBrushes[length.Group % Constants.AnnotationBrushes.Length];

            context.DrawLine(new Pen(brush, 1.0), CommonUtil.GetScaledPoint(length.FirstPoint, xScale, yScale), CommonUtil.GetScaledPoint(length.SecondPoint, xScale, yScale));

            DrawLengthLabel(context, length.FirstPoint, length.SecondPoint, GetLabelText(length.Length), xScale, yScale);
        }

        public static void DrawAngleGeometry(DrawingContext context, AngleGeometry angle, double xScale, double yScale)
        {
            // add by gordon 250718 보류 사용처를 모르겠음
            //Brush brush = Constants.AnnotationBrushes[angle.Group % Constants.AnnotationBrushes.Length];

            //context.DrawLine(new Pen(brush, 1.0), CommonUtil.GetScaledPoint(length.FirstPoint, xScale, yScale), CommonUtil.GetScaledPoint(length.SecondPoint, xScale, yScale));

            //DrawLengthLabel(context, length.FirstPoint, length.SecondPoint, GetLabelText(length.Length), xScale, yScale);
        }

        public static void DrawTextGeometry(DrawingContext context, TextGeometry text, double xScale, double yScale)
        {
            Rect rect = DrawText(context, text.TextPoint, text.Text, xScale, yScale);
            Rect boundingBox = DrawBoundingBox(context, text.Group, rect, 8, 8);

            Point point = CommonUtil.GetScaledPoint(text.PointerPoint, xScale, yScale);
            Brush brush = Constants.AnnotationBrushes[text.Group % Constants.AnnotationBrushes.Length];
            Pen pen = new Pen(brush, 1.0);
            pen.DashStyle = Constants.AnnotationDashLarge;
            point.X -= (Constants.AnnotationTextPointSize / 2);
            point.Y -= (Constants.AnnotationTextPointSize / 2);
            context.DrawLine(pen, point, boundingBox.TopLeft);
            context.DrawEllipse(Brushes.White, new Pen(brush, 1.0), point, Constants.AnnotationTextPointSize, Constants.AnnotationTextPointSize);
        }

        public static Rect DrawBoundingBox(DrawingContext context, int group, Rect rect, int paddingX, int paddingY)
        {
            Brush brush = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            Rect box = new Rect(rect.X, rect.Y, rect.Width, rect.Height);

            box.X -= paddingX;
            box.Y -= paddingY;
            box.Width += (paddingX * 2);
            box.Height += (paddingY * 2);

            context.DrawRectangle(null, new Pen(brush, 1.0), box);

            return box;
        }

        public static Rect DrawText(DrawingContext context, Point point, string value, double xScale, double yScale)
        {
            System.Drawing.Font font = System.Drawing.SystemFonts.DefaultFont;

            FormattedText label = new FormattedText(value,
                System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface(font.Name), Constants.ExportAnnotationFontSize, Brushes.White, 1.0f);

            Point scaledPoint = CommonUtil.GetScaledPoint(point, xScale, yScale);
            scaledPoint.X -= (label.Width / 2);
            scaledPoint.Y -= (label.Height / 2);
            context.DrawText(label, scaledPoint);

            return new Rect(scaledPoint, new Size(label.Width, label.Height));
        }
        public static void DrawLengthLabel(DrawingContext context, Point firstPoint, Point secondPoint, string value, double xScale, double yScale)
        {
            System.Drawing.Font font = System.Drawing.SystemFonts.DefaultFont;

            FormattedText label = new FormattedText(value,
                System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface(font.Name), Constants.ExportAnnotationFontSize, Brushes.White, 1.0f);

            Point scaledFirstPoint = CommonUtil.GetScaledPoint(firstPoint, xScale, yScale);
            Point scaledSecondPoint = CommonUtil.GetScaledPoint(secondPoint, xScale, yScale);

            // find center
            Point ptCenter = new Point();
            ptCenter.X = scaledFirstPoint.X + (scaledSecondPoint.X - scaledFirstPoint.X) / 2;
            ptCenter.Y = scaledFirstPoint.Y + (scaledSecondPoint.Y - scaledFirstPoint.Y) / 2;

            // rotate
            RotateTransform rotate = new RotateTransform();
            rotate.Angle = GetLabelAngle(scaledFirstPoint, scaledSecondPoint, out _);
            rotate.CenterX = ptCenter.X;
            rotate.CenterY = ptCenter.Y;
            context.PushTransform(rotate);

            Point ptLabel = new Point();
            ptLabel.X = ptCenter.X - label.Width / 2;
            ptLabel.Y = ptCenter.Y + label.Height / 2;
            context.DrawText(label, ptLabel);

            context.Pop();
        }

        public static string GetLabelText(int group, double value)
        {
            return "[" + (group + 1) + "] " + (Math.Round(value, 2)).ToString();
        }

        public static string GetLabelText(double value)
        {
            return (Math.Round(value, 2)).ToString();
        }

        public static double GetLabelAngle(Point firstPoint, Point secondPoint, out bool flip)
        {
            double angle = Math.Atan2(firstPoint.Y - secondPoint.Y, firstPoint.X - secondPoint.X) * 180.0f / Math.PI;

            angle = (angle < 0) ? 360 + angle : angle;  // degree -> 0 ~ 360
            angle -= 180;   // degree 180 == 0
            if (Math.Abs(angle) > 90)
            {
                flip = true;
                angle += 180; // flip
            }
            else {
                flip = false;
            }

            return angle;
        }
    }
}
