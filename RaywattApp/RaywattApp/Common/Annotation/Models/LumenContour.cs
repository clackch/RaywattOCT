using CommunityToolkit.Mvvm.ComponentModel;

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
            Valid = MlContour.Valid;
        }
    }
}
