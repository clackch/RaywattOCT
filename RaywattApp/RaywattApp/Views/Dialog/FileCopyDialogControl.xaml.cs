using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// FileCopyDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileCopyDialogControl : UserControl
    {
        public FileCopyDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileCopyDialogViewModel));
        }
    }
}
