using System.Windows.Controls;
using RaywattApp.ViewModels.Dialog;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// DicomPacsDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DicomPacsDialogControl : UserControl
    {
        public DicomPacsDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(DicomPacsDialogViewModel));
        }
    }
}
