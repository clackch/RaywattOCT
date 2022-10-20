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

        private string _accessionNumber;
        public string AccessionNumber
        {
            get { return _accessionNumber; }
            set
            {
                if (value.Length <= 6)
                {
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
                if (value.Length <= 200)
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
        private uint image;

        [ObservableProperty]
        private string createDate;

        [ObservableProperty]
        private string updateDate;

        [ObservableProperty]
        private bool isChecked;
    }
}
