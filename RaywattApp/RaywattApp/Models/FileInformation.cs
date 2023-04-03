using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RaywattApp.Models
{
    public partial class FileInformation : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private long _size;

        [ObservableProperty]
        private string _displaySize;

        [ObservableProperty]
        private DateTime _lastWriteTime;

        [ObservableProperty]
        private DateTime _creationTime;
    }
}
