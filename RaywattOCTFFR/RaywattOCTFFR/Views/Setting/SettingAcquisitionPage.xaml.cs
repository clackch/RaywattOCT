using RaywattOCTFFR.ViewModels.Setting;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Setting
{
    /// <summary>
    /// SettingAcquisitionPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingAcquisitionPage : Page
    {
        public SettingAcquisitionPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingAcquisitionViewModel));
        }
    }
}
