using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Models;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public partial class LumenPlaque : ObservableObject
    {
        [ObservableProperty]
        private Calcium calcium;
    }
}
