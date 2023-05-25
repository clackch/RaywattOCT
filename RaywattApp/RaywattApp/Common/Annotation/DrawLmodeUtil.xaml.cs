using log4net;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RaywattApp.Common.Annotation
{
    /// <summary>
    /// DrawLmodeUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawLmodeUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawLmodeUtil));

        //---------------------------------------------------------------------------------------------------- Field

        private Brush[] brushes = Constants.AnnotationBrushes;

        private bool isDrawing;

        private bool isCanvasClicked;

        private bool isErasing;

        public string InCommand
        {
            get { return (string)GetValue(InCommandProperty); }
            set { this.SetValue(InCommandProperty, value); }
        }

        private static readonly DependencyProperty InCommandProperty =
            DependencyProperty.Register("InCommand", typeof(string), typeof(DrawLmodeUtil), new PropertyMetadata(ReceiveCommand));

        public ObservableCollection<LengthGeometry> LModeLengthGeometries
        {
            get { return (ObservableCollection<LengthGeometry>)GetValue(LModeLengthGeometriesProperty); }
            set { SetValue(LModeLengthGeometriesProperty, value); }
        }

        public static readonly DependencyProperty LModeLengthGeometriesProperty =
            DependencyProperty.Register("LModeLengthGeometries", typeof(ObservableCollection<LengthGeometry>), typeof(DrawLmodeUtil), new PropertyMetadata(null));

        public List<TextGeometry> LModeTextGeometries
        {
            get { return (List<TextGeometry>)GetValue(LModeTextGeometriesProperty); }
            set { SetValue(LModeTextGeometriesProperty, value); }
        }

        public static readonly DependencyProperty LModeTextGeometriesProperty =
            DependencyProperty.Register("LModeTextGeometries", typeof(List<TextGeometry>), typeof(DrawLmodeUtil), new PropertyMetadata(null));

        public int CommandType
        {
            get { return (int)GetValue(CommandTypeProperty); }
            set { this.SetValue(CommandTypeProperty, value); }
        }

        private static readonly DependencyProperty CommandTypeProperty =
            DependencyProperty.Register("CommandType", typeof(int), typeof(DrawLmodeUtil), new PropertyMetadata(default(int)));

        public bool CommandOff
        {
            get { return (bool)GetValue(CommandOffProperty); }
            set { this.SetValue(CommandOffProperty, value); }
        }

        private static readonly DependencyProperty CommandOffProperty =
            DependencyProperty.Register("CommandOff", typeof(bool), typeof(DrawLmodeUtil), new PropertyMetadata(default(bool)));

        public double LModeIndicatorX
        {
            get { return (double)GetValue(LModeIndicatorXProperty); }
            set { this.SetValue(LModeIndicatorXProperty, value); }
        }

        private static readonly DependencyProperty LModeIndicatorXProperty =
            DependencyProperty.Register("LModeIndicatorX", typeof(double), typeof(DrawLmodeUtil), new PropertyMetadata(default(double)));

        public Zoom Zoom
        {
            get { return (Zoom)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(Zoom), typeof(DrawLmodeUtil), new PropertyMetadata(null));

        //---------------------------------------------------------------------------------------------------- Constructor
        public DrawLmodeUtil()
        {
            InitializeComponent();

            //Default Setting
            isDrawing = false;
            isCanvasClicked = false;
            isErasing = false;

            LengthInit();
            TextInit();
        }

        //---------------------------------------------------------------------------------------------------- Event
        private static void ReceiveCommand(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var drawLmodeUtil = dependencyObject as DrawLmodeUtil;
            if (drawLmodeUtil == null || drawLmodeUtil.InCommand == null)
                return;

            if (drawLmodeUtil.lengthGeometries == null)
                drawLmodeUtil.lengthGeometries = drawLmodeUtil.LModeLengthGeometries;

            if (drawLmodeUtil.textGeometries == null)
                drawLmodeUtil.textGeometries = drawLmodeUtil.LModeTextGeometries;

            if (drawLmodeUtil.Zoom == null)
                drawLmodeUtil.Zoom = new Zoom(1);

            string[] command = drawLmodeUtil.InCommand.Split("|");

            switch (command[0])
            {
                case Constants.MeasureDrawAll:
                    drawLmodeUtil.DrawAll();
                    break;
                case Constants.MeasureAddLength:
                    drawLmodeUtil.AddLength(command[1]);
                    break;
                case Constants.MeasureAddText:
                    drawLmodeUtil.AddText(command[1]);
                    break;
                case Constants.MeasureErasePoint:
                    drawLmodeUtil.ErasePoint(command[1]);
                    break;
                case Constants.MeasureDisableLength:
                    drawLmodeUtil.DisableCommand();
                    break;
                case Constants.MeasureDisableText:
                    drawLmodeUtil.DisableCommand();
                    break;
                case Constants.MeasureDisableErase:
                    drawLmodeUtil.DisableCommand();
                    break;
                default:
                    break;
            }

            drawLmodeUtil.InCommand = null;
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
