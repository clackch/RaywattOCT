using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Setting;

namespace RaywattOCTFFR.Views.Setting
{
    /// <summary>
    /// SettingDicomPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingDicomPage : Page
    {
        public SettingDicomPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingDicomViewModel));
        }
    }
}
