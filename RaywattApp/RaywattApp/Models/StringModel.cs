using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class StringModel : ObservableObject
    {
        [ObservableProperty]
        string _returnString;

        [ObservableProperty]
        string _returnString2;
    }
}
