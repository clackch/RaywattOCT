using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Util;
using System.Collections.Generic;

namespace RaywattOCTFFR.Models
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
        private bool angioEnabled;

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
        private string externalDrive;

        [ObservableProperty]
        private string externalDrivePath;

        [ObservableProperty]
        private bool patientInfoAnonymize; //Patient Information Anonymize

        [ObservableProperty]
        private string pullback; //AVI, TIFF

        [ObservableProperty]
        private string stillFrame; //JPEG, Bitmap, TIFF

        [ObservableProperty]
        private bool longitude; //Advanced View - longitude

        [ObservableProperty]
        private bool measureAuto;

        [ObservableProperty]
        private bool measureManual;
    }
}
