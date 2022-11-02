using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// AlertDialogView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlertDialogControl : UserControl
    {
        public AlertDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(AlertDialogViewModel));
        }
    }
}
