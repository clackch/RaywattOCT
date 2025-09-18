using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// RecordingCalibrationPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingCalibrationPage : Page
    {
        public RecordingCalibrationPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingCalibrationViewModel));
        }
    }
}
