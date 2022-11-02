using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// ConfirmDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConfirmDialogControl : UserControl
    {
        public ConfirmDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ConfirmDialogViewModel));
        }
    }
}
