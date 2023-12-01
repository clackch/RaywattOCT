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
using PointF = System.Drawing.PointF;
using RaywattApp.Common.Bases;
using OpenCvSharp;
using System.Runtime.InteropServices;
using RaywattApp.Common.Annotation.LiveWire;
using LiveWire;
using log4net;
using RaywattApp.Common.Annotation.Models;
using System.Diagnostics;

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

        public List<AngioFrame> AngioTrackPoints
        {
            get { return (List<AngioFrame>)GetValue(AngioTrackPointsProperty); }
            set { this.SetValue(AngioTrackPointsProperty, value); }
        }

        public static readonly DependencyProperty AngioTrackPointsProperty =
            DependencyProperty.Register("AngioTrackPoints", typeof(List<AngioFrame>), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

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

        private String curveType = "Spline"; // Bezier or Spline
        private BezierCurve bezierCurve;
        private SplineCurve splineCurve;
        private List<DijkstraHeap> dijkstraHeap;
        private List<DijkstraHeap> dijkstraHeapLegacy;
        private List<Mat> motionVector;

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

        private void WireChange(int index)
        {
            //frame이 변경 될 때마다 경로 초기화
            InitializePath();

            //기존에 탐색했던 경로를 다시 그림
            DrawWire(dijkstraHeap[index]);
        }

        private void InitializePath()
        {
            this.canvas.Children.Clear();
        }

        public void DrawWire(DijkstraHeap dh, bool isFirstFrame = false)
        {
            //선 그리기
            foreach (PointF pathPoint in dh.line)
            {
                Ellipse path = new Ellipse();
                path.Style = (Style)this.Resources["StylePathEllipse"];
                Canvas.SetLeft(path, pathPoint.X - path.Width / 2);
                Canvas.SetTop(path, pathPoint.Y - path.Height / 2);
                this.canvas.Children.Add(path);
            }


            if (isFirstFrame) return;
            // 추적된 점 그리기
            foreach (PointF trackPoint in dh.trackPoint)
            {
                Rectangle rectangle = new Rectangle();
                AddRecEvents(rectangle);
                Canvas.SetLeft(rectangle, trackPoint.X - rectangle.Width / 2);
                Canvas.SetTop(rectangle, trackPoint.Y - rectangle.Height / 2);
                this.canvas.Children.Add(rectangle);
            }
        }

        private void CalculateAllPath(int currFrameNum, int imageLength, int gapValue)
        {
            int gapMinus = currFrameNum - gapValue > 0 ? currFrameNum - gapValue : 0;
            int gapPlus = currFrameNum + gapValue < imageLength - 1 ? currFrameNum + gapValue : imageLength - 1;

            if (gapValue == 0) // 현재 프레임 표현을 위한 값조정.
            {
                gapPlus += 1;
            }

            for (int angioIndex = gapMinus; angioIndex < gapPlus; angioIndex++)
            {
                if (gapValue != 0 && angioIndex == currFrameNum) continue; // 현재 프레임은 호출부 밖에서 먼저 계산하고 표현.

                for (int numOfTrackPoint = 0; numOfTrackPoint < dijkstraHeap[angioIndex].trackPoint.Count - 1; numOfTrackPoint++)
                {
                    int[] vx = new int[dijkstraHeap[angioIndex].width * dijkstraHeap[angioIndex].height];
                    int[] vy = new int[dijkstraHeap[angioIndex].width * dijkstraHeap[angioIndex].height];
                    int startX = (int)dijkstraHeap[angioIndex].trackPoint[numOfTrackPoint].X;
                    int startY = (int)dijkstraHeap[angioIndex].trackPoint[numOfTrackPoint].Y;
                    int endX = (int)dijkstraHeap[angioIndex].trackPoint[numOfTrackPoint + 1].X;
                    int endY = (int)dijkstraHeap[angioIndex].trackPoint[numOfTrackPoint + 1].Y;
                    int[] pixelValue = new int[dijkstraHeap[angioIndex].width * dijkstraHeap[angioIndex].height];
                    int pathLength = 0;
                    List<PointF> points = new List<PointF>();
                    int prevIndex, currIndex, distanceLimit, totalDistance, numOfPoints;

                    dijkstraHeap[angioIndex].run(startX, startY, endX, endY);
                    dijkstraHeap[angioIndex].returnPath(endX, endY, vx, vy, out pathLength, pixelValue);

                    bezierCurve = new BezierCurve();
                    splineCurve = new SplineCurve();

                    // 모든 점 전달하여 Spline 곡선 형성
                    if (curveType == "Spline")
                    {
                        for (int i = 0; i < pathLength; i++)
                        {
                            points.Add(new PointF(vx[i], vy[i]));
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AddSplineCurvePoints(points, angioIndex);
                        });
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
                                points.Add(new PointF(vx[prevIndex], vy[prevIndex]));
                            }
                            else if (currIndex == pathLength - 1 && points.Count < numOfPoints)
                            {// 가이드 점이 3개 이하인데, 경로의 마지막 인덱스에 도달한 경우
                                for (int k = points.Count; k < numOfPoints; k++)
                                {// 가이드 점 마지막 인덱스로 모두 추가 (최대 3개)
                                    points.Add(new PointF(vx[currIndex], vy[currIndex]));
                                }
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    AddBezierCurvePoints(points, angioIndex, totalDistance - (int)(distanceWeight * totalDistance)); // 가이드 점 2개와, 보간에 사용할 점 2개 전달.
                                });
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
                                    points.Add(new PointF(vx[currIndex], vy[currIndex]));
                                    //가이드 점 이동
                                    prevIndex = currIndex;
                                    // 총 거리에 추가 -> 추후 곡선 분할 기준으로 사용
                                    totalDistance += tmpDistance;

                                    if (points.Count == numOfPoints)
                                    { // 가이드 점이 4(2 가이드, 2 보간)개인 경우엔 곡선 그리기.
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            AddBezierCurvePoints(points, angioIndex, totalDistance - (int)(distanceWeight * totalDistance));
                                        });
                                        points.Clear();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Spline
        private void AddSplineCurvePoints(List<PointF> points, int frameIndex)
        {
            List<PointF> curvePointFs = splineCurve.GetSplinePoints(points, points.Count() * 2/* Spline 곡선을 점 몇개로 표현할 지 설정*/);
            AngioFrame angioFrame = new AngioFrame();
            angioFrame.AngioFrameNumber = frameIndex;
            angioFrame.TrackPoint = new List<Point>();
            foreach (PointF curvexy in curvePointFs)
            {
                dijkstraHeap[frameIndex].line.Add(curvexy);
                angioFrame.TrackPoint.Add(new Point(curvexy.X, curvexy.Y));
            }
            AngioTrackPoints.Add(angioFrame);
        }

        // Bezier
        void AddBezierCurvePoints(List<PointF> points, int frameIndex, int totalDistance)
        {
            List<PointF> curvePointFs = bezierCurve.GenerateBezierCurve(points[0], points[1], points[2], points[3], totalDistance/* Bezier 곡선을 점 몇개로 표현할 지 설정*/);
            AngioFrame angioFrame = new AngioFrame();
            angioFrame.AngioFrameNumber = frameIndex;
            foreach (PointF curvexy in curvePointFs)
            {
                dijkstraHeap[frameIndex].line.Add(curvexy);
                angioFrame.TrackPoint.Add(new Point(curvexy.X, curvexy.Y));
            }
            AngioTrackPoints.Add(angioFrame);
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

        private void PointTracking(float x, float y, int direction)
        {
            PointF prevPoint = new PointF(x, y);
            int halfSize = 5; // halfSize*2 x halfSize*2 크기
            int startIndex;

            if (direction == -1) // 방향에 따른 MotionVector의 Index 초기화
            {
                startIndex = AngioFrameNumber - 1;
            }
            else
            {
                startIndex = AngioFrameNumber;
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

                PointF currPoint = new PointF(prevPoint.X + (direction) * averageVector.Item0, prevPoint.Y + (direction) * averageVector.Item1);

                //Check Boundary
                if (currPoint.X < 0) currPoint.X = 0;
                if (currPoint.X > Constants.AngioSize) currPoint.X = (float)Constants.AngioSize;
                if (currPoint.Y < 0) currPoint.Y = 0;
                if (currPoint.Y > Constants.AngioSize) currPoint.Y = (float)Constants.AngioSize;

                // trackPoint의 방향에 따른 인덱싱 처리
                if (direction == 1) 
                {
                    dijkstraHeap[i+1].trackPoint.Add(currPoint);
                }
                else
                {
                    dijkstraHeap[i].trackPoint.Add(currPoint);
                }
                
                prevPoint = currPoint;
            }
        }
        private void setDHLegacy()
        {
            for(int i = 0; i < dijkstraHeap.Count; i++)
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

        #endregion

        #region PropertyEvent

        private static void OnAngioImagesPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            var newImages = (List<Mat>)dependencyPropertyChangedEventArgs.NewValue;
            if (newImages.Count > 0)
            {
                control.dijkstraHeap = new List<DijkstraHeap>();
                control.dijkstraHeapLegacy = new List<DijkstraHeap>();
                control.motionVector = new List<Mat>();
                control.ImageProcessing(newImages);
            }
        }

        private static void OnAngioFrameNumberPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;
            int AngioFrameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            control.WireChange(AngioFrameNumber);
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
                        heap.line = new List<PointF>();
                        heap.clickPoint = new List<PointF>();
                        heap.trackPoint = new List<PointF>();
                    }
                }
                else if (control.dijkstraHeap[currFrameNum].trackPoint.Count == 1) // 경로는 없지만 첫번째 포인트를 찍은 경우
                {
                    foreach (DijkstraHeap heap in control.dijkstraHeap) // 새로운 경로 받기 위한 초기화
                    {
                        heap.clickPoint = new List<PointF>();
                        heap.trackPoint = new List<PointF>();
                    }
                }
                foreach (AngioFrame angioFrame in control.AngioTrackPoints) // DB에 업데이트 할 TrackPoint도 초기화
                {
                    angioFrame.AngioFrameNumber = 0;
                    angioFrame.TrackPoint = new List<Point>();
                }
                control.canvas.Children.Clear();
                control.IsReset = control.IsResetOn = false;
            }
        }

        private static void OnCancelPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (DrawAngioPathUtil)dependencyObject;

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

        #endregion

        #region MouseEvent

        async private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PointF clickPosition = new PointF((float)e.GetPosition(canvas).X, (float)e.GetPosition(canvas).Y);
            Rectangle rectangle = new Rectangle();
            AddRecEvents(rectangle);
            Canvas.SetLeft(rectangle, clickPosition.X - Constants.AnnotationRectWidth / 2);
            Canvas.SetTop(rectangle, clickPosition.Y - Constants.AnnotationRectHeight / 2);
            canvas.Children.Add(rectangle);

            int imageLength = AngioImages.Count;
            int currFrameNum = AngioFrameNumber;

            // 첫번째 점
            if (dijkstraHeap[AngioFrameNumber].trackPoint.Count == 0)
            {
                dijkstraHeap[AngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, 1);
                PointTracking(clickPosition.X, clickPosition.Y, -1);

                IsResetOn = true;
                return;
            }
            // 두번째 점 이후
            else
            {
                IsAngioTrackCompleted = IsResetOn = false;

                dijkstraHeap[AngioFrameNumber].trackPoint.Add(clickPosition);
                PointTracking(clickPosition.X, clickPosition.Y, 1);
                PointTracking(clickPosition.X, clickPosition.Y, -1);

                CalculateAllPath(currFrameNum, imageLength, 0/*현재 프레임만 찾기*/);
                DrawWire(dijkstraHeap[AngioFrameNumber], true);

                await Task.Run(() =>
                {
                    CalculateAllPath(currFrameNum, imageLength, 10/*현재 프레임 기준으로 앞뒤 몇장까지 경로 찾을지 결정*/);
                });

                IsAngioTrackCompleted = IsResetOn = true;
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
            if (rectangle != null)
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
            }
        }

        #endregion
    }
}
