using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// RecordingConfirmPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingConfirmPage : Page
    {
        public RecordingConfirmPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingConfirmViewModel));
        }
    }
}
