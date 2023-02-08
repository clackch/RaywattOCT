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

        public DrawCommandUtil()
        {
            InitializeComponent();
        }

        private void area_Add(object sender, RoutedEventArgs e)
        {
            OutCommand = Constants.MeasureAddArea;
        }

        private void length_Add(object sender, RoutedEventArgs e)
        {
            OutCommand = Constants.MeasureAddLeng;
        }

        private void text_Add(object sender, RoutedEventArgs e)
        {
            OutCommand = Constants.MeasureAddText;
        }

        private void delete_All(object sender, RoutedEventArgs e)
        {
            OutCommand = Constants.MeasureDeleAll;
        }
    }
}
