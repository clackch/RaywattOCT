using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace RaywattApp.Common.Messages
{
    public class PatientMessage : ValueChangedMessage<bool>
    {
        public string Id { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public DateTime Birthdate { get; set; }
        public string Gender { get; set; }

        public PatientMessage(bool value) : base(value)
        {
        }
    }
}
