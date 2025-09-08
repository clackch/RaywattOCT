using RaywattApp.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattApp.Views.Setting
{
    public partial class SettingPasswordChangePage : Page
    {
        public SettingPasswordChangePage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingPasswordChangeViewModel));
        }
    }
}
