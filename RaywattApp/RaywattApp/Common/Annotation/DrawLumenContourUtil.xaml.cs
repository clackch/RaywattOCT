using log4net;
using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Annotation.Util;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using RaywattApp.Models;
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
    public class LumenContourHistory
    {
        public List<Point>? points;

        public double area;

        public DiameterInfo? minDiameter;

        public DiameterInfo? maxDiameter;

        public double meanDiameter;
    }

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

        const string constMinDiameter = "MinDiameter";

        const string constMaxDiameter = "MaxDiameter";

        List<Stack<LumenContourHistory>> lumenContourHistory;

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

        public bool IsContourMouseOver
        {
            get { return (bool)GetValue(IsContourMouseOverProperty); }
            set { this.SetValue(IsContourMouseOverProperty, value); }
        }

        private static readonly DependencyProperty IsContourMouseOverProperty =
            DependencyProperty.Register("IsContourMouseOver", typeof(bool), typeof(DrawLumenContourUtil), new PropertyMetadata(default(bool)));

        public int CommandType
        {
            get { return (int)GetValue(CommandTypeProperty); }
            set { this.SetValue(CommandTypeProperty, value); }
        }

        private static readonly DependencyProperty CommandTypeProperty =
            DependencyProperty.Register("CommandType", typeof(int), typeof(DrawLumenContourUtil), new PropertyMetadata(default(int)));

        public bool IsEditOn
        {
            get { return (bool)GetValue(IsEditOnProperty); }
            set { this.SetValue(IsEditOnProperty, value); }
        }

        private static readonly DependencyProperty IsEditOnProperty =
            DependencyProperty.Register("IsEditOn", typeof(bool), typeof(DrawLumenContourUtil), new PropertyMetadata(default(bool)));

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

        public LumenContour CurrentLumenContour
        {
            get { return (LumenContour)GetValue(CurrentLumenContourProperty); }
            set { SetValue(CurrentLumenContourProperty, value); }
        }

        public static readonly DependencyProperty CurrentLumenContourProperty =
            DependencyProperty.Register("CurrentLumenContour", typeof(LumenContour), typeof(DrawLumenContourUtil), new PropertyMetadata(null));

        public Zoom Zoom
        {
            get { return (Zoom)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(Zoom), typeof(DrawLumenContourUtil), new PropertyMetadata(null));

        public string? InCommand
        {
            get { return (string)GetValue(InCommandProperty); }
            set { this.SetValue(InCommandProperty, value); }
        }

        private static readonly DependencyProperty InCommandProperty =
            DependencyProperty.Register("InCommand", typeof(string), typeof(DrawLumenContourUtil), new PropertyMetadata(ReceiveCommand));

        public bool IsDrawOn
        {
            get { return (bool)GetValue(IsDrawOnProperty); }
            set { this.SetValue(IsDrawOnProperty, value); }
        }

        private static readonly DependencyProperty IsDrawOnProperty =
            DependencyProperty.Register("IsDrawOn", typeof(bool), typeof(DrawLumenContourUtil), new PropertyMetadata(DrawPropertyChanged));

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

            if (!drawUtil.isInit && drawUtil.IsEditOn)
            {
                drawUtil.newPoints = new List<Point>();
                drawUtil.contourLines = new List<Line>();
                drawUtil.isFirstPoint = true;

                drawUtil.lumenContourHistory = new List<Stack<LumenContourHistory>>(drawUtil.LumenContours.Count);
                foreach(LumenContour lumenContour in drawUtil.LumenContours)
                {
                    Stack<LumenContourHistory> stack = new Stack<LumenContourHistory>();
                    stack.Push(drawUtil.CopyLumenContourToHistory(lumenContour));
                    drawUtil.lumenContourHistory.Add(stack);
                }

                drawUtil.isInit = true;
            }

            drawUtil.CurrentLumenContour = drawUtil.LumenContours[frameNumber];

            if(drawUtil.IsEditOn || drawUtil.IsDrawOn)
                drawUtil.DrawLumenContour(drawUtil.LumenContours[frameNumber], drawUtil.IsEditOn);
        }

        private static void DrawPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            bool isDrawOn = (bool)dependencyPropertyChangedEventArgs.NewValue;

            var drawUtil = dependencyObject as DrawLumenContourUtil;

            if (drawUtil == null || drawUtil.LumenContours == null || drawUtil.FrameNumber < 0)
                return;

            if (isDrawOn)
            {
                drawUtil.DrawLumenContour(drawUtil.CurrentLumenContour, drawUtil.IsEditOn);
            }
            else
            {
                drawUtil.canvas.Children.Clear();
            }
        }

        private static void ReceiveCommand(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var drawUtil = dependencyObject as DrawLumenContourUtil;
            if (drawUtil == null || drawUtil.InCommand == null)
                return;

            switch (drawUtil.InCommand)
            {
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

                CommandType = 1;
            }
            else
            {
                secondPointIndex = lineIndex;
                isFirstPoint = true;
                IsContourMouseOver = false;

                DeactivateEvent();
                ReDrawLumenContour();

                CommandType = 0;
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

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _log.Debug("Canvas_MouseLeave");

            this.newPoints.Clear();
            DrawLumenContour(LumenContours[FrameNumber], true);

            DeactivateEvent();
            isFirstPoint = true;
            IsContourMouseOver = false;
        }

        private void Line_MouseEnter(object sender, MouseEventArgs e)
        {
            IsContourMouseOver = true;
        }

        private void Line_MouseLeave(object sender, MouseEventArgs e)
        {
            IsContourMouseOver = false;
        }

        private void Polygon_MouseEnter(object sender, MouseEventArgs e)
        {
            IsContourMouseOver = true;
        }

        private void Polygon_MouseLeave(object sender, MouseEventArgs e)
        {
            IsContourMouseOver = false;
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void ActivateEvent()
        {
            _log.Debug("ActivateEvent");

            this.canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            this.canvas.MouseMove += Canvas_MouseMove;
            this.canvas.MouseRightButtonDown += Canvas_MouseRightButtonDown;
            this.canvas.MouseLeave += Canvas_MouseLeave;

            this.canvas.Background = Brushes.Transparent;
        }

        private void DeactivateEvent()
        {
            _log.Debug("DeactivateEvent");

            this.canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            this.canvas.MouseMove -= Canvas_MouseMove;
            this.canvas.MouseRightButtonDown -= Canvas_MouseRightButtonDown;
            this.canvas.MouseLeave -= Canvas_MouseLeave;

            this.canvas.Background = null;
        }

        private void DrawLumenContour(LumenContour lumenContour, bool isEditOn)
        {
            _log.Debug("DrawLumenContour");

            this.canvas.Children.Clear();

            List<Point>? pointList = lumenContour.Points;

            if (pointList == null || pointList.Count < 3)
                return;

            if (isEditOn)
            {
                contourLines.Clear();

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
                    line.MouseEnter += Line_MouseEnter;
                    line.MouseLeave += Line_MouseLeave;
                    line.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                    contourLines.Add(line);
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
                lineConnect.MouseEnter += Line_MouseEnter;
                lineConnect.MouseLeave += Line_MouseLeave;
                lineConnect.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                contourLines.Add(lineConnect);
                this.canvas.Children.Add(lineConnect);
            }
            else
            {
                Polygon polygon = new Polygon();
                polygon.Style = (Style)this.Resources["StylePolygon"];
                foreach (Point point in pointList)
                {
                    polygon.Points.Add(point);
                }
                polygon.MouseEnter += Polygon_MouseEnter;
                polygon.MouseLeave += Polygon_MouseLeave;
                this.canvas.Children.Add(polygon);

                if (lumenContour.Valid)
                {
                    DrawDiameter(lumenContour.MinDiameter.point1, lumenContour.MinDiameter.point2, constMinDiameter);
                    DrawDiameter(lumenContour.MaxDiameter.point1, lumenContour.MaxDiameter.point2, constMaxDiameter);
                }
            }
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
                    }
                    else if(s is LineSegment)
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
                DrawLumenContour(LumenContours[FrameNumber], true);
            }
            else
            {
                LumenContours[FrameNumber].Points = finalPoint;
                LumenContours[FrameNumber].MaxDiameter = new DiameterInfo();
                LumenContours[FrameNumber].MinDiameter = new DiameterInfo();
                LumenContours[FrameNumber].MeanDiameter = 0.0f;

                ContourMeasurement measurement = new ContourMeasurement();
                measurement.Measure(LumenContours[FrameNumber], imageContour);
                if (LumenContours[FrameNumber].Valid)
                {
                    measurement.CalculateDiameter(LumenContours[FrameNumber]);
                }

                lumenContourHistory[FrameNumber].Push(CopyLumenContourToHistory(LumenContours[FrameNumber]));

                DrawLumenContour(LumenContours[FrameNumber], true);
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
            Cv2.FindContours(imageContour, out contours, out hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);

            Mat imgInnerContour = new Mat(1024, 1024, MatType.CV_8UC1);
            for (int i = 0; i < contours.Length; i++)
            {
                if (hierarchy[i].Parent != -1)
                    Cv2.DrawContours(imgInnerContour, contours, i, Scalar.White, -1);
            }
            Cv2.FindContours(imgInnerContour, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            return contours.Length == 1 ? true : false;
        }

        private void DrawLumenContourPoint(Point point, int index)
        {
            _log.Debug("DrawLumenContourPoint");

            Rectangle rectangle = new Rectangle();
            rectangle.Name = constContourPoint + "_" + index;
            rectangle.Style = (Style)this.Resources["StyleRectangle"];
            Canvas.SetLeft(rectangle, point.X - (Constants.AnnotationRectWidth / Zoom.ScaleX) / 2);
            Canvas.SetTop(rectangle, point.Y - (Constants.AnnotationRectHeight / Zoom.ScaleY) / 2);

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

            if(this.curPath == null)
            {
                this.curPath = new Path();
                this.curPath.Name = constContourCurve;
                this.curPath.Style = (Style)this.Resources["StylePath"];
            }                        
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

        private void DrawDiameter(Point firstPoint, Point secondPoint, string prefix)
        {
            Path path = new Path();
            path.Style = (Style)this.Resources["StylePathDiameter"];
            path.Data = CommonUtil.GetLine(firstPoint, secondPoint);
            
            if (prefix.Equals(constMinDiameter))
                path.StrokeDashArray.Add(2);
            else
                path.StrokeDashArray.Add(4);
            
            this.canvas.Children.Add(path);
        }

        private void Restore()
        {
            _log.Debug("Restore");

            if (lumenContourHistory[FrameNumber].Count < 2)
                return;

            lumenContourHistory[FrameNumber].Pop();
            CopyHistoryToLumenContour(lumenContourHistory[FrameNumber].Peek());
            DrawLumenContour(LumenContours[FrameNumber], true);
        }

        private void Reset()
        {
            _log.Debug("Reset");

            if (lumenContourHistory[FrameNumber].Count < 2)
                return;

            CopyHistoryToLumenContour(lumenContourHistory[FrameNumber].ToArray()[lumenContourHistory[FrameNumber].Count - 1]);
            lumenContourHistory[FrameNumber].Clear();
            lumenContourHistory[FrameNumber].Push(CopyLumenContourToHistory(LumenContours[FrameNumber]));
            DrawLumenContour(LumenContours[FrameNumber], true);
        }

        private void AutoDetect()
        {
            _log.Debug("AutoDetect");

            LumenContours[FrameNumber].CopyMlToLumenContour();
            lumenContourHistory[FrameNumber].Push(CopyLumenContourToHistory(LumenContours[FrameNumber]));
            DrawLumenContour(LumenContours[FrameNumber], true);
        }

        private LumenContourHistory CopyLumenContourToHistory(LumenContour lumenContour)
        {
            LumenContourHistory lumenContourHistory = new LumenContourHistory();
            lumenContourHistory.points = lumenContour.Points;
            lumenContourHistory.minDiameter = lumenContour.MinDiameter;
            lumenContourHistory.maxDiameter = lumenContour.MaxDiameter;
            lumenContourHistory.meanDiameter = lumenContour.MeanDiameter;
            lumenContourHistory.area = lumenContour.Area;

            return lumenContourHistory;
        }

        private void CopyHistoryToLumenContour(LumenContourHistory lumenContourHistory)
        {
            LumenContours[FrameNumber].Points = lumenContourHistory.points;
            LumenContours[FrameNumber].MinDiameter = lumenContourHistory.minDiameter;
            LumenContours[FrameNumber].MaxDiameter = lumenContourHistory.maxDiameter;
            LumenContours[FrameNumber].MeanDiameter = lumenContourHistory.meanDiameter;
            LumenContours[FrameNumber].Area = lumenContourHistory.area;
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
