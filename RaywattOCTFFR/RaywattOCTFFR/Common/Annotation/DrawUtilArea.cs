using RaywattOCTFFR.Common.Annotation.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Path = System.Windows.Shapes.Path;
using OpenCvSharp;
using System.Collections.ObjectModel;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Common.Annotation.Util;
using RaywattOCTFFR.Common.Bases;

namespace RaywattOCTFFR.Common.Annotation
{
    public partial class DrawUtil
    {
        private const string constRectangle = "Rectangle";

        private const string constCurve = "Curve";

        private const string constArea = "Area";

        private const string constMinDiameter = "MinDiameter";

        private const string constMaxDiameter = "MaxDiameter";

        private ObservableCollection<AreaGeometry> areaGeometrys;

        private List<Point> pointList;

        private bool groupFirst;

        private bool isRectClicked;

        private double lastX;

        private double lastY;

        private int movingPointIndex;

        private PathGeometry overlayPathGeometry;

        private Mat imageContour = new Mat();

        private void AreaInit()
        {
            this.isRectClicked = false;
            this.movingPointIndex = -1;
        }

        //---------------------------------------------------------------------------------------------------- Event
        private void area_canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _log.Debug("area_canvas_MouseLeave");

            if (this.isDrawing && this.isCanvasClicked)
            {
                this.pointList.Clear();
                this.groupFirst = true;
                this.isCanvasClicked = false;

                DeleteCurve(this.areaGeometrys.Count);
                DeleteRectagle(this.areaGeometrys.Count);
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

        private void area_canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("area_canvas_MouseRightButtonDown");

            if (this.isDrawing)
            {
                if (this.pointList.Count > 2)
                {
                    this.pointList.RemoveAt(this.pointList.Count - 1);
                    this.pointList.RemoveAt(this.pointList.Count - 1);

                    Point point = e.GetPosition(this.canvas);
                    this.pointList.Add(point);

                    DrawCurve(this.pointList, false, this.areaGeometrys.Count);
                    DrawRectangle(this.pointList, false, this.areaGeometrys.Count);
                }
                else if(this.pointList.Count == 2)//Point 1개 일 경우(마우스 따라다니는 Point가 1개 더 있어서 Count가 2)
                {
                    this.pointList.Clear();
                    this.groupFirst = true;
                    this.isCanvasClicked = false;

                    DeleteCurve(this.areaGeometrys.Count);
                    DeleteRectagle(this.areaGeometrys.Count);
                }
                else//Point 없을 경우, Disable Command 처리
                {
                    if (!IsFfr)
                    {
                        DisableCommand();
                        CommandOff = true;
                    }
                }
            }
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

            if (this.isErasing)
            {
                Rectangle rectangle = sender as Rectangle;
                string[] tempArr = rectangle.Name.Split('_');
                int group = int.Parse(tempArr[1]);
                int index = int.Parse(tempArr[2]);

                //Point 삭제
                if (this.areaGeometrys[group].Points.Count > 3)
                {
                    this.areaGeometrys[group].Points.RemoveAt(index);

                    DrawCurve(this.areaGeometrys[group]);

                    DrawRectangle(this.areaGeometrys[group]);
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
                return;
            }

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

                Rectangle rectangle = sender as Rectangle;

                string[] tempArr = rectangle.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DrawCurve(this.areaGeometrys[group]);
                DrawRectangle(this.areaGeometrys[group]);

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

                Canvas.SetLeft(rectangle, point.X);
                Canvas.SetTop(rectangle, point.Y);

                rectPoint.X = point.X;
                rectPoint.Y = point.Y;
                areaGeometry.Points[index] = rectPoint;

                DrawCurve(areaGeometry);

                //Label 추가
                DrawLabel(areaGeometry);
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

            AreaGeometry areaGeometry = new AreaGeometry();
            areaGeometry.Points = this.pointList;
            areaGeometry.Group = this.areaGeometrys.Count;
            areaGeometry.IsClosed = true;
            this.areaGeometrys.Add(areaGeometry);

            DrawCurve(areaGeometry);
            DrawRectangle(areaGeometry);

            DisableCommand();
            CommandOff = true;
        }

        private void rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            PathBoldOn = true;
        }

        private void rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            PathBoldOn = false;
        }

