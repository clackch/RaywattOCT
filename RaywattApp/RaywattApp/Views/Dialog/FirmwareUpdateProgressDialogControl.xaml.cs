using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// FirmwareUpdateProgressDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FirmwareUpdateProgressDialogControl : UserControl
    {
        public FirmwareUpdateProgressDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FirmwareUpdateProgressDialogViewModel));
        }
    }
}
