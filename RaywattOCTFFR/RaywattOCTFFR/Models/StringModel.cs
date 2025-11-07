using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattOCTFFR.Models
{
    public partial class StringModel : ObservableObject
    {
        [ObservableProperty]
        string _returnString;

        [ObservableProperty]
        string _returnString2;
    }
}
