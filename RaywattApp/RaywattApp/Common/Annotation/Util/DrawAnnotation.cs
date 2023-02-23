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
        public static void DrawMeasurements(OpenCvSharp.Mat image, int frame, List<Measurement>? Measurements)
        {
            if (Measurements == null) return;

            double xScale = image.Width / Constants.CrossSectionSize;
            double yScale = image.Height / Constants.CrossSectionSize;

            foreach (Measurement measurement in Measurements)
            {
                if (frame == measurement.FrameNumber)
                {
                    DrawingVisual visual = new DrawingVisual();
                    DrawingContext context = visual.RenderOpen();

                    foreach (AreaGeometry area in measurement.AreaGeometries)
                    {
                        DrawAreaGeometry(context, area, xScale, yScale);
                    }

                    foreach (LengthGeometry length in measurement.LengthGeometries)
                    {
                        DrawLengthGeometry(context, length, xScale, yScale);
                    }

                    foreach (TextGeometry text in measurement.TextGeometries)
                    {
                        Rect rect = DrawText(context, text.TextPoint, text.Text, xScale, yScale);
                        Rect boundingBox = DrawBoundingBox(context, text.Group, rect, 8, 8);

                        Point point = CommonUtil.GetScaledPoint(text.PointerPoint, xScale, yScale);
                        const double ellipseSize = 6;
                        Brush brush = Constants.AnnotationBrushes[text.Group % Constants.AnnotationBrushes.Length];
                        Pen pen = new Pen(brush, 1.0);
                        pen.DashStyle = new DashStyle(new double[] { 7, 7 }, 0);
                        point.X -= (ellipseSize / 2);
                        point.Y -= (ellipseSize / 2);
                        context.DrawLine(pen, point, boundingBox.TopLeft);
                        context.DrawEllipse(Brushes.White, new Pen(brush, 1.0), point, ellipseSize, ellipseSize);
                    }

                    context.Close();
                    CommonUtil.RenderVisualToMat(visual, image);
                    break;
                }
            }
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
                penMin.DashStyle = new DashStyle(new double[] { 2, 5 }, 0);
                context.DrawLine(penMin, CommonUtil.GetScaledPoint(area.MinDiameter.point1, xScale, yScale), CommonUtil.GetScaledPoint(area.MinDiameter.point2, xScale, yScale));

                Pen penMax = new Pen(brush, 1.0);
                penMax.DashStyle = new DashStyle(new double[] { 7, 7 }, 0);
                context.DrawLine(penMax, CommonUtil.GetScaledPoint(area.MaxDiameter.point1, xScale, yScale), CommonUtil.GetScaledPoint(area.MaxDiameter.point2, xScale, yScale));

                DrawLabel(context, area.CenterOfMass, area.Group, area.Area, xScale, yScale);
            }
        }
        public static void DrawLengthGeometry(DrawingContext context, LengthGeometry length, double xScale, double yScale)
        {
            Brush brush = Constants.AnnotationBrushes[length.Group % Constants.AnnotationBrushes.Length];

            context.DrawLine(new Pen(brush, 1.0), CommonUtil.GetScaledPoint(length.FirstPoint, xScale, yScale), CommonUtil.GetScaledPoint(length.SecondPoint, xScale, yScale));

            DrawLabel(context, length.SecondPoint, length.Group, length.Length, xScale, yScale);
        }

        public static void DrawLabel(DrawingContext context, Point point, int group, double value, double xScale, double yScale)
        {
            DrawText(context, point, "[" + (group + 1) + "] " + (Math.Round(value, 3)).ToString(), xScale, yScale);
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
    }
}
