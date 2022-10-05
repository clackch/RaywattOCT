using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Controls
{
    /// <summary>
    /// SettingPopupControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingPopupControl : UserControl
    {
        public SettingPopupControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "MessagePopupControl", Type = (int)CommonDefinition.PopupType.Message, Level = (int)CommonDefinition.PopupLevel.Info, Parameter ="ID is duplicated." });
        }
    }
}
