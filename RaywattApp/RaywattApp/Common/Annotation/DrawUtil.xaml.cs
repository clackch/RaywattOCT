using log4net;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
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

        private bool isDrawing;

        private bool isCanvasClicked;

        public string InCommand
        {
            get { return (string)GetValue(InCommandProperty); }
            set { this.SetValue(InCommandProperty, value); }
        }

        private static readonly DependencyProperty InCommandProperty =
            DependencyProperty.Register("InCommand", typeof(string), typeof(DrawUtil), new PropertyMetadata(ReceiveCommand));

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
        private static void ReceiveCommand(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var drawUtil = dependencyObject as DrawUtil;
            if (drawUtil == null || drawUtil.InCommand == null)
                return;

            switch (drawUtil.InCommand.Substring(0, 2))
            {
                case Constants.MeasureAddArea:
                    drawUtil.AddArea();
                    break;
                case Constants.MeasureAddLeng:
                    drawUtil.AddLength();
                    break;
                case Constants.MeasureAddText:
                    drawUtil.AddText();
                    break;
                case Constants.MeasureDeleAll:
                    drawUtil.DeleteAll();
                    break;
                case Constants.MeasureDelArea:
                    drawUtil.DeleteArea(drawUtil.InCommand);
                    break;
                case Constants.MeasureDelLeng:
                    drawUtil.DeleteLength(drawUtil.InCommand);
                    break;
                case Constants.MeasureDsbCLen:
                    drawUtil.length_canvas_MouseLeave(null, null);
                    break;
                case Constants.MeasureDsbText:
                    drawUtil.text_canvas_MouseLeave(null, null);
                    break;
                default:
                    break;
            }

            drawUtil.InCommand = null;
        }

        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            int frameNumber = (int)dependencyPropertyChangedEventArgs.NewValue;
            _log.Debug("frameNumber : " + frameNumber);

            if (frameNumber < 0)
                return;

            var drawUtil = dependencyObject as DrawUtil;

            if(drawUtil == null || drawUtil.Measurements == null)
                return;

            drawUtil.areaGeometrys = drawUtil.Measurements[frameNumber].AreaGeometries;
            drawUtil.lengthGeometries = drawUtil.Measurements[frameNumber].LengthGeometries;
            drawUtil.textGeometries = drawUtil.Measurements[frameNumber].TextGeometries;

            drawUtil.DrawAll();
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void DrawAll()
        {
            _log.Debug("DrawAll");

            this.canvas.Children.Clear();
            DrawAreaAll();
            DrawLengthAll();
            DrawTextAll();
        }

        private void DeleteAll()
        {
            _log.Debug("DeleteAll");

            if (this.isDrawing)
                return;

            this.canvas.Children.Clear();
            this.canvasBackground.Children.Clear();
            this.areaGeometrys.Clear();
            this.lengthGeometries.Clear();
            this.textGeometries.Clear();
        }

        private void DeleteArea(string param)
        {
            DeleteAreaAll();

            int groupIdx = int.Parse(param.Substring(2, 1));

            for (int i = groupIdx + 1; i < this.areaGeometrys.Count; i++)
            {
                this.areaGeometrys[i].Group--;
            }
            this.areaGeometrys.RemoveAt(groupIdx);

            DrawAreaAll();
        }

        private void DeleteLength(string param)
        {
            DeleteLengthAll();

            int groupIdx = int.Parse(param.Substring(2, 1));

            for (int i = groupIdx + 1; i < this.lengthGeometries.Count; i++)
            {
                this.lengthGeometries[i].Group--;
            }
            this.lengthGeometries.RemoveAt(groupIdx);

            DrawLengthAll();
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

        private Size GetLabelSize(string style, string text = "")
        {
            Label label = new Label();
            label.Style = (Style)this.Resources[style];
            label.Content = text;

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return label.DesiredSize;
        }

        private Size GetTextBoxSize(string style)
        {
            TextBox textBox = new TextBox();
            textBox.Style = (Style)this.Resources[style];

            textBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return textBox.DesiredSize;
        }
    }
}
