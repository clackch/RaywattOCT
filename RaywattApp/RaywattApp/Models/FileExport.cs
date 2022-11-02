using CommunityToolkit.Mvvm.ComponentModel;
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
        private string type; //Native, DICOM, Standard

        [ObservableProperty]
        private string material; //Pullback, Current Frame, Bookmarked Frames

        [ObservableProperty]
        private string purpose; //Archive, Share, Report a Problem

        [ObservableProperty]
        private bool passwordProtected;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string confirmPassword;

        [ObservableProperty]
        private Dictionary<string, string> alternatePatientId; //key=id, value=alternate id. value가 빈 값이면, 대체 ID 설정이 안된 상태

        [ObservableProperty]
        private string fileOption; //Leave Unchanged, Mark as Archived, Remove when Complete

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
    }
}
