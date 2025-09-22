using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// SettingDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingDialogControl : UserControl
    {
        public SettingDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingDialogViewModel));
        }
    }
}
