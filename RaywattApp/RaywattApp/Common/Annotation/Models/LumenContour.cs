using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Models;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public partial class LumenContour : Contour
    {
        [ObservableProperty]
        private Contour mlContour = new Contour();

        [ObservableProperty]
        private Calcium calcium = new Calcium();

        public new List<Point>? Points
        {
            get { return IsContourEdited() ? points : MlContour.Points; }
            set { points = value; }
        }

        public new double Area
        {
            get { return IsContourEdited() ? area : MlContour.Area; }
            set { area = value; }
        }

        public new Point CenterOfMass
        {
            get { return IsContourEdited() ? centerOfMass : MlContour.CenterOfMass; }
            set { centerOfMass = value; }
        }

        public new DiameterInfo? MinDiameter
        {
            get { return IsContourEdited() ? minDiameter : MlContour.MinDiameter; }
            set { minDiameter = value; }
        }

        public new DiameterInfo? MaxDiameter
        {
            get { return IsContourEdited() ? maxDiameter : MlContour.MaxDiameter; }
            set { maxDiameter = value; }
        }

        public new double MeanDiameter
        {
            get { return IsContourEdited() ? meanDiameter : MlContour.MeanDiameter; }
            set { meanDiameter = value; }
        }

        public new bool Valid
        {
            get { return IsContourEdited() ? valid : MlContour.Valid; }
            set { valid = value; }
        }

        private bool IsOriginData;

        public void SetOriginData(bool flag)
        {
            IsOriginData = flag;
        }

        public void ResetLumenContour()
        {
            Points = new List<Point>();
            Area = 0.0;
            CenterOfMass = new Point();
            MinDiameter = new DiameterInfo();
            MaxDiameter = new DiameterInfo();
            MeanDiameter = 0.0;
            Valid = false;
        }

        private bool IsContourEdited()
        {
            if (IsOriginData)
                return true;

            if(points == null || points.Count == 0)
                return false;

            return true;
        }
    }
}
