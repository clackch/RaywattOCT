using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class Calcium : ObservableObject
    {
        [ObservableProperty]
        private List<Tuple<double, double>> _list;

        [ObservableProperty]
        private int _totalAngle;

        [ObservableProperty]
        private double _maxThickness;

        [ObservableProperty]
        private double _maxThicknessDegree;
    }
}
