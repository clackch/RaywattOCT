using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Label = System.Windows.Controls.Label;


namespace RaywattApp.Common.Annotation
{
    public partial class DrawUtil
    {
        private const string constAngle = "Angle";

        private const string constAngleArc = "Arc";

        private ObservableCollection<AngleGeometry> angleGeometries;

        private Point angleFirstPoint;

        private Point angleSecondPoint;

        private Point angleThirdPoint;

        private Point angleMovingPoint;

        private Point arcPoint1;

        private Point arcPoint2;

        private Point diffAngleFirst;

        private Point diffAngleSecond;

        private Point diffAngleThird;

        private double angle;

        private bool isAngleFirstPoint;

        private bool isAngleSecondPoint;

        private bool isAngleClicked;


        private void AngleInit() //각도 초기화
        {
            this.isAngleFirstPoint = false;
            this.isAngleSecondPoint = false;
            this.isAngleClicked = false;
        }

        private void AddAngle(string isAngleOn)
        {
            _log.Debug("AddAngle");

            if (CommandType != 0)
                DisableCommand();
            if (Convert.ToBoolean(isAngleOn))
            {

                this.isDrawing = true;
                this.isAngleFirstPoint = true;

                //Canvas 마우스 이벤트 활성화
                this.canvas.MouseLeftButtonDown += angle_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove += angle_canvas_MouseMove;
                this.canvas.MouseLeave += angle_canvas_MouseLeave;

                this.canvas.Background = Brushes.Transparent;
                CommandType = Constants.MeasureCmdAngle;

                MouseCursor = 2;
            }
        }

        private void angle_canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _log.Debug("angle_canvas_MouseLeave");

            if (this.isDrawing && this.isCanvasClicked)
            {    
                this.isAngleFirstPoint = true;
                this.isCanvasClicked = false;

                DeleteLine(true, this.angleGeometries.Count);
                DeleteLine(false, this.angleGeometries.Count);
                DeleteEllipse(this.angleGeometries.Count, 1);
                DeleteEllipse(this.angleGeometries.Count, 2);
                DeleteLabel(constAngle, this.angleGeometries.Count);

                //DeleteAngleAll();
                //DrawAngleAll();
                
                MouseCursor = 2;
            }
        }

        private void angle_canvas_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            _log.Debug("angle_canvas_MouseLeftButtonDown");

