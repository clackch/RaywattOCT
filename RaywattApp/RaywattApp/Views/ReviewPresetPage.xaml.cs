using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// ReviewPresetPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewPresetPage : Page
    {
        public ReviewPresetPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewPresetViewModel));
        }
    }
}
