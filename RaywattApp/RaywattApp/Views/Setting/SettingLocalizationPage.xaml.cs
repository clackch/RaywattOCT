using RaywattApp.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattApp.Views.Setting
{
    /// <summary>
    /// SettingLocalizationPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingLocalizationPage : Page
    {
        public SettingLocalizationPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingLocalizationViewModel));
        }
    }
}
