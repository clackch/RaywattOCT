using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Admin;

namespace RaywattOCTFFR.Views.Admin
{
    /// <summary>
    /// UserEditPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserEditPage : Page
    {
        public UserEditPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(UserEditViewModel));
        }
    }
}
