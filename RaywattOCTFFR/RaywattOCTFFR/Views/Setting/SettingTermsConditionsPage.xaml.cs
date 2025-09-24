using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
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
