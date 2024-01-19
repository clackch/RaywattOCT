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
using System.Runtime.InteropServices;
using log4net;
using System.Diagnostics;
using System.Threading;

namespace RaywattApp.Common.Angio.CoRegRelatedFiles
{
    /// <summary>
    /// DrawAngioPathUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawAngioPathUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawAngioPathUtil));

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

        public List<Mat> MotionVector
        {
            get { return (List<Mat>)GetValue(MotionVectorProperty); }
            set { this.SetValue(MotionVectorProperty, value); }
        }

        public static readonly DependencyProperty MotionVectorProperty =
            DependencyProperty.Register("MotionVector", typeof(List<Mat>), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

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

        public bool IsCancel
        {
            get { return (bool)GetValue(IsCancelProperty); }
            set { this.SetValue(IsCancelProperty, value); }
        }

        public static readonly DependencyProperty IsCancelProperty =
            DependencyProperty.Register("IsCancel", typeof(bool), typeof(DrawAngioPathUtil), new PropertyMetadata(false, OnCancelPropertyChanged));

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
        private List<DijkstraHeap> dijkstraHeapLegacy;
        private int mainAngioFrameNum, angioImageTotalNum;
        private bool isMoved = false, isDrawing = true;
        private int trackPointNum;
        private CancellationTokenSource cancellationTokenSource;

        public DrawAngioPathUtil()
        {
            InitializeComponent();
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


        private void AddRecEvents(Rectangle rectangle)
        {
            rectangle.Style = (Style)this.Resources["StyleRectangle"];
            rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
            rectangle.MouseLeftButtonUp += Rectangle_MouseLeftButtonUp;
            rectangle.MouseMove += Rectangle_MouseMove;
        }

        public Mat Skeletonize(Mat img)
        {
            Mat skel = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Mat temp = new Mat();
            Mat eroded = new Mat();
            int i = 0;

            var element = Cv2.GetStructuringElement(MorphShapes.Cross, new OpenCvSharp.Size(3, 3));

            bool done;
            do
            {
                i++;
                Cv2.MorphologyEx(img, eroded, MorphTypes.Erode, element); // 침식(Erode)
                Cv2.MorphologyEx(eroded, temp, MorphTypes.Dilate, element); // 팽창(Dilate)
                Cv2.Subtract(img, temp, temp);
                Cv2.BitwiseOr(skel, temp, skel);
                eroded.CopyTo(img);
                if (i == 100) break; // 검은 화면의 경우 무한반복 탈출

                done = (Cv2.CountNonZero(img) == 0);
            } while (!done);

            return skel;
        }

        private void PathChange(int index)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //frame이 변경 될 때마다 경로 초기화
                InitializePath();
                DrawPath(DijkstraHeap[index]);
            });
        }

        private void TrackPointChange()
        {
            InitializePath();

            DrawTrackPoint();
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
                AddRecEvents(rectangle);
                rectangle.Name = $"rectangle{count:D3}";

                Canvas.SetLeft(rectangle, trackPoint.X - rectangle.Width / 2);
                Canvas.SetTop(rectangle, trackPoint.Y - rectangle.Height / 2);
                this.canvas.Children.Add(rectangle);

                count++;
            }
        }

        public void DrawTrackPoint()
        {
            foreach (Point tp in CurrentTrackPoint.TrackPoint)
            {
                Ellipse path = new Ellipse();
                path.Style = (Style)this.Resources["StylePathEllipse"];
                Canvas.SetLeft(path, tp.X - path.Width / 2);
                Canvas.SetTop(path, tp.Y - path.Height / 2);
                this.canvas.Children.Add(path);
            }
        }

        private void ProcessSingleImage(int imageIndex, CancellationToken token, int movedRecIndex)
        {
            int startX, startY, endX, endY, pathLength;
            int[] vx, vy, pixelValue;

            int movedRecPrevIndex = movedRecIndex == 0 ? 0 : movedRecIndex - 1; // 첫번째 점 수정 : 마지막 점 수정 or 중간 점 수정, 단 점 추가는 항상
            int centerPos = trackPointNum - movedRecIndex >= 2 ? 1 : 0; // 수정할 점이 중간에 있는 경우엔 Path를 두개 변경해야 하므로, centerPos를 초기화.

            for (int trackIndex = movedRecPrevIndex; trackIndex < movedRecIndex + centerPos; trackIndex++)
            {
                if (token.IsCancellationRequested) break;

                vx = new int[DijkstraHeap[imageIndex].width * DijkstraHeap[imageIndex].height];
                vy = new int[DijkstraHeap[imageIndex].width * DijkstraHeap[imageIndex].height];
                pixelValue = new int[DijkstraHeap[imageIndex].width * DijkstraHeap[imageIndex].height];
                startX = (int)DijkstraHeap[imageIndex].trackPoint[trackIndex].X;
                startY = (int)DijkstraHeap[imageIndex].trackPoint[trackIndex].Y;
                endX = (int)DijkstraHeap[imageIndex].trackPoint[trackIndex + 1].X;
                endY = (int)DijkstraHeap[imageIndex].trackPoint[trackIndex + 1].Y;
                DijkstraHeap[imageIndex].CalculatePathCost(startX, startY, endX, endY);
                DijkstraHeap[imageIndex].ReturnPath(endX, endY, vx, vy, out pathLength, pixelValue);
                GenerateCurvePath(vx, vy, pixelValue, imageIndex, pathLength, curveType, trackIndex);
            }
        }

        private async Task ProcessLeftSideAsync(int left, int leftEnd, CancellationToken token, int movedRecIndex)
        {
            var tasks = new List<Task>();
            for (int i = left; i >= leftEnd; i--)
            {
                if (token.IsCancellationRequested) break;
                int currentIndex = i;
                var tmpTask = Task.Run(() => ProcessSingleImage(currentIndex, token, movedRecIndex));
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

        private async Task ProcessRightSideAsync(int right, int rightEnd, CancellationToken token, int movedRecIndex)
        {
            var tasks = new List<Task>();
            for (int i = right; i < rightEnd; i++)
            {
                if (token.IsCancellationRequested) break;
                int currentIndex = i;
                var tmpTask = Task.Run(() => ProcessSingleImage(currentIndex, token, movedRecIndex));
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

        private async Task CalculateAllPathAsync(int currFrameNum, bool leftSideOnly, bool rightSideOnly, int movedRecIndex, CancellationToken token)
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
                    leftTask = ProcessLeftSideAsync(currFrameNum, 0, token, movedRecIndex);
                else
                    leftTask = ProcessLeftSideAsync(currFrameNum - 1, 0, token, movedRecIndex);
            }

            if (!leftSideOnly)
            {
                rightTask = ProcessRightSideAsync(currFrameNum, angioImageTotalNum, token, movedRecIndex);
            }

            if (leftTask != null)
                await leftTask;

            if (rightTask != null)
                await rightTask;

            Application.Current.Dispatcher.Invoke(() =>
            {
                DrawPath(DijkstraHeap[currFrameNum]); // 현재 프레임 경로 표현
                IsRendering = false;
                IsAngioTrackCompleted = IsResetOn = isDrawing = true;
            });
        }

        private void CalculateSubPathWhenModified(float x, float y, int index, int currFrameNum)
        {
            int direction = currFrameNum - mainAngioFrameNum;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                if (direction > 0) // 수정된 FrameNumber상 높은 이미지(들)만 -> rightSideOnly
                {
                    PointTracking(x, y, 1, currFrameNum, index);
                    await CalculateAllPathAsync(currFrameNum, false, true, index, token);
                }
                else if (direction < 0) // 수정된 FrameNumber상 낮은 이미지(들)만 -> leftSideOnly
                {
                    PointTracking(x, y, -1, currFrameNum, index);
                    await CalculateAllPathAsync(currFrameNum, true, false, index, token);
                }
                else // 전체
                {
                    PointTracking(x, y, 1, currFrameNum, index);
                    PointTracking(x, y, -1, currFrameNum, index);
                    await CalculateAllPathAsync(currFrameNum, false, false, index, token);
                }
            }, token);
        }

        // Spline
        private void AddSplineCurvePoints(List<Point> points, int frameIndex, int lineIndex)
        {
            SplineCurve splineCurve = new SplineCurve();
            List<Point> curvePointFs = splineCurve.GetSplinePoints(points, points.Count() * 2/* Spline 곡선을 점 몇개로 표현할 지 설정*/);

            foreach (Point curvexy in curvePointFs)
            {
                DijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
        }

        // Bezier
        void AddBezierCurvePoints(List<Point> points, int frameIndex, int totalDistance, int lineIndex)
        {
            BezierCurve bezierCurve = new BezierCurve();
            List<Point> curvePointFs = bezierCurve.GenerateBezierCurve(points[0], points[1], points[2], points[3], totalDistance/* Bezier 곡선을 점 몇개로 표현할 지 설정*/);
            foreach (Point curvexy in curvePointFs)
            {
                DijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
        }

        private void GenerateCurvePath(int[] vx, int[] vy, int[] pixelValue, int frameIndex, int pathLength, string curveType, int lineIndex)
        {
            // line 자체를 List로 가지고 있으면서, Index를 조절하여 어디구간의 경로인지 파악하여 처리해야 됨.
            List<Point> points = new List<Point>();
            int prevIndex, currIndex, distanceLimit, totalDistance, numOfPoints;

            if (lineIndex >= DijkstraHeap[frameIndex].line.Count) // 새로운 line을 추가했을 시 Add로 초기화
            {
                DijkstraHeap[frameIndex].line.Add(new List<Point>());
            }
            else // 수정 작업 일 때는 Index에 해당하는 line을 초기화
            {
                DijkstraHeap[frameIndex].line[lineIndex] = new List<Point>();
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

        private void PointTracking(double x, double y, int direction, int currFrameNum, int pointPosModifiedIndex = -1)
        {
            Point prevPoint = new Point(x, y);
            int halfSize = 5; // halfSize*2 x halfSize*2 크기 -> 최적 파라미터 찾을 필요 있음. todo
            int startIndex;

            if (direction == -1) // 방향에 따른 MotionVector의 Index 초기화
            {
                startIndex = currFrameNum - 1;
            }
            else
            {
                startIndex = currFrameNum; // +1이 붙지 않는 이유는, MotionVector가 한장 모자르고, 이를 인덱싱하기 위해서 하지 않음.
            }

            for (int i = startIndex; i < MotionVector.Count && i >= 0; i += direction)
            {
                Vec2f sumVector = new Vec2f(0, 0);
                int count = 0;

                // 주어진 점을 중심으로 halfSize*2 x halfSize*2 영역 내의 모션 벡터의 누적합 구하기
                for (int yy = -halfSize; yy <= halfSize; yy++)
                {
                    for (int xx = -halfSize; xx <= halfSize; xx++)
                    {
                        int newX = (int)prevPoint.X + xx;
                        int newY = (int)prevPoint.Y + yy;

                        if (newX >= 0 && newX < MotionVector[i].Cols && newY >= 0 && newY < MotionVector[i].Rows)
                        {
                            Vec2f vector = MotionVector[i].At<Vec2f>(newY, newX);
                            sumVector.Item0 += vector.Item0;
                            sumVector.Item1 += vector.Item1;
                            count++;
                        }
                    }
                }

                Vec2f averageVector = new Vec2f(sumVector.Item0 / count, sumVector.Item1 / count);

                Point currPoint = new Point(prevPoint.X + (direction) * averageVector.Item0, prevPoint.Y + (direction) * averageVector.Item1);

                //Check Boundary
                if (currPoint.X < 0) currPoint.X = 0;
                if (currPoint.X > Constants.AngioSize) currPoint.X = (float)Constants.AngioSize;
                if (currPoint.Y < 0) currPoint.Y = 0;
                if (currPoint.Y > Constants.AngioSize) currPoint.Y = (float)Constants.AngioSize;

                // trackPoint의 방향에 따른 인덱싱 처리
                if (direction == 1)
                {
                    if (pointPosModifiedIndex >= 0)
                        DijkstraHeap[i + 1].trackPoint[pointPosModifiedIndex] = currPoint;
                    else
                        DijkstraHeap[i + 1].trackPoint.Add(currPoint);
                }
                else
                {
                    if (pointPosModifiedIndex >= 0)
                        DijkstraHeap[i].trackPoint[pointPosModifiedIndex] = currPoint;
                    else
                        DijkstraHeap[i].trackPoint.Add(currPoint);
                }
                prevPoint = currPoint;
            }
        }
        private void setDHLegacy()
        {
            dijkstraHeapLegacy = new List<DijkstraHeap>();
            for (int i = 0; i < DijkstraHeap.Count; i++)
            {
                dijkstraHeapLegacy.Add(null);
                dijkstraHeapLegacy[i] = DijkstraHeap[i];
            }
        }

        private void getDHLegacy()
        {
            for (int i = 0; i < dijkstraHeapLegacy.Count; i++)
            {
                DijkstraHeap[i] = dijkstraHeapLegacy[i];
            }
        }

        private void CancelTask()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        #endregion

        #region PropertyEvent

        private static void OnAngioImagesPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            var newImages = (List<Mat>)dependencyPropertyChangedEventArgs.NewValue;
            if (newImages.Count > 0) control.angioImageTotalNum = newImages.Count;

            if (control.IsEditOn) control.ActivateEvent();
            else control.DeactivateEvent();
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

            if (AngioFrameNumber < 0 || drawUtil == null || drawUtil.AngioTrackPoints == null || drawUtil.AngioTrackPoints.Count < AngioFrameNumber) return;

            drawUtil.CurrentTrackPoint = drawUtil.AngioTrackPoints[AngioFrameNumber];
            drawUtil.TrackPointChange();
        }

        private static void OnResetPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                var control = (DrawAngioPathUtil)dependencyObject;
                int currFrameNum = control.AngioFrameNumber;

                if (control.DijkstraHeap[currFrameNum].trackPoint.Count >= 2) // 경로가 있는 경우
                {
                    control.setDHLegacy();

                    foreach (DijkstraHeap heap in control.DijkstraHeap) // 새로운 경로 받기 위한 초기화
                    {
                        List<List<Point>> newPoints = new List<List<Point>>();
                        for (int i = 0; i < control.trackPointNum-1; i++)
                        {
                            newPoints.Add(new List<Point>());
                        }
                        heap.line = newPoints;
                        heap.trackPoint = new List<Point>();
                    }
                }
                else if (control.DijkstraHeap[currFrameNum].trackPoint.Count == 1) // 경로는 없지만 첫번째 포인트를 찍은 경우
                {
                    foreach (DijkstraHeap heap in control.DijkstraHeap) 
                    {
                        heap.trackPoint = new List<Point>();
                    }
                }
                control.canvas.Children.Clear();
                control.IsReset = control.IsResetOn = false;
            }
        }

        private static void OnCancelPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            control.CancelTask();

            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                if (control.dijkstraHeapLegacy == null)
                {
                    return;
                }
                else
                {
                    control.getDHLegacy();
                }
            }
        }

        private static void OnOkPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            control.AngioTrackPoints.Clear();

            for (int i = 0; i<control.angioImageTotalNum; i++)
            {
                CoRegistration coRegistration= new CoRegistration();
                coRegistration.TrackPoint = new List<Point>();

                foreach (List<Point> trackPoint in control.DijkstraHeap[i].line)
                {
                    foreach(Point point in trackPoint)
                    {
                        coRegistration.TrackPoint.Add(point);
                    }
                }
                control.AngioTrackPoints.Add(coRegistration);
            }
        }

        #endregion

        #region MouseEvent

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(!isDrawing)
            {
                return;
            }
            Debug.WriteLine("Canvas_MouseLeftButtonDown");

            Point clickPosition = new Point((float)e.GetPosition(canvas).X, (float)e.GetPosition(canvas).Y);
            Rectangle rectangle = new Rectangle();
            AddRecEvents(rectangle);
            rectangle.Name = $"rectangle{DijkstraHeap[AngioFrameNumber].trackPoint.Count:D3}";
            Canvas.SetLeft(rectangle, clickPosition.X - Constants.AnnotationRectWidth / 2);
            Canvas.SetTop(rectangle, clickPosition.Y - Constants.AnnotationRectHeight / 2);
            canvas.Children.Add(rectangle);

            int imageLength = AngioImages.Count;
            int currFrameNum = mainAngioFrameNum = AngioFrameNumber;
            

            // 첫번째 점
            if (DijkstraHeap[AngioFrameNumber].trackPoint.Count == 0)
            {
                trackPointNum = 1;
                DijkstraHeap[AngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, 1, currFrameNum);
                PointTracking(clickPosition.X, clickPosition.Y, -1, currFrameNum);

                IsResetOn = true;
                return;
            }
            // 두번째 점 이후
            else
            {
                DijkstraHeap[AngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, 1, currFrameNum);
                PointTracking(clickPosition.X, clickPosition.Y, -1, currFrameNum);
                trackPointNum++;

                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                Task.Run(async () =>
                {
                    await CalculateAllPathAsync(currFrameNum, false, false, trackPointNum - 1, token);
                }, token);
                                
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            MousePosition = e.GetPosition(this.canvas);
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
                    string numberPart = rectangle.Name.Substring(rectangle.Name.Length - 3);
                    int.TryParse(numberPart, out int index);
                    
                    float x = (float)(Canvas.GetLeft(rectangle) + rectangle.Width / 2);
                    float y = (float)(Canvas.GetTop(rectangle) + rectangle.Height / 2);
                    
                    DijkstraHeap[AngioFrameNumber].trackPoint[index] = new Point(x, y);

                    CalculateSubPathWhenModified(x, y, index, AngioFrameNumber);
                    isMoved = false;
                }
            }
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            var rectangle = sender as Rectangle;
            if (rectangle != null && rectangle.IsMouseCaptured)
            {
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
