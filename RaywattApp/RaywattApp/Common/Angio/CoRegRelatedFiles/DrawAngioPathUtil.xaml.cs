using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using RaywattApp.Common.Bases;
using OpenCvSharp;
using log4net;
using System.Diagnostics;

namespace RaywattApp.Common.Angio.CoRegRelatedFiles
{
    /// <summary>
    /// DrawAngioPathUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawAngioPathUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawAngioPathUtil));

        /* AngioFrameNumber = ImageIndex */
        public int AngioFrameNumber 
        {
            get { return (int)GetValue(AngioFrameNumberProperty); }
            set { this.SetValue(AngioFrameNumberProperty, value); }
        }

        private static readonly DependencyProperty AngioFrameNumberProperty =
            DependencyProperty.Register("AngioFrameNumber", typeof(int), typeof(DrawAngioPathUtil), new PropertyMetadata(-1, OnAngioFrameNumberPropertyChanged));

        public int CurrentAngioFrameNumber
        {
            get { return (int)GetValue(CurrentAngioFrameNumberProperty); }
            set { this.SetValue(CurrentAngioFrameNumberProperty, value); }
        }

        private static readonly DependencyProperty CurrentAngioFrameNumberProperty =
        DependencyProperty.Register("CurrentAngioFrameNumber", typeof(int), typeof(DrawAngioPathUtil), new PropertyMetadata(-1, OnCurrentAngioFrameNumberPropertyChanged));

        public List<Mat> AngioImages
        {
            get { return (List<Mat>)GetValue(AngioImagesProperty); }
            set { this.SetValue(AngioImagesProperty, value); }
        }

        public static readonly DependencyProperty AngioImagesProperty =
            DependencyProperty.Register("AngioImages", typeof(List<Mat>), typeof(DrawAngioPathUtil), new PropertyMetadata(null, OnAngioImagesPropertyChanged));

        public List<CoRegistration> AngioTrackPoints
        {
            get { return (List<CoRegistration>)GetValue(AngioTrackPointsProperty); }
            set { this.SetValue(AngioTrackPointsProperty, value); }
        }

