using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// ReviewFfrSettingPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewFfrSettingPage : Page
    {
        public ReviewFfrSettingPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewFfrSettingViewModel));
        }
    }
}
