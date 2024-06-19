using RaywattApp.ViewModels;
using System;
using System.Windows.Controls;

namespace RaywattApp.Views
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
        }
    }
}
