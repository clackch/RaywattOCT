using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RaywattApp.Common.Messages
{
    /// <summary>
    /// PopupMessage
    /// </summary>
    public class PopupMessage : ValueChangedMessage<bool>
    {
        /// <summary>
        /// 컨트롤 이름
        /// </summary>
        public string ControlName { get; set; }

        public int Type { get; set; }

        public int Level { get; set; }

        public int FileType { get; set; }
        
        /// <summary>
        /// 컨트롤에 전달할 파라메터
        /// </summary>
        public object? Parameter { get; set; } = null;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="value">true : 레이어팝업 오픈, false : 레이어 팝업 닫기</param>
        public PopupMessage(bool value) : base(value)
        {
        }
    }
}
