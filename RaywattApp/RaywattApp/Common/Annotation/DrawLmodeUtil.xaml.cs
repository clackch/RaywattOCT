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
    /// DrawLmodeUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawLmodeUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawLmodeUtil));

        //---------------------------------------------------------------------------------------------------- Field

        private Brush[] brushes = Constants.AnnotationBrushes;

        private bool isDrawing;

        private bool isCanvasClicked;

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

        public int MouseCursor
        {
            get { return (int)GetValue(MouseCursorProperty); }
            set { this.SetValue(MouseCursorProperty, value); }
        }

        private static readonly DependencyProperty MouseCursorProperty =
            DependencyProperty.Register("MouseCursor", typeof(int), typeof(DrawLmodeUtil), new PropertyMetadata(default(int)));

        public double LModeIndicatorX
        {
            get { return (double)GetValue(LModeIndicatorXProperty); }
            set { this.SetValue(LModeIndicatorXProperty, value); }
        }

        private static readonly DependencyProperty LModeIndicatorXProperty =
            DependencyProperty.Register("LModeIndicatorX", typeof(double), typeof(DrawLmodeUtil), new PropertyMetadata(default(double)));

        //---------------------------------------------------------------------------------------------------- Constructor
        public DrawLmodeUtil()
        {
            InitializeComponent();

            //Default Setting
            isDrawing = false;
            isCanvasClicked = false;

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

            switch (drawLmodeUtil.InCommand.Substring(0, 2))
            {
                case Constants.MeasureDrawAll:
                    drawLmodeUtil.DrawAll();
                    break;
                case Constants.MeasureAddLeng:
                    drawLmodeUtil.AddLength();
                    break;
                case Constants.MeasureAddText:
                    drawLmodeUtil.AddText();
                    break;
                case Constants.MeasureDelLMod:
                    drawLmodeUtil.DeleteLength(drawLmodeUtil.InCommand);
                    break;
                case Constants.MeasureDsbLLen:
                    drawLmodeUtil.length_canvas_MouseLeave(null, null);
                    break;
                case Constants.MeasureDsbText:
                    drawLmodeUtil.text_canvas_MouseLeave(null, null);
                    break;
                default:
                    break;
            }

            drawLmodeUtil.InCommand = null;
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void DrawAll()
        {
            _log.Debug("DrawAll");

            this.canvas.Children.Clear();
            DrawLengthAll();
            DrawTextAll();
        }

        private void DeleteLength(string param)
        {
            _log.Debug("DeleteLength : " + param);

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
