using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class FileExport : ObservableValidator
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
        private string imageType; //Multi-frame True Color Secondary Capture, Secondary Capture, Ultrasound Multi-frame, Intravascular OCT - For Presentation

        [ObservableProperty]
        private string format; //RGB, Palette

        [ObservableProperty]
        private string measurements; //Show All, Hide Lumen Contour, Hide All

        [ObservableProperty]
        private string modality; //OCT, Other(OT), Ultrasound(US)

        private int frameWidth; //Frame Resolution Width
        public int FrameWidth
        {
            get { return frameWidth; }
            set 
            { 
                frameWidth = value;
                frameHeight = (int)(value * Constants.FrameWidthHeight);
                OnPropertyChanged(nameof(FrameWidth));
                OnPropertyChanged(nameof(FrameHeight));
            }
        }

        [ObservableProperty]
        private int frameHeight; //Frame Resolution Height

        [ObservableProperty]
        private bool patientInfoAnonymize; //Patient Information Anonymize

        [ObservableProperty]
        private bool includeRegionCalibration; // Include Region Calibration(Calibrated images only)

        [ObservableProperty]
        private string pullback; //AVI, TIFF

        [ObservableProperty]
        private string compressor; //None, MS-MPEG4 V2, Microsoft Video 1

        [ObservableProperty]
        private string stillFrame; //JPEG, Bitmap, TIFF

        [ObservableProperty]
        private bool angioView; //Advanced View - Angio Co-Registration
    }
}
