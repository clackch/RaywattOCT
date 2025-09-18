using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace RaywattOCTFFR.Common.Angio
{
    public partial class CoRegistration : ObservableObject
    {
        [ObservableProperty]
        private List<Point> _trackPoints;

        [ObservableProperty]
        private List<List<Point>> _line;

        [ObservableProperty]
        private Point _markerPoint;

        public CoRegistration()
        {
            Line = new List<List<Point>>();
            TrackPoints = new List<Point>();
            MarkerPoint = new Point();
        }
    }
}
