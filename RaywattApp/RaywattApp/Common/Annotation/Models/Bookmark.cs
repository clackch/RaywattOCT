using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class Bookmark : ObservableObject
    {
        [ObservableProperty]
        private int frameNumber;

        [ObservableProperty]
        private double longitudeX;
    }
}
