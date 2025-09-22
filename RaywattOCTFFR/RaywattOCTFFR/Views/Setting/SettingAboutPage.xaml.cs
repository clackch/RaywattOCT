using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
{
    /// <summary>
    /// SettingAboutPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingAboutPage : Page
    {
        public SettingAboutPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingAboutViewModel));
        }
    }
}
