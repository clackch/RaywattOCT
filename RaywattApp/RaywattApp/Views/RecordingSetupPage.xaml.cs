using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// RecordingSetupPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RecordingSetupPage : Page
    {
        public RecordingSetupPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(RecordingSetupViewModel));
        }
    }
}
