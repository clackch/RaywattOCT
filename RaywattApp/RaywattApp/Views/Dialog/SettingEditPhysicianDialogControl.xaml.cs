using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// SettingEditPhysicianDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingEditPhysicianDialogControl : UserControl
    {
        public SettingEditPhysicianDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SettingEditPhysicianDialogViewModel));
        }
    }
}
