using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// ReviewPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewPage : Page
    {
        public ReviewPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewViewModel));
        }
    }
}
