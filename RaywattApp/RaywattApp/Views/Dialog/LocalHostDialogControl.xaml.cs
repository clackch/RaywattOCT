using System.Windows.Controls;
using RaywattApp.ViewModels.Dialog;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// LocalHostDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LocalHostDialogControl : UserControl
    {
        public LocalHostDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(LocalHostDialogViewModel));
        }
    }
}
