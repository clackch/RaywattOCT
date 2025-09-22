using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// FileAlternateIdDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileAlternateIdDialogControl : UserControl
    {
        public FileAlternateIdDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileAlternateIdDialogViewModel));
        }
    }
}
