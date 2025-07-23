using RaywattApp.ViewModels.Admin;
using RaywattApp.ViewModels.Password;
using System.Windows.Controls;

namespace RaywattApp.Views.Password
{
    /// <summary>
    /// InitialPasswordSetupPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InitialPasswordSetupPage : Page
    {
        public InitialPasswordSetupPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(InitialPasswordSetupViewModel));
        }
    }
}
