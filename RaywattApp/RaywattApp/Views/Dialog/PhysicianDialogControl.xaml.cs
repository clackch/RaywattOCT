using RaywattApp.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattApp.Views.Dialog
{
    /// <summary>
    /// PhysicianDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PhysicianDialogControl : UserControl
    {
        public PhysicianDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PhysicianDialogViewModel));
        }
    }
}
