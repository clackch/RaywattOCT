using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RaywattApp.Models
{
    public partial class PatientCase : ObservableObject
    {
        [ObservableProperty]
        private AngioFrame angioFrame;

        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private string patientId;

        [ObservableProperty]
        private string patientName;

        [ObservableProperty]
        private DateTime birthdate;

        [ObservableProperty]
        private string gender;

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
        private string procedure;

        [ObservableProperty]
        private string vessel;

        [ObservableProperty]
        private string location;

        [ObservableProperty]
        private int numOfFrames;

        [ObservableProperty]
        private string image;

        private string imageFullPath;
        public string ImageFullPath
        {
            get { return IsAnonymize ? imageFullPath : Constants.DataRootPath + "\\" + PatientId + "\\" + Image; }
            set { imageFullPath = value; }
        }

        [ObservableProperty]
        private double imageResolution;

        [ObservableProperty]
        private int _zOffset;

        [ObservableProperty]
        private double fieldOfView;

        [ObservableProperty]
        private bool isAnonymize;

        [ObservableProperty]
        private long imageSize;

        [ObservableProperty]
        private string? _pullbackType;

        [ObservableProperty]
        private string? _pullbackLength;

        [ObservableProperty]
        private bool _angioYn;

        [ObservableProperty]
        private bool _angioCoRegistration;

        [ObservableProperty]
        private double _indicatorDegree;

        [ObservableProperty]
        private string? _flushMedia;

        [ObservableProperty]
        private string? _pullbackTrigger;

        [ObservableProperty]
        private string? _colormap;

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
        private int _brightness;

        [ObservableProperty]
        private int _contrast;

        [ObservableProperty]
        private int _sectionProximal;

        [ObservableProperty]
        private int _sectionDistal;

        [ObservableProperty]
        private double _sheathDiameter;

        [ObservableProperty]
        private string? _bookmark;

        [ObservableProperty]
        private string? _longitude;

        [ObservableProperty]
        private string? _crossSection;

        [ObservableProperty]
        private List<LumenContour>? _lumenContours;

        [ObservableProperty]
        private string _strLumenContour;

        [ObservableProperty]
        private List<LumenSidebranch>? _lumenSidebranches;

        [ObservableProperty]
        private string _strLumenSidebranch;

        [ObservableProperty]
        private List<LumenStent>? _lumenStents;

        [ObservableProperty]
        private string _strLumenStent;

        [ObservableProperty]
        private List<LumenGuidewire>? _lumenGuidewires;

        [ObservableProperty]
        private string _strLumenGuidewire;

        [ObservableProperty]
        private double _guidewireRadius;
        
        [ObservableProperty]
        private string _ffrPlaque;

        [ObservableProperty]
        private FfrFeature _ffrFeature;

        [ObservableProperty]
        private string _strCoRegistration;

        [ObservableProperty]
        private List<List<UIElement>> _measureUIElements;

        [ObservableProperty]
        private DateTime createDate;

        [ObservableProperty]
        private DateTime updateDate;

        [ObservableProperty]
        private bool isChecked;
    }
}
