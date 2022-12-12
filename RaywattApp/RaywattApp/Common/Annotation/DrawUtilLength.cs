using RaywattApp.Common.Annotation.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RaywattApp.Common.Annotation
{
    public partial class DrawUtil
    {
        private const string constEllipse = "Ellipse";

        private const string constLine = "Line";

        private const string constLength = "Length";

        private List<LengthGeometry> lengthGeometries;

        private Point firstPoint;

        private Point secondPoint;

        private double length;

        private bool isFisrtPoint;

        private bool isEllipseClicked;

        private void LengthInit()
        {
            this.isFisrtPoint = false;
            this.isEllipseClicked = false;
        }

        //---------------------------------------------------------------------------------------------------- Event
        private void length_Add(object sender, RoutedEventArgs e)
        {
            _log.Debug("length_Add");

            if (!this.isDrawing)
            {
                this.isDrawing = true;
                this.isFisrtPoint = true;

                //Canvas 마우스 이벤트 활성화
                this.canvas.MouseLeftButtonDown += length_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove += length_canvas_MouseMove;
                this.canvas.MouseLeave += length_canvas_MouseLeave;

                MouseCursor = 2;
            }
        }

        private void length_canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _log.Debug("length_canvas_MouseLeave");

            if (this.isDrawing)
            {
                this.isDrawing = false;
                this.isFisrtPoint = false;
                this.isCanvasClicked = false;

                DeleteLine(this.lengthGeometries.Count);
                DeleteEllipse(this.lengthGeometries.Count, true);
                DeleteLabel(constLength, this.lengthGeometries.Count);

                DeleteLengthAll();
                DrawLengthAll();

                //Canvas 마우스 이벤트 비활성화
                this.canvas.MouseLeftButtonDown -= length_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove -= length_canvas_MouseMove;
                this.canvas.MouseLeave -= length_canvas_MouseLeave;

                MouseCursor = 0;
            }
        }

        private void length_canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("length_canvas_MouseLeftButtonDown");

            if (this.isDrawing)
            {
                if (this.isFisrtPoint)
                {
                    this.firstPoint = e.GetPosition(this.canvas);
                    this.secondPoint = e.GetPosition(this.canvas);

                    this.isFisrtPoint = false;
                    this.isCanvasClicked = true;

                    DrawEllipse(this.firstPoint, this.lengthGeometries.Count, true);
                }
                else
                {
                    this.isDrawing = false;
                    this.isCanvasClicked = false;

                    DeleteLine(this.lengthGeometries.Count);
                    DrawLine(this.firstPoint, this.secondPoint, this.lengthGeometries.Count);

                    DeleteEllipse(this.lengthGeometries.Count, true);
                    DrawEllipse(this.firstPoint, this.lengthGeometries.Count, true);
                    DrawEllipse(this.secondPoint, this.lengthGeometries.Count, false);

                    LengthGeometry lengthGeometry = new LengthGeometry();
                    lengthGeometry.FirstPoint = this.firstPoint;
                    lengthGeometry.SecondPoint = this.secondPoint;
                    lengthGeometry.Group = this.lengthGeometries.Count;
                    lengthGeometry.Length = this.length;
                    this.lengthGeometries.Add(lengthGeometry);

                    //Canvas 마우스 이벤트 비활성화
                    this.canvas.MouseLeftButtonDown -= length_canvas_MouseLeftButtonDown;
                    this.canvas.MouseMove -= length_canvas_MouseMove;
                    this.canvas.MouseLeave -= length_canvas_MouseLeave;

                    MouseCursor = 0;
                }
            }
        }

        private void length_canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isDrawing && this.isCanvasClicked)
            {

                Point point = e.GetPosition(this.canvas);

                if (lastX == point.X && lastY == point.Y)
                    return;

                lastX = point.X;
                lastY = point.Y;

                this.secondPoint = e.GetPosition(this.canvas);

                DeleteLine(this.lengthGeometries.Count);
                DrawLine(this.firstPoint, this.secondPoint, this.lengthGeometries.Count);
            }
        }

        private void ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("ellipse_MouseLeftButtonDown");

            this.isEllipseClicked = true;
            Mouse.Capture((FrameworkElement)sender);
        }

        private void ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("ellipse_MouseLeftButtonUp");

            if (this.isEllipseClicked)
            {
                this.isEllipseClicked = false;

                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DeleteEllipse(group, true);
                DrawEllipse(this.lengthGeometries[group].FirstPoint, group, true);
                DeleteEllipse(group, false);
                DrawEllipse(this.lengthGeometries[group].SecondPoint, group, false);

                this.lengthGeometries[group].Length = this.length;

                Mouse.Capture(null);
            }
        }

        private void ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isEllipseClicked)
            {
                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);
                bool isFirst = bool.Parse(tempArr[2]);
                LengthGeometry lengthGeometry = this.lengthGeometries[group];

                Point point = e.GetPosition(this.canvas);

                if (isFirst)
                {
                    if (point.X > this.canvas.ActualWidth - ellipse.Width / 2 || point.X < ellipse.Width / 2)
                        point.X = lengthGeometry.FirstPoint.X;

                    if (point.Y > this.canvas.ActualHeight - ellipse.Height / 2 || point.Y < ellipse.Height / 2)
                        point.Y = lengthGeometry.FirstPoint.Y;

                    lengthGeometry.FirstPoint = point;
                }
                else
                {
                    if (point.X > this.canvas.ActualWidth - ellipse.Width / 2 || point.X < ellipse.Width / 2)
                        point.X = lengthGeometry.SecondPoint.X;

                    if (point.Y > this.canvas.ActualHeight - ellipse.Height / 2 || point.Y < ellipse.Height / 2)
                        point.Y = lengthGeometry.SecondPoint.Y;

                    lengthGeometry.SecondPoint = point;
                }

                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                DeleteLine(group);
                DrawLine(lengthGeometry.FirstPoint, lengthGeometry.SecondPoint, group);
            }
        }

        private void ellipse_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("ellipse_MouseRightButtonDown");

            Ellipse ellipse = sender as Ellipse;
            string[] tempArr = ellipse.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            DeleteLengthAll();

            for (int i = group + 1; i < this.lengthGeometries.Count; i++)
            {
                this.lengthGeometries[i].Group--;
            }
            this.lengthGeometries.RemoveAt(group);

            DrawLengthAll();
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void DrawLengthAll()
        {
            _log.Debug("DrawLengthAll");

            foreach (var lengthGeometry in this.lengthGeometries)
            {
                DrawLine(lengthGeometry.FirstPoint, lengthGeometry.SecondPoint, lengthGeometry.Group);
                DrawEllipse(lengthGeometry.FirstPoint, lengthGeometry.Group, true);
                DrawEllipse(lengthGeometry.SecondPoint, lengthGeometry.Group, false);
            }
        }

        private void DrawLine(Point firstPoint, Point secondPoint, int group)
        {
            Path path = new Path();
            path.Style = (Style)this.Resources["StylePath"];
            path.Data = GetLine(firstPoint, secondPoint);
            path.Stroke = brushes[group % brushes.Length];
            path.Name = constLine + "_" + group;

            if (this.isDrawing)
            {
                path.StrokeDashArray.Add(2);
            }

            this.canvas.Children.Add(path);

            //Length
            double length = Math.Sqrt(Math.Pow(firstPoint.X - secondPoint.X, 2) + Math.Pow(firstPoint.Y - secondPoint.Y, 2));
            this.length = length;

            DeleteLabel(constLength, group);
            Label label = new Label();
            label.Style = (Style)this.Resources["StyleLabel"];
            label.Name = constLength + "_" + group;
            label.Content = "[" + (group + 1) + "] " + (Math.Round(length, 3)).ToString();
            Canvas.SetLeft(label, secondPoint.X);
            Canvas.SetTop(label, secondPoint.Y);
            this.canvas.Children.Add(label);
        }

        private PathGeometry? GetLine(Point firstPoint, Point secondPoint)
        {
            var myPathFigure = new PathFigure { StartPoint = firstPoint };
            var myPathSegmentCollection = new PathSegmentCollection();
            var myLineSegment = new LineSegment { Point = secondPoint };
            myPathSegmentCollection.Add(myLineSegment);
            myPathFigure.Segments = myPathSegmentCollection;
            var myPathFigureCollection = new PathFigureCollection { myPathFigure };
            var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };

            return myPathGeometry;
        }

        private void DrawEllipse(Point point, int group, bool isFirst)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Name = constEllipse + "_" + group + "_" + isFirst;
            ellipse.Style = (Style)this.Resources["StyleEllipse"];
            ellipse.Stroke = brushes[group % brushes.Length];
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            ellipse.MouseLeftButtonDown += ellipse_MouseLeftButtonDown;
            ellipse.MouseLeftButtonUp += ellipse_MouseLeftButtonUp;
            ellipse.MouseMove += ellipse_MouseMove;
            ellipse.MouseRightButtonDown += ellipse_MouseRightButtonDown;

            this.canvas.Children.Add(ellipse);
        }

        private void DeleteLengthAll()
        {
            _log.Debug("DeleteLengthAll");

            foreach (var lengthGeometry in this.lengthGeometries)
            {
                DeleteLine(lengthGeometry.Group);
                DeleteEllipse(lengthGeometry.Group, true);
                DeleteEllipse(lengthGeometry.Group, false);
                DeleteLabel(constLength, lengthGeometry.Group);
            }
        }

        private void DeleteLine(int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constLine + "_" + group)
                    {
                        this.canvas.Children.Remove((Path)item);
                        break;
                    }
                }
            }
        }

        private void DeleteEllipse(int group, bool isFirst)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Ellipse))
                {
                    Ellipse temp = (Ellipse)item;

                    if (temp.Name == constEllipse + "_" + group + "_" + isFirst)
                    {
                        this.canvas.Children.Remove((Ellipse)item);
                        break;
                    }
                }
            }
        }

    }
}
