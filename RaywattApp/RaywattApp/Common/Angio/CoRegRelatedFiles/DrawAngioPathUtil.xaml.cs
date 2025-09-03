using log4net;
using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static RaywattOCT.RayCoreWrapper;
using Point = System.Windows.Point;

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

        // OCT Frame Number
        public int FrameNumber
        {
            get { return (int)GetValue(FrameNumberProperty); }
            set { this.SetValue(FrameNumberProperty, value); }
        }

        private static readonly DependencyProperty FrameNumberProperty =
            DependencyProperty.Register("FrameNumber", typeof(int), typeof(DrawAngioPathUtil), new PropertyMetadata(-1, OnFrameNumberPropertyChanged));

        // OCT Number Of Frames
        public int NumberOfFrames
        {
            get { return (int)GetValue(NumberOfFramesProperty); }
            set { this.SetValue(NumberOfFramesProperty, value); }
        }

        private static readonly DependencyProperty NumberOfFramesProperty =
            DependencyProperty.Register("NumberOfFrames", typeof(int), typeof(DrawAngioPathUtil), new PropertyMetadata(null));


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

        public List<CoRegistration> CoRegistrations
        {
            get { return (List<CoRegistration>)GetValue(CoRegistrationsProperty); }
            set { this.SetValue(CoRegistrationsProperty, value); }
        }

        public static readonly DependencyProperty CoRegistrationsProperty =
            DependencyProperty.Register("CoRegistrations", typeof(List<CoRegistration>), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public CoRegistration CurrentCoRegistration
        {
            get { return (CoRegistration)GetValue(CurrentCoRegistrationProperty); }
            set { this.SetValue(CurrentCoRegistrationProperty, value); }
        }

        public static readonly DependencyProperty CurrentCoRegistrationProperty =
            DependencyProperty.Register("CurrentCoRegistration", typeof(CoRegistration), typeof(DrawAngioPathUtil), new PropertyMetadata(null));


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

        public string PullbackType
        {
            get { return (string)GetValue(PullbackTypeProperty); }
            set { this.SetValue(PullbackTypeProperty, value); }
        }

        public static readonly DependencyProperty PullbackTypeProperty =
            DependencyProperty.Register("PullbackType", typeof(string), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        private String curveType = "Spline"; // Bezier or Spline
        private List<DijkstraHeap> localDijkstraHeap;
        private List<CoRegistration> localCoRegistrations;
        private int angioImageTotalNum;
        private bool isMoved;
        private bool isDrawing = true;
        private int trackPointNum;
        private Image coregiCursor_cross;
        private Image coregiCursor_no_cross;
        private List<Superpixel> superpixelList;
        private double markerDrawInterval;
        private int prevOCTFrameNum;
        private int prevAngioFrameNum;
        private int angioPlayDirection = 1;

        public DrawAngioPathUtil()
        {
            InitializeComponent();

            coregiCursor_cross = new Image();
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

        // for use in Coregistration Page
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

        // for use in Review Page
        private void MarkerChange()
        {
            InitializePath();
            DrawTrackPoint();
        }

        // for use in Coregistration Page
        private void DrawPDICon(DijkstraHeap dh)
        {
            if (dh.trackPoints.Count < 1)
            {
                return;
            }

            double px, py, dx, dy;
            px = dh.trackPoints[0].X;
            py = dh.trackPoints[0].Y;
            Image proximalImage = new Image();
            proximalImage.Style = (Style)this.FindResource("AngioProximalIcon");

            Canvas.SetLeft(proximalImage, px - proximalImage.Width / 2);
            Canvas.SetTop(proximalImage, py - proximalImage.Height);

            if (dh.trackPoints.Count > 1/* 시작점 외 추가 점을 찍은 경우*/)
            {
                dx = dh.trackPoints[dh.trackPoints.Count - 1].X;
                dy = dh.trackPoints[dh.trackPoints.Count - 1].Y;
                Image distalImage = new Image();
                distalImage.Style = (Style)this.FindResource("AngioDistalIcon");

                Canvas.SetLeft(distalImage, dx - distalImage.Width / 2);
                Canvas.SetTop(distalImage, dy - distalImage.Height);
                this.canvas.Children.Add(distalImage);
            }
            // Canvas에 이미지 추가
            this.canvas.Children.Add(proximalImage);
        }

        // for use in Coregistration Page
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

        // for use in Coregistration Page
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
            foreach (Point trackPoint in dh.trackPoints)
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

        // for use in Review Page
        public void DrawTrackPoint()
        {
            canvas.Children.Clear();

            List<Point> path = new List<Point>();
            foreach (var list in CurrentCoRegistration.Line)
            {
                // P to D
                path.AddRange(list);
            }

            if (path.Count == 0 || (CurrentCoRegistration.MarkerPoint.X == 0 && CurrentCoRegistration.MarkerPoint.Y == 0))
            {
                return;
            }

            string pullbackType = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                pullbackType = PullbackType;
            });

            int markerIndex = path.IndexOf(CurrentCoRegistration.MarkerPoint);
            if (markerIndex == -1)
            {
                _log.Debug("-------------------marker is not detected #" + CurrentAngioFrameNumber);
                List<CoRegistration> coregistrations = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    coregistrations = CoRegistrations;
                });

                double minDistance = double.MaxValue;
                for (int i = 0; i < path.Count; i++)
                {
                    double tmp = GetDistance(path[i], coregistrations[CurrentAngioFrameNumber].MarkerPoint);
                    if (minDistance > tmp)
                    {
                        minDistance = tmp;
                        CurrentCoRegistration.MarkerPoint = path[i];
                        markerIndex = i;
                    }
                }
            }

            if (angioPlayDirection > 0 /* P to D */ && CurrentAngioFrameNumber > 0)
            {
                markerIndex = markerIndex - GetPathInterval(PullbackType) < 0 ? 0 : markerIndex - GetPathInterval(PullbackType);
            }
            else if (angioPlayDirection < 0 /* D to P */ && CurrentAngioFrameNumber == 0)
            {
                markerIndex = markerIndex + GetPathInterval(PullbackType) < 0 ? 0 : markerIndex + GetPathInterval(PullbackType);
            }

            //마커 그리기
            Ellipse marker = new Ellipse();
            marker.Style = (Style)this.Resources["StyleTrackEllipse"];

            //int pathIndex = markerIndex + (int)markerDrawInterval >= path.Count ? path.Count - 1 : 
            //    markerIndex + (int)markerDrawInterval < 0 ? 0 : markerIndex + (int)markerDrawInterval;

            int pathIndex = markerIndex - (int)markerDrawInterval;
            pathIndex = pathIndex < 0 ? 0 : pathIndex >= path.Count ? path.Count - 1 : pathIndex;

            Canvas.SetLeft(marker, path[pathIndex].X - marker.Width / 2);
            Canvas.SetTop(marker, path[pathIndex].Y - marker.Height / 2);

            this.canvas.Children.Add(marker);

            Ellipse outerMarker = new Ellipse();
            outerMarker.Style = (Style)this.Resources["StyleOuterTrackEllipse"];

            Canvas.SetLeft(outerMarker, path[pathIndex].X - outerMarker.Width / 2);
            Canvas.SetTop(outerMarker, path[pathIndex].Y - outerMarker.Height / 2);

            this.canvas.Children.Add(outerMarker);
        }

        private static int GetPathInterval(string pullbackType)
        {
            double pathInterval = Constants.pathInterval;
            switch (pullbackType)
            {
                case "HISH":
                    pathInterval = pathInterval * Constants.pathIntervalPowerHISH;
                    break;
                case "HILO":
                    pathInterval = pathInterval * Constants.pathIntervalPowerHILO;
                    break;
                case "STSH":
                    pathInterval = pathInterval * Constants.pathIntervalPowerSTSH;
                    break;
                case "STLO":
                    pathInterval = pathInterval * Constants.pathIntervalPowerSTLO;
                    break;
                case "FAST":
                    pathInterval = pathInterval * Constants.pathIntervalPowerFAST;
                    break;
            }

            return (int)pathInterval;
        }

        private void PointTracking(double x, double y, int currFrameNum)
        {
            int initialPointX = (int)x;
            int initialPointY = (int)y;
            int templateSize = 100;
            int searchRange = 100;

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

                localDijkstraHeap[i].trackPoints.Add(new Point(trackPointX, trackPointY));
            }
        }

        private void ProcessSingleImage(int imageIndex, int movedRecIndex)
        {
            int startX, startY, endX, endY, pathLength;
            int[] vx, vy, pixelValue;

            int movedRecPrevIndex = movedRecIndex == 0 ? 0 : movedRecIndex - 1; // 첫번째 점 수정 : 마지막 점 수정 or 중간 점 수정, 단 점 추가는 항상
            int centerPos = localDijkstraHeap[imageIndex].trackPoints.Count - movedRecIndex >= 2 ? 1 : 0; // 수정할 점이 중간에 있는 경우엔 Path를 두개 변경해야 하므로, centerPos를 초기화.

            for (int trackIndex = movedRecPrevIndex; trackIndex < movedRecIndex + centerPos; trackIndex++)
            {
                vx = new int[localDijkstraHeap[imageIndex].width * localDijkstraHeap[imageIndex].height];
                vy = new int[localDijkstraHeap[imageIndex].width * localDijkstraHeap[imageIndex].height];
                pixelValue = new int[localDijkstraHeap[imageIndex].width * localDijkstraHeap[imageIndex].height];
                startX = (int)localDijkstraHeap[imageIndex].trackPoints[trackIndex].X;
                startY = (int)localDijkstraHeap[imageIndex].trackPoints[trackIndex].Y;
                endX = (int)localDijkstraHeap[imageIndex].trackPoints[trackIndex + 1].X;
                endY = (int)localDijkstraHeap[imageIndex].trackPoints[trackIndex + 1].Y;
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
                CoregistrationCompleted();
            });
        }

        // Spline
        private void AddSplineCurvePoints(List<Point> points, int frameIndex, int lineIndex)
        {
            List<Point> curvePointFs = SplineCurve.GetSplinePoints(points, points.Count() * 2/* Spline 곡선을 점 몇개로 표현할 지 설정*/);
            
            foreach (Point curvexy in curvePointFs)
            {
                localDijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
            localDijkstraHeap[frameIndex].line[lineIndex].Reverse();
        }

        // Bezier
        void AddBezierCurvePoints(List<Point> points, int frameIndex, int totalDistance, int lineIndex)
        {
            List<Point> curvePointFs = BezierCurve.GenerateBezierCurve(points[0], points[1], points[2], points[3], totalDistance/* Bezier 곡선을 점 몇개로 표현할 지 설정*/);
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

        public static void SetCoRegistrationMarkers(List<CoRegistration> coRegistrations, List<Mat> angioImages)
        {
            int angioImageNum = coRegistrations.Count;
            List<System.Windows.Point> path = new List<System.Windows.Point>();

            for (int i = 0; i < angioImageNum; i++)
            {
                foreach (List<System.Windows.Point> line in coRegistrations[i].Line)
                {
                    line.Reverse();
                    path.AddRange(line);
                }

                if (i == 0)
                {
                    coRegistrations[i].MarkerPoint = GetMarkerPosition(coRegistrations[i].TrackPoints[0/*Proximal Point*/], path, angioImages[i]);
                }
                else
                {
                    int ratio = path.Count / angioImageNum * i;
                    coRegistrations[i].MarkerPoint = GetMarkerPosition(path[ratio - 1], path, angioImages[i]);
                }
            }
        }

        private static System.Windows.Point GetMarkerPosition(System.Windows.Point centerPoint, List<System.Windows.Point> path, Mat angioImage)
        {
            int searchBoxXY = 15;

            List<(System.Windows.Point position, double avgValue)> list3x3 = new List<(System.Windows.Point, double)>();
            List<(System.Windows.Point position, double avgValue)> list5x5 = new List<(System.Windows.Point, double)>();

            for (int x = (int)centerPoint.X - searchBoxXY; x <= centerPoint.X + searchBoxXY; x++)
            {
                for (int y = (int)centerPoint.Y - searchBoxXY; y <= centerPoint.Y + searchBoxXY; y++)
                {
                    if (x >= 0 && y >= 0 && x < angioImage.Width && y < angioImage.Height)
                    {
                        byte pixelValue = angioImage.At<byte>(y, x);

                        if (pixelValue >= 10 && pixelValue <= 60)
                        {
                            double avg3x3 = CalculateMaskAverage(angioImage, x, y, 3);
                            double avg5x5 = CalculateMaskAverage(angioImage, x, y, 5);

                            list3x3.Add((new System.Windows.Point(x, y), avg3x3));
                            list5x5.Add((new System.Windows.Point(x, y), avg5x5));
                        }
                    }
                }
            }

            if (list3x3.Count == 0 || list5x5.Count == 0)
            {
                return new System.Windows.Point(0, 0);
            }

            list3x3.Sort((a, b) => a.avgValue.CompareTo(b.avgValue));
            list5x5.Sort((a, b) => a.avgValue.CompareTo(b.avgValue));

            Point pos3x3 = list3x3[0].position;
            Point pos5x5 = list5x5[0].position;
            Point marker;

            double taxiDistance = Math.Abs(pos3x3.X - pos5x5.X) + Math.Abs(pos3x3.Y - pos5x5.Y);
            if (taxiDistance >= 4)
            {
                marker = pos3x3;
            }
            else
            {
                double avgX = (pos3x3.X + pos5x5.X) / 2;
                double avgY = (pos3x3.Y + pos5x5.Y) / 2;
                marker = new Point(avgX, avgY);
            }

            Point closestPoint = path[0];
            double minDistance = GetDistance(marker, closestPoint);

            foreach (Point point in path)
            {
                double distance = GetDistance(marker, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point;
                }
            }

            return closestPoint;
        }

        private static double CalculateMaskAverage(Mat image, int centerX, int centerY, int maskSize)
        {
            int halfSize = maskSize / 2;
            double sum = 0;
            int count = 0;

            for (int x = centerX - halfSize; x <= centerX + halfSize; x++)
            {
                for (int y = centerY - halfSize; y <= centerY + halfSize; y++)
                {
                    if (x >= 0 && y >= 0 && x < image.Width && y < image.Height)
                    {
                        sum += image.At<byte>(y, x);
                        count++;
                    }
                }
            }

            return sum / count;
        }

        private static double GetDistance(System.Windows.Point p1, System.Windows.Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private async Task SPProcessingAsync(List<Mat> frames)
        {
            int numSuperpixels = 20 * 20;
            float compactness = 2;
            int maxIterations = 5;
            float alpha = 50;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            superpixelList = new List<Superpixel>();
            for (int i = 0; i < frames.Count; i++)
            {
                superpixelList.Add(new Superpixel());
            }

            try
            {
                Parallel.For(0, frames.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, frameNumber =>
                {
                    Mat frame = frames[frameNumber];
                    Superpixel superpixel = new Superpixel();
                    superpixel.Initialize(numSuperpixels, compactness, maxIterations);
                    superpixel.Fit(frame);
                    superpixelList[frameNumber] = superpixel;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CoRegistrations[frameNumber].MarkerPoint = FindMarkerPosition(frameNumber, CoRegistrations[frameNumber].Line);
                    });
                });
            }
            catch (Exception ex)
            {
                _log.Debug("outer Error message: " + ex.Message);
                _log.Debug("outer Stack trace: " + ex.StackTrace);
            }

            stopwatch.Stop();
            _log.Debug($"Total Running Time : {stopwatch.Elapsed.TotalSeconds} seconds");

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsRendering = false;
                IsAngioTrackCompleted = IsResetOn = isDrawing = true;
            });
        }

        private Point FindMarkerPosition(int currentFrameIdx, List<List<Point>> coregPath)
        {
            try
            {
                // Superpixel의 Segments 지나가는 Path 확인
                List<List<Point>> segmentsList = superpixelList[currentFrameIdx].GetSegmentsPoints();
                int[,] labels = superpixelList[currentFrameIdx].GetLabels();
                List<int> labelList = new List<int>();
                Dictionary<int, List<Point>> labelToPointsMap = new Dictionary<int, List<Point>>();

                foreach (var segment in segmentsList)
                {
                    foreach (var partPath in coregPath)
                    {
                        foreach (var point in partPath)
                        {
                            if (segment.Contains(point))
                            {
                                int label = labels[(int)point.Y, (int)point.X];

                                if (!labelList.Contains(label))
                                {
                                    labelList.Add(label);
                                }

                                if (!labelToPointsMap.TryGetValue(label, out _))
                                {
                                    labelToPointsMap[label] = new List<Point>();
                                }
                                labelToPointsMap[label].Add(point);
                            }
                        }
                    }
                }

                float searchRange = ((float)currentFrameIdx / (float)angioImageTotalNum) * (labelList.Count - 1);
                _log.Debug($"currentFrameIdx = {currentFrameIdx}, searchRange = {searchRange}");
                int lowerIndex = Math.Max(0, (int)Math.Floor(searchRange));
                int upperIndex = Math.Min(labelList.Count - 1, lowerIndex + labelList.Count / angioImageTotalNum + 4);
                int xMax = int.MinValue, yMax = int.MinValue;
                int xMin = int.MaxValue, yMin = int.MaxValue;

                Mat angioImage = AngioImages[currentFrameIdx].Clone();
                List<OpenCvSharp.Point> extractedPoints = new List<OpenCvSharp.Point>();
                List<Point> pathROI = new List<Point>();

                for (int index = lowerIndex; index <= upperIndex; index++)
                {
                    int selectedSemgentLabel = labelList[index];
                    List<Point> segmentPoints = segmentsList[selectedSemgentLabel];

                    pathROI.AddRange(labelToPointsMap[selectedSemgentLabel]);

                    for (int i = 0; i < segmentPoints.Count; i++)
                    {
                        var point = segmentPoints[i];
                        int x = (int)point.X;
                        int y = (int)point.Y;

                        xMax = Math.Max(x, xMax);
                        xMin = Math.Min(x, xMin);
                        yMax = Math.Max(y, yMax);
                        yMin = Math.Min(y, yMin);

                        int intensity = angioImage.At<byte>(y, x);

                        if (intensity >= 35 && intensity <= 65)
                        {
                            extractedPoints.Add(new OpenCvSharp.Point((int)point.X, (int)point.Y));
                        }
                    }
                }

                Mat roi = new Mat(angioImage, new OpenCvSharp.Rect(xMin, yMin, xMax - xMin, yMax - yMin));
                _log.Debug($"roi x = {xMin}, y = {yMin} , width = {xMax - xMin}, height = {yMax - yMin}");
                Cv2.ImWrite($".\\angio\\roi_{currentFrameIdx}.png", roi);

                Mat sobelX = new Mat();
                Mat sobelY = new Mat();
                Cv2.Sobel(roi, sobelX, MatType.CV_64F, 1, 0, ksize: 3);
                Cv2.Sobel(roi, sobelY, MatType.CV_64F, 0, 1, ksize: 3);

                Mat magnitude = new Mat();
                Cv2.Magnitude(sobelX, sobelY, magnitude);

                Cv2.ImWrite($".\\angio\\gredient_{currentFrameIdx}.png", magnitude);

                Point grediantPosition = new Point();
                double maxWeight = 0;

                foreach (var point in extractedPoints)
                {
                    int roiX = point.X - xMin;
                    int roiY = point.Y - yMin;

                    double sumMagnitude = 0;
                    int count = 0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int neighborX = roiX + dx;
                            int neighborY = roiY + dy;

                            if (neighborX >= 0 && neighborX < magnitude.Cols && neighborY >= 0 && neighborY < magnitude.Rows)
                            {
                                double intensity = magnitude.At<double>(neighborY, neighborX);
                                if (intensity >= 200) continue;

                                sumMagnitude += magnitude.At<double>(neighborY, neighborX);
                                count++;
                            }
                        }
                    }

                    double meanMagnitude = sumMagnitude / count;
                    double weight = meanMagnitude / angioImage.At<byte>(point.Y, point.X);

                    if (weight > maxWeight)
                    {
                        maxWeight = weight;
                        grediantPosition = new Point(point.X, point.Y);
                    }
                }

                double minDistance = double.MaxValue;
                Point markerPoint = new Point();

                foreach (var point in pathROI)
                {
                    double distance = GetDistance(point, grediantPosition);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        markerPoint = point;
                    }
                }

                return new Point((int)markerPoint.X, (int)markerPoint.Y);
            }
            catch (Exception ex)
            {
                _log.Debug("Error message: " + ex.Message);
                _log.Debug("Stack trace: " + ex.StackTrace);
                return new Point(0, 0);
            }
        }

        private void MarkerRelocation()
        {
            List<CoRegistration> coregistrations = new List<CoRegistration>();
            Application.Current.Dispatcher.Invoke(() =>
            {
                coregistrations = CoRegistrations;
            });

            Point prevMarkerPoint, currentMarkerPoint;
            prevMarkerPoint = coregistrations[angioImageTotalNum - 1].MarkerPoint;
            for (int i = coregistrations.Count - 1; i >= 0; i--)
            {
                currentMarkerPoint = coregistrations[i].MarkerPoint;
                if (prevMarkerPoint == currentMarkerPoint) continue;

                double distance = GetDistance(prevMarkerPoint, currentMarkerPoint);

                int pointIndex = -1;
                int lineIndex = -1;

                for (int j = 0; j < coregistrations[i].Line.Count; j++)
                {
                    pointIndex = coregistrations[i].Line[j].IndexOf(currentMarkerPoint);
                    if (pointIndex > 0)
                    {
                        lineIndex = j;
                        break;
                    }
                }

                if (distance > 45)
                {
                    double gap = double.MaxValue;
                    while (gap > 30)
                    {
                        Point tmpPoint = coregistrations[i].Line[lineIndex][pointIndex++];
                        gap = GetDistance(tmpPoint, prevMarkerPoint);
                        if (gap <= 30 || pointIndex == coregistrations[i].Line[lineIndex].Count - 1)
                        {
                            coregistrations[i].MarkerPoint = prevMarkerPoint = tmpPoint;
                            break;
                        }
                    }
                }
                else if (distance < 15)
                {
                    double gap = 0;
                    while (gap < 20)
                    {
                        Point tmpPoint = coregistrations[i].Line[lineIndex][pointIndex--];
                        gap = GetDistance(tmpPoint, prevMarkerPoint);
                        if (gap >= 20 || pointIndex == 0)
                        {
                            coregistrations[i].MarkerPoint = prevMarkerPoint = tmpPoint;
                            break;
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CoRegistrations = coregistrations;
            });
        }


        private async Task PredictMarkers(List<CoRegistration> coRegistrations)
        {
            try
            {
                if (coRegistrations == null || coRegistrations.Count == 0)
                {
                    _log.Debug("coRegistrations is null or empty.");
                    return;
                }

                double pathInterval = Constants.pathInterval;
                string pullbackType = "";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    pullbackType = PullbackType;
                });

                pathInterval = GetPathInterval(pullbackType);

                int angioFrameNum = coRegistrations.Count;

                List<Point> tmpPathStart = new List<Point>();
                foreach (var line in coRegistrations[0].Line)
                {
                    tmpPathStart.AddRange(line);
                }
                coRegistrations[0].MarkerPoint = tmpPathStart.First();

                List<Point> tmpPathEnd = new List<Point>();
                foreach (var line in coRegistrations[angioFrameNum - 1].Line)
                {
                    tmpPathEnd.AddRange(line);
                }
                coRegistrations[angioFrameNum - 1].MarkerPoint = tmpPathEnd.Last();

                for (int i = angioFrameNum - 1; i > 1; i--)
                {
                    Point prevMarkerPoint = coRegistrations[i].MarkerPoint;

                    List<Point> currPath = new List<Point>();
                    foreach (var line in coRegistrations[i].Line)
                    {
                        currPath.AddRange(line);
                    }
                    currPath.Reverse(); // Distal 쪽이 0번 인덱스로

                    Point nextMarkerPoint = currPath[Math.Min(currPath.Count - 1, currPath.IndexOf(prevMarkerPoint) + (int)pathInterval)];

                    List<Point> nextPath = new List<Point>();
                    foreach (var line in coRegistrations[i - 1].Line)
                    {
                        nextPath.AddRange(line);
                    }
                    nextPath.Reverse();

                    double minDistance = double.MaxValue;
                    for (int j = 0; j < nextPath.Count; j++)
                    {
                        double tmp = GetDistance(nextPath[j], nextMarkerPoint);
                        if (minDistance > tmp)
                        {
                            minDistance = tmp;
                            coRegistrations[i - 1].MarkerPoint = nextPath[j];
                        }
                    }
                }


                Application.Current.Dispatcher.Invoke(() =>
                {
                    CoRegistrations = coRegistrations;
                    IsRendering = false;
                    IsAngioTrackCompleted = IsResetOn = isDrawing = true;
                });
            }
            catch (Exception ex)
            {
                _log.Debug($"Error : {ex.Message}, Source : {ex.Source}");

            }
        }

        private void ProcessFrameCorrection(int currAngioFrameNumber, List<Mat> angioImages)
        {
            for (int i = currAngioFrameNumber; i > 0; i--)
            {
                TrackPointCorrection(i, -1, angioImages);
            }

            for (int i = currAngioFrameNumber; i < angioImages.Count - 1; i++)
            {
                TrackPointCorrection(i, 1, angioImages);
            }
        }

        private void TrackPointCorrection(int i, int direction, List<Mat> angioImages)
        {
            int numTrackPoints = localDijkstraHeap[i].trackPoints.Count;
            int currX = (int)localDijkstraHeap[i].trackPoints[numTrackPoints - 1].X;
            int currY = (int)localDijkstraHeap[i].trackPoints[numTrackPoints - 1].Y;
            int nextX = (int)localDijkstraHeap[i + direction].trackPoints[numTrackPoints - 1].X;
            int nextY = (int)localDijkstraHeap[i + direction].trackPoints[numTrackPoints - 1].Y;

            int currIntensity = angioImages[i].At<byte>(currY, currX);
            int nextIntensity = angioImages[i + direction].At<byte>(nextY, nextX);

            if (Math.Abs(nextIntensity - currIntensity) > 30 || nextIntensity >= 90)
            {
                int width = angioImages[i].Cols;
                int height = angioImages[i].Rows;

                // gradient, pixelMean for curr Image
                float curr3x3Mean = 0;
                float curr3x3GradientMean = 0;
                Mat currMagnitudeImg = GetGradientMagnitude(angioImages[i].Clone());

                for (int y = currY; y < currY + 3; y++)
                {
                    for (int x = currX; x < currX + 3; x++)
                    {
                        curr3x3Mean += angioImages[i].At<byte>(y, x);
                        curr3x3GradientMean += currMagnitudeImg.At<byte>(y, x);
                    }
                }
                curr3x3Mean /= 9.0f;
                curr3x3GradientMean /= 9.0f;

                // gradient, pixelMean for next Image
                Mat nextMagnitudeImg = GetGradientMagnitude(angioImages[i + direction].Clone());

                // pixel Mean, Gradient Mean, XY Position
                List<Tuple<float, float, Point>> next3x3Mean = new List<Tuple<float, float, Point>>();

                int roiStep = 5;
                int roiCenterX = currX;
                int roiCenterY = currY;
                int xStart = MinMax(width, roiCenterX - roiStep);
                int yStart = MinMax(height, roiCenterY - roiStep);
                int xEnd = MinMax(width, roiCenterX + roiStep);
                int yEnd = MinMax(height, roiCenterY + roiStep);

                if (xEnd - xStart < 5 || yEnd - yStart < 5)
                {
                    // 변위가 충분히 크지 않으면 Pass
                    return;
                }

                for (int y = yStart + 1; y < yEnd - 1; y++)
                {
                    for (int x = xStart + 1; x < xEnd - 1; x++)
                    {
                        float pixelSum = 0.0f;
                        float gradSum = 0.0f;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                pixelSum += angioImages[i + direction].At<byte>(y + dy, x + dx);
                                gradSum += nextMagnitudeImg.At<float>(y + dy, x + dx);
                            }
                        }
                        next3x3Mean.Add(new Tuple<float, float, Point>(pixelSum / 9.0f, gradSum / 9.0f, new Point(x, y)));
                    }
                }

                float minWeight = float.MaxValue;
                Point minPosition = new Point(0, 0);

                for (int k = 0; k < next3x3Mean.Count; k++)
                {
                    float tmpWeight = Math.Abs(next3x3Mean[k].Item1 - curr3x3Mean) + Math.Abs(next3x3Mean[k].Item2 - curr3x3GradientMean);

                    if (tmpWeight < minWeight &&
                        next3x3Mean[k].Item1 < 100 &&
                        angioImages[i + direction].At<byte>((int)next3x3Mean[k].Item3.Y, (int)next3x3Mean[k].Item3.X) < 50)
                    {
                        minPosition = next3x3Mean[k].Item3;
                    }
                }

                if (minPosition.X == 0 && minPosition.X == 0)
                {
                    localDijkstraHeap[i + direction].trackPoints[numTrackPoints - 1] = new Point(roiCenterX, roiCenterY);
                }
                else
                {
                    localDijkstraHeap[i + direction].trackPoints[numTrackPoints - 1] = new Point((int)minPosition.X, (int)minPosition.Y);
                }
            }
        }

        private static int MinMax(int threshold, int value)
        {
            if (value > threshold)
            {
                return threshold;
            }
            else if (value < 0)
            {
                return 0;
            }
            return value;
        }

        private static Mat GetGradientMagnitude(Mat roi)
        {
            Mat sobelX = roi.Clone();
            Mat sobelY = roi.Clone();
            Cv2.Sobel(sobelX, sobelX, MatType.CV_64F, 1, 0, ksize: 3);
            Cv2.Sobel(sobelY, sobelY, MatType.CV_64F, 0, 1, ksize: 3);

            Mat gradMagnitude = new Mat();
            Cv2.Magnitude(sobelX, sobelY, gradMagnitude);
            return gradMagnitude;
        }

        #endregion

        #region PropertyEvent

        private static void OnAngioImagesPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            var newImages = (List<Mat>)dependencyPropertyChangedEventArgs.NewValue;
            if (newImages.Count > 0) control.angioImageTotalNum = newImages.Count;

            control.ActivateEvent();
            control.localDijkstraHeap = new List<DijkstraHeap>(control.DijkstraHeap);
            control.localCoRegistrations = new List<CoRegistration>();
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

            if (AngioFrameNumber < 0 || drawUtil == null || drawUtil.CoRegistrations == null || drawUtil.CoRegistrations.Count == 0 || drawUtil.CoRegistrations.Count < AngioFrameNumber) return;

            if (drawUtil.angioImageTotalNum == 0)
            {
                drawUtil.angioImageTotalNum = drawUtil.CoRegistrations.Count;
            }

            drawUtil.CurrentCoRegistration = drawUtil.CoRegistrations[AngioFrameNumber];
            drawUtil.markerDrawInterval = 0;

            int direction = AngioFrameNumber - drawUtil.prevAngioFrameNum;

            drawUtil.angioPlayDirection = direction > 0 ? 1 : -1;

            if (drawUtil.AngioFrameNumber == 0 && drawUtil.prevAngioFrameNum > 1 /*Angio Last Frame*/)
            {
                drawUtil.angioPlayDirection = 1;
            }
            else if (drawUtil.prevAngioFrameNum == 0 && drawUtil.AngioFrameNumber > 1 /*Angio Last Frame*/)
            {
                drawUtil.angioPlayDirection = -1;
            }

            drawUtil.prevAngioFrameNum = AngioFrameNumber;
            drawUtil.MarkerChange();
        }

        private static void OnFrameNumberPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var drawUtil = dependencyObject as DrawAngioPathUtil;

            if (drawUtil.CoRegistrations == null || drawUtil.CoRegistrations.Count == 0)
                return;

            int direction = drawUtil.FrameNumber - drawUtil.prevOCTFrameNum;

            if (drawUtil.FrameNumber == 0 && drawUtil.NumberOfFrames - 1 == drawUtil.prevOCTFrameNum)
            {
                /*FrameNum = 0, prevOCTFrameNum = 500*/
                direction = -1;
                drawUtil.CurrentCoRegistration = drawUtil.CoRegistrations[0];
            }
            else if (drawUtil.prevOCTFrameNum == 0 && drawUtil.FrameNumber == drawUtil.NumberOfFrames - 1)
            {
                /*FrameNum = 500, prevOCTFrameNum = 0*/
                direction = 1;
                drawUtil.CurrentCoRegistration = drawUtil.CoRegistrations[drawUtil.CoRegistrations.Count - 1];
            }

            drawUtil.markerDrawInterval += drawUtil.GetMarkerInterval() * direction;
            drawUtil.MarkerChange();
            drawUtil.prevOCTFrameNum = drawUtil.FrameNumber;
        }

        private static void OnResetPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                var control = (DrawAngioPathUtil)dependencyObject;
                int currFrameNum = control.AngioFrameNumber;

                if (control.localDijkstraHeap[currFrameNum].trackPoints.Count >= 2) // 경로가 있는 경우
                {
                    foreach (DijkstraHeap heap in control.localDijkstraHeap) // 새로운 경로 받기 위한 초기화
                    {
                        List<List<Point>> newPoints = new List<List<Point>>();
                        for (int i = 0; i < control.localDijkstraHeap[currFrameNum].trackPoints.Count - 1; i++)
                        {
                            newPoints.Add(new List<Point>());
                        }
                        heap.line = newPoints;
                        heap.trackPoints = new List<Point>();
                    }
                }
                else if (control.localDijkstraHeap[currFrameNum].trackPoints.Count == 1) // 경로는 없지만 첫번째 포인트를 찍은 경우
                {
                    foreach (DijkstraHeap heap in control.localDijkstraHeap)
                    {
                        heap.trackPoints = new List<Point>();
                    }
                }
                control.canvas.Children.Clear();
                control.IsReset = control.IsResetOn = false;
            }
        }

        private void CoregistrationCompleted()
        {
            _log.Debug("CoregistrationCompleted()");
            localCoRegistrations = new List<CoRegistration>();
            for (int i = 0; i < angioImageTotalNum; i++)
            {
                CoRegistration coRegistration = new CoRegistration();
                foreach (Point point in localDijkstraHeap[i].trackPoints)
                {
                    coRegistration.TrackPoints.Add(point);
                }

                int cnt = 0;
                foreach (List<Point> line in localDijkstraHeap[i].line)
                {
                    coRegistration.Line.Add(new List<Point>());
                    foreach (Point point in line)
                    {
                        coRegistration.Line[cnt].Add(new Point((int)point.X, (int)point.Y));
                    }
                    cnt++;
                }
                localCoRegistrations.Add(coRegistration);
            }
            CoRegistrations = localCoRegistrations;
            DijkstraHeap = localDijkstraHeap;

            //SetCoRegistrationMarkers(CoRegistrations, AngioImages);
        }

        public double GetMarkerInterval()
        {
            string pullbacktype = "";
            double pathInterval = (Constants.pathInterval) / angioImageTotalNum; // angio frame number : oct frame number
            Application.Current.Dispatcher.Invoke(() =>
            {
                pullbacktype = PullbackType;
            });

            switch (pullbacktype)
            {
                case "HISH":
                    pathInterval = pathInterval * Constants.pathIntervalPowerHISH;
                    break;
                case "HILO":
                    pathInterval = pathInterval * Constants.pathIntervalPowerHILO;
                    break;
                case "STSH":
                    pathInterval = pathInterval * Constants.pathIntervalPowerSTSH;
                    break;
                case "STLO":
                    pathInterval = pathInterval * Constants.pathIntervalPowerSTLO;
                    break;
                case "FAST":
                    pathInterval = pathInterval * Constants.pathIntervalPowerFAST;
                    break;
            }

            return pathInterval;
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
            rectangle.Name = $"rectangle{localDijkstraHeap[currAngioFrameNumber].trackPoints.Count:D3}";
            Canvas.SetLeft(rectangle, clickPosition.X - Constants.AnnotationRectWidth / 2);
            Canvas.SetTop(rectangle, clickPosition.Y - Constants.AnnotationRectHeight / 2);
            canvas.Children.Add(rectangle);

            // 첫번째 점
            if (localDijkstraHeap[currAngioFrameNumber].trackPoints.Count == 0)
            {
                trackPointNum = 1;
                localDijkstraHeap[currAngioFrameNumber].trackPoints.Add(clickPosition);

                IsResetOn = true;

                PointTracking(clickPosition.X, clickPosition.Y, currAngioFrameNumber);
                ProcessFrameCorrection(currAngioFrameNumber, AngioImages);
                PathChange(AngioFrameNumber);
                return;
            }
            // 두번째 점 이후
            else
            {
                trackPointNum = localDijkstraHeap[currAngioFrameNumber].trackPoints.Count;
                localDijkstraHeap[currAngioFrameNumber].trackPoints.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, currAngioFrameNumber);
                ProcessFrameCorrection(currAngioFrameNumber, AngioImages);
                PathChange(currAngioFrameNumber);
                trackPointNum++;

                List<CoRegistration> coRegistrations = null;
                Task.Run(async () =>
                {
                    await CalculateAllPathAsync(currAngioFrameNumber, false, false, trackPointNum - 1);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        coRegistrations = CoRegistrations.ToList();
                    });

                    await PredictMarkers(coRegistrations);
                });
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            MousePosition = e.GetPosition(this.canvas);

            if (!isMoved)
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
                    int currFrameNum = 0;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        currFrameNum = AngioFrameNumber;
                    });

                    rectangle.Opacity = 1.0; //visible
                    string numberPart = rectangle.Name.Substring(rectangle.Name.Length - 3);
                    if (!int.TryParse(numberPart, out int index))
                        index = 0;

                    float x = (float)(Canvas.GetLeft(rectangle) + rectangle.Width / 2);
                    float y = (float)(Canvas.GetTop(rectangle) + rectangle.Height / 2);

                    localDijkstraHeap[currFrameNum].trackPoints[index] = new Point(x, y);

                    ProcessSingleImage(currFrameNum, index);
                    PathChange(currFrameNum);
                    CoregistrationCompleted();

                    List<CoRegistration> coRegistrations = null;
                    Task.Run(async () =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            coRegistrations = CoRegistrations.ToList();
                        });

                        await PredictMarkers(coRegistrations);
                    });

                    IsAngioTrackCompleted = true;
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
