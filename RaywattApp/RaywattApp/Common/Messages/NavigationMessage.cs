using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RaywattApp.Common.Messages
{
    /// <summary>
    /// NavigationMessage
    /// </summary>
    public class NavigationMessage : ValueChangedMessage<string>
    {
        public object? Parameter { get; set; } = null;

        public NavigationMessage(string value) : base(value)
        {
        }
    }
}
