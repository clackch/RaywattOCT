using System.Windows.Controls;
using RaywattApp.ViewModels.Setting;

namespace RaywattApp.Views.Setting
{
    /// <summary>
    /// SettingTermsConditionsPage_RV200.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingTermsConditionsPage_RV200 : Page
    {
        public SettingTermsConditionsPage_RV200()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingTermsConditionsViewModel_RV200));
        }
    }
}
