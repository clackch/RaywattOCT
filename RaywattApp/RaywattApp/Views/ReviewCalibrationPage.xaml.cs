using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// ReviewCalibrationPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewCalibrationPage : Page
    {
        public ReviewCalibrationPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewCalibrationViewModel));
        }
    }
}
