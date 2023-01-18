using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// Review3dPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Review3dPage : Page
    {
        public Review3dPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(Review3dViewModel));
        }
    }
}
