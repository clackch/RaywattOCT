using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;

namespace RaywattApp.Models
{
    public partial class Physician : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        private string _lastname;
        public string Lastname
        {
            get { return _lastname; }
            set
            {
                if (value.Length <= Constants.MaxLastname)
                {
                    if (!CommonUtil.ValidateText(value))
                        return;

                    _lastname = value;
                    OnPropertyChanged(nameof(Lastname));
                }
            }
        }

        private string _firstname;
        public string Firstname
        {
            get { return _firstname; }
            set
            {
                if (value.Length <= Constants.MaxFirstname)
                {
                    if (!CommonUtil.ValidateText(value))
                        return;

                    _firstname = value;
                    OnPropertyChanged(nameof(Firstname));
                }
            }
        }

        [ObservableProperty]
        private string? _name;

        [ObservableProperty]
        private string? _flushMedia;

        [ObservableProperty]
        private string? _pullbackTrigger;

        [ObservableProperty]
        private string? _pullbackType;

        [ObservableProperty]
        private string? _colormap;

        [ObservableProperty]
        private int _calciumThreshold;

        [ObservableProperty]
        private int _expansionThreshold;

        private double _appositionThreshold;
        public double AppositionThreshold
        {
            get { return _appositionThreshold; }
            set
            {
                if (value >= 0.0 && value <= 1.0)
                {
                    _appositionThreshold = Math.Round(value, 2);
                    OnPropertyChanged(nameof(AppositionThreshold));
                }
            }
        }

        [ObservableProperty]
        private DateTime _createDate;

        [ObservableProperty]
        private DateTime _updateDate;
    }
}
