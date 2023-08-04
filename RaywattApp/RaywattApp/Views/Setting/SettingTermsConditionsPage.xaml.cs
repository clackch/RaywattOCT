using RaywattApp.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattApp.Views.Setting
{
    /// <summary>
    /// SettingTermsConditionsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingTermsConditionsPage : Page
    {
        public SettingTermsConditionsPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingTermsConditionsViewModel));
        }
    }
}
