using log4net;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Common.Annotation
{
    /// <summary>
    /// DrawListUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawListUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawListUtil));

        public string OutCommand
        {
            get { return (string)GetValue(OutCommandProperty); }
            set { this.SetValue(OutCommandProperty, value); }
        }

        private static readonly DependencyProperty OutCommandProperty =
            DependencyProperty.Register("OutCommand", typeof(string), typeof(DrawListUtil), new PropertyMetadata(null));

        public int FrameNumber
        {
            get { return (int)GetValue(FrameNumberProperty); }
            set { this.SetValue(FrameNumberProperty, value); }
        }

        private static readonly DependencyProperty FrameNumberProperty =
            DependencyProperty.Register("FrameNumber", typeof(int), typeof(DrawListUtil), new PropertyMetadata(-1, OnPropertyChanged));

        public int OutFrameNumber
        {
            get { return (int)GetValue(OutFrameNumberProperty); }
            set { this.SetValue(OutFrameNumberProperty, value); }
        }

        private static readonly DependencyProperty OutFrameNumberProperty =
            DependencyProperty.Register("OutFrameNumber", typeof(int), typeof(DrawListUtil), new PropertyMetadata(default(int)));

        public List<Measurement> Measurements
        {
            get { return (List<Measurement>)GetValue(MeasurementsProperty); }
            set { SetValue(MeasurementsProperty, value); }
        }

        public static readonly DependencyProperty MeasurementsProperty =
            DependencyProperty.Register("Measurements", typeof(List<Measurement>), typeof(DrawListUtil), new PropertyMetadata(null));

        public ObservableCollection<LengthGeometry> LModeLengthGeometries
        {
            get { return (ObservableCollection<LengthGeometry>)GetValue(LModeLengthGeometriesProperty); }
            set { SetValue(LModeLengthGeometriesProperty, value); }
        }

        public static readonly DependencyProperty LModeLengthGeometriesProperty =
            DependencyProperty.Register("LModeLengthGeometries", typeof(ObservableCollection<LengthGeometry>), typeof(DrawListUtil), new PropertyMetadata(null));

        public ObservableCollection<AreaGeometry> CurrAreaGeometries
        {
            get { return (ObservableCollection<AreaGeometry>)GetValue(CurrAreaGeometriesProperty); }
            set { SetValue(CurrAreaGeometriesProperty, value); }
        }

        public static readonly DependencyProperty CurrAreaGeometriesProperty =
            DependencyProperty.Register("CurrAreaGeometries", typeof(ObservableCollection<AreaGeometry>), typeof(DrawListUtil), new PropertyMetadata(null));

        public ObservableCollection<LengthGeometry> CurrLengthGeometries
        {
            get { return (ObservableCollection<LengthGeometry>)GetValue(CurrLengthGeometriesProperty); }
            set { SetValue(CurrLengthGeometriesProperty, value); }
        }

        public static readonly DependencyProperty CurrLengthGeometriesProperty =
            DependencyProperty.Register("CurrLengthGeometries", typeof(ObservableCollection<LengthGeometry>), typeof(DrawListUtil), new PropertyMetadata(null));

        //---------------------------------------------------------------------------------------------------- Constructor
        public DrawListUtil()
        {
            InitializeComponent();
        }

        //---------------------------------------------------------------------------------------------------- Event
        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            int frameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            _log.Debug("frameNumber : " + frameNumber);

            if (frameNumber < 0)
                return;

            var drawListUtil = dependencyObject as DrawListUtil;

            if (drawListUtil == null)
                return;

            for (int i = 0; i < drawListUtil.Measurements.Count; i++)
            {
                if (drawListUtil.Measurements[i].FrameNumber == frameNumber)
                {
                    drawListUtil.CurrAreaGeometries = drawListUtil.Measurements[i].AreaGeometries;
                    drawListUtil.CurrLengthGeometries = drawListUtil.Measurements[i].LengthGeometries;
                    break;
                }                
            }
        }

        private void delete_Area(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            AreaGeometry areaGeometry = button.CommandParameter as AreaGeometry;

            OutCommand = Constants.MeasureDelArea + areaGeometry.Group;
        }

        private void delete_Length(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            LengthGeometry lengthGeometry = button.CommandParameter as LengthGeometry;

            OutCommand = Constants.MeasureDelLeng + lengthGeometry.Group;
        }

        private void delete_LModeLength(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            LengthGeometry lengthGeometry = button.CommandParameter as LengthGeometry;

            OutCommand = Constants.MeasureDelLMod + lengthGeometry.Group;
        }

        private void move_Frame(object sender, RoutedEventArgs e)
        {
            if (this.Measurements == null || this.Measurements.Count == 0)
                return;

            List<Measurement> orderedMeasurements = this.Measurements.OrderBy(x => x.FrameNumber).ToList();
            List<Measurement> notEmptyMeasurments = new List<Measurement>();

            foreach (Measurement measurement in orderedMeasurements)
            {
                if (measurement.AreaGeometries.Count + measurement.LengthGeometries.Count + measurement.TextGeometries.Count > 0)
                    notEmptyMeasurments.Add(measurement);
            }

            if (notEmptyMeasurments.Count == 0)
                return;

            int currPosition = -1, nextPosition = 0, temp = -1;

            for (int i = 0; i < notEmptyMeasurments.Count; i++)
            {
                if (this.FrameNumber == notEmptyMeasurments[i].FrameNumber)
                {
                    currPosition = i;
                    break;
                }

                if(this.FrameNumber > notEmptyMeasurments[i].FrameNumber)
                {
                    temp = i;
                }
            }

            Button button = (Button)sender;
            string param = button.CommandParameter.ToString();

            if ("prev".Equals(param))
            {
                if(currPosition == -1)
                {
                    currPosition = temp + 1;   
                }

                if (currPosition == 0)
                {
                    nextPosition = notEmptyMeasurments.Count - 1;
                }
                else
                {
                    nextPosition = currPosition - 1;
                }
            }
            else
            {
                if(currPosition == -1)
                {
                    currPosition = temp;
                }

                if (currPosition == notEmptyMeasurments.Count - 1)
                {
                    nextPosition = 0;
                }
                else
                {
                    nextPosition = currPosition + 1;
                }
            }

            OutFrameNumber = notEmptyMeasurments[nextPosition].FrameNumber;
        }
    }
}
