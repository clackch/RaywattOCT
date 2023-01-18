using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class StringModel : ObservableValidator
    {
        [ObservableProperty]
        string _returnString;

        [ObservableProperty]
        string _returnString2;
    }
}
