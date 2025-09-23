using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
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
