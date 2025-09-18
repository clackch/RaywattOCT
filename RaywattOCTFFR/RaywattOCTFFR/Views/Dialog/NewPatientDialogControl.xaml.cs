using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Dialog;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// NewPatientDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NewPatientDialogControl : UserControl
    {
        public NewPatientDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(NewPatientDialogViewModel));
        }
    }
}
