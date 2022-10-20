using RaywattApp.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattApp.Views.Setting
{
    /// <summary>
    /// SettingPhysicianPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingPhysicianPage : Page
    {
        public SettingPhysicianPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingPhysicianViewModel));
        }
    }
}
