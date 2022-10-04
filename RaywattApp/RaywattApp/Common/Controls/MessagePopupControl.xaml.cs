using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Common.Controls
{
    /// <summary>
    /// InformationPopupControl.xaml 코드 비하인드
    /// </summary>
    public partial class MessagePopupControl : UserControl
    {
        public MessagePopupControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Message });
        }

    }
}
