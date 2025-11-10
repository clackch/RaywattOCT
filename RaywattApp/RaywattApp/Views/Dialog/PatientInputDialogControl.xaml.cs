using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// PatientInputDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientInputDialogControl : UserControl
    {
        public PatientInputDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientInputDialogViewModel));
        }
    }
}
