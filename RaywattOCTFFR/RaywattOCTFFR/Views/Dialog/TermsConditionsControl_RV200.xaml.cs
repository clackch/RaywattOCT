using System.Windows.Controls;
using RaywattOCTFFR.ViewModels.Dialog;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// TermsConditionsControl_RV200.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TermsConditionsControl_RV200 : UserControl
    {
        public TermsConditionsControl_RV200()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(TermsConditionsDialogViewModel_RV200));
        }
    }
}
