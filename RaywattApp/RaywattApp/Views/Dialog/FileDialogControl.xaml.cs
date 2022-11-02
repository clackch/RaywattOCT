using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// FileDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileDialogControl : UserControl
    {
        public FileDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileDialogViewModel));
        }
    }
}
