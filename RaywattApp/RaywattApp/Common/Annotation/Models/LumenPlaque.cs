using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Models;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenPlaque : ObservableObject
    {
        [ObservableProperty]
        private Calcium calcium;
    }
}
