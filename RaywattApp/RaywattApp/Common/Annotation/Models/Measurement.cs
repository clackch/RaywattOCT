using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace RaywattApp.Common.Annotation.Models
{
    public class Measurement : ObservableObject
    {
        private int frameNumber;
        public int FrameNumber { get { return frameNumber; } set { frameNumber = value; } }

        private List<AreaGeometry> areaGeometries;
        public List<AreaGeometry> AreaGeometrys { get { return areaGeometries; } set { areaGeometries = value; OnPropertyChanged(nameof(AreaGeometrys)); } }

        private List<LengthGeometry> lengthGeometries;
        public List<LengthGeometry> LengthGeometries { get { return lengthGeometries; } set { lengthGeometries = value; OnPropertyChanged(nameof(LengthGeometries)); } }

        private List<TextGeometry> textGeometries;
        public List<TextGeometry> TextGeometries { get { return textGeometries; } set { textGeometries = value; OnPropertyChanged(nameof(TextGeometries)); } }
    }
}