        private void rectangle_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("rectangle_MouseRightButtonDown");

            if (this.isDrawing || this.isErasing)
                return;

            Rectangle rectangle = sender as Rectangle;
            string[] tempArr = rectangle.Name.Split('_');
            int group = int.Parse(tempArr[1]);
            int index = int.Parse(tempArr[2]);

            //Point 삭제
            if (this.areaGeometrys[group].Points.Count > 3)
            {
                this.areaGeometrys[group].Points.RemoveAt(index);

                DrawCurve(this.areaGeometrys[group]);

                DrawRectangle(this.areaGeometrys[group]);
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

                if (IsFfr)
                {
                    AddArea("True");
                }
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

                    DrawCurve(this.areaGeometrys[group]);
                    DrawRectangle(this.areaGeometrys[group]);

                    break;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void AddArea(string isAreaOn)
        {
            _log.Debug("AddArea");

            if(CommandType != 0)
                DisableCommand();

            if(Convert.ToBoolean(isAreaOn))
            {
                this.pointList = new List<Point>();
                this.isDrawing = true;
                this.groupFirst = true;

                //Canvas 마우스 이벤트 활성화
                this.canvas.MouseLeftButtonDown += area_canvas_MouseLeftButtonDown;
                this.canvas.MouseMove += area_canvas_MouseMove;
                this.canvas.MouseLeave += area_canvas_MouseLeave;
                this.canvas.MouseRightButtonDown += area_canvas_MouseRightButtonDown;

                this.canvas.Background = Brushes.Transparent;
                CommandType = Constants.MeasureCmdArea;
            }
        }

        private void DrawAreaAll()
        {
            _log.Debug("DrawAreaAll");

            foreach (var areaGeometry in this.areaGeometrys)
            {
                DrawCurve(areaGeometry, false);

                if (IsEditOn)
                    DrawRectangle(areaGeometry);
                else
                    DrawLabel(areaGeometry);
            }
        }

        private void DrawCurve(AreaGeometry areaGeometry, bool isCurrentEditOn = true) {
            areaGeometry.Path = DrawCurve(areaGeometry.Points, areaGeometry.IsClosed, areaGeometry.Group, isCurrentEditOn);

            if (IsFfr)
            {
                if (areaGeometry.IsClosed)
                {
                    areaGeometry.MaxDiameter = new DiameterInfo();
                    areaGeometry.MinDiameter = new DiameterInfo();
                    areaGeometry.MeanDiameter = 0.0f;

                    ContourMeasurement measurement = new ContourMeasurement();
                    measurement.Measure(areaGeometry, imageContour);

                    FfrFeature.IsPlaqueAreaValid = false;

                    if (areaGeometry.Valid)
                    {
                        FfrFeature.PlaqueArea = areaGeometry.Area;
                        FfrFeature.PercentAreaStenosis = (FfrFeature.PlaqueArea - FfrFeature.MinimalLumenArea) / FfrFeature.PlaqueArea * 100;
                        if(FfrFeature.PercentAreaStenosis > 0)
                            FfrFeature.IsPlaqueAreaValid = true;
                    }
                }
            }
            else
            {
                if (IsEditOn && isCurrentEditOn)
                {
                    if (areaGeometry.IsClosed)
                    {
                        areaGeometry.MaxDiameter = new DiameterInfo();
                        areaGeometry.MinDiameter = new DiameterInfo();
                        areaGeometry.MeanDiameter = 0.0f;

                        ContourMeasurement measurement = new ContourMeasurement();
                        measurement.Measure(areaGeometry, imageContour);

                        measurement.CalculateDiameter(areaGeometry);

                        DrawDiameter(areaGeometry.MinDiameter.point1, areaGeometry.MinDiameter.point2, areaGeometry.Group, constMinDiameter);
                        DrawDiameter(areaGeometry.MaxDiameter.point1, areaGeometry.MaxDiameter.point2, areaGeometry.Group, constMaxDiameter);
                    }
                }
                else
                {
                    DrawDiameter(areaGeometry.MinDiameter.point1, areaGeometry.MinDiameter.point2, areaGeometry.Group, constMinDiameter);
                    DrawDiameter(areaGeometry.MaxDiameter.point1, areaGeometry.MaxDiameter.point2, areaGeometry.Group, constMaxDiameter);
                }
            }
        }

        private Path DrawCurve(List<Point> pointList, bool isClosed, int group, bool isCurrentEditOn = false)
        {
            Path path = null;
            DeleteCurve(group);

            if (pointList.Count > 1)
            {
                PathGeometry pathGeometry = CommonUtil.GetBezierCurve(pointList, isClosed);
                this.overlayPathGeometry = pathGeometry;

                path = new Path();
                path.Style = (Style)this.Resources["StylePathCurve"];
                path.Data = pathGeometry;
                path.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
                path.Name = constCurve + "_" + group;

                if (!isClosed)
                {
                    path.StrokeDashArray.Add(2);
                }

                if (isClosed)
                {
                    path.Style = (Style)this.Resources["StylePathCurveClosed"];
                    if (IsEditOn)
                    {
                        path.MouseLeftButtonDown += path_MouseLeftButtonDown;
                        if (isCurrentEditOn)
                        {
                            DrawContourToBackBuffer(path);
                        }
                    }
                }

                this.canvas.Children.Add(path);
            }

            return path;
        }

        private void DrawRectangle(AreaGeometry areaGeometry) 
        {
            if (areaGeometry.IsClosed)
            {
                DrawLabel(areaGeometry);
            }

            DrawRectangle(areaGeometry.Points, areaGeometry.IsClosed, areaGeometry.Group);
        }

        private void DrawRectangle(List<Point> pointList, bool isClosed, int group)
        {
            DeleteRectagle(group);

            int cnt = isClosed ? pointList.Count : pointList.Count - 1;

            for (int i = cnt - 1; i >= 0; i--)
            {
                Rectangle rectangle = new Rectangle();
                rectangle.Style = (Style)this.Resources["StyleRectangle"];
                rectangle.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
                rectangle.Name = constRectangle + "_" + group + "_" + i;
                Canvas.SetLeft(rectangle, pointList[i].X);
                Canvas.SetTop(rectangle, pointList[i].Y);

                if (i == 0 && !isClosed)
                {
                    rectangle.Style = (Style)this.Resources["StyleRectangleFirst"];
                    rectangle.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
                    rectangle.Fill = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
                    rectangle.MouseLeftButtonDown += rectangle_Connect;
                    rectangle.MouseEnter += rectangle_MouseEnter;
                    rectangle.MouseLeave += rectangle_MouseLeave;
                }
                else if (isClosed)
                {
                    rectangle.Style = (Style)this.Resources["StyleRectangleClosed"];

                    rectangle.MouseLeftButtonDown += rectangle_MouseLeftButtonDown;
                    rectangle.MouseLeftButtonUp += rectangle_MouseLeftButtonUp;
                    rectangle.MouseMove += rectangle_MouseMove;
                    rectangle.MouseRightButtonDown += rectangle_MouseRightButtonDown;
                }
                this.canvas.Children.Add(rectangle);
            }
        }

        private void DrawLabel(AreaGeometry areaGeometry)
        {
            if (IsFfr)
                return;

            //Label 삭제
            DeleteLabel(constArea, areaGeometry.Group);

            Label label = new Label();
            label.Style = (Style)this.Resources["StyleLabel"];
            label.Name = constArea + "_" + areaGeometry.Group;
            label.Content = DrawAnnotation.GetLabelText(areaGeometry.Group, areaGeometry.Area * Constants.ImageResolution * Constants.ImageResolution * Constants.ZOffsetScale / Constants.ZOffsetScale);

            Point centerdPoint = areaGeometry.CenterOfMass;

            Canvas.SetLeft(label, centerdPoint.X - 40);
            Canvas.SetTop(label, centerdPoint.Y - 10);
            this.canvas.Children.Add(label);
        }

        private void DrawDiameter(Point firstPoint, Point secondPoint, int group, string prefix)
        {
            Path path = new Path();
            path.Style = (Style)this.Resources["StylePath"];
            path.Data = CommonUtil.GetLine(firstPoint, secondPoint);
            path.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            path.Name = prefix + "_" + group;
            if (prefix.Equals(constMinDiameter))
                path.StrokeDashArray.Add(2);
            else
                path.StrokeDashArray.Add(4);

            this.canvas.Children.Add(path);
        }

        private void DeleteAreaAll()
        {
            foreach (var areaGeometry in this.areaGeometrys)
            {
                DeleteCurve(areaGeometry.Group);
                DeleteRectagle(areaGeometry.Group);
                DeleteLabel(constArea, areaGeometry.Group);
            }
            this.canvasBackground.Children.Clear();

            if (IsFfr)
            {
                FfrFeature.PlaqueArea = 0;
                FfrFeature.PercentAreaStenosis = 0;
                FfrFeature.IsPlaqueAreaValid = false;
            }
        }

        private void DeleteCurve(int group)
        {
            List<int> indexList = new List<int>();

            for(int i=this.canvas.Children.Count-1; i>=0; i--)
            {
                UIElement item = this.canvas.Children[i];
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constCurve + "_" + group)
                    {
                        this.canvas.Children.RemoveAt(i);
                    }
                    else if (temp.Name == constMinDiameter + "_" + group)
                    {
                        this.canvas.Children.RemoveAt(i);
                    }
                    else if (temp.Name == constMaxDiameter + "_" + group)
                    {
                        this.canvas.Children.RemoveAt(i);
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
                        {
                            temp.MouseLeftButtonDown -= rectangle_Connect;
                            temp.MouseEnter -= rectangle_MouseEnter;
                            temp.MouseLeave -= rectangle_MouseLeave;
                        }                            

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

        private bool IsOverlayedComplete(List<Point> pointList, int group)
        {
            if (this.areaGeometrys.Count <= group)
                return false;

            PathFigureCollection pathFigures = this.overlayPathGeometry.Figures;
            PathFigure pathFigure = pathFigures[0];

            bool isFind = false;
            int segmentCnt = pathFigure.Segments.Count;

            for (int i = 0; i < segmentCnt; i++)
            {
                var myPathFigure = new PathFigure { StartPoint = this.areaGeometrys[group].Points[i] };
                var myPathSegmentCollection = new PathSegmentCollection();
                myPathSegmentCollection.Add(pathFigure.Segments[i]);
                myPathFigure.Segments = myPathSegmentCollection;
                var myPathFigureCollection = new PathFigureCollection { myPathFigure };
                var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };
                for (int j = 0; j < segmentCnt; j++)
                {
                    //인접한 라인은 비교 대상에서 제외
                    if (i == j)
                        continue;
                    var pathFigureCompare = new PathFigure { StartPoint = this.areaGeometrys[group].Points[j] };
                    var pathSegmentCollectionCompare = new PathSegmentCollection();
                    pathSegmentCollectionCompare.Add(pathFigure.Segments[j]);
                    pathFigureCompare.Segments = pathSegmentCollectionCompare;
                    var pathFigureCollectionCompare = new PathFigureCollection { pathFigureCompare };
                    var pathGeometryCompare = new PathGeometry { Figures = pathFigureCollectionCompare };
                    isFind = myPathGeometry.FillContainsWithDetail(pathGeometryCompare) == IntersectionDetail.Intersects ? true : false;
                    if (isFind)
                        break;
                }
                if (isFind)
                    break;
            }
            return isFind;
        }

        private bool IsOverlayed(List<Point> pointList, int group)
        {
            if (this.movingPointIndex == -1)
                return IsOverlayedComplete(pointList, group);

            if (this.areaGeometrys.Count <= group)
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

        private void DrawContourToBackBuffer(Path path)
        {
            Path copiedPath = new Path();
            copiedPath.Style = (Style)this.Resources["StylePathBackground"];
            copiedPath.Data = path.Data;
            copiedPath.Stroke = Brushes.White;
            copiedPath.Name = "BackBuffer_" + path.Name;

            this.canvasBackground.Children.Clear();
            this.canvasBackground.Children.Add(copiedPath);
            this.canvasBackground.UpdateLayout();

            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)canvasBackground.ActualWidth, (int)canvasBackground.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvasBackground);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            var bitmapImage = new BitmapImage();
            using (var stream = new System.IO.MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }

            Mat image = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(bitmapImage);
            Mat imageCvt = new Mat();
            image.ConvertTo(imageCvt, MatType.CV_8UC1);
            Cv2.CvtColor(imageCvt, imageContour, ColorConversionCodes.RGBA2GRAY);

            this.canvasBackground.Children.Clear();
            this.canvasBackground.UpdateLayout();
        }
    }
}
