using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// PasswordChangeDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PasswordChangeDialogControl : UserControl
    {
        public PasswordChangeDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PasswordChangeDialogViewModel));
        }
    }
}
