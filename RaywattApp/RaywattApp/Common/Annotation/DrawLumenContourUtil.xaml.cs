using log4net;
using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace RaywattApp.Common.Annotation
{
    /// <summary>
    /// DrawLumenContourUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawLumenContourUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawLumenContourUtil));

        //---------------------------------------------------------------------------------------------------- Field

        const string constContourPoint = "ContourPoint";

        const string constContourLine = "ContourLine";

        const string constContourCurve = "ContourCurve";

        List<Stack<List<Point>>> lumenContourHistory;

        List<Point> newPoints;

        List<Line> contourLines;

        Path curPath;

        bool isInit;

        bool isFirstPoint;

        int firstPointIndex;

        int secondPointIndex;

        private double lastPointX;

        private double lastPointY;

        private Mat imageContour = new Mat();

        private OpenCvSharp.Rect contourBounds;

        public bool IsContourMouseOver
        {
            get { return (bool)GetValue(IsContourMouseOverProperty); }
            set { this.SetValue(IsContourMouseOverProperty, value); }
        }

        private static readonly DependencyProperty IsContourMouseOverProperty =
            DependencyProperty.Register("IsContourMouseOver", typeof(bool), typeof(DrawLumenContourUtil), new PropertyMetadata(default(bool)));

        public int FrameNumber
        {
            get { return (int)GetValue(FrameNumberProperty); }
            set { this.SetValue(FrameNumberProperty, value); }
        }

        private static readonly DependencyProperty FrameNumberProperty =
            DependencyProperty.Register("FrameNumber", typeof(int), typeof(DrawLumenContourUtil), new PropertyMetadata(-1, OnPropertyChanged));

        public List<LumenContour> LumenContours
        {
            get { return (List<LumenContour>)GetValue(LumenContoursProperty); }
            set { SetValue(LumenContoursProperty, value); }
        }

        public static readonly DependencyProperty LumenContoursProperty =
            DependencyProperty.Register("LumenContours", typeof(List<LumenContour>), typeof(DrawLumenContourUtil), new PropertyMetadata(null));

        public string? InCommand
        {
            get { return (string)GetValue(InCommandProperty); }
            set { this.SetValue(InCommandProperty, value); }
        }

        private static readonly DependencyProperty InCommandProperty =
            DependencyProperty.Register("InCommand", typeof(string), typeof(DrawLumenContourUtil), new PropertyMetadata(ReceiveCommand));

        //---------------------------------------------------------------------------------------------------- Constructor
        public DrawLumenContourUtil()
        {
            InitializeComponent();

            isInit = false;
        }

        //---------------------------------------------------------------------------------------------------- Event
        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            int frameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            _log.Debug("frameNumber : " + frameNumber);

            if (frameNumber < 0)
                return;

            var drawUtil = dependencyObject as DrawLumenContourUtil;
            
            if (drawUtil == null || drawUtil.LumenContours == null)
                return;                       

            if (frameNumber >= drawUtil.LumenContours.Count)
            {
                drawUtil.canvas.Children.Clear();
                return;
            }

            if (!drawUtil.isInit && drawUtil.IsEnabled)
            {
                drawUtil.newPoints = new List<Point>();
                drawUtil.contourLines = new List<Line>();
                drawUtil.curPath = new Path();
                drawUtil.curPath.Name = constContourCurve;
                drawUtil.curPath.Style = (Style)drawUtil.Resources["StylePath"];
                drawUtil.isFirstPoint = true;

                drawUtil.lumenContourHistory = new List<Stack<List<Point>>>(drawUtil.LumenContours.Count);
                foreach(LumenContour lumenContour in drawUtil.LumenContours)
                {
                    Stack<List<Point>> stack = new Stack<List<Point>>();
                    stack.Push(lumenContour.Points.Count == 0 ? lumenContour.PointsFromMl : lumenContour.Points);
                    drawUtil.lumenContourHistory.Add(stack);
                }

                drawUtil.isInit = true;
            }

            List<Point>? points = drawUtil.LumenContours[frameNumber].Points.Count == 0 ? drawUtil.LumenContours[frameNumber].PointsFromMl : drawUtil.LumenContours[frameNumber].Points;

            drawUtil.DrawLumenContour(points, drawUtil.IsEnabled);
        }

        private static void ReceiveCommand(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var drawUtil = dependencyObject as DrawLumenContourUtil;
            if (drawUtil == null || drawUtil.InCommand == null)
                return;

            switch (drawUtil.InCommand)
            {
                case Constants.LumenContourZoomIn:
                    drawUtil.ZoomIn();
                    break;
                case Constants.LumenContourZoomOut:
                    drawUtil.ZoomOut();
                    break;
                case Constants.LumenContourRestore:
                    drawUtil.Restore();
                    break;
                case Constants.LumenContourReset:
                    drawUtil.Reset();
                    break;
                case Constants.LumenContourAutoDetect:
                    drawUtil.AutoDetect();
                    break;
                default:
                    break;
            }

            drawUtil.InCommand = null;
        }
    
        private void Line_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("Line_MouseLeftButtonDown");

            Line? line = sender as Line;
            if (line == null)
                return;

            int lineIndex = int.Parse(line.Name.Replace(constContourLine + "_", ""));

            if (isFirstPoint)
            {
                firstPointIndex = lineIndex;
                isFirstPoint = false;

                Point point = e.GetPosition(this.canvas);
                this.newPoints.Clear();
                newPoints.Add(point);
                ActivateEvent();
            }
            else
            {
                secondPointIndex = lineIndex;
                isFirstPoint = true;
                IsContourMouseOver = false;

                DeactivateEvent();
                ReDrawLumenContour();
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("Canvas_MouseLeftButtonDown");
            DrawLumenContourPoint(this.newPoints[this.newPoints.Count - 1], this.newPoints.Count);

            Point point = e.GetPosition(this.canvas);
            this.newPoints.Add(point);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(this.canvas);

            if (this.lastPointX == point.X && this.lastPointY == point.Y)
                return;

            this.lastPointX = point.X;
            this.lastPointY = point.Y;

            this.newPoints[this.newPoints.Count - 1] = point;

            DrawLumenContourCurve();
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("Canvas_MouseRightButtonDown");

            if (this.newPoints.Count > 2)
            {
                this.newPoints.RemoveAt(this.newPoints.Count - 1);
                this.newPoints[this.newPoints.Count - 1] = e.GetPosition(this.canvas);
                DeleteLumenContourPoint(this.newPoints.Count);
                DrawLumenContourCurve();
            }
            else
            {
                this.newPoints.RemoveAt(this.newPoints.Count - 1);
                DeleteLumenContourPoint(this.newPoints.Count);
                DeleteLumenContourCurve();

                DeactivateEvent();
                isFirstPoint = true;
                IsContourMouseOver = false;
            }
        }

        private void Line_MouseEnter(object sender, MouseEventArgs e)
        {
            IsContourMouseOver = true;
        }

        private void Line_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isFirstPoint)
                IsContourMouseOver = false;
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void ActivateEvent()
        {
            _log.Debug("ActivateEvent");

            this.canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            this.canvas.MouseMove += Canvas_MouseMove;
            this.canvas.MouseRightButtonDown += Canvas_MouseRightButtonDown;

            this.canvas.Background = Brushes.Transparent;
        }

        private void DeactivateEvent()
        {
            _log.Debug("DeactivateEvent");

            this.canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            this.canvas.MouseMove -= Canvas_MouseMove;
            this.canvas.MouseRightButtonDown -= Canvas_MouseRightButtonDown;

            this.canvas.Background = null;
        }

        private void DrawLumenContour(List<Point>? pointList, bool isEditOn)
        {
            _log.Debug("DrawLumenContour");

            this.canvas.Children.Clear();

            if (pointList == null || pointList.Count < 3)
                return;

            if (isEditOn)
            {
                contourLines.Clear();
            }

            //Contour Lines
            for (int i = 0; i < pointList.Count - 1; i++)
            {
                Line line = new Line();
                line.Name = constContourLine + "_" + i;
                line.Style = (Style)this.Resources["StyleLine"];
                line.X1 = pointList[i].X;
                line.Y1 = pointList[i].Y;
                line.X2 = pointList[i + 1].X;
                line.Y2 = pointList[i + 1].Y;
                if (isEditOn)
                {
                    line.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                    line.MouseEnter += Line_MouseEnter;
                    line.MouseLeave += Line_MouseLeave;
                    contourLines.Add(line);
                }
                this.canvas.Children.Add(line);
            }

            //Contour Line Closed
            Line lineConnect = new Line();
            lineConnect.Name = constContourLine + "_" + (pointList.Count - 1);
            lineConnect.Style = (Style)this.Resources["StyleLine"];
            lineConnect.X1 = pointList[pointList.Count - 1].X;
            lineConnect.Y1 = pointList[pointList.Count - 1].Y;
            lineConnect.X2 = pointList[0].X;
            lineConnect.Y2 = pointList[0].Y;
            if (isEditOn)
            {
                lineConnect.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                lineConnect.MouseEnter += Line_MouseEnter;
                lineConnect.MouseLeave += Line_MouseLeave;
                contourLines.Add(lineConnect);
            }
            this.canvas.Children.Add(lineConnect);
        }

        private void ReDrawLumenContour()
        {
            _log.Debug("ReDrawLumenContour");

            List<Point> points = new List<Point>();
            List<Point> reversePoints = new List<Point>();

            PathGeometry g = this.curPath.Data.GetFlattenedPathGeometry();
            foreach (var f in g.Figures)
            {
                foreach (var s in f.Segments)
                {
                    if (s is PolyLineSegment)
                    {
                        foreach (var pt in ((PolyLineSegment)s).Points)
                        {
                            points.Add(pt);
                            reversePoints.Add(pt);
                        }
                    }else if(s is LineSegment)
                    {
                        points.Add(f.StartPoint);
                        reversePoints.Add(f.StartPoint);

                        var pt = (LineSegment)s;
                        points.Add(pt.Point);
                        reversePoints.Add(pt.Point);
                    }
                }
            }

            if (firstPointIndex > secondPointIndex)
            {
                for (int i = secondPointIndex; i < firstPointIndex; i++)
                {
                    points.Add(new Point(contourLines[i].X2, contourLines[i].Y2));
                }
            }
            else
            {
                for (int i = secondPointIndex; i < contourLines.Count; i++)
                {
                    points.Add(new Point(contourLines[i].X2, contourLines[i].Y2));
                }

                for (int i = 0; i < firstPointIndex; i++)
                {
                    points.Add(new Point(contourLines[i].X2, contourLines[i].Y2));
                }
            }

            reversePoints.Reverse();

            if (secondPointIndex > firstPointIndex)
            {
                for (int i = firstPointIndex; i < secondPointIndex; i++)
                {
                    reversePoints.Add(new Point(contourLines[i].X2, contourLines[i].Y2));
                }
            }
            else
            {
                for (int i = firstPointIndex; i < contourLines.Count; i++)
                {
                    reversePoints.Add(new Point(contourLines[i].X2, contourLines[i].Y2));
                }

                for (int i = 0; i < secondPointIndex; i++)
                {
                    reversePoints.Add(new Point(contourLines[i].X2, contourLines[i].Y2));
                }
            }

            PathGeometry pathGeometry = GetPathGeometry(points);
            PathGeometry reversePathGeometry = GetPathGeometry(reversePoints);
            PathGeometry finalPathGeometry;
            List<Point> finalPoint;

            if (pathGeometry.GetArea() > reversePathGeometry.GetArea())
            {
                finalPathGeometry = pathGeometry;
                finalPoint = points;
            }
            else
            {
                finalPathGeometry = reversePathGeometry;
                finalPoint = reversePoints;
            }

            if (!IsValidPathGeometry(finalPathGeometry))
            {
                DrawLumenContour(LumenContours[FrameNumber].Points, true);
            }
            else
            {
                DrawLumenContour(finalPoint, true);

                lumenContourHistory[FrameNumber].Push(finalPoint);

                LumenContours[FrameNumber].Points = finalPoint;
            }
        }

        private PathGeometry GetPathGeometry(List<Point> points)
        {
            _log.Debug("GetPathGeometry");

            var pathSegmentCollection = new PathSegmentCollection();
            foreach (Point point in points.GetRange(1, points.Count - 1))
            {
                var lineSegment = new LineSegment { Point = point };
                pathSegmentCollection.Add(lineSegment);
            }

            var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = true, Segments = pathSegmentCollection };
            var pathFigureCollection = new PathFigureCollection { pathFigure };
            var pathGeometry = new PathGeometry { Figures = pathFigureCollection };

            return pathGeometry;
        }

        private bool IsValidPathGeometry(PathGeometry pathGeometry)
        {
            _log.Debug("IsValidPathGeometry");

            Path path = new Path();
            path.Style = (Style)this.Resources["StylePathBackground"];
            path.Data = pathGeometry;
            DrawContourToBackBuffer(path);

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(imageContour, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            //_log.Debug(contours.Length);
            //Cv2.ImShow(name, imageContour);
            //Cv2.WaitKey(1);

            return contours.Length == 2 ? true : false;
        }

        private void DrawLumenContourPoint(Point point, int index)
        {
            _log.Debug("DrawLumenContourPoint");

            Rectangle rectangle = new Rectangle();
            rectangle.Name = constContourPoint + "_" + index;
            rectangle.Style = (Style)this.Resources["StyleRectangle"];
            Canvas.SetLeft(rectangle, point.X - rectangle.Width / 2);
            Canvas.SetTop(rectangle, point.Y - rectangle.Height / 2);

            this.canvas.Children.Add(rectangle);
        }

        private void DeleteLumenContourPoint(int index)
        {
            _log.Debug("DeleteLumenContourPoint");

            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Rectangle))
                {
                    Rectangle temp = (Rectangle)item;

                    if (temp.Name == constContourPoint + "_" + index)
                    {
                        this.canvas.Children.Remove((Rectangle)item);
                        break;
                    }
                }
            }
        }

        private void DrawLumenContourCurve()
        {
            DeleteLumenContourCurve();

            this.curPath.Data = CommonUtil.GetBezierCurve(this.newPoints, false); ;

            //Lumen Contour 보다 아래쪽에 배치되도록 Index 0에 추가(마우스 클릭 이벤트 처리 때문)
            this.canvas.Children.Insert(0, this.curPath);
        }

        private void DeleteLumenContourCurve()
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constContourCurve)
                    {
                        this.canvas.Children.Remove((Path)item);
                        break;
                    }
                }
            }
        }

        private void ZoomIn()
        {
            _log.Debug("ZoomIn");
        }

        private void ZoomOut()
        {
            _log.Debug("ZoomOut");
        }

        private void Restore()
        {
            _log.Debug("Restore");

            if (lumenContourHistory.Count <= FrameNumber || lumenContourHistory[FrameNumber].Count < 2)
                return;

            lumenContourHistory[FrameNumber].Pop();
            List<Point> points = lumenContourHistory[FrameNumber].Peek();
            DrawLumenContour(points, true);

            LumenContours[FrameNumber].Points = points;
        }

        private void Reset()
        {
            _log.Debug("Reset");

            if (lumenContourHistory.Count <= FrameNumber || lumenContourHistory[FrameNumber].Count < 2)
                return;

            List<Point> points = lumenContourHistory[FrameNumber].ToArray()[lumenContourHistory[FrameNumber].Count - 1];
            lumenContourHistory[FrameNumber].Clear();
            lumenContourHistory[FrameNumber].Push(points);
            DrawLumenContour(points, true);

            LumenContours[FrameNumber].Points = points;
        }

        private void AutoDetect()
        {
            _log.Debug("AutoDetect");

            if (LumenContours.Count <= FrameNumber)
                return;

            List<Point> points = LumenContours[FrameNumber].PointsFromMl;
            DrawLumenContour(points, true);

            LumenContours[FrameNumber].Points = points;
        }

        private void DrawContourToBackBuffer(Path path)
        {
            _log.Debug("DrawContourToBackBuffer");

            Path copiedPath = new Path();
            copiedPath.Style = path.Style;
            copiedPath.Data = path.Data;

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
