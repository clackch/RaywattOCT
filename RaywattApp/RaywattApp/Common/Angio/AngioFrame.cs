using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Angio;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using OpenCvSharp;

namespace RaywattApp.Common.Angio
{
    public partial class AngioFrame : ObservableObject
    {
        [ObservableProperty]
        private List<CoRegistration> _coRegistration;

        [ObservableProperty]
        private List<ImageSource> _angioImage;

        [ObservableProperty]
        private List<DijkstraHeap> _dijkstraHeap;

        [ObservableProperty]
        private List<Mat> _motionVector;

        public AngioFrame()
        {
            CoRegistration = new List<CoRegistration>();
            AngioImage = new List<ImageSource>();
            DijkstraHeap = new List<DijkstraHeap>();
            MotionVector = new List<Mat>();
        }
    }
}
