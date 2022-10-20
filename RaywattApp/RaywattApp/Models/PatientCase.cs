using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class PatientCase : ObservableValidator
    {
        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private string patientId;

        [ObservableProperty]
        private string patientName;

        [ObservableProperty]
        private string physicianName;

        [ObservableProperty]
        private string accessionNumber;

        [ObservableProperty]
        private string accessionName;

        [ObservableProperty]
        private string comment;

        [ObservableProperty]
        private string vessel;

        [ObservableProperty]
        private string procedure;

        [ObservableProperty]
        private int thumbnailNo;

        [ObservableProperty]
        private string stillImageYn;

        [ObservableProperty]
        private uint image;

        [ObservableProperty]
        private string createDate;

        [ObservableProperty]
        private string updateDate;

        [ObservableProperty]
        private bool isChecked;
    }
}
