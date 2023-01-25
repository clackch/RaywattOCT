using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;

namespace RaywattApp.Models
{
    public partial class PatientCase : ObservableObject
    {
        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private string patientId;

        [ObservableProperty]
        private string patientName;

        [ObservableProperty]
        private string physicianName;

        private string _accessionNumber;
        public string AccessionNumber
        {
            get { return _accessionNumber; }
            set
            {
                if (value.Length <= Constants.MaxPatientCaseAccessionNumber)
                {
                    if (!CommonUtil.ValidateNumber(value))
                        return;

                    _accessionNumber = value;
                    OnPropertyChanged(nameof(AccessionNumber));
                }
            }
        }

        [ObservableProperty]
        private string accessionName;

        private string _comment;
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (value.Length <= Constants.MaxPatientCaseComment)
                {
                    _comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }

        [ObservableProperty]
        private string vessel;

        [ObservableProperty]
        private string procedure;

        [ObservableProperty]
        private int thumbnailNo;

        [ObservableProperty]
        private string stillImageYn;

        [ObservableProperty]
        private string image;

        [ObservableProperty]
        private long imageSize;

        [ObservableProperty]
        private string? _pullbackType;

        [ObservableProperty]
        private int _brightness;

        [ObservableProperty]
        private int _contrast;

        [ObservableProperty]
        private bool _angioCoRegistration;

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
                }
            }
        }

        [ObservableProperty]
        private int _calciumThreshold;

        [ObservableProperty]
        private string? _expansionCalculation;

        [ObservableProperty]
        private int _expansionThreshold;

        [ObservableProperty]
        private double _appositionThreshold;

        [ObservableProperty]
        private string? _measurements;
        
        [ObservableProperty]
        private string? _bookmarks;

        [ObservableProperty]
        private DateTime createDate;

        [ObservableProperty]
        private DateTime updateDate;

        [ObservableProperty]
        private bool isChecked;
    }
}
