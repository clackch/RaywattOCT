using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// CathRoomDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CathRoomDialogControl : UserControl
    {
        public CathRoomDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(CathRoomDialogViewModel));
        }
    }
}
