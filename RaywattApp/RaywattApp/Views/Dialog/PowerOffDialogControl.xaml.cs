using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// PowerOffControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PowerOffDialogControl : UserControl
    {
        public PowerOffDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PowerOffDialogViewModel));
        }
    }
}
