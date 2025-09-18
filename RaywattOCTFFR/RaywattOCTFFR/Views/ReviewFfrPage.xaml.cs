using RaywattOCTFFR.ViewModels;
using System;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// ReviewFfrPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewFfrPage : Page
    {
        public ReviewFfrPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewFfrViewModel));

            mediaElement.MediaEnded += MediaElement_MediaEnded;
            mediaElement.Play();
        }

        private void MediaElement_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.mediaElement.Stop();
            this.mediaElement.Position = TimeSpan.FromSeconds(0);
            this.mediaElement.Play();
        }
    }
}
