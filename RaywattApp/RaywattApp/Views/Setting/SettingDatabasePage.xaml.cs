using RaywattApp.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattApp.Views.Setting
{
    /// <summary>
    /// SettingDatabasePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingDatabasePage : Page
    {
        public SettingDatabasePage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingDatabaseViewModel));
        }
    }
}
