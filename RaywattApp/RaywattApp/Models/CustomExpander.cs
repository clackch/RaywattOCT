using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class CustomExpander : ObservableValidator
    {
        [ObservableProperty]
        private string key;

        [ObservableProperty]
        private string value;

        [ObservableProperty]
        private bool isSelected;
    }
}
