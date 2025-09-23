using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RaywattOCTFFR.Common.Messages
{
    public class PopupNavigationMessage : ValueChangedMessage<string>
    {
        public object? Parameter { get; set; }

        public PopupNavigationMessage(string value) : base(value)
        {

        }
    }
}
