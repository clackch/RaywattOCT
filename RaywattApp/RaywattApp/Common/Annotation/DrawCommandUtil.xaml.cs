using log4net;
using RaywattApp.Common.Bases;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Common.Annotation
{
    /// <summary>
    /// DrawCommandUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawCommandUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DrawCommandUtil));

        public string OutCommand
        {
            get { return (string)GetValue(OutCommandProperty); }
            set { this.SetValue(OutCommandProperty, value); }
        }

        private static readonly DependencyProperty OutCommandProperty =
            DependencyProperty.Register("OutCommand", typeof(string), typeof(DrawCommandUtil), new PropertyMetadata(null));

        public bool CommandOff
        {
            get { return (bool)GetValue(CommandOffProperty); }
            set { this.SetValue(CommandOffProperty, value); }
        }

        private static readonly DependencyProperty CommandOffProperty =
            DependencyProperty.Register("CommandOff", typeof(bool), typeof(DrawCommandUtil), new PropertyMetadata(ReceiveCommand));

        public bool IsAreaOn
        {
            get { return (bool)GetValue(IsAreaOnProperty); }
            set { this.SetValue(IsAreaOnProperty, value); }
        }

        private static readonly DependencyProperty IsAreaOnProperty =
            DependencyProperty.Register("IsAreaOn", typeof(bool), typeof(DrawCommandUtil), new PropertyMetadata(default(bool)));

        public bool IsLengthOn
        {
            get { return (bool)GetValue(IsLengthOnProperty); }
            set { this.SetValue(IsLengthOnProperty, value); }
        }

        private static readonly DependencyProperty IsLengthOnProperty =
            DependencyProperty.Register("IsLengthOn", typeof(bool), typeof(DrawCommandUtil), new PropertyMetadata(default(bool)));
         
        public bool IsAngleOn
        {
            get { return (bool)GetValue(IsAngleOnProperty); }
            set { this.SetValue(IsAngleOnProperty, value); }
        }
        private static readonly DependencyProperty IsAngleOnProperty =
            DependencyProperty.Register("IsAngleOn", typeof(bool), typeof(DrawCommandUtil), new PropertyMetadata(default(bool)));

        public bool IsTextOn
        {
            get { return (bool)GetValue(IsTextOnProperty); }
            set { this.SetValue(IsTextOnProperty, value); }
        }

        private static readonly DependencyProperty IsTextOnProperty =
          DependencyProperty.Register("IsTextOn", typeof(bool), typeof(DrawCommandUtil), new PropertyMetadata(default(bool)));

        public bool IsEraseOn
        {
            get { return (bool)GetValue(IsEraseOnProperty); }
            set { this.SetValue(IsEraseOnProperty, value); }
        }

        private static readonly DependencyProperty IsEraseOnProperty =
            DependencyProperty.Register("IsEraseOn", typeof(bool), typeof(DrawCommandUtil), new PropertyMetadata(default(bool)));

        public DrawCommandUtil()
        {
            InitializeComponent();
        }

        private static void ReceiveCommand(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var drawCommandUtil = dependencyObject as DrawCommandUtil;
            if (drawCommandUtil == null || drawCommandUtil.CommandOff == false)
                return;

            drawCommandUtil.DisableCommand(false, false, false, false, false);

            drawCommandUtil.CommandOff = false;
        }

        private void area_Add(object sender, RoutedEventArgs e)
        {
            DisableCommand(true, false, false, false, false);
            OutCommand = Constants.MeasureAddArea + "|" + IsAreaOn;
        }

        private void length_Add(object sender, RoutedEventArgs e)
        {
            DisableCommand(false, true, false, false, false);
            OutCommand = Constants.MeasureAddLength + "|" + IsLengthOn;
        }
        private void angle_Add(object sender, RoutedEventArgs e)
        {
            DisableCommand(false, false, false, false, true);
            OutCommand = Constants.MeasureAddAngle + "|" + IsAngleOn;
        }

        private void text_Add(object sender, RoutedEventArgs e)
        {
            DisableCommand(false, false, true, false, false);
            OutCommand = Constants.MeasureAddText + "|" + IsTextOn;
        }

        private void erase_point(object sender, RoutedEventArgs e)
        {
            DisableCommand(false, false, false, true, false);
            OutCommand = Constants.MeasureErasePoint + "|" + IsEraseOn;
        }

        private void delete_All(object sender, RoutedEventArgs e)
        {
            DisableCommand(false, false, false, false, false);
            OutCommand = Constants.MeasureDeleteAll;
        }

        private void DisableCommand(bool isAreaOn, bool isLengthOn, bool isTextOn, bool isEraseOn, bool isAngleOn)
        {
            if(!isAreaOn)
                IsAreaOn = false;
            if(!isLengthOn)
                IsLengthOn = false;
            if (!isAngleOn)
                IsAngleOn = false;
            if (!isTextOn)
                IsTextOn = false;
            if(!isEraseOn)
                IsEraseOn = false;
        }
    }
}
