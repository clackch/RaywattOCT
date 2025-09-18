using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// RecordingPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingPage : Page
    {
        public RecordingPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingViewModel));
        }
    }
}
