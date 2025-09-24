using RaywattOCTFFR.ViewModels.Password;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Password
{
    /// <summary>
    /// PasswordExpiryCheckPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PasswordExpiryCheckPage : Page
    {
        public PasswordExpiryCheckPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PasswordExpiryCheckViewModel));
        }
    }
}
