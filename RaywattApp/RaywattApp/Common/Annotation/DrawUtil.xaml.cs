using log4net;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private bool isErasing;

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

        public int CommandType
        {
            get { return (int)GetValue(CommandTypeProperty); }
            set { this.SetValue(CommandTypeProperty, value); }
        }

        private static readonly DependencyProperty CommandTypeProperty =
            DependencyProperty.Register("CommandType", typeof(int), typeof(DrawUtil), new PropertyMetadata(default(int)));

        public bool CommandOff
        {
            get { return (bool)GetValue(CommandOffProperty); }
            set { this.SetValue(CommandOffProperty, value); }
        }

        private static readonly DependencyProperty CommandOffProperty =
            DependencyProperty.Register("CommandOff", typeof(bool), typeof(DrawUtil), new PropertyMetadata(default(bool)));

        //---------------------------------------------------------------------------------------------------- Constructor
        public DrawUtil()
        {
            InitializeComponent();

            //Default Setting
            isDrawing = false;
            isCanvasClicked = false;
            isErasing = false;

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

            string[] command = drawUtil.InCommand.Split("|");

            switch (command[0])
            {
                case Constants.MeasureAddArea:
                    drawUtil.AddArea(command[1]);
                    break;
                case Constants.MeasureAddLength:
                    drawUtil.AddLength(command[1]);
                    break;
                case Constants.MeasureAddText:
                    drawUtil.AddText(command[1]);
                    break;
                case Constants.MeasureErasePoint:
                    drawUtil.ErasePoint(command[1]);
                    break;
                case Constants.MeasureDeleteAll:
                    drawUtil.DeleteAll();
                    break;
                case Constants.MeasureDeleteArea:
                    drawUtil.DeleteArea(command[1]);
                    break;
                case Constants.MeasureDeleteLength:
                    drawUtil.DeleteLength(command[1]);
                    break;
                case Constants.MeasureDisableLength:
                    drawUtil.DisableCommand();
                    break;
                case Constants.MeasureDisableText:
                    drawUtil.DisableCommand();
                    break;
                case Constants.MeasureDisableErase:
                    drawUtil.DisableCommand();
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

        private void erase_canvas_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            _log.Debug("erase_canvas_MouseRightButtonDown");

            InCommand = Constants.MeasureDisableErase;
            CommandOff = true;
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

            DisableCommand();
        }

        private void DeleteArea(string param)
        {
            DeleteAreaAll();

            int groupIdx = int.Parse(param);

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

            int groupIdx = int.Parse(param);

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

        private void ErasePoint(string isEraseOn)
        {
            DisableCommand();

            if (Convert.ToBoolean(isEraseOn))
            {
                this.isErasing = true;

                this.canvas.MouseRightButtonDown += erase_canvas_MouseRightButtonDown;

                this.canvas.Background = Brushes.Transparent;
                CommandType = Constants.MeasureCmdErase;
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

        private void DisableCommand()
        {
            switch (CommandType)
            {
                case 1://Area
                    this.canvas.MouseLeftButtonDown -= area_canvas_MouseLeftButtonDown;
                    this.canvas.MouseMove -= area_canvas_MouseMove;
                    this.canvas.MouseLeave -= area_canvas_MouseLeave;
                    this.canvas.MouseRightButtonDown -= area_canvas_MouseRightButtonDown;
                    this.pointList = null;
                    this.groupFirst = true;
                    this.isCanvasClicked = false;
                    break;
                case 2://Length
                    this.canvas.MouseLeftButtonDown -= length_canvas_MouseLeftButtonDown;
                    this.canvas.MouseMove -= length_canvas_MouseMove;
                    this.canvas.MouseLeave -= length_canvas_MouseLeave;
                    this.canvas.MouseRightButtonDown -= length_canvas_MouseRightButtonDown;
                    this.isFisrtPoint = true;
                    this.isCanvasClicked = false;
                    break;
                case 3://Text
                    this.canvas.MouseLeftButtonDown -= text_canvas_MouseLeftButtonDown;
                    this.canvas.MouseRightButtonDown -= text_canvas_MouseRightButtonDown;
                    break;
                case 4://Erase
                    this.canvas.MouseRightButtonDown -= erase_canvas_MouseRightButtonDown;                    
                    this.isErasing = false;
                    break;
                default:
                    break;
            }

            this.isDrawing = false;
            this.canvas.Background = null;
            CommandType = Constants.MeasureCmdDefault;
        }
    }
}