            if (this.isDrawing)
            {

                if (this.isAngleFirstPoint)
                {
                    _log.Debug("FirstPoint");
                    this.angleFirstPoint = e.GetPosition(this.canvas);

                    this.isAngleFirstPoint = false;
                    this.isCanvasClicked = true;
                    this.isAngleSecondPoint = true;


                    DrawEllipse(this.angleFirstPoint, this.angleGeometries.Count, 1);


                }
                else if (this.isAngleSecondPoint)
                {
                    _log.Debug("SecondPoint");
                    this.angleSecondPoint = e.GetPosition(this.canvas);

                    DeleteEllipse(this.angleGeometries.Count, 1);
                    DrawEllipse(this.angleFirstPoint, this.angleGeometries.Count, 1);
                    DrawEllipse(this.angleSecondPoint, this.angleGeometries.Count, 2);
                    this.isAngleSecondPoint = false;
                }
                else
                {
                    _log.Debug("ThirdPoint");
                    this.angleThirdPoint = e.GetPosition(this.canvas);
                    this.isDrawing = false;
                    this.isCanvasClicked = false;

                    double distance1 = CalculateDistance(this.angleFirstPoint, this.angleSecondPoint);
                    double distance2 = CalculateDistance(this.angleThirdPoint, this.angleSecondPoint);

                    double distance = distance1 < distance2 ? distance1 : distance2;

                    if (distance < 10)
                    {
                        distance *= 0.9;
                    }
                    else
                    {
                        distance = 10;
                    }

                    this.arcPoint1 = getArcPoint(this.angleSecondPoint, this.angleFirstPoint, distance);
                    this.arcPoint2 = getArcPoint(this.angleSecondPoint, this.angleThirdPoint, distance);

                    this.angle = getAngle(this.angleFirstPoint, this.angleSecondPoint, this.angleThirdPoint);

                    DrawLine(this.angleFirstPoint, this.angleSecondPoint, true, this.angleGeometries.Count);
                    DrawLine(this.angleSecondPoint, this.angleThirdPoint, false, this.angleGeometries.Count);

                    DrawArc(this.arcPoint1, this.arcPoint2, this.angleSecondPoint, this.angleGeometries.Count, this.angle);

                    DeleteEllipse(this.angleGeometries.Count, 1);
                    DeleteEllipse(this.angleGeometries.Count, 2);

                    DrawEllipse(this.angleFirstPoint, this.angleGeometries.Count, 1);
                    DrawEllipse(this.angleSecondPoint, this.angleGeometries.Count, 2);
                    DrawEllipse(this.angleThirdPoint, this.angleGeometries.Count, 3);
                    DeleteLabel(constAngle, this.angleGeometries.Count);
                    DrawLabel(this.angleSecondPoint, this.angleGeometries.Count, this.angle);

                    DeleteLine(false, this.angleGeometries.Count);
                    DeleteLine(true, this.angleGeometries.Count);


                    AngleGeometry angleGeometry = new AngleGeometry();
                    angleGeometry.AngleFirstPoint = this.angleFirstPoint;
                    angleGeometry.AngleSecondPoint = this.angleSecondPoint;
                    angleGeometry.AngleThirdPoint = this.angleThirdPoint;
                    angleGeometry.AngleGroup = this.angleGeometries.Count;

                    this.angleGeometries.Add(angleGeometry);

                    InCommand = Constants.MeasureDisableAngle;
                    CommandOff = true;
                    
                    //Canvas 마우스 이벤트 비활성화
                    this.canvas.MouseLeftButtonDown -= angle_canvas_MouseLeftButtonDown;
                    this.canvas.MouseMove -= angle_canvas_MouseMove;
                    this.canvas.MouseLeave -= angle_canvas_MouseLeave;

                    MouseCursor = 0;
                }
            }

        }

        public static Point getArcPoint(Point firstPoint, Point secondPoint, double distance)
        {
            // 벡터 계산
            double vectorX = secondPoint.X - firstPoint.X;
            double vectorY = secondPoint.Y - firstPoint.Y;

            // 길이 계산
            double vectorLength = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);

            // 방향 벡터 계산
            double vectorDirectionX = vectorX / vectorLength;
            double vectorDirectionY = vectorY / vectorLength;

            // 결과 좌표 계산
            double resultX = firstPoint.X + vectorDirectionX * distance;
            double resultY = firstPoint.Y + vectorDirectionY * distance;

            return new Point(resultX, resultY);
        }


        private void angle_canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isDrawing && this.isCanvasClicked)
            {
                Point point = e.GetPosition(this.canvas);

                if (lastX == point.X && lastY == point.Y)
                    return;

                lastX = point.X;
                lastY = point.Y;

                this.angleMovingPoint = e.GetPosition(this.canvas);


                if (this.isAngleSecondPoint == true)
                {
                    DeleteLine(true, this.angleGeometries.Count);
                    DrawLine(this.angleFirstPoint, this.angleMovingPoint, true, this.angleGeometries.Count);
                }
                else
                {
                    DeleteLine(false, this.angleGeometries.Count);
                    DrawLine(this.angleSecondPoint, this.angleMovingPoint, false, this.angleGeometries.Count);
                    this.angle = getAngle(this.angleFirstPoint, this.angleSecondPoint, this.angleMovingPoint);
                    DeleteLabel(constAngle, this.angleGeometries.Count);
                    DrawLabel(this.angleSecondPoint, this.angleGeometries.Count, this.angle);

                }

            }
        }

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("ellipse_MouseLeftButtonDown");

            if (this.isDrawing)
                return;

            if (this.isErasing)
            {
                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DeleteAngleAll();

                for (int i = group; i < this.angleGeometries.Count; i++)
                {
                    this.angleGeometries[i].AngleGroup--;
                }
                this.angleGeometries.RemoveAt(group);

                DrawAngleAll();

                return;
            }

            this.isEllipseClicked = true;
            Mouse.Capture((FrameworkElement)sender);
        }

        private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("ellipse_MouseLeftButtonUp");

            if (this.isEllipseClicked)
            {
                this.isEllipseClicked = false;

                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DeleteEllipse(group, 1);
                DrawEllipse(this.angleGeometries[group].AngleFirstPoint, group, 1);

                DeleteEllipse(group, 2);
                DrawEllipse(this.angleGeometries[group].AngleSecondPoint, group, 2);

                DeleteEllipse(group, 3);
                DrawEllipse(this.angleGeometries[group].AngleThirdPoint, group, 3);

                Mouse.Capture(null);
            }
        }

        private void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isEllipseClicked)
            {
                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);
                int index = int.Parse(tempArr[2]);
                AngleGeometry angleGeometry = this.angleGeometries[group];


                Point point = e.GetPosition(this.canvas);

                if (index == 1)
                {
                    if (point.X > this.canvas.ActualWidth - ellipse.Width / 2 || point.X < ellipse.Width / 2)
                        point.X = angleGeometry.AngleFirstPoint.X;

                    if (point.Y > this.canvas.ActualHeight - ellipse.Height / 2 || point.Y < ellipse.Height / 2)
                        point.Y = angleGeometry.AngleFirstPoint.Y;

                    angleGeometry.AngleFirstPoint = point;
                }
                else if (index == 2)
                {
                    if (point.X > this.canvas.ActualWidth - ellipse.Width / 2 || point.X < ellipse.Width / 2)
                        point.X = angleGeometry.AngleSecondPoint.X;

                    if (point.Y > this.canvas.ActualHeight - ellipse.Height / 2 || point.Y < ellipse.Height / 2)
                        point.Y = angleGeometry.AngleSecondPoint.Y;

                    angleGeometry.AngleSecondPoint = point;
                }
                else if (index == 3)
                {
                    if (point.X > this.canvas.ActualWidth - ellipse.Width / 2 || point.X < ellipse.Width / 2)
                        point.X = angleGeometry.AngleThirdPoint.X;

                    if (point.Y > this.canvas.ActualHeight - ellipse.Height / 2 || point.Y < ellipse.Height / 2)
                        point.Y = angleGeometry.AngleThirdPoint.Y;

                    angleGeometry.AngleThirdPoint = point;
                }

                //to-do
                Canvas.SetLeft(ellipse, point.X - (ellipseWidth / scaleFactor) / 2);
                Canvas.SetTop(ellipse, point.Y - (ellipseHeight / scaleFactor) / 2);

                double distance1 = CalculateDistance(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint);
                double distance2 = CalculateDistance(angleGeometry.AngleThirdPoint, angleGeometry.AngleSecondPoint);

                double distance = distance1 < distance2 ? distance1 : distance2;

                if (distance < 10)
                {
                    distance *= 0.9;
                }
                else
                {
                    distance = 10;
                }

                angleGeometry.ArcPoint1 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint, distance);
                angleGeometry.ArcPoint2 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, distance);

                angleGeometry.Angle = getAngle(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);

                DeleteLine(true, group);
                DeleteLine(false, group);
                DeleteArc(group);
                DeleteLabel(constAngle, group);
                DrawLine(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, true, group);
                DrawLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, false, group);
                DrawArc(angleGeometry.ArcPoint1, angleGeometry.ArcPoint2, angleGeometry.AngleSecondPoint, group,
                angleGeometry.Angle);
                DrawLabel(angleGeometry.AngleSecondPoint, group, angleGeometry.Angle);
            }
        }

        private double getAngle(Point angleFirstPoint, Point angleSecondPoint, Point angleThirdPoint)
        {
            Vector vector1 = angleFirstPoint - angleSecondPoint;
            Vector vector2 = angleThirdPoint - angleSecondPoint;


            // 벡터의 외적 계산 -> 각도의 부호 판단
            double crossProduct = vector1.X * vector2.Y - vector1.Y * vector2.X;
            double dotProduct = vector1.X * vector2.X + vector1.Y * vector2.Y;

            double angleInRadians = Math.Atan2(crossProduct, dotProduct);
            double angleInDegrees = angleInRadians * (180 / Math.PI);

            // 각도가 음수일 경우 양수로 변환
            if (angleInDegrees < 0)
            {
                angleInDegrees += 360;
            }

            return angleInDegrees;
        }

        private void Ellipse_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("ellipse_MouseRightButtonDown");

            Ellipse ellipse = sender as Ellipse;
            string[] tempArr = ellipse.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            DeleteAngleAll();

            for (int i = group + 1; i < this.angleGeometries.Count; i++)
            {
                this.angleGeometries[i].AngleGroup--;
            }
            this.angleGeometries.RemoveAt(group);

            DrawAngleAll();

        }

        private void angle_MouseOver(object sender, MouseEventArgs e)
        {
            if (!isMove) return;

            if (angleGeometries.Count > 0)
            {

                Path path = sender as Path;
                if (path != null)
                {
                    Point point = e.GetPosition(this.canvas);
                    string[] tempArr = path.Name.Split('_');
                    int group = int.Parse(tempArr[1]);

                    MouseCursor = 4;

                    string targetName = "";
                    string arcName = constAngleArc + "_" + group;

                    if (bool.Parse(tempArr[2]))
                    {
                        targetName = constAngle + "_" + group + "_" + false;
                    }
                    else
                    {
                        targetName = constAngle + "_" + group + "_" + true;
                    }

                    Path foundPath = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == targetName);
                    Path foundArc = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == arcName);

                    if (foundPath == null || foundArc == null) return;
                    if (Zoom.ScaleX == 1)
                    {
                        path.StrokeThickness = 2;
                        foundPath.StrokeThickness = 2;
                        foundArc.StrokeThickness = 2;
                    }
                    else
                    {
                        path.StrokeThickness = 2 / Zoom.ScaleX;
                        foundPath.StrokeThickness = 2 / Zoom.ScaleX;
                        foundArc.StrokeThickness = 2 / Zoom.ScaleX;
                    }
                }
            }

        }

        private void angle_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!isMove) return;

            if (angleGeometries.Count > 0)
            {
                Path path = sender as Path;
                if (path != null)
                {
                    Point point = e.GetPosition(this.canvas);
                    string[] tempArr = path.Name.Split('_');
                    int group = int.Parse(tempArr[1]);

                    MouseCursor = 0;

                    string targetName = "";
                    string arcName = constAngleArc + "_" + group;

                    if (bool.Parse(tempArr[2]))
                    {
                        targetName = constAngle + "_" + group + "_" + false;
                    }
                    else
                    {
                        targetName = constAngle + "_" + group + "_" + true;
                    }

                    Path foundPath = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == targetName);
                    Path foundArc = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == arcName);

                    if (foundPath == null || foundArc == null) return;
                    if (Zoom.ScaleX == 1)
                    {
                        path.StrokeThickness = 1;
                        foundPath.StrokeThickness = 1;
                        foundArc.StrokeThickness = 1;
                    }
                    else
                    {
                        path.StrokeThickness = 1 / Zoom.ScaleX;
                        foundPath.StrokeThickness = 1 / Zoom.ScaleX;
                        foundArc.StrokeThickness = 1 / Zoom.ScaleX;
                    }
                }

            }
        }



        private Point[] diffPoint = new Point[5];

        private void angle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isMove) return;

            _log.Debug("angle_MouseLeftButtonDown");

            if (angleGeometries.Count > 0)
            {
                Path path = sender as Path;
                Point point = e.GetPosition(this.canvas);
                string[] tempArr = path.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                if (group >= angleGeometries.Count)
                    return;

                this.isAngleClicked = true;

                AngleGeometry angleGeometry = this.angleGeometries[group];
                diffPoint[0] = (Point)(point - angleGeometry.AngleFirstPoint);
                diffPoint[1] = (Point)(point - angleGeometry.AngleSecondPoint);
                diffPoint[2] = (Point)(point - angleGeometry.AngleThirdPoint);

                double distance1 = CalculateDistance(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint);
                double distance2 = CalculateDistance(angleGeometry.AngleThirdPoint, angleGeometry.AngleSecondPoint);

                double distance = distance1 < distance2 ? distance1 : distance2;

                if (distance < 10)
                {
                    distance *= 0.9;
                }
                else
                {
                    distance = 10;
                }

                diffPoint[3] = getArcPoint(diffPoint[1], diffPoint[0], distance);
                diffPoint[4] = getArcPoint(diffPoint[1], diffPoint[2], distance);


                Mouse.Capture((FrameworkElement)sender);
            }

        }

        private void angle_MouseMove(object sender, MouseEventArgs e)
        {

            if (this.isAngleClicked)
            {

                Path path = sender as Path;
                Point point = e.GetPosition(this.canvas);

                Geometry pathGeometry = path.Data;

                string[] tempArr = path.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                AngleGeometry angleGeometry = this.angleGeometries[group];

                Point[] prePoint = new Point[5];
                prePoint[0] = angleGeometry.AngleFirstPoint;
                prePoint[1] = angleGeometry.AngleSecondPoint;
                prePoint[2] = angleGeometry.AngleThirdPoint;
                prePoint[3] = angleGeometry.ArcPoint1;
                prePoint[4] = angleGeometry.ArcPoint2;

                double preAngle = angleGeometry.Angle;

                angleGeometry.AngleFirstPoint = (Point)(point - diffPoint[0]);
                angleGeometry.AngleSecondPoint = (Point)(point - diffPoint[1]);
                angleGeometry.AngleThirdPoint = (Point)(point - diffPoint[2]);


                double distance1 = CalculateDistance(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint);
                double distance2 = CalculateDistance(angleGeometry.AngleThirdPoint, angleGeometry.AngleSecondPoint);

                double distance = distance1 < distance2 ? distance1 : distance2;

                if (distance < 10)
                {
                    distance *= 0.9;
                }
                else
                {
                    distance = 10;
                }

                angleGeometry.ArcPoint1 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint, distance);
                angleGeometry.ArcPoint2 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, distance);

                angleGeometry.Angle = getAngle(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);

                Path foundPath = new Path();


                if (bool.Parse(tempArr[2]))
                {
                    string targetName = constAngle + "_" + group + "_" + false;
                    foundPath = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == targetName);

                    foundPath.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint);
                    path.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);


                }
                else
                {
                    string targetName = constAngle + "_" + group + "_" + true;
                    foundPath = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == targetName);

                    foundPath.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);
                    path.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint);
                }

                if (angleInCanvas(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint) == 1)
                { //x,y 모두 바깥으로 나갈 때
                    angleGeometry.AngleFirstPoint = prePoint[0];
                    angleGeometry.AngleSecondPoint = prePoint[1];
                    angleGeometry.AngleThirdPoint = prePoint[2];
                    angleGeometry.ArcPoint1 = prePoint[3];
                    angleGeometry.ArcPoint2 = prePoint[4];
                    angleGeometry.Angle = preAngle;

                }
                else if (angleInCanvas(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint) == 2)
                {
                    angleGeometry.AngleFirstPoint = new Point(prePoint[0].X, (point.Y - diffPoint[0].Y));
                    angleGeometry.AngleSecondPoint = new Point(prePoint[1].X, (point.Y - diffPoint[1].Y));
                    angleGeometry.AngleThirdPoint = new Point(prePoint[2].X, (point.Y - diffPoint[2].Y));
                    //angleGeometry.ArcPoint1 = new Point(prePoint[3].X, diffPoint[3].Y);
                    //angleGeometry.ArcPoint2 = new Point(prePoint[4].X, diffPoint[4].Y);
                    angleGeometry.ArcPoint1 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint, distance);
                    angleGeometry.ArcPoint2 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, distance);
                    angleGeometry.Angle = preAngle;

                }
                else if (angleInCanvas(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint) == 3)
                {
                    angleGeometry.AngleFirstPoint = new Point((point.X - diffPoint[0].X), prePoint[0].Y);
                    angleGeometry.AngleSecondPoint = new Point((point.X - diffPoint[1].X), prePoint[1].Y);
                    angleGeometry.AngleThirdPoint = new Point((point.X - diffPoint[2].X), prePoint[2].Y);
                    //angleGeometry.ArcPoint1 = new Point(diffPoint[3].X, prePoint[3].Y);
                    //angleGeometry.ArcPoint2 = new Point(diffPoint[4].X, prePoint[4].Y);
                    angleGeometry.ArcPoint1 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint, distance);
                    angleGeometry.ArcPoint2 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, distance);
                    angleGeometry.Angle = preAngle;
                }

                // length 랑 text 도 바꿔놓기
                if (bool.Parse(tempArr[2]))
                {
                    string targetName = constAngle + "_" + group + "_" + false;
                    foundPath = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == targetName);

                    foundPath.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint);
                    path.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);


                }
                else
                {
                    string targetName = constAngle + "_" + group + "_" + true;
                    foundPath = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == targetName);

                    foundPath.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);
                    path.Data = getLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint);
                }

                DeleteArc(group);
                DrawArc(angleGeometry.ArcPoint1, angleGeometry.ArcPoint2, angleGeometry.AngleSecondPoint, group,
                angleGeometry.Angle);

                string arcName = constAngleArc + "_" + group;
                Path foundArc = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == arcName);
                foundArc = canvas.Children.OfType<Path>().FirstOrDefault(path => path.Name == arcName);

                if (Zoom.ScaleX == 1)
                {
                    foundArc.StrokeThickness = 2;
                }
                else
                {
                    foundArc.StrokeThickness = 2 / Zoom.ScaleX;
                }


                DeleteLabel(constAngle, group);
                DrawLabel(angleGeometry.AngleSecondPoint, group, angleGeometry.Angle);

                DeleteEllipse(group, 1);
                DrawEllipse(angleGeometry.AngleFirstPoint, group, 1);

                DeleteEllipse(group, 2);
                DrawEllipse(angleGeometry.AngleSecondPoint, group, 2);

                DeleteEllipse(group, 3);
                DrawEllipse(angleGeometry.AngleThirdPoint, group, 3);


            }

        }

        private int angleInCanvas(Point first, Point second, Point third)
        {
            double minX = Math.Min(Math.Min(first.X, third.X), second.X);
            double minY = Math.Min(Math.Min(first.Y, third.Y), second.Y);
            double maxX = Math.Max(Math.Max(first.X, third.X), second.X);
            double maxY = Math.Max(Math.Max(first.Y, third.Y), second.Y);

            if ((minX < 0 || maxX > canvas.ActualWidth) && (minY < 0 || maxY > canvas.ActualHeight))
            {
                return 1;
            }
            else if (minX < 0 || maxX > canvas.ActualWidth)
            {
                return 2;
            }
            else if (minY < 0 || maxY > canvas.ActualHeight)
            {
                return 3;
            }
            else
            {
                return 4;
            }

        }

        private void angle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("angle_MouseLeftButtonUp");

            if (this.isAngleClicked)
            {
                this.isAngleClicked = false;

                Mouse.Capture(null);
            }

        }



        private void DrawAngleAll()
        {
            _log.Debug("DrawAngleAll");

            foreach (var angleGeometry in this.angleGeometries)
            {
                angleGeometry.Angle = getAngle(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint);
                angleGeometry.ArcPoint1 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleFirstPoint, 10);
                angleGeometry.ArcPoint2 = getArcPoint(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, 10);

                DrawLine(angleGeometry.AngleFirstPoint, angleGeometry.AngleSecondPoint, true, angleGeometry.AngleGroup);
                DrawLine(angleGeometry.AngleSecondPoint, angleGeometry.AngleThirdPoint, false, angleGeometry.AngleGroup);          

                DrawLabel(angleGeometry.AngleSecondPoint, angleGeometry.AngleGroup, angleGeometry.Angle);
                DrawEllipse(angleGeometry.AngleFirstPoint, angleGeometry.AngleGroup, 1);
                DrawEllipse(angleGeometry.AngleSecondPoint, angleGeometry.AngleGroup, 2);
                DrawEllipse(angleGeometry.AngleThirdPoint, angleGeometry.AngleGroup, 3);
                DrawArc(angleGeometry.ArcPoint1, angleGeometry.ArcPoint2, angleGeometry.AngleSecondPoint, angleGeometry.AngleGroup, angleGeometry.Angle);
            }
        }
        private void DeleteAngleAll()
        {
            _log.Debug("DeleteAngleAll");

            foreach (var angleGeometry in this.angleGeometries)
            {
                DeleteEllipse(angleGeometry.AngleGroup, 1);
                DeleteEllipse(angleGeometry.AngleGroup, 2);
                DeleteEllipse(angleGeometry.AngleGroup, 3);
                DeleteArc(angleGeometry.AngleGroup);
                DeleteLine(true, angleGeometry.AngleGroup);
                DeleteLine(false, angleGeometry.AngleGroup);
                DeleteLabel(constAngle, angleGeometry.AngleGroup);
            }
        }

        private void DrawEllipse(Point point, int group, int index)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Name = constEllipse + "_" + group + "_" + index;
            ellipse.Style = (Style)this.Resources["StyleEllipse"];
            ellipse.Stroke = brushes[group % brushes.Length];
            Canvas.SetLeft(ellipse, point.X);
            Canvas.SetTop(ellipse, point.Y);


            ellipse.MouseLeftButtonDown += Ellipse_MouseLeftButtonDown;
            ellipse.MouseLeftButtonUp += Ellipse_MouseLeftButtonUp;
            ellipse.MouseMove += Ellipse_MouseMove;
            ellipse.MouseRightButtonDown += Ellipse_MouseRightButtonDown;

            this.canvas.Children.Add(ellipse);
        }

        private void DeleteEllipse(int group, int index)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Ellipse))
                {
                    Ellipse temp = (Ellipse)item;

                    if (temp.Name == constEllipse + "_" + group + "_" + index)
                    {
                        this.canvas.Children.Remove((Ellipse)item);
                        break;
                    }
                }
            }
        }

        private void DrawLine(Point firstPoint, Point secondPoint, bool isAngleSecond, int group)
        {
            Path path = new Path();
            path.Style = (Style)this.Resources["StylePath"];
            path.Data = getLine(firstPoint, secondPoint);
            path.Stroke = brushes[group % brushes.Length];
            path.Name = constAngle + "_" + group + "_" + isAngleSecond;

            if (this.isDrawing)
            {
                path.StrokeDashArray.Add(2);
            }

            path.MouseLeftButtonDown += angle_MouseLeftButtonDown;
            path.MouseMove += angle_MouseMove;
            path.MouseLeftButtonUp += angle_MouseLeftButtonUp;
            path.MouseEnter += angle_MouseOver;
            path.MouseLeave += angle_MouseLeave;

            this.canvas.Children.Add(path);
        }


        private PathGeometry? getLine(Point firstPoint, Point secondPoint)
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

        private void DeleteLine(bool isAngleSecond, int group)
        {
            ;
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constAngle + "_" + group + "_" + isAngleSecond)
                    {
                        this.canvas.Children.Remove((Path)item);
                        break;
                    }
                }
            }
        }
        private void DrawLabel(Point centerPoint, int group, double angle)
        {
            Label label = new Label();
            label.Style = (Style)this.Resources["StyleLabel"];
            label.Name = constAngle + "_" + group;
            label.Content = Math.Round(angle, 2).ToString() + "°";

            int plusX = 5;
            int plusY = 10;
                        
            Canvas.SetLeft(label, centerPoint.X + plusX);
            Canvas.SetTop(label, centerPoint.Y - plusY);
            this.canvas.Children.Add(label);
        }

        private void DrawArc(Point startPoint, Point endPoint, Point centerPoint, int group, double angle)
        {

            // 반지름 계산
            double radius = CalculateDistance(startPoint, centerPoint);

            // 호 그리기
            Path arcPath = new Path();
            arcPath.Style = (Style)this.Resources["StylePath"];
            arcPath.Stroke = brushes[group % brushes.Length];
            arcPath.Name = constAngleArc + "_" + group;

            PathGeometry pathGeometry = new PathGeometry();
            ArcSegment arcSegment = new ArcSegment(new Point(endPoint.X, endPoint.Y), new Size(radius, radius), angle, angle >= 180, SweepDirection.Clockwise, true);
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(startPoint.X, startPoint.Y);
            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);
            arcPath.Data = pathGeometry;

            canvas.Children.Add(arcPath);

        }

        public static double CalculateDistance(Point start, Point end)
        {
            double distanceX = end.X - start.X;
            double distanceY = end.Y - start.Y;
            double distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
            return distance;
        }


        private void DeleteArc(int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constAngleArc + "_" + group)
                    {
                        this.canvas.Children.Remove((Path)item);
                        break;
                    }
                }
            }
        }


    }
}