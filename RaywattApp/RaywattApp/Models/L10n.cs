using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class L10n : ObservableValidator
    {
        [ObservableProperty]
        private string lang;

        [ObservableProperty]
        private string choice;
    }
}
