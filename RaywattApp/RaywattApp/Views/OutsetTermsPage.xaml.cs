using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// OutsetTermsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OutsetTermsPage : Page
    {
        public OutsetTermsPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(OutsetTermsViewModel));
        }
    }
}
