using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// AlertTimerDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlertTimerDialogControl : UserControl
    {
        public AlertTimerDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(AlertTimerDialogViewModel));
        }
    }
}
