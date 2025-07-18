using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaywattApp.Common.Annotation.Models
{
    public class Measurement : ObservableObject
    {
        private int frameNumber;
        public int FrameNumber { get { return frameNumber; } set { frameNumber = value; } }

        private ObservableCollection<AreaGeometry> areaGeometries = new ObservableCollection<AreaGeometry>();
        public ObservableCollection<AreaGeometry> AreaGeometries { get { return areaGeometries; } set { areaGeometries = value; OnPropertyChanged(nameof(AreaGeometries)); } }

        private ObservableCollection<LengthGeometry> lengthGeometries = new ObservableCollection<LengthGeometry>();
        public ObservableCollection<LengthGeometry> LengthGeometries { get { return lengthGeometries; } set { lengthGeometries = value; OnPropertyChanged(nameof(LengthGeometries)); } }

        // add by gordon 250716
        private ObservableCollection<AngleGeometry> angleGeometries = new ObservableCollection<AngleGeometry>();
        public ObservableCollection<AngleGeometry> AngleGeometries { get { return angleGeometries; } set { angleGeometries = value; OnPropertyChanged(nameof(AngleGeometries)); } }
        // add by gordon 250716

        private List<TextGeometry> textGeometries = new List<TextGeometry>();
        public List<TextGeometry> TextGeometries { get { return textGeometries; } set { textGeometries = value; OnPropertyChanged(nameof(TextGeometries)); } }
    }
}
