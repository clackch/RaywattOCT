using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// SoftwareUpdateDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SoftwareUpdateDialogControl : UserControl
    {
        public SoftwareUpdateDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(SoftwareUpdateDialogViewModel));
        }
    }
}