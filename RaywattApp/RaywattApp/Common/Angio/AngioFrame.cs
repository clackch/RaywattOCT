using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Angio;
using System.Collections.Generic;
using System.Windows.Media;

namespace RaywattApp.Common.Angio
{
    internal partial class AngioFrame : ObservableObject
    {
        [ObservableProperty]
        private List<CoRegistration> _coRegistration;

        [ObservableProperty]
        private List<ImageSource> _angioImage;

        // 추가 필요한 멤버 및 함수는 여기에.
    }
}
