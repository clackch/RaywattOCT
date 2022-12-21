using log4net;
using RaywattApp.Common.Annotation.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RaywattApp.Common.Annotation
{
    /// <summary>
    /// DrawUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawUtil));

        //---------------------------------------------------------------------------------------------------- Field

        private Brush[] brushes = { Brushes.Red, Brushes.Orange, Brushes.Yellow, Brushes.Green, Brushes.Blue, Brushes.Navy, Brushes.Purple };

        private bool isDrawing;

        private bool isCanvasClicked;

        public List<Measurement> Measurements
        {
            get { return (List<Measurement>)GetValue(MeasurementsProperty); }
            set { SetValue(MeasurementsProperty, value); }
        }

        public static readonly DependencyProperty MeasurementsProperty =
            DependencyProperty.Register("Measurements", typeof(List<Measurement>), typeof(DrawUtil), new PropertyMetadata(null));

        public int FrameNumber
        {
            get { return (int)GetValue(FrameNumberProperty); }
            set { this.SetValue(FrameNumberProperty, value); }
        }

        private static readonly DependencyProperty FrameNumberProperty =
            DependencyProperty.Register("FrameNumber", typeof(int), typeof(DrawUtil), new PropertyMetadata(-1, OnPropertyChanged));

        public int MouseCursor
        {
            get { return (int)GetValue(MouseCursorProperty); }
            set { this.SetValue(MouseCursorProperty, value); }
        }

        private static readonly DependencyProperty MouseCursorProperty =
            DependencyProperty.Register("MouseCursor", typeof(int), typeof(DrawUtil), new PropertyMetadata(default(int)));

        //---------------------------------------------------------------------------------------------------- Constructor
        public DrawUtil()
        {
            InitializeComponent();

            //Default Setting
            isDrawing = false;
            isCanvasClicked = false;

            AreaInit();
            LengthInit();
            TextInit();
        }

        //---------------------------------------------------------------------------------------------------- Event
        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            int frameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            _log.Debug("frameNumber : " + frameNumber);

            if (frameNumber < 0)
                return;

            var drawUtil = dependencyObject as DrawUtil;

            if(drawUtil == null || drawUtil.Measurements == null)
                return;

            bool isFind = false;

            for (int i = 0; i < drawUtil.Measurements.Count; i++)
            {
                if (drawUtil.Measurements[i].FrameNumber == frameNumber)
                {
                    //Area
                    if (drawUtil.Measurements[i].AreaGeometrys == null)
                    {
                        drawUtil.areaGeometrys = new List<AreaGeometry>();
                        drawUtil.Measurements[i].AreaGeometrys = drawUtil.areaGeometrys;
                    }
                    else
                    {
                        drawUtil.areaGeometrys = drawUtil.Measurements[i].AreaGeometrys;
                    }

                    //Length
                    if (drawUtil.Measurements[i].LengthGeometries == null)
                    {
                        drawUtil.lengthGeometries = new List<LengthGeometry>();
                        drawUtil.Measurements[i].LengthGeometries = drawUtil.lengthGeometries;
                    }
                    else
                    {
                        drawUtil.lengthGeometries = drawUtil.Measurements[i].LengthGeometries;
                    }

                    //Text
                    if (drawUtil.Measurements[i].TextGeometries == null)
                    {
                        drawUtil.textGeometries = new List<TextGeometry>();
                        drawUtil.Measurements[i].TextGeometries = drawUtil.textGeometries;
                    }
                    else
                    {
                        drawUtil.textGeometries = drawUtil.Measurements[i].TextGeometries;
                    }

                    isFind = true;
                    break;
                }
            }

            if (!isFind)
            {
                drawUtil.areaGeometrys = new List<AreaGeometry>();
                drawUtil.lengthGeometries = new List<LengthGeometry>();
                drawUtil.textGeometries = new List<TextGeometry>();

                Measurement measurement = new Measurement();
                measurement.FrameNumber = frameNumber;
                measurement.AreaGeometrys = drawUtil.areaGeometrys;
                measurement.LengthGeometries = drawUtil.lengthGeometries;
                measurement.TextGeometries = drawUtil.textGeometries;
                drawUtil.Measurements.Add(measurement);
            }

            drawUtil.DrawAll();
        }

        //---------------------------------------------------------------------------------------------------- Function

        private void delete_All(object sender, RoutedEventArgs e)
        {
            _log.Debug("delete_All");

            if (this.isDrawing)
                return;

            this.canvas.Children.Clear();
            this.canvasBackground.Children.Clear();
            this.areaGeometrys.Clear();
            this.lengthGeometries.Clear();
            this.textGeometries.Clear();
        }

        private void DrawAll()
        {
            _log.Debug("DrawAll");

            this.canvas.Children.Clear();
            DrawAreaAll();
            DrawLengthAll();
            DrawTextAll();
        }

        private void DeleteLabel(string classfication, int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Label))
                {
                    Label temp = (Label)item;

                    if (temp.Name == classfication + "_" + group)
                    {
                        this.canvas.Children.Remove((Label)item);
                        break;
                    }
                }
            }
        }

    }
}