        public static readonly DependencyProperty AngioTrackPointsProperty =
            DependencyProperty.Register("AngioTrackPoints", typeof(List<CoRegistration>), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public CoRegistration CurrentTrackPoint
        {
            get { return (CoRegistration)GetValue(CurrentTrackPointProperty); }
            set { this.SetValue(CurrentTrackPointProperty, value); }
        }

        public static readonly DependencyProperty CurrentTrackPointProperty =
            DependencyProperty.Register("CurrentTrackPoint", typeof(CoRegistration), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public List<DijkstraHeap> DijkstraHeap
        {
            get { return (List<DijkstraHeap>)GetValue(DijkstraHeapProperty); }
            set { this.SetValue(DijkstraHeapProperty, value); }
        }

        public static readonly DependencyProperty DijkstraHeapProperty =
            DependencyProperty.Register("DijkstraHeap", typeof(List<DijkstraHeap>), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public Point MousePosition
        {
            get { return (Point)GetValue(MousePositionProperty); }
            set { this.SetValue(MousePositionProperty, value); }
        }

        public static readonly DependencyProperty MousePositionProperty =
            DependencyProperty.Register("MousePosition", typeof(Point), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public bool IsAngioTrackCompleted
        {
            get { return (bool)GetValue(IsAngioTrackCompletedProperty); }
            set { this.SetValue(IsAngioTrackCompletedProperty, value); }
        }

        public static readonly DependencyProperty IsAngioTrackCompletedProperty =
            DependencyProperty.Register("IsAngioTrackCompleted", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(true));


        public bool IsReset
        {
            get { return (bool)GetValue(IsResetProperty); }
            set { this.SetValue(IsResetProperty, value); }
        }

        public static readonly DependencyProperty IsResetProperty =
            DependencyProperty.Register("IsReset", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(false, OnResetPropertyChanged));

        public bool IsResetOn
        {
            get { return (bool)GetValue(IsResetOnProperty); }
            set { this.SetValue(IsResetOnProperty, value); }
        }

        public static readonly DependencyProperty IsResetOnProperty =
            DependencyProperty.Register("IsResetOn", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(false));

        public bool IsEditOn
        {
            get { return (bool)GetValue(IsEditOnProperty); }
            set { this.SetValue(IsEditOnProperty, value); }
        }

        public static readonly DependencyProperty IsEditOnProperty =
            DependencyProperty.Register("IsEditOn", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(false));

        public bool IsRendering
        {
            get { return (bool)GetValue(IsRenderingProperty); }
            set { this.SetValue(IsRenderingProperty, value); }
        }

        public static readonly DependencyProperty IsRenderingProperty =
            DependencyProperty.Register("IsRendering", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(false));

        public bool IsOk
        {
            get { return (bool)GetValue(IsOkProperty); }
            set { this.SetValue(IsOkProperty, value); }
        }

        public static readonly DependencyProperty IsOkProperty =
            DependencyProperty.Register("IsOk", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(false, OnOkPropertyChanged));

        private String curveType = "Spline"; // Bezier or Spline
        private List<DijkstraHeap> localDijkstraHeap;
        private int angioImageTotalNum;
        private bool isMoved = false, isDrawing = true;
        private int trackPointNum;
        private int maxPathLength = 0;
        private Image coregiCursor_cross;
        private Image coregiCursor_no_cross;

        public DrawAngioPathUtil()
        {
            InitializeComponent();

            coregiCursor_cross= new Image();
            coregiCursor_cross.Style = (Style)this.FindResource("CoregistrationCursorCross");
            coregiCursor_cross.IsHitTestVisible = false;
            coregiCursor_no_cross = new Image();
            coregiCursor_no_cross.Style = (Style)this.FindResource("CoregistrationCursorNoCross");
            coregiCursor_no_cross.IsHitTestVisible = false;
        }

        #region Method

        private void ActivateEvent()
        {
            canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            canvas.MouseMove += Canvas_MouseMove;
            this.canvas.Background = Brushes.Transparent;
        }

        private void DeactivateEvent()
        {
            canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            canvas.MouseMove -= Canvas_MouseMove;
            this.canvas.Background = null;
        }


        private void ActivateRecEvents(Rectangle rectangle)
        {
            rectangle.Style = (Style)this.Resources["StyleRectangle"];
            rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
            rectangle.MouseLeftButtonUp += Rectangle_MouseLeftButtonUp;
            rectangle.MouseMove += Rectangle_MouseMove;
        }

        private void DeactivateRecEvents()
        {
            for (int i = this.canvas.Children.Count - 1; i >= 0; i--)
            {
                if (this.canvas.Children[i] is Ellipse)
                {
                    Rectangle rectangle = (Rectangle)this.canvas.Children[i];
                    rectangle.MouseLeftButtonDown -= Rectangle_MouseLeftButtonDown;
                    rectangle.MouseLeftButtonUp -= Rectangle_MouseLeftButtonUp;
                    rectangle.MouseMove -= Rectangle_MouseMove;
                }
            }
        }

        private void PathChange(int index)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //frame이 변경 될 때마다 경로 초기화
                InitializePath();
                DrawPath(localDijkstraHeap[index]);
                DrawPDICon(localDijkstraHeap[index]);
            });
        }

        private void TrackPointChange()
        {
            InitializePath();
            DrawTrackPoint();
        }

        private void DrawPDICon(DijkstraHeap dh)
        {
            if(dh.trackPoint.Count < 1)
            {
                return;
            }

            double px, py, dx, dy;
            px = dh.trackPoint[0].X;
            py = dh.trackPoint[0].Y;
            Image proximalImage = new Image();
            proximalImage.Style = (Style)this.FindResource("AngioProximalIcon");

            Canvas.SetLeft(proximalImage, px - proximalImage.Width/2);
            Canvas.SetTop(proximalImage, py - proximalImage.Height);

            if (dh.trackPoint.Count > 1/* 시작점 외 추가 점을 찍은 경우*/)
            {
                dx = dh.trackPoint[dh.trackPoint.Count - 1].X;
                dy = dh.trackPoint[dh.trackPoint.Count - 1].Y;
                Image distalImage = new Image();
                distalImage.Style = (Style)this.FindResource("AngioDistalIcon");

                Canvas.SetLeft(distalImage,dx - distalImage.Width/2);
                Canvas.SetTop(distalImage, dy - distalImage.Height);
                this.canvas.Children.Add(distalImage);
            }
            // Canvas에 이미지 추가
            this.canvas.Children.Add(proximalImage);
        }

        private void InitializePath(bool isPathOnly = false)
        {
            if (isPathOnly)
            {
                for (int i = this.canvas.Children.Count - 1; i >= 0; i--)
                {
                    if (this.canvas.Children[i] is Ellipse)
                    {
                        this.canvas.Children.RemoveAt(i);
                    }
                }
            }
            else
            {
                this.canvas.Children.Clear();
            }
        }

        public void DrawPath(DijkstraHeap dh)
        {
            canvas.Children.Clear();

            //경로 그리기
            foreach (List<Point> pathPoints in dh.line)
            {
                foreach (var pathPoint in pathPoints)
                {
                    Ellipse path = new Ellipse();
                    path.Style = (Style)this.Resources["StylePathEllipse"];
                    Canvas.SetLeft(path, pathPoint.X - path.Width / 2);
                    Canvas.SetTop(path, pathPoint.Y - path.Height / 2);
                    this.canvas.Children.Add(path);
                }
            }

            // 추적된 점 그리기
            int count = 0;
            foreach (Point trackPoint in dh.trackPoint)
            {
                Rectangle rectangle = new Rectangle();
                ActivateRecEvents(rectangle);
                rectangle.Name = $"rectangle{count:D3}";

                Canvas.SetLeft(rectangle, trackPoint.X - rectangle.Width / 2);
                Canvas.SetTop(rectangle, trackPoint.Y - rectangle.Height / 2);
                this.canvas.Children.Add(rectangle);

                count++;
            }
        }

        public void DrawTrackPoint()
        {
            canvas.Children.Clear();

            List<Point> path = new List<Point>();
            foreach (var list in CurrentTrackPoint.Line)
            {
                path.InsertRange(0, list);
            }

            if(path.Count == 0)
            {
                return;
            }

            int pathLength = path.Count;

            if(maxPathLength == 0 || maxPathLength < pathLength) {
                maxPathLength = pathLength;
            }

            double pathLostScale = 2.0 / 3.0;
            if(pathLostScale * maxPathLength > pathLength ) // 길이 값으로 path 잘못 찾았을 경우 거르기
            {
                return;
            }

            //마커 그리기
            Ellipse marker = new Ellipse();
            marker.Style = (Style)this.Resources["StyleTrackEllipse"];
            double rate = (double)CurrentAngioFrameNumber / AngioTrackPoints.Count;
            
            if(pathLength < 0)
            {
                pathLength = 0;
            }
            if (pathLength == path.Count)
            {
                pathLength -= 1;
            }
            int currPos = (int)(pathLength - pathLength * rate);

            Canvas.SetLeft(marker, path[currPos].X - marker.Width / 2);
            Canvas.SetTop(marker, path[currPos].Y - marker.Height / 2);
            this.canvas.Children.Add(marker);
        }

        private void PointTracking(double x, double y, int currFrameNum, int pointPosModifiedIndex = -1 /* all Tracking Point has ID If not -1*/)
        {
            int initialPointX = (int)x;
            int initialPointY = (int)y;
            int templateSize = 100;
            int searchRange = 200;

            List<Mat> Images = new List<Mat>();
            Images.AddRange(AngioImages);

            // 패치 영역이 이미지 경계를 넘지 않도록 조절
            int patchStartX = Math.Max(0, initialPointX - templateSize / 2);
            int patchStartY = Math.Max(0, initialPointY - templateSize / 2);
            int patchEndX = Math.Min(Images[currFrameNum].Width, initialPointX + templateSize / 2);
            int patchEndY = Math.Min(Images[currFrameNum].Height, initialPointY + templateSize / 2);
            int adjustedTemplateWidth = patchEndX - patchStartX;
            int adjustedTemplateHeight = patchEndY - patchStartY;

            Mat initialPatch = new Mat(Images[currFrameNum], new OpenCvSharp.Rect(patchStartX, patchStartY, adjustedTemplateWidth, adjustedTemplateHeight));

            for (int i = 0; i < Images.Count; i++)
            {
                if (i == currFrameNum) 
                    continue;

                Mat newFrame = Images[i].Clone();

                int min_x = Math.Max(initialPointX - searchRange, 0);
                int max_x = Math.Min(initialPointX + searchRange, newFrame.Width);
                int min_y = Math.Max(initialPointY - searchRange, 0);
                int max_y = Math.Min(initialPointY + searchRange, newFrame.Height);

                OpenCvSharp.Rect searchAreaRect = new OpenCvSharp.Rect(min_x, min_y, max_x - min_x, max_y - min_y);
                Mat searchArea = new Mat(newFrame, searchAreaRect);

                Mat result = new Mat();
                Cv2.MatchTemplate(searchArea, initialPatch, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);
                OpenCvSharp.Point top_left = new OpenCvSharp.Point(maxLoc.X + min_x, maxLoc.Y + min_y);

                int trackPointX = (int)top_left.X + adjustedTemplateWidth / 2;
                int trackPointY = (int)top_left.Y + adjustedTemplateHeight / 2;

                if (pointPosModifiedIndex >= 0 /*Coregi Point 위치 수정의 경우*/)
                {
                    localDijkstraHeap[i].trackPoint[pointPosModifiedIndex] = new Point(trackPointX, trackPointY);
                }
                else
                { /*Coregi Point 추가의 경우 */
                    localDijkstraHeap[i].trackPoint.Add(new Point(trackPointX, trackPointY));
                }
            }
        }

        private void ProcessSingleImage(int imageIndex, int movedRecIndex)
        {
            int startX, startY, endX, endY, pathLength;
            int[] vx, vy, pixelValue;

            int movedRecPrevIndex = movedRecIndex == 0 ? 0 : movedRecIndex - 1; // 첫번째 점 수정 : 마지막 점 수정 or 중간 점 수정, 단 점 추가는 항상
            int centerPos = localDijkstraHeap[imageIndex].trackPoint.Count - movedRecIndex >= 2 ? 1 : 0; // 수정할 점이 중간에 있는 경우엔 Path를 두개 변경해야 하므로, centerPos를 초기화.

            for (int trackIndex = movedRecPrevIndex; trackIndex < movedRecIndex + centerPos; trackIndex++)
            {
                vx = new int[localDijkstraHeap[imageIndex].width * localDijkstraHeap[imageIndex].height];
                vy = new int[localDijkstraHeap[imageIndex].width * localDijkstraHeap[imageIndex].height];
                pixelValue = new int[localDijkstraHeap[imageIndex].width * localDijkstraHeap[imageIndex].height];
                startX = (int)localDijkstraHeap[imageIndex].trackPoint[trackIndex].X;
                startY = (int)localDijkstraHeap[imageIndex].trackPoint[trackIndex].Y;
                endX = (int)localDijkstraHeap[imageIndex].trackPoint[trackIndex + 1].X;
                endY = (int)localDijkstraHeap[imageIndex].trackPoint[trackIndex + 1].Y;
                localDijkstraHeap[imageIndex].CalculatePathCost(startX, startY, endX, endY);
                localDijkstraHeap[imageIndex].ReturnPath(endX, endY, vx, vy, out pathLength, pixelValue);
                GenerateCurvePath(vx, vy, pixelValue, imageIndex, pathLength, curveType, trackIndex);
            }
        }

        private async Task ProcessLeftSideAsync(int left, int leftEnd, int movedRecIndex)
        {
            var tasks = new List<Task>();
            for (int i = left; i >= leftEnd; i--)
            {
                int currentIndex = i;
                var tmpTask = Task.Run(() => ProcessSingleImage(currentIndex, movedRecIndex));
                tasks.Add(tmpTask);
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessLeftSideAsync: {ex.Message}");
            }
        }

        private async Task ProcessRightSideAsync(int right, int rightEnd, int movedRecIndex)
        {
            var tasks = new List<Task>();
            for (int i = right; i < rightEnd; i++)
            {
                int currentIndex = i;
                var tmpTask = Task.Run(() => ProcessSingleImage(currentIndex, movedRecIndex));
                tasks.Add(tmpTask);
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessRightSideAsync: {ex.Message}");
            }
        }

        private async Task CalculateAllPathAsync(int currFrameNum, bool leftSideOnly, bool rightSideOnly, int movedRecIndex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsRendering = true;
                IsAngioTrackCompleted = IsResetOn = isDrawing = false;
            });

