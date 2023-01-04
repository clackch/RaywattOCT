using log4net;
using RaywattApp.Common.Annotation.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<AreaGeometry> CurrAreaGeometries
        {
            get { return (ObservableCollection<AreaGeometry>)GetValue(CurrAreaGeometriesProperty); }
            set { SetValue(CurrAreaGeometriesProperty, value); }
        }

        public static readonly DependencyProperty CurrAreaGeometriesProperty =
            DependencyProperty.Register("CurrAreaGeometries", typeof(ObservableCollection<AreaGeometry>), typeof(DrawUtil), new PropertyMetadata(null));

        public ObservableCollection<LengthGeometry> CurrLengthGeometries
        {
            get { return (ObservableCollection<LengthGeometry>)GetValue(CurrLengthGeometriesProperty); }
            set { SetValue(CurrLengthGeometriesProperty, value); }
        }

        public static readonly DependencyProperty CurrLengthGeometriesProperty =
            DependencyProperty.Register("CurrLengthGeometries", typeof(ObservableCollection<LengthGeometry>), typeof(DrawUtil), new PropertyMetadata(null));

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
                    if (drawUtil.Measurements[i].AreaGeometries == null)
                    {
                        drawUtil.areaGeometrys = new ObservableCollection<AreaGeometry>();
                        drawUtil.Measurements[i].AreaGeometries = drawUtil.areaGeometrys;
                    }
                    else
                    {
                        drawUtil.areaGeometrys = drawUtil.Measurements[i].AreaGeometries;
                    }

                    //Length
                    if (drawUtil.Measurements[i].LengthGeometries == null)
                    {
                        drawUtil.lengthGeometries = new ObservableCollection<LengthGeometry>();
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
                drawUtil.areaGeometrys = new ObservableCollection<AreaGeometry>();
                drawUtil.lengthGeometries = new ObservableCollection<LengthGeometry>();
                drawUtil.textGeometries = new List<TextGeometry>();

                Measurement measurement = new Measurement();
                measurement.FrameNumber = frameNumber;
                measurement.AreaGeometries = drawUtil.areaGeometrys;
                measurement.LengthGeometries = drawUtil.lengthGeometries;
                measurement.TextGeometries = drawUtil.textGeometries;
                drawUtil.Measurements.Add(measurement);
            }

            drawUtil.SetMeasurements(drawUtil);

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

        private void SetMeasurements(DrawUtil drawUtil)
        {
            CurrAreaGeometries = drawUtil.areaGeometrys;
            CurrLengthGeometries = drawUtil.lengthGeometries;
        }

        private void delete_Area(object sender, RoutedEventArgs e)
        {
            DeleteAreaAll();

            Button button = (Button)sender;
            AreaGeometry areaGeometry = button.CommandParameter as AreaGeometry;
            
            for (int i = areaGeometry.Group + 1; i < this.areaGeometrys.Count; i++)
            {
                this.areaGeometrys[i].Group--;
            }
            this.areaGeometrys.Remove(areaGeometry);
            
            DrawAreaAll();
        }

        private void delete_Length(object sender, RoutedEventArgs e)
        {
            DeleteLengthAll();

            Button button = (Button)sender;
            LengthGeometry lengthGeometry = button.CommandParameter as LengthGeometry;

            for (int i = lengthGeometry.Group + 1; i < this.lengthGeometries.Count; i++)
            {
                this.lengthGeometries[i].Group--;
            }
            this.lengthGeometries.Remove(lengthGeometry);

            DrawLengthAll();
        }
    }
}
