using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// EditCaseInfoDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EditCaseInfoDialogControl : UserControl
    {
        public EditCaseInfoDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(EditCaseInfoDialogViewModel));
        }
    }
}
