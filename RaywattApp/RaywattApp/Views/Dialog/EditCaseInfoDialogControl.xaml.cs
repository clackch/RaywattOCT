using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
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
