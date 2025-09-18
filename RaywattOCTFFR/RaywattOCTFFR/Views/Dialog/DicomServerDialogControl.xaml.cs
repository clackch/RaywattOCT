using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Dialog;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// DicomServerDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DicomServerDialogControl : UserControl
    {
        public DicomServerDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(DicomServerDialogViewModel));
        }
    }
}
