using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenContour : Contour
    {
        [ObservableProperty]
        private Contour mlContour = new Contour();

        public void CopyMlToLumenContour()
        {
            Points = MlContour.Points;
            MinDiameter = MlContour.MinDiameter;
            MaxDiameter = MlContour.MaxDiameter;
            MeanDiameter = MlContour.MeanDiameter;
            Area = MlContour.Area;
        }
    }
}
