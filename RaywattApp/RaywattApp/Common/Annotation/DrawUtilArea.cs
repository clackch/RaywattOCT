using RaywattApp.Common.Annotation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace RaywattApp.Common.Annotation
{
    public partial class DrawUtil
    {
        private const string constRectangle = "Rectangle";

        private const string constCurve = "Curve";

        private const string constArea = "Area";

        private List<AreaGeometry> areaGeometrys;

        private List<Point> pointList;

        private bool groupFirst;

        private double area;

        private bool isRectClicked;

        private double lastX;

        private double lastY;

        private int movingPointIndex;

        private PathGeometry overlayPathGeometry;

        private void AreaInit()
        {
            this.isRectClicked = false;
            this.movingPointIndex = -1;
        }

        //---------------------------------------------------------------------------------------------------- Event
        private void area_Add(object sender, RoutedEventArgs e)
        {
            _log.Debug("area_Add");

            if (!this.isDrawing)
            {
                this.pointList = new List<Point>();
                this.isDrawing = true;
                this.groupFirst = true;

                //Canvas 마우스 이벤트 활성화
                this.canvas.MouseLeftButtonDown += area_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove += area_canvas_MouseMove;
                this.canvas.MouseLeave += area_canvas_MouseLeave;

                MouseCursor = 1;
            }
        }

        private void area_canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _log.Debug("area_canvas_MouseLeave");

            if (this.isDrawing)
            {
                //this.pointList = null;
                this.isDrawing = false;
                this.groupFirst = false;
                this.isCanvasClicked = false;

                DeleteCurve(this.areaGeometrys.Count);
                DeleteRectagle(this.areaGeometrys.Count);

                DeleteAreaAll();
                DrawAreaAll();

                //Canvas 마우스 이벤트 비활성화
                this.canvas.MouseLeftButtonDown -= area_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove -= area_canvas_MouseMove;
                this.canvas.MouseLeave -= area_canvas_MouseLeave;

                MouseCursor = 0;
            }
        }

        private void area_canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("area_canvas_MouseLeftButtonDown");

            if (this.isDrawing)
            {
                this.pointList.Add(e.GetPosition(this.canvas));
                if (this.groupFirst)
                {
                    this.groupFirst = false;
                    Point point = e.GetPosition(this.canvas);
                    this.pointList.Add(point);
                }
                this.isCanvasClicked = true;
            }

            DrawRectangle(this.pointList, false, this.areaGeometrys.Count);
        }

        private void area_canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isDrawing && this.isCanvasClicked)
            {
                Point point = e.GetPosition(this.canvas);

                if (this.lastX == point.X && this.lastY == point.Y)
                    return;

                this.lastX = point.X;
                this.lastY = point.Y;

                if (this.pointList.Count > 1)
                {
                    this.pointList[pointList.Count - 1] = e.GetPosition(this.canvas);
                }

                DrawCurve(this.pointList, false, this.areaGeometrys.Count);
                DrawRectangle(this.pointList, false, this.areaGeometrys.Count);
            }
        }

        private void rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("rectangle_MouseLeftButtonDown");

            if (this.isDrawing)
                return;

            this.isRectClicked = true;
            Mouse.Capture((FrameworkElement)sender);
        }

        private void rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("rectangle_MouseLeftButtonUp");

            if (this.isDrawing)
                return;

            if (this.isRectClicked)
            {
                this.isRectClicked = false;

                DrawAreaAll();

                Mouse.Capture(null);
            }
        }

        private void rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isDrawing)
                return;

            if (this.isRectClicked)
            {
                Rectangle rectangle = sender as Rectangle;
                Point point = e.GetPosition(this.canvas);

                string[] tempArr = rectangle.Name.Split('_');
                int group = int.Parse(tempArr[1]);
                int index = int.Parse(tempArr[2]);
                movingPointIndex = index;
                AreaGeometry areaGeometry = this.areaGeometrys[group];
                Point rectPoint = areaGeometry.Points[index];

                if (point.X > this.canvas.ActualWidth - rectangle.Width / 2 || point.X < rectangle.Width / 2)
                    point.X = rectPoint.X;

                if (point.Y > this.canvas.ActualHeight - rectangle.Height / 2 || point.Y < rectangle.Height / 2)
                    point.Y = rectPoint.Y;

                Canvas.SetLeft(rectangle, point.X - rectangle.Width / 2);
                Canvas.SetTop(rectangle, point.Y - rectangle.Height / 2);


                rectPoint.X = point.X;
                rectPoint.Y = point.Y;
                areaGeometry.Points[index] = rectPoint;

                DrawCurve(areaGeometry.Points, areaGeometry.IsClosed, areaGeometry.Group);

                //Label 삭제
                DeleteLabel(constArea, group);

                //Label 추가
                DrawLabel(this.areaGeometrys[group].Points, group);
            }
        }

        private void rectangle_Connect(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("rectangle_Connect");

            if (this.pointList.Count < 4)
                return;

            this.isDrawing = false;
            this.isCanvasClicked = false;
            this.pointList.RemoveAt(this.pointList.Count - 1);

            DrawCurve(this.pointList, true, this.areaGeometrys.Count);
            DrawRectangle(this.pointList, true, this.areaGeometrys.Count);

            AreaGeometry areaGeometry = new AreaGeometry();
            areaGeometry.Points = this.pointList;
            areaGeometry.Group = this.areaGeometrys.Count;
            areaGeometry.Area = this.area;
            areaGeometry.IsClosed = true;
            this.areaGeometrys.Add(areaGeometry);

            //Canvas 마우스 이벤트 비활성화
            this.canvas.MouseLeftButtonDown -= area_canvas_MouseLeftButtonDown;
            this.canvas.MouseMove -= area_canvas_MouseMove;
            this.canvas.MouseLeave -= area_canvas_MouseLeave;

            MouseCursor = 0;
        }

        private void rectangle_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("rectangle_MouseRightButtonDown");

            if (this.isDrawing)
                return;

            Rectangle rectangle = sender as Rectangle;
            string[] tempArr = rectangle.Name.Split('_');
            int group = int.Parse(tempArr[1]);
            int index = int.Parse(tempArr[2]);

            //Point 삭제
            if(this.areaGeometrys[group].Points.Count > 3)
            {
                this.areaGeometrys[group].Points.RemoveAt(index);

                DrawCurve(this.areaGeometrys[group].Points, this.areaGeometrys[group].IsClosed, group);

                DrawRectangle(this.areaGeometrys[group].Points, this.areaGeometrys[group].IsClosed, group);

                //Label 삭제
                DeleteLabel(constArea, group);

                //Label 추가
                DrawLabel(this.areaGeometrys[group].Points, group);
            }
            else//Area 삭제(Point 3개 이하 일 경우)
            {
                DeleteAreaAll();

                for (int i = group + 1; i < this.areaGeometrys.Count; i++)
                {
                    this.areaGeometrys[i].Group--;
                }
                this.areaGeometrys.RemoveAt(group);

                DrawAreaAll();
            }
        }

        private void path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("path_MouseLeftButtonDown");

            Path path = sender as Path;
            string[] tempArr = path.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            Point point = e.GetPosition(this.canvas);

            PathGeometry pathGeometry = (PathGeometry)path.Data;
            PathFigureCollection pathFigures = pathGeometry.Figures;
            PathFigure pathFigure = pathFigures[0];

            int index = 0;

            foreach (BezierSegment segment in pathFigure.Segments)
            {
                var myPathFigure = new PathFigure { StartPoint = this.areaGeometrys[group].Points[index++] };
                var myPathSegmentCollection = new PathSegmentCollection();
                myPathSegmentCollection.Add(segment);
                myPathFigure.Segments = myPathSegmentCollection;
                var myPathFigureCollection = new PathFigureCollection { myPathFigure };
                var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };
                Path myPath = new Path();
                myPath.Data = myPathGeometry;
                bool isFind = myPath.Data.FillContains(point, 1, ToleranceType.Absolute);

                if (isFind)
                {
                    this.areaGeometrys[group].Points.Insert(index, point);

                    DeleteCurve(group);
                    DrawCurve(this.areaGeometrys[group].Points, this.areaGeometrys[group].IsClosed, group);
                    DrawRectangle(this.areaGeometrys[group].Points, this.areaGeometrys[group].IsClosed, group);

                    break;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void DrawAreaAll()
        {
            _log.Debug("DrawAreaAll");

            foreach (var areaGeometry in this.areaGeometrys)
            {
                DrawCurve(areaGeometry.Points, areaGeometry.IsClosed, areaGeometry.Group);
                DrawRectangle(areaGeometry.Points, areaGeometry.IsClosed, areaGeometry.Group);
            }
        }

        private void DrawCurve(List<Point> pointList, bool isClosed, int group)
        {
            DeleteCurve(group);

            if (pointList.Count > 1)
            {
                Path path = new Path();
                path.Style = (Style)this.Resources["StylePath"];
                path.Data = SetPathData(pointList, isClosed);
                path.Stroke = brushes[group % brushes.Length];
                path.Name = constCurve + "_" + group;

                if (this.isDrawing)
                {
                    path.StrokeDashArray.Add(2);
                }

                if (isClosed)
                {
                    path.Style = (Style)this.Resources["StylePathCurve"];
                    this.area = path.Data.GetArea();

                    foreach (var areaGeometry in this.areaGeometrys)
                    {
                        if (areaGeometry.Group == group)
                        {
                            areaGeometry.Area = this.area;
                            break;
                        }
                    }

                    path.MouseLeftButtonDown += path_MouseLeftButtonDown;
                }

                this.canvas.Children.Add(path);
            }
        }

        private void DrawRectangle(List<Point> pointList, bool isClosed, int group)
        {
            DeleteRectagle(group);

            int cnt = isClosed ? pointList.Count : pointList.Count - 1;

            for (int i = cnt - 1; i >= 0; i--)
            {
                Rectangle rectangle = new Rectangle();
                rectangle.Style = (Style)this.Resources["StyleRectangle"];
                rectangle.Stroke = brushes[group % brushes.Length];
                rectangle.Name = constRectangle + "_" + group + "_" + i;
                Canvas.SetLeft(rectangle, pointList[i].X - rectangle.Width / 2);
                Canvas.SetTop(rectangle, pointList[i].Y - rectangle.Height / 2);

                if (i == 0 && !isClosed)
                {
                    rectangle.Style = (Style)this.Resources["StyleRectangleFirst"];
                    rectangle.Stroke = brushes[group % brushes.Length];
                    rectangle.Fill = brushes[group % brushes.Length];
                    rectangle.MouseLeftButtonDown += rectangle_Connect;
                }
                else if (isClosed)
                {
                    rectangle.Style = (Style)this.Resources["StyleRectangleClosed"];

                    if (i == cnt - 1)
                    {
                        DeleteLabel(constArea, group);
                        DrawLabel(pointList, group);
                    }
                    rectangle.MouseLeftButtonDown += rectangle_MouseLeftButtonDown;
                    rectangle.MouseLeftButtonUp += rectangle_MouseLeftButtonUp;
                    rectangle.MouseMove += rectangle_MouseMove;
                    rectangle.MouseRightButtonDown += rectangle_MouseRightButtonDown;
                }
                this.canvas.Children.Add(rectangle);
            }
        }

        private void DrawLabel(List<Point> pointList, int group)
        {
            Label label = new Label();
            label.Style = (Style)this.Resources["StyleLabel"];
            label.Name = constArea + "_" + group;
            label.Content = "[" + (group + 1) + "] " + (Math.Round(this.area, 3)).ToString();

            Point centerdPoint = GetCenterPoint(pointList);
            
            if (IsOverlayed(pointList, group, centerdPoint))
                return;
            
            Canvas.SetLeft(label, centerdPoint.X - 40);
            Canvas.SetTop(label, centerdPoint.Y - 10);
            this.canvas.Children.Add(label);
        }

        private void DeleteAreaAll()
        {
            foreach (var areaGeometry in this.areaGeometrys)
            {
                DeleteCurve(areaGeometry.Group);
                DeleteRectagle(areaGeometry.Group);
                DeleteLabel(constArea, areaGeometry.Group);
            }
        }

        private void DeleteCurve(int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constCurve + "_" + group)
                    {
                        this.canvas.Children.Remove((Path)item);
                        break;
                    }
                }
            }
        }

        private void DeleteRectagle(int group)
        {
            List<int> rectArr = new List<int>();

            for (int i = 0; i < this.canvas.Children.Count; i++)
            {
                if (this.canvas.Children[i].GetType() == typeof(Rectangle))
                {
                    Rectangle temp = (Rectangle)this.canvas.Children[i];

                    if (temp.Name.StartsWith(constRectangle + "_" + group))
                    {
                        if (temp.Name == constRectangle + "_" + group + "_0")
                            temp.MouseLeftButtonDown -= rectangle_Connect;

                        temp.MouseLeftButtonDown -= rectangle_MouseLeftButtonDown;
                        temp.MouseLeftButtonUp -= rectangle_MouseLeftButtonUp;
                        temp.MouseMove -= rectangle_MouseMove;
                        temp.MouseRightButtonDown -= rectangle_MouseRightButtonDown;

                        rectArr.Add(i);
                    }
                }
            }
            for (int i = rectArr.Count - 1; i >= 0; i--)
            {
                this.canvas.Children.RemoveAt(rectArr[i]);
            }
        }

        private Point GetCenterPoint(List<Point> pointList)
        {
            double cx = 0, cy = 0, area = 0;

            for(int i = 0, j = pointList.Count - 1; i < pointList.Count; j = i++)
            {
                double tri_area = pointList[i].X * pointList[j].Y - pointList[i].Y * pointList[j].X;
                cx += (pointList[i].X + pointList[j].X) * tri_area;
                cy += (pointList[i].Y + pointList[j].Y) * tri_area;
                area += tri_area;
            }
            cx /= 3 * area;
            cy /= 3 * area;

            return new Point(cx, cy);
        }

        private bool IsOverlayed(List<Point> pointList, int group, Point point)
        {
            if (this.movingPointIndex == -1)
                return false;

            if(this.areaGeometrys.Count <= group)
                return false;

            PathFigureCollection pathFigures = this.overlayPathGeometry.Figures;
            PathFigure pathFigure = pathFigures[0];
            bool isFind = false;
            int segmentCnt = pathFigure.Segments.Count;

            var pathFigureFirst = new PathFigure { StartPoint = this.areaGeometrys[group].Points[this.movingPointIndex] };
            var pathSegmentCollectionFirst = new PathSegmentCollection();
            pathSegmentCollectionFirst.Add(pathFigure.Segments[this.movingPointIndex]);
            pathFigureFirst.Segments = pathSegmentCollectionFirst;
            var pathFigureCollectionFirst = new PathFigureCollection { pathFigureFirst };
            var pathGeometryFirst = new PathGeometry { Figures = pathFigureCollectionFirst };

            int movingPointIndexSecond = 0;
            if (movingPointIndex == 0)
                movingPointIndexSecond = pointList.Count - 1;
            else
                movingPointIndexSecond = this.movingPointIndex - 1;
            var pathFigureSecond = new PathFigure { StartPoint = this.areaGeometrys[group].Points[movingPointIndexSecond] };
            var pathSegmentCollectionSecond = new PathSegmentCollection();
            pathSegmentCollectionSecond.Add(pathFigure.Segments[movingPointIndexSecond]);
            pathFigureSecond.Segments = pathSegmentCollectionSecond;
            var pathFigureCollectionSecond = new PathFigureCollection { pathFigureSecond };
            var pathGeometrySecond = new PathGeometry { Figures = pathFigureCollectionSecond };

            for (int i = 0; i < segmentCnt; i++)
            {
                if (i == this.movingPointIndex || i == movingPointIndexSecond)
                    continue;

                var myPathFigure = new PathFigure { StartPoint = this.areaGeometrys[group].Points[i] };
                var myPathSegmentCollection = new PathSegmentCollection();
                myPathSegmentCollection.Add(pathFigure.Segments[i]);
                myPathFigure.Segments = myPathSegmentCollection;
                var myPathFigureCollection = new PathFigureCollection { myPathFigure };
                var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };

                isFind = myPathGeometry.FillContainsWithDetail(pathGeometryFirst) == IntersectionDetail.Intersects ? true : false;
                if (isFind)
                    break;

                isFind = myPathGeometry.FillContainsWithDetail(pathGeometrySecond) == IntersectionDetail.Intersects ? true : false;
                if (isFind)
                    break;
            }

            this.movingPointIndex = -1;
            return isFind;
        }

        //---------------------------------------------------------------------------------------------------- Function (Bezier Curve)
        private PathGeometry? SetPathData(List<Point> pointList, bool isClosed)
        {
            if (pointList == null)
                return null;

            var points = new List<Rulyotano.Math.Geometry.Point>();

            foreach (var point in pointList)
            {
                points.Add(new Rulyotano.Math.Geometry.Point(point.X, point.Y));
            }

            if (points.Count <= 1)
                return null;

            var myPathFigure = new PathFigure { StartPoint = ConvertToVisualPoint(points.FirstOrDefault()) };
            var myPathSegmentCollection = new PathSegmentCollection();
            var bezierSegments = Rulyotano.Math.Interpolation.Bezier.BezierInterpolation.PointsToBezierCurves(points, isClosed);

            if (bezierSegments == null || bezierSegments.Count < 1)
            {
                //Add a line segment <this is generic for more than one line>
                foreach (var point in points.GetRange(1, points.Count - 1))
                {
                    var myLineSegment = new LineSegment { Point = ConvertToVisualPoint(point) };
                    myPathSegmentCollection.Add(myLineSegment);
                }
            }
            else
            {
                foreach (var bezierCurveSegment in bezierSegments)
                {
                    var segment = new BezierSegment
                    {
                        Point1 = ConvertToVisualPoint(bezierCurveSegment.FirstControlPoint),
                        Point2 = ConvertToVisualPoint(bezierCurveSegment.SecondControlPoint),
                        Point3 = ConvertToVisualPoint(bezierCurveSegment.EndPoint)
                    };
                    myPathSegmentCollection.Add(segment);
                }
            }

            myPathFigure.Segments = myPathSegmentCollection;
            var myPathFigureCollection = new PathFigureCollection { myPathFigure };
            var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };

            this.overlayPathGeometry = myPathGeometry;

            return myPathGeometry;
        }

        private Point ConvertToVisualPoint(Rulyotano.Math.Geometry.Point p)
        {
            return new Point(p.X, p.Y);
        }
    }
}
