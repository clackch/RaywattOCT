using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// LiveViewPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CalibrationPage : Page
    {
        public CalibrationPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(CalibrationViewModel));
        }
    }
}
