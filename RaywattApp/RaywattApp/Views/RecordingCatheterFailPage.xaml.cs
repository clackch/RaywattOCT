using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// RecordingCatheterFailPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingCatheterFailPage : Page
    {
        public RecordingCatheterFailPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingCatheterFailViewModel));
        }
    }
}
