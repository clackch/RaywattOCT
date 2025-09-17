using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// FileExportDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileExportDialogControl : UserControl
    {
        public FileExportDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileExportDialogViewModel));
        }
    }
}
