using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public class LengthGeometry : ObservableObject
    {
        private Point firstPoint;
        public Point FirstPoint { get { return firstPoint; } set { firstPoint = value; } }

        private Point secondPoint;
        public Point SecondPoint { get { return secondPoint; } set { secondPoint = value; } }

        private int group;
        public int Group { get { return group; } set { group = value; OnPropertyChanged(nameof(Group)); } }

        private double length;
        public double Length { get { return length; } set { length = value; OnPropertyChanged(nameof(Length)); } }
    }
}