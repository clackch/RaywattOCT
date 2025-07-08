using System.Windows.Controls;
using RaywattApp.ViewModels;

namespace RaywattApp.Views
{
    /// <summary>
    /// OutsetLoginPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OutsetLoginPage : Page
    {
        public OutsetLoginPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(OutsetLoginViewModel));
        }
    }
}
