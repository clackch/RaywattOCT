using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// RecordingPresetPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingPresetPage : Page
    {
        public RecordingPresetPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingPresetViewModel));
        }
    }
}
