using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// UpdateSelectionDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UpdateSelectionDialogControl : UserControl
    {
        public UpdateSelectionDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(UpdateSelectionDialogViewModel));
        }
    }
}