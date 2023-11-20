using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using PointF = System.Drawing.PointF;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using OpenCvSharp;
using System.Diagnostics;
using OpenCvSharp.Extensions;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RaywattApp.Common.Annotation.LiveWire;
using System.Runtime.CompilerServices;
using OpenCvSharp.Flann;
using LiveWire;

namespace RaywattApp.Common.Annotation
{
    /// <summary>
    /// DrawAngioPathUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawAngioPathUtil : UserControl
    {
        public Zoom Zoom
        {
            get { return (Zoom)GetValue(ZoomProperty); }
            set { this.SetValue(ZoomProperty, value); }
        }
        public int FrameNumber
        {
            get { return (int)GetValue(FrameNumberProperty); }
            set { this.SetValue(FrameNumberProperty, value); }
        }
        public List<Mat> AngioImages
        {
            get { return (List<Mat>)GetValue(AngioImagesProperty); }
            set { this.SetValue(AngioImagesProperty, value); }
        }

        private String curveType = "Spline";
        private BezierCurve bezierCurve;
        private SplineCurve splineCurve;

        // todo
        // Curr-FrameNum과 해당하는 TrackPoint 2개 
        // Curr-FrameNum과 Total-FrameNum사이의 비율로 GuidePoint 표시 -> AngioFrmaes : OCTFrames 만큼
        // 첫번째 Frame에 2개의 Point를 찍는다고 가정하고, 나머지 Frame에 대한 TrackedPoints 구하기
        // DrawPath()에서 TrackedPoint에 대한 Path 그리기

        private List<DijkstraHeap>  dijkstraHeap;
        private List<Point> trackPoints; //Co-Registration 에 쓸 initial 위치

        private static readonly DependencyProperty FrameNumberProperty =
        DependencyProperty.Register("FrameNumber", typeof(int), typeof(DrawAngioPathUtil), new PropertyMetadata(-1, OnPropertyChanged));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(Zoom), typeof(DrawAngioPathUtil), new PropertyMetadata(null));

        public static readonly DependencyProperty AngioImagesProperty =
            DependencyProperty.Register("AngioImages", typeof(List<Mat>), typeof(DrawAngioPathUtil), new PropertyMetadata(null, OnAngioImagesPropertyChanged));

        public DrawAngioPathUtil()
        {
            InitializeComponent();
            ActivateEvent();
            dijkstraHeap = new List<DijkstraHeap>();
            
        }

        // ---------------------------------------Method

        // Spline
        private void DrawPath(List<PointF> points)
        {
            List<PointF> curvePointFs = splineCurve.GetSplinePoints(points, points.Count() * 2);
            foreach (PointF curvexy in curvePointFs)
            {
                Ellipse path = new Ellipse
                {
                    Width = 2,
                    Height = 2,
                    Fill = System.Windows.Media.Brushes.Yellow
                };
                dijkstraHeap[FrameNumber].line.Add(curvexy);
                Canvas.SetLeft(path, curvexy.X - path.Width / 2);
                Canvas.SetTop(path, curvexy.Y - path.Height / 2);
                this.canvas.Children.Add(path);
            }
            dijkstraHeap[FrameNumber].sy = dijkstraHeap[FrameNumber].sx = -1;
        }

        // Bezier
        void DrawPath(List<PointF> points, int totalDistance)
        {
            List<PointF> curvePointFs = bezierCurve.GenerateBezierCurve(points[0], points[1], points[2], points[3], totalDistance);
            foreach (PointF curvexy in curvePointFs)
            {
                Ellipse path = new Ellipse
                {
                    Width = 2,
                    Height = 2,
                    Fill = System.Windows.Media.Brushes.Yellow
                };
                dijkstraHeap[FrameNumber].line.Add(curvexy);
                Canvas.SetLeft(path, curvexy.X - path.Width / 2);
                Canvas.SetTop(path, curvexy.Y - path.Height / 2);
                this.canvas.Children.Add(path);
            }
            dijkstraHeap[FrameNumber].sy = dijkstraHeap[FrameNumber].sx = -1;
        }

        private void ActivateEvent()
        {
            canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            this.canvas.Background = Brushes.Transparent;
        }

        private void DeactivateEvent()
        {
            canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            this.canvas.Background = null;
        }

