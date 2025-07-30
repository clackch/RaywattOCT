using System.Windows.Controls;
using RaywattApp.ViewModels.Dialog;


namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// MwlSearchDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MwlSearchDialogControl : UserControl
    {
        public MwlSearchDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(MwlSearchDialogViewModel));
        }
    }
}
