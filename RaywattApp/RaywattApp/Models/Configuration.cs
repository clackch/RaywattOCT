using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class Configuration : ObservableObject
    {
        [ObservableProperty]
        private string? _classification;

        [ObservableProperty]
        private string? _key;

        [ObservableProperty]
        private string? _value;

        [ObservableProperty]
        private string? _buffer;
    }
}
