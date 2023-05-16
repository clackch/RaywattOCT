using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class FileExport : ObservableObject
    {
        [ObservableProperty]
        private string patientId;

        [ObservableProperty]
        private List<string> patientList;

        [ObservableProperty]
        private List<string> selectedItem;

        [ObservableProperty]
        private bool isFromReview;

        [ObservableProperty]
        private int currentFrame;

        [ObservableProperty]
        private List<int> bookmarkedFrames;

        private string type; //Native, DICOM, Standard
        public string Type
        {
            get { return type; }
            set
            {
                type = value;
                OnPropertyChanged(nameof(Type));

                if (isFromReview && type == Constants.ExportTypeNative)
                {
                    material = Constants.ExportMaterialPullback;
                    OnPropertyChanged(nameof(Material));
                }       
            }
        }

        [ObservableProperty]
        private string material; //Pullback, Current Frame, Bookmarked Frames

        [ObservableProperty]
        private bool removeWhenComplete; //Remove when Complete

        [ObservableProperty]
        private Dictionary<string, string> alternatePatientId; //key=id, value=alternate id. value가 빈 값이면, 대체 ID 설정이 안된 상태

        [ObservableProperty]
        private string diskType; //CD/DVD, External Drive

        [ObservableProperty]
        private string externalDrive;

        [ObservableProperty]
        private string externalDrivePath;

        [ObservableProperty]
        private string mediaType;

        private string volumeLabel;
        public string VolumeLabel
        {
            get { return volumeLabel; }
            set 
            {
                if (value.Length <= Constants.MaxVolumeLabel)
                {
                    if (!CommonUtil.ValidatevolumeLabel(value))
                        return;

                    volumeLabel = value;
                    OnPropertyChanged(nameof(VolumeLabel));

                    validateVolumeLabel = "";
                    OnPropertyChanged(nameof(ValidateVolumeLabel));
                }
            }
        }

        [ObservableProperty]
        private string validateVolumeLabel;

        [ObservableProperty]
        private bool isDiskFormat;

        [ObservableProperty]
        private bool ejectWhenComplete;

        [ObservableProperty]
        private bool patientInfoAnonymize; //Patient Information Anonymize

        [ObservableProperty]
        private bool includeRegionCalibration; // Include Region Calibration(Calibrated images only)

        [ObservableProperty]
        private string pullback; //AVI, TIFF

        [ObservableProperty]
        private string stillFrame; //JPEG, Bitmap, TIFF

        [ObservableProperty]
        private bool angioView; //Advanced View - Angio Co-Registration

        [ObservableProperty]
        private bool longitude; //Advanced View - longitude

        [ObservableProperty]
        private bool measureAuto;

        [ObservableProperty]
        private bool measureManual;
    }
}
