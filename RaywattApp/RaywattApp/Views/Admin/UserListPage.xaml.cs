using System.Windows.Controls;
using RaywattApp.ViewModels.Admin;

namespace RaywattApp.Views.Admin
{
    /// <summary>
    /// UserListPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserListPage : Page
    {
        public UserListPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(UserListViewModel));
        }
    }
}
