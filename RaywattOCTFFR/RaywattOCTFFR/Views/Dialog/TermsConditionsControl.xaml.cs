using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// TermsConditionsControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TermsConditionsControl : UserControl
    {
        public TermsConditionsControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(TermsConditionsDialogViewModel));
        }
    }
}
