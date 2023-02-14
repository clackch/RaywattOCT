using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
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

        [ObservableProperty]
        private string type; //Native, DICOM, Standard

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
        private string volumeLabel;

        [ObservableProperty]
        private bool ejectWhenComplete;

        [ObservableProperty]
        private string measurements; //Show All, Hide Lumen Contour, Hide All

        [ObservableProperty]
        private bool patientInfoAnonymize; //Patient Information Anonymize

        [ObservableProperty]
        private bool includeRegionCalibration; // Include Region Calibration(Calibrated images only)

        [ObservableProperty]
        private string standardFormat; //AVI, JPEG, Bitmap, TIFF

        [ObservableProperty]
        private bool angioView; //Advanced View - Angio Co-Registration

        [ObservableProperty]
        private bool lumenProfileView; //Advanced View - Lumen Profile

        [ObservableProperty]
        private bool lModeView; //Advanced View - L-Mode
    }
}
