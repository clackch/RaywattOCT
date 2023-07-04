using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// OutsetLoadingPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OutsetLoadingPage : Page
    {
        public OutsetLoadingPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(OutsetLoadingViewModel));
        }
    }
}
