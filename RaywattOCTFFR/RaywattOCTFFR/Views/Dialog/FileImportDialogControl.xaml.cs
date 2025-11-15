using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// FileImportDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportDialogControl : UserControl
    {
        public FileImportDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportDialogViewModel));
        }
    }
}
