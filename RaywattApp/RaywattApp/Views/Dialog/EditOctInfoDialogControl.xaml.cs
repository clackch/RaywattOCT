using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// EditOctInfoDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EditOctInfoDialogControl : UserControl
    {
        public EditOctInfoDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(EditOctInfoDialogViewModel));
        }
    }
}
