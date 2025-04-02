using System.Windows.Controls;
using RaywattApp.ViewModels.Setting;

namespace RaywattApp.Views.Setting
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
