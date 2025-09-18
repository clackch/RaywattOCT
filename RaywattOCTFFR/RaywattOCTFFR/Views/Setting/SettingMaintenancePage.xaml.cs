using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
{
    /// <summary>
    /// SettingMaintenancePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingMaintenancePage : Page
    {
        public SettingMaintenancePage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingMaintenanceViewModel));
        }
    }
}
