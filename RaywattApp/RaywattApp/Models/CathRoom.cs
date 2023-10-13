using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RaywattApp.Models
{
    public partial class CathRoom : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _chpFile;

        [ObservableProperty]
        private double _rectLeft;

        [ObservableProperty]
        private double _rectTop;

        [ObservableProperty]
        private double _rectRight;

        [ObservableProperty]
        private double _rectBottom;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private DateTime createDate;

        [ObservableProperty]
        private DateTime updateDate;
    }
}
