using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// FileBrowseFolderDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileFolderBrowseDialogControl : UserControl
    {
        public FileFolderBrowseDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileFolderBrowseDialogViewModel));
        }
    }
}
