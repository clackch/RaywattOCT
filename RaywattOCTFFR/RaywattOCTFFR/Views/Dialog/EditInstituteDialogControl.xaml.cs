using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Dialog;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// EditInstituteDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EditInstituteDialogControl : UserControl
    {
        public EditInstituteDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(EditInstituteDialogViewModel));
        }
    }
}
