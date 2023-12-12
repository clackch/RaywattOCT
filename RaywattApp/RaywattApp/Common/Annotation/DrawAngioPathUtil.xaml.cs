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
using RaywattApp.Common.Annotation.LiveWire;
using LiveWire;
using log4net;
using RaywattApp.Common.Angio;
using System.Diagnostics;
using System.Threading;

namespace RaywattApp.Common.Annotation
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
        public List<Mat> AngioImages
        {
            get { return (List<Mat>)GetValue(AngioImagesProperty); }
            set { this.SetValue(AngioImagesProperty, value); }
        }

        public static readonly DependencyProperty AngioImagesProperty =
            DependencyProperty.Register("AngioImages", typeof(List<Mat>), typeof(DrawAngioPathUtil), new PropertyMetadata(null, OnAngioImagesPropertyChanged));

        public Point MousePosition
        {
            get { return (Point)GetValue(MousePositionProperty); }
            set { this.SetValue(MousePositionProperty, value); }
        }

        public static readonly DependencyProperty MousePositionProperty =
            DependencyProperty.Register("MousePosition", typeof(Point), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public List<CoRegistration> AngioTrackPoints
        {
            get { return (List<CoRegistration>)GetValue(AngioTrackPointsProperty); }
            set { this.SetValue(AngioTrackPointsProperty, value); }
        }

        public static readonly DependencyProperty AngioTrackPointsProperty =
            DependencyProperty.Register("AngioTrackPoints", typeof(List<CoRegistration>), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

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
        private List<DijkstraHeap> dijkstraHeap;
        private List<DijkstraHeap> dijkstraHeapLegacy;
        private List<Mat> motionVector;
        private int mainAngioFrameNum, angioImageTotalNum;
        private bool isMoved = false, isDrawing = true;
        private int trackPointNum;
        private CancellationTokenSource cancellationTokenSource;

        public DrawAngioPathUtil()
        {
            InitializeComponent();
            ActivateEvent();
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
            rectangle.MouseEnter += Rectangle_MouseEnter;
            rectangle.MouseLeave += Rectangle_MouseLeave;
            rectangle.MouseMove += Rectangle_MouseMove;
        }

        private void ImageProcessing(List<Mat> frames)
        {
            Mat prevEqualImg = null, currEqualImg;

            foreach (var frame in frames)
            {
                Mat blurredImage = new Mat();
                Cv2.Blur(frame, blurredImage, new OpenCvSharp.Size(7, 7));

                // HE 영역 분할 처리
                Mat equalizedImage = new Mat();
                var clahe = Cv2.CreateCLAHE(clipLimit: 10, new OpenCvSharp.Size(9, 9));
                clahe.Apply(blurredImage, equalizedImage);

                currEqualImg = equalizedImage.Clone();
                if (prevEqualImg != null)
                {
                    CalculateMotionVector(prevEqualImg, currEqualImg); // Constants.AngioSize Square 
                }
                prevEqualImg = equalizedImage.Clone();

                // 픽셀 100 미만 값 -> 255, 픽셀 100 이상 값 -> 0
                Mat thresholdImage = new Mat();
                Cv2.Threshold(equalizedImage, thresholdImage, 100, 255, ThresholdTypes.BinaryInv);

                // 이미지 변형(분할 : Segmentation) 처리
                Mat morphedImage = new Mat();
                var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
                Cv2.MorphologyEx(thresholdImage, morphedImage, MorphTypes.Open, kernel, iterations: 2);

                // 변형 처리 반복 -> 스켈레톤(골격화)
                Mat skeleton = new Mat();
                skeleton = Skeletonize(morphedImage);

                byte[] imageData = new byte[frame.Rows * frame.Cols * frame.ElemSize()];
                Marshal.Copy(skeleton.Data, imageData, 0, imageData.Length);

                dijkstraHeap.Add(new DijkstraHeap(imageData, frame.Rows, frame.Cols));
            }
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
                DrawPath(dijkstraHeap[index]);
            });
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
            //선 그리기
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

        private void ProcessSingleImage(int imageIndex, CancellationToken token, int movedRecIndex)
        {
            Debug.WriteLine("imageIndex =" + imageIndex.ToString());
            int startX, startY, endX, endY, pathLength;
            int[] vx, vy, pixelValue;

            int movedRecPrevIndex = movedRecIndex == 0 ? 0 : movedRecIndex - 1; // 첫번째 점 수정 : 마지막 점 수정 or 중간 점 수정, 단 점 추가는 항상
            int centerPos = trackPointNum - movedRecIndex >= 2 ? 1 : 0; // 수정할 점이 중간에 있는 경우엔 Path를 두개 변경해야 하므로, centerPos를 초기화.

            for (int trackIndex = movedRecPrevIndex; trackIndex < movedRecIndex + centerPos; trackIndex++)
            {
                if (token.IsCancellationRequested) break;

                vx = new int[dijkstraHeap[imageIndex].width * dijkstraHeap[imageIndex].height];
                vy = new int[dijkstraHeap[imageIndex].width * dijkstraHeap[imageIndex].height];
                pixelValue = new int[dijkstraHeap[imageIndex].width * dijkstraHeap[imageIndex].height];
                startX = (int)dijkstraHeap[imageIndex].trackPoint[trackIndex].X;
                startY = (int)dijkstraHeap[imageIndex].trackPoint[trackIndex].Y;
                endX = (int)dijkstraHeap[imageIndex].trackPoint[trackIndex + 1].X;
                endY = (int)dijkstraHeap[imageIndex].trackPoint[trackIndex + 1].Y;
                dijkstraHeap[imageIndex].CalculatePathCost(startX, startY, endX, endY);
                dijkstraHeap[imageIndex].ReturnPath(endX, endY, vx, vy, out pathLength, pixelValue);
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
                    leftTask = ProcessLeftSideAsync(currFrameNum-1, 0, token, movedRecIndex);
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
                DrawPath(dijkstraHeap[currFrameNum]); // 현재 프레임 경로 표현
                IsRendering = false;
                IsAngioTrackCompleted = IsResetOn = isDrawing = true;
            });
        }

        private void CalculateSubPathWhenModified(float x, float y, int index, int currFrameNum)
        {
            IsAngioTrackCompleted = IsResetOn = false;
            IsRendering = true;

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
                    await CalculateAllPathAsync(currFrameNum, false, false, index, token);
                }
            }, token);

            IsRendering = false;
            IsAngioTrackCompleted = IsResetOn = true;
        }

        // Spline
        private void AddSplineCurvePoints(List<Point> points, int frameIndex, int lineIndex)
        {
            SplineCurve splineCurve = new SplineCurve();
            List<Point> curvePointFs = splineCurve.GetSplinePoints(points, points.Count() * 2/* Spline 곡선을 점 몇개로 표현할 지 설정*/);

            foreach (Point curvexy in curvePointFs)
            {
                dijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
        }

        // Bezier
        void AddBezierCurvePoints(List<Point> points, int frameIndex, int totalDistance, int lineIndex)
        {
            BezierCurve bezierCurve = new BezierCurve();
            List<Point> curvePointFs = bezierCurve.GenerateBezierCurve(points[0], points[1], points[2], points[3], totalDistance/* Bezier 곡선을 점 몇개로 표현할 지 설정*/);
            foreach (Point curvexy in curvePointFs)
            {
                dijkstraHeap[frameIndex].line[lineIndex].Add(curvexy);
            }
        }

        private void GenerateCurvePath(int[] vx, int[] vy, int[] pixelValue, int frameIndex, int pathLength, string curveType, int lineIndex)
        {
            // line 자체를 List로 가지고 있으면서, Index를 조절하여 어디구간의 경로인지 파악하여 처리해야 됨.
            List<Point> points = new List<Point>();
            int prevIndex, currIndex, distanceLimit, totalDistance, numOfPoints;

            if (lineIndex >= dijkstraHeap[frameIndex].line.Count) // 배열 크기를 넘어선 경우 추가 작업
            {
                dijkstraHeap[frameIndex].line.Add(new List<Point>());
            }
            else // 수정 작업
            {
                dijkstraHeap[frameIndex].line[lineIndex] = new List<Point>();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
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
            });
        }

        private void CalculateMotionVector(Mat prevFrame, Mat nextFrame)
        {
            Mat flow = new Mat();
            Cv2.CalcOpticalFlowFarneback(prevFrame, nextFrame, flow, 0.5, 5, 21, 7, 5, 1.1, 0);

            //curr, next: 이전 영상과 현재 영상. 그레이스케일 영상.
            //flow: (출력)계산된 옵티컬플로우.np.ndarray.shape = (h, w, 2(for x, y vector)), dtype = np.float32.
            //pyr_scale: 피라미드 영상을 만들 때 축소 비율. (e.g.) 0.5 ~0.7, 클수록 계산량감소, 오차확률 상승
            //levels: 피라미드 영상 개수. (e.g.) 3
            //winsize: 평균 윈도우 크기. (e.g.) 15 ~21
            //iterations: 각 피라미드 레벨에서 알고리즘 반복 횟수. (e.g.) 3 다다익선(tradeOff -> 계산량)
            //poly_n: 다항식 확장을 위한 이웃 픽셀 크기. 보통 5 또는 7.
            //poly_sigma: 가우시안 표준편차. 보통 poly_n = 5-> 1.1, poly_n = 7-> 1.5.
            //flags: 0, cv2.OPTFLOW_USE_INITIAL_FLOW, cv2.OPTFLOW_FARNEBACK_GAUSSIAN.

            motionVector.Add(flow);
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
                startIndex = currFrameNum;
            }

            for (int i = startIndex; i < motionVector.Count && i >= 0; i += direction)
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

                        if (newX >= 0 && newX < motionVector[i].Cols && newY >= 0 && newY < motionVector[i].Rows)
                        {
                            Vec2f vector = motionVector[i].At<Vec2f>(newY, newX);
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
                        dijkstraHeap[i + 1].trackPoint[pointPosModifiedIndex] = currPoint;
                    else
                        dijkstraHeap[i + 1].trackPoint.Add(currPoint);
                }
                else
                {
                    if (pointPosModifiedIndex >= 0)
                        dijkstraHeap[i].trackPoint[pointPosModifiedIndex] = currPoint;
                    else
                        dijkstraHeap[i].trackPoint.Add(currPoint);
                }
                prevPoint = currPoint;
            }
        }
        private void setDHLegacy()
        {
            dijkstraHeapLegacy = new List<DijkstraHeap>();
            for (int i = 0; i < dijkstraHeap.Count; i++)
            {
                dijkstraHeapLegacy.Add(null);
                dijkstraHeapLegacy[i] = dijkstraHeap[i];
            }
        }

        private void getDHLegacy()
        {
            for (int i = 0; i < dijkstraHeapLegacy.Count; i++)
            {
                dijkstraHeap[i] = dijkstraHeapLegacy[i];
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
            if (newImages.Count > 0)
            {
                control.dijkstraHeap = new List<DijkstraHeap>();
                control.motionVector = new List<Mat>();
                control.angioImageTotalNum = newImages.Count;
                control.ImageProcessing(newImages);
            }
        }

        private static void OnAngioFrameNumberPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            int AngioFrameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            control.PathChange(AngioFrameNumber);
        }

        private static void OnResetPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                var control = (DrawAngioPathUtil)dependencyObject;
                int currFrameNum = control.AngioFrameNumber;

                if (control.dijkstraHeap[currFrameNum].trackPoint.Count >= 2) // 경로가 있는 경우
                {
                    control.setDHLegacy();

                    foreach (DijkstraHeap heap in control.dijkstraHeap) // 새로운 경로 받기 위한 초기화
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
                else if (control.dijkstraHeap[currFrameNum].trackPoint.Count == 1) // 경로는 없지만 첫번째 포인트를 찍은 경우
                {
                    foreach (DijkstraHeap heap in control.dijkstraHeap) 
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

                foreach (List<Point> trackPoint in control.dijkstraHeap[i].line)
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

            Point clickPosition = new Point((float)e.GetPosition(canvas).X, (float)e.GetPosition(canvas).Y);
            Rectangle rectangle = new Rectangle();
            AddRecEvents(rectangle);
            Canvas.SetLeft(rectangle, clickPosition.X - Constants.AnnotationRectWidth / 2);
            Canvas.SetTop(rectangle, clickPosition.Y - Constants.AnnotationRectHeight / 2);
            canvas.Children.Add(rectangle);

            int imageLength = AngioImages.Count;
            int currFrameNum = mainAngioFrameNum = AngioFrameNumber;
            

            // 첫번째 점
            if (dijkstraHeap[AngioFrameNumber].trackPoint.Count == 0)
            {
                trackPointNum = 1;
                dijkstraHeap[AngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, 1, currFrameNum);
                PointTracking(clickPosition.X, clickPosition.Y, -1, currFrameNum);

                IsResetOn = true;
                return;
            }
            // 두번째 점 이후
            else
            {
                dijkstraHeap[AngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, 1, currFrameNum);
                PointTracking(clickPosition.X, clickPosition.Y, -1, currFrameNum);
                trackPointNum++;

                //IsAngioTrackCompleted = IsResetOn = false;
                //IsRendering = true;

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

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            this.canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
        }

        private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            this.canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var rectangle = sender as Rectangle;
            if (rectangle != null && IsResetOn)
            {
                Debug.WriteLine("Rectangle_MouseLeftButtonDown");
                rectangle.CaptureMouse();
            }
        }

        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var rectangle = sender as Rectangle;
            if (rectangle != null)
            {
                Debug.WriteLine("Rectangle_MouseLeftButtonUp");
                rectangle.ReleaseMouseCapture();

                if (isMoved)
                {
                    string numberPart = rectangle.Name.Substring(rectangle.Name.Length - 3);
                    int.TryParse(numberPart, out int index);
                    
                    float x = (float)(Canvas.GetLeft(rectangle) + rectangle.Width / 2);
                    float y = (float)(Canvas.GetTop(rectangle) + rectangle.Height / 2);
                    
                    dijkstraHeap[AngioFrameNumber].trackPoint[index] = new Point(x, y);

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
