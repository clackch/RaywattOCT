using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System;

namespace RaywattApp.Models
{
    public partial class PatientCasePreset : ObservableObject
    {
        [ObservableProperty]
        private string? id;

        private string? _presetName;
        public string PresetName
        {
            get { return _presetName; }
            set
            {
                if (value.Length <= Constants.MaxPatientCasePresetName)
                {
                    _presetName = value;
                    OnPropertyChanged(nameof(PresetName));

                    validatePresetName = "";
                    OnPropertyChanged(nameof(ValidatePresetName));
                }
            }
        }

        [ObservableProperty]
        private string validatePresetName;

        [ObservableProperty]
        private int _calciumThreshold;

        [ObservableProperty]
        private string? _expansionCalculation;

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
        private bool _defaultSet;

        [ObservableProperty]
        private DateTime _createDate;

        [ObservableProperty]
        private DateTime _updateDate;
    }
}
