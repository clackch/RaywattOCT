using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
{
    /// <summary>
    /// SettingLogPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingLogPage : Page
    {
        public SettingLogPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingLogViewModel));
        }
    }
}
