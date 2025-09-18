using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RaywattOCTFFR.Common.Messages
{
    /// <summary>
    /// NavigationMessage
    /// </summary>
    public class NavigationMessage : ValueChangedMessage<string>
    {
        public object? Parameter { get; set; }

        public NavigationMessage(string value) : base(value)
        {
        }
    }
}
