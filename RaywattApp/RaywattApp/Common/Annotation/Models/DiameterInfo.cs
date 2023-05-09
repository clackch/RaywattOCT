using RaywattApp.Common.Bases;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public class DiameterInfo
    {
        public Point point1, point2;
        public double diameter { get; set; }

        public double DisplayDiameter { get { return diameter / Constants.ScaleLength; } }
    }
}
