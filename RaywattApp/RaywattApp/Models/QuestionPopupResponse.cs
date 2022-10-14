using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class QuestionPopupResponse : ObservableValidator
    {
        [ObservableProperty]
        private int questionId;

        [ObservableProperty]
        private bool questionResponse;
    }
}
