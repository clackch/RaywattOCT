using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class User : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string _password;

        [ObservableProperty]
        private string _comment;

        [ObservableProperty]
        private DateTime _passwordChangedAt;

        [ObservableProperty]
        private bool _passwordReset;

        [ObservableProperty]
        private DateTime _termsAgreedAt;

        [ObservableProperty]
        private DateTime _createDate;

        [ObservableProperty]
        private DateTime _updateDate;
    }
}
