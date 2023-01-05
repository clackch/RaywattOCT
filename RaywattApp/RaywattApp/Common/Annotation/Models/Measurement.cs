using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaywattApp.Common.Annotation.Models
{
    public class Measurement : ObservableObject
    {
        private int frameNumber;
        public int FrameNumber { get { return frameNumber; } set { frameNumber = value; } }

        private ObservableCollection<AreaGeometry> areaGeometries;
        public ObservableCollection<AreaGeometry> AreaGeometries { get { return areaGeometries; } set { areaGeometries = value; OnPropertyChanged(nameof(AreaGeometries)); } }

        private ObservableCollection<LengthGeometry> lengthGeometries;
        public ObservableCollection<LengthGeometry> LengthGeometries { get { return lengthGeometries; } set { lengthGeometries = value; OnPropertyChanged(nameof(LengthGeometries)); } }

        private List<TextGeometry> textGeometries;
        public List<TextGeometry> TextGeometries { get { return textGeometries; } set { textGeometries = value; OnPropertyChanged(nameof(TextGeometries)); } }
    }
}