        private void ImageProcessing(List<Mat> frames)
        {
            foreach (var frame in frames)
            {
                Cv2.ImWrite("frame.png", frame);
                Mat blurredImage = new Mat();
                Cv2.Blur(frame, blurredImage, new OpenCvSharp.Size(7, 7));

                // HE 영역 분할 처리
                Mat equalizedImage = new Mat();
                var clahe = Cv2.CreateCLAHE(clipLimit: 10, new OpenCvSharp.Size(9, 9));
                clahe.Apply(blurredImage, equalizedImage);
                Cv2.ImWrite("equalizedImage.png", equalizedImage);

                // 픽셀 100 미만 값 -> 255, 픽셀 100 이상 값 -> 0
                Mat thresholdImage = new Mat();
                Cv2.Threshold(equalizedImage, thresholdImage, 100, 255, ThresholdTypes.BinaryInv);
                Cv2.ImWrite("thresholdImage.png", thresholdImage);

                // 이미지 변형(분할 : Segmentation) 처리
                Mat morphedImage = new Mat();
                var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
                Cv2.MorphologyEx(thresholdImage, morphedImage, MorphTypes.Open, kernel, iterations: 2);

                // 변형 처리 반복 -> 스켈레톤(골격화)
                Mat skeleton = new Mat();
                skeleton = Skeletonize(morphedImage);
                Cv2.ImWrite("skeleton.png", skeleton);

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

        // ---------------------------------------Event

        private static void OnAngioImagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DrawAngioPathUtil)d;
            var newImages = (List<Mat>)e.NewValue;
            if (newImages.Count > 0)
            {
                control.ImageProcessing(newImages);
            }
        }


        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            int frameNumber = (int)e.NewValue;

        }

        async private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PointF clickPosition = new PointF((float)e.GetPosition(canvas).X, (float)e.GetPosition(canvas).Y);
            Rectangle rectangle = new Rectangle();
            rectangle.Style = (Style)this.Resources["StyleRectangle"];
            Canvas.SetLeft(rectangle, clickPosition.X - (Constants.AnnotationRectWidth / Zoom.ScaleX) / 2);
            Canvas.SetTop(rectangle, clickPosition.Y - (Constants.AnnotationRectHeight / Zoom.ScaleY) / 2);
            canvas.Children.Add(rectangle);

            int index = FrameNumber;
            // 경로 반환받을 x, y 좌표, 경로 길이 설정
            int[] vx = new int[dijkstraHeap[index].width * dijkstraHeap[index].height];
            int[] vy = new int[dijkstraHeap[index].width * dijkstraHeap[index].height];

            // 첫번째 점
            if (dijkstraHeap[index].sx == -1 && dijkstraHeap[index].sy == -1)
            {
                dijkstraHeap[index].sx = (int)clickPosition.X;
                dijkstraHeap[index].sy = (int)clickPosition.Y;
                dijkstraHeap[index].clickPoint.Add(clickPosition);
                //PointTracking(motionVectors, frames, point.X, point.Y);
                return;
            }
            // 두번째 점 이후
            else
            {
                bezierCurve = new BezierCurve();
                splineCurve = new SplineCurve();
                int x = (int)clickPosition.X;
                int y = (int)clickPosition.Y;
                int[] pixelValue = new int[dijkstraHeap[index].width * dijkstraHeap[index].height];
                int pathLength = 0;

                List<PointF> points;
                int prevIndex, currIndex, distanceLimit, totalDistance, numOfPoints;

                dijkstraHeap[index].clickPoint.Add(new PointF((int)clickPosition.X, (int)clickPosition.Y));
                //PointTracking(motionVectors, frames, x, y);

                await Task.Run(() =>
                {
                    dijkstraHeap[index].run(dijkstraHeap[index].sx, dijkstraHeap[index].sy);
                    dijkstraHeap[index].returnPath(x, y, vx, vy, out pathLength, pixelValue);
                });

                points = new List<PointF>();

                // 모든 점 전달하여 Spline 곡선 형성
                if (curveType == "Spline")
                {
                    for (int i = 0; i < pathLength; i++)
                    {
                        points.Add(new PointF(vx[i], vy[i]));
                    }
                    DrawPath(points);
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
                            DrawPath(points, totalDistance - (int)(distanceWeight * totalDistance)); // 가이드 점 4개와, 보간에 사용할 점 개수 전달.
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
                                { // 가이드 점이 4개인 경우엔 곡선 그리기.
                                    DrawPath(points, totalDistance - (int)(distanceWeight * totalDistance));
                                    points.Clear();
                                }
                            }
                        }
                    }
                }
                dijkstraHeap[index].sx = x;
                dijkstraHeap[index].sy = y;
            }
        }
    }




}
