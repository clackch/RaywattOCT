using RaywattApp.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattApp.Views.Setting
{
    /// <summary>
    /// SettingServicePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingServicePage : Page
    {
        public SettingServicePage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingServiceViewModel));
        }
    }
}
