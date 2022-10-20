using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class PopupResponse : ObservableValidator
    {
        [ObservableProperty]
        private int popupId;

        [ObservableProperty]
        private bool popupAnswer;

        [ObservableProperty]
        private object? popupParameter;
    }
}
