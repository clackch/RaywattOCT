using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// ConfirmDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Review3dViewMenuControl : UserControl
    {
        public Review3dViewMenuControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(Review3dViewMenuViewModel));
        }
    }
}
