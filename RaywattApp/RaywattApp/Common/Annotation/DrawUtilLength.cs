using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Annotation.Util;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.ObjectModel;
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

        private ObservableCollection<LengthGeometry> lengthGeometries;

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

                this.canvas.Background = null;
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

                    InCommand = Constants.MeasureDsbLLen;
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

                    this.canvas.Background = null;
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
        private void AddLength()
        {
            _log.Debug("AddLength");

            if (!this.isDrawing)
            {
                this.isDrawing = true;
                this.isFisrtPoint = true;

                //Canvas 마우스 이벤트 활성화
                this.canvas.MouseLeftButtonDown += length_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove += length_canvas_MouseMove;
                this.canvas.MouseLeave += length_canvas_MouseLeave;

                this.canvas.Background = Brushes.Transparent;
                MouseCursor = 2;
            }
        }

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
            path.Data = CommonUtil.GetLine(firstPoint, secondPoint);
            path.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            path.Name = constLine + "_" + group;

            if (this.isDrawing)
            {
                path.StrokeDashArray.Add(2);
            }

            this.canvas.Children.Add(path);

            //Length
            double deltaX = secondPoint.X - firstPoint.X;
            double deltaY = secondPoint.Y - firstPoint.Y;
            double length = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            this.length = length;
            if(this.lengthGeometries.Count > group)
                this.lengthGeometries[group].Length = this.length;

            DeleteLabel(constLength, group);

            // find center
            Point ptLabel = new Point();
            ptLabel.X = firstPoint.X + (secondPoint.X - firstPoint.X) / 2;
            ptLabel.Y = firstPoint.Y + (secondPoint.Y - firstPoint.Y) / 2;
            
            // rotate
            bool flip;
            double angle = DrawAnnotation.GetLabelAngle(firstPoint, secondPoint, out flip);

            // move label point along the line
            double unitX = deltaX / length;
            double unitY = deltaY / length;
            ptLabel.X += (unitX * 40 * (flip ? 1 : -1));
            ptLabel.Y += (unitY * 40 * (flip ? 1 : -1));

            Label label = new Label();
            label.Style = (Style)this.Resources["StyleLabel"];
            label.Name = constLength + "_" + group;
            label.Content = DrawAnnotation.GetLabelText(group, length);
            label.RenderTransform = new RotateTransform(angle);

            Canvas.SetLeft(label, ptLabel.X);
            Canvas.SetTop(label, ptLabel.Y);
            this.canvas.Children.Add(label);
        }


        private void DrawEllipse(Point point, int group, bool isFirst)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Name = constEllipse + "_" + group + "_" + isFirst;
            ellipse.Style = (Style)this.Resources["StyleEllipse"];
            ellipse.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
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
