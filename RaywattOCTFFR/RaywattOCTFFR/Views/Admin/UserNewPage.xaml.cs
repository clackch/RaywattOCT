using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Admin;

namespace RaywattOCTFFR.Views.Admin
{
    /// <summary>
    /// UserNewPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserNewPage : Page
    {
        public UserNewPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(UserNewViewModel));
        }
    }
}
