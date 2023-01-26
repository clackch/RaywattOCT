using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class Code : ObservableObject
    {
        [ObservableProperty]
        private string classification;

        [ObservableProperty]
        private string key;

        [ObservableProperty]
        private string value;

        [ObservableProperty]
        private string buffer1;

        [ObservableProperty]
        private string buffer2;
    }
}
