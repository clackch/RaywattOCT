using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// FileFolderActionDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileFolderActionDialogControl : UserControl
    {
        public FileFolderActionDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileFolderActionDialogViewModel));
        }
    }
}
