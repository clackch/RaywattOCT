using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
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
