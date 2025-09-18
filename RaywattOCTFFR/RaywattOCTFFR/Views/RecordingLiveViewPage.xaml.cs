using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// RecordingLiveViewPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingLiveViewPage : Page
    {
        public RecordingLiveViewPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingLiveViewViewModel));
        }
    }
}
