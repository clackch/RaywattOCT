using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RaywattApp.Models
{
    public partial class Patient : ObservableValidator
    {
        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private string lastname;

        [ObservableProperty]
        private string firstname;

        [ObservableProperty]
        private DateTime birthdate;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        private string createDate;

        [ObservableProperty]
        private string updateDate;
    }
}
