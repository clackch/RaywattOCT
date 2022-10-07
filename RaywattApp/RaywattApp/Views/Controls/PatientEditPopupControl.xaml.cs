using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Controls
{
    /// <summary>
    /// PatientEditPopupControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientEditPopupControl : UserControl
    {
        public PatientEditPopupControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.PatientEdit });
        }
    }
}
