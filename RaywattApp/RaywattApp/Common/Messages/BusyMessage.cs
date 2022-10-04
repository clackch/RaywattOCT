using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RaywattApp.Common.Messages
{
    /// <summary>
    /// BusyMessage
    /// </summary>
    public class BusyMessage : ValueChangedMessage<bool>
    {
        /// <summary>
        /// BusyId
        /// </summary>
        public string BusyId { get; set; }

        /// <summary>
        /// BusyText
        /// </summary>
        public string BusyText { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public BusyMessage(bool value) : base(value)
        {
        }
    }
}