            Task leftTask = null;
            Task rightTask = null;

            if (!rightSideOnly)
            {
                if (leftSideOnly)
                    leftTask = ProcessLeftSideAsync(currFrameNum, 0, movedRecIndex);
                else
                    leftTask = ProcessLeftSideAsync(currFrameNum - 1, 0, movedRecIndex);
            }

            if (!leftSideOnly)
            {
                rightTask = ProcessRightSideAsync(currFrameNum, angioImageTotalNum, movedRecIndex);
            }

            if (leftTask != null)
                await leftTask;

            if (rightTask != null)
                await rightTask;

            PathChange(currFrameNum); // 현재 프레임 경로 표현

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsRendering = false;
                IsAngioTrackCompleted = IsResetOn = isDrawing = true;
            });
        }

        // Spline
        private void AddSplineCurvePoints(List<Point> points, int frameIndex, int lineIndex)
        { 
            SplineCurve splineCurve = new SplineCurve();
            List<Point> curvePointFs = splineCurve.GetSplinePoints(points, points.Count() * 2/* Spline 곡선을 점 몇개로 표현할 지 설정*/);

            foreach (Point curvexy in curvePointFs)
            {
                localDijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
        }

        // Bezier
        void AddBezierCurvePoints(List<Point> points, int frameIndex, int totalDistance, int lineIndex)
        {
            BezierCurve bezierCurve = new BezierCurve();
            List<Point> curvePointFs = bezierCurve.GenerateBezierCurve(points[0], points[1], points[2], points[3], totalDistance/* Bezier 곡선을 점 몇개로 표현할 지 설정*/);
            foreach (Point curvexy in curvePointFs)
            {
                localDijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
        }

        private void GenerateCurvePath(int[] vx, int[] vy, int[] pixelValue, int frameIndex, int pathLength, string curveType, int lineIndex)
        {
            // line 자체를 List로 가지고 있으면서, Index를 조절하여 어디구간의 경로인지 파악하여 처리해야 됨.
            List<Point> points = new List<Point>();
            int prevIndex, currIndex, distanceLimit, totalDistance, numOfPoints;

            if (lineIndex >= localDijkstraHeap[frameIndex].line.Count) // 새로운 line을 추가했을 시 Add로 초기화
            {
                localDijkstraHeap[frameIndex].line.Add(new List<Point>());
            }
            else // 수정 작업 일 때는 Index에 해당하는 line을 초기화
            {
                localDijkstraHeap[frameIndex].line[lineIndex] = new List<Point>();
            }

            // 모든 점 전달하여 Spline 곡선 형성
            if (curveType == "Spline")
            {
                for (int i = 0; i < pathLength; i++)
                {
                    points.Add(new Point(vx[i], vy[i]));
                }
                AddSplineCurvePoints(points, frameIndex, lineIndex);
            }
            // 베지어의 경우 점을 4개씩 끊어서 전달하여 곡선 형성
            else if (curveType == "Bezier")
            {
                double distanceWeight = 0.2; // 보간을 위한 가중치 -> 클수록 보간할 점 개수가 적어져 곡선 표현이 불가능할 수 있음
                prevIndex = totalDistance = 0;
                numOfPoints = 4; // 가이드 점 개수 (4-2)
                distanceLimit = 10; // 점 간격
                for (currIndex = 0; currIndex < pathLength; currIndex++)
                {
                    if (points.Count == 0)
                    {// 가이드 점이 없는 경우 1개 추가
                        points.Add(new Point(vx[prevIndex], vy[prevIndex]));
                    }
                    else if (currIndex == pathLength - 1 && points.Count < numOfPoints)
                    {// 가이드 점이 3개 이하인데, 경로의 마지막 인덱스에 도달한 경우
                        for (int k = points.Count; k < numOfPoints; k++)
                        {// 가이드 점 마지막 인덱스로 모두 추가 (최대 3개)
                            points.Add(new Point(vx[currIndex], vy[currIndex]));
                        }
                        AddBezierCurvePoints(points, frameIndex, totalDistance - (int)(distanceWeight * totalDistance), lineIndex); // 가이드 점 2개와, 보간에 사용할 점 2개 전달.
                        points.Clear();
                    }
                    else if (pixelValue[currIndex] == 0)
                    {// 픽셀값이 0인 경우 제외 -> 곡선 보간을 통해 그려지는 부분임.
                        continue;
                    }
                    else
                    { // 일반적인 가이드 점 추가
                        // 최근에 추가된 가이드 점과 거리 계산
                        int tmpDistance = (Math.Abs(vx[prevIndex] - vx[currIndex]) + Math.Abs(vy[prevIndex] - vy[currIndex]));
                        if (tmpDistance > distanceLimit)
                        { // 이전 가이드 점과의 거리가 x+y > 10경우에 새로운 가이드 점으로 추가
                            points.Add(new Point(vx[currIndex], vy[currIndex]));
                            //가이드 점 이동
                            prevIndex = currIndex;
                            // 총 거리에 추가 -> 추후 곡선 분할 기준으로 사용
                            totalDistance += tmpDistance;

                            if (points.Count == numOfPoints)
                            { // 가이드 점이 4(2 가이드, 2 보간)개인 경우엔 곡선 그리기.
                                AddBezierCurvePoints(points, frameIndex, totalDistance - (int)(distanceWeight * totalDistance), lineIndex);
                                points.Clear();
                            }
                        }
                    }
                }
            }
        }

        private void MoveCoregistrationCursor(Point mousePosition, Image cursorImg)
        {
            this.canvas.Children.Remove(coregiCursor_cross);
            this.canvas.Children.Remove(coregiCursor_no_cross);

            double x, y;
            x = mousePosition.X;
            y = mousePosition.Y;
            Canvas.SetLeft(cursorImg, x - Constants.coregistrationCursorSize / 2);
            Canvas.SetTop(cursorImg, y - Constants.coregistrationCursorSize / 2);
            this.canvas.Children.Add(cursorImg);
        }

        #endregion

        #region PropertyEvent

        private static void OnAngioImagesPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            var newImages = (List<Mat>)dependencyPropertyChangedEventArgs.NewValue;
            if (newImages.Count > 0) control.angioImageTotalNum = newImages.Count;

            if (control.IsEditOn) control.ActivateEvent();
            else
            {
                control.DeactivateEvent();
                control.DeactivateRecEvents();
            }

            control.localDijkstraHeap = new List<DijkstraHeap>(control.DijkstraHeap);
        }

        private static void OnAngioFrameNumberPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            int AngioFrameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            control.PathChange(AngioFrameNumber);
        }

        private static void OnCurrentAngioFrameNumberPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            int AngioFrameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;

            var drawUtil = dependencyObject as DrawAngioPathUtil;

            if (AngioFrameNumber < 0 || drawUtil == null || drawUtil.AngioTrackPoints == null || drawUtil.AngioTrackPoints.Count == 0 || drawUtil.AngioTrackPoints.Count < AngioFrameNumber) return;

            drawUtil.CurrentTrackPoint = drawUtil.AngioTrackPoints[AngioFrameNumber];
            drawUtil.TrackPointChange();
        }

        private static void OnResetPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                var control = (DrawAngioPathUtil)dependencyObject;
                int currFrameNum = control.AngioFrameNumber;

                if (control.localDijkstraHeap[currFrameNum].trackPoint.Count >= 2) // 경로가 있는 경우
                {
                    foreach (DijkstraHeap heap in control.localDijkstraHeap) // 새로운 경로 받기 위한 초기화
                    {
                        List<List<Point>> newPoints = new List<List<Point>>();
                        for (int i = 0; i < control.localDijkstraHeap[currFrameNum].trackPoint.Count - 1; i++)
                        {
                            newPoints.Add(new List<Point>());
                        }
                        heap.line = newPoints;
                        heap.trackPoint = new List<Point>();
                    }
                }
                else if (control.localDijkstraHeap[currFrameNum].trackPoint.Count == 1) // 경로는 없지만 첫번째 포인트를 찍은 경우
                {
                    foreach (DijkstraHeap heap in control.localDijkstraHeap)
                    {
                        heap.trackPoint = new List<Point>();
                    }
                }
                control.canvas.Children.Clear();
                control.IsReset = control.IsResetOn = false;
            }
        }

        private static void OnOkPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            control.AngioTrackPoints.Clear();

            for (int i = 0; i < control.angioImageTotalNum; i++)
            {
                CoRegistration coRegistration = new CoRegistration();

                foreach(Point point in control.localDijkstraHeap[i].trackPoint)
                {
                    coRegistration.TrackPoint.Add(point);
                }

                int cnt = 0;
                foreach (List<Point> line in control.localDijkstraHeap[i].line)
                {
                    coRegistration.Line.Add(new List<Point>());
                    foreach (Point point in line)
                    {
                        coRegistration.Line[cnt].Add(point);
                    }
                    cnt++;
                }
                control.AngioTrackPoints.Add(coRegistration);
            }

            control.DijkstraHeap = control.localDijkstraHeap;
        }

        #endregion

        #region MouseEvent

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawing)
            {
                return;
            }
            Debug.WriteLine("Canvas_MouseLeftButtonDown");

            int currAngioFrameNumber = AngioFrameNumber;
            Point clickPosition = new Point((float)e.GetPosition(canvas).X, (float)e.GetPosition(canvas).Y);
            Rectangle rectangle = new Rectangle();
            ActivateRecEvents(rectangle);
            rectangle.Name = $"rectangle{localDijkstraHeap[currAngioFrameNumber].trackPoint.Count:D3}";
            Canvas.SetLeft(rectangle, clickPosition.X - Constants.AnnotationRectWidth / 2);
            Canvas.SetTop(rectangle, clickPosition.Y - Constants.AnnotationRectHeight / 2);
            canvas.Children.Add(rectangle);

            // 첫번째 점
            if (localDijkstraHeap[currAngioFrameNumber].trackPoint.Count == 0)
            {
                trackPointNum = 1;
                localDijkstraHeap[currAngioFrameNumber].trackPoint.Add(clickPosition);

                IsResetOn = true;

                PointTracking(clickPosition.X, clickPosition.Y, currAngioFrameNumber);
                PathChange(AngioFrameNumber);
                return;
            }
            // 두번째 점 이후
            else
            {
                trackPointNum = localDijkstraHeap[currAngioFrameNumber].trackPoint.Count;
                _log.Debug("current Track Point Count = " + trackPointNum);
                localDijkstraHeap[currAngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, currAngioFrameNumber);
                PathChange(currAngioFrameNumber);
                trackPointNum++;

                Task.Run(async ()=>
                {
                    await CalculateAllPathAsync(currAngioFrameNumber, false, false, trackPointNum - 1);
                });

            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            MousePosition = e.GetPosition(this.canvas);

            if(!isMoved)
                MoveCoregistrationCursor(e.GetPosition(this.canvas), coregiCursor_cross);
            else
            {
                MoveCoregistrationCursor(e.GetPosition(this.canvas), coregiCursor_no_cross);
            }
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var rectangle = sender as Rectangle;
            if (rectangle != null && IsResetOn)
            {
                rectangle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var rectangle = sender as Rectangle;
            if (rectangle != null)
            {
                rectangle.ReleaseMouseCapture();

                if (isMoved)
                {
                    rectangle.Opacity = 1.0; //visible
                    string numberPart = rectangle.Name.Substring(rectangle.Name.Length - 3);
                    int.TryParse(numberPart, out int index);

                    float x = (float)(Canvas.GetLeft(rectangle) + rectangle.Width / 2);
                    float y = (float)(Canvas.GetTop(rectangle) + rectangle.Height / 2);

                    localDijkstraHeap[AngioFrameNumber].trackPoint[index] = new Point(x, y);

                    ProcessSingleImage(AngioFrameNumber, index);
                    PathChange(AngioFrameNumber);

                    IsOk = true;
                    isMoved = false;
                }
            }
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            var rectangle = sender as Rectangle;
            if (rectangle != null && rectangle.IsMouseCaptured)
            {
                rectangle.Opacity = 0; //invisible
                var mousePosition = e.GetPosition(this.canvas);
                Canvas.SetLeft(rectangle, mousePosition.X - (rectangle.Width / 2));
                Canvas.SetTop(rectangle, mousePosition.Y - (rectangle.Height / 2));

                if (!isMoved)
                {
                    InitializePath(true);
                }
                isMoved = true;
            }
        }

        #endregion
    }
}
