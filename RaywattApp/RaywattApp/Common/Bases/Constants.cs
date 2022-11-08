namespace RaywattApp.Common.Bases
{
    public class Constants
    {
        //Max Length
        public const int MaxPatientId = 9;
        public const int MaxPatientLastname = 20;
        public const int MaxPatientFirstname = 20;
        public const int MaxPatientCaseAccessionNumber = 6;
        public const int MaxPatientCaseComment = 200;
        public const int MaxPhysicianName = 40;

        //Page
        public const int PageNumberMax = 5;
        public const int PageSizeList = 10;
        public const int PageSizeListChoice1 = 10;
        public const int PageSizeListChoice2 = 30;
        public const int PageSizeListChoice3 = 50;
        public const int PageSizeDetail = 5;

        //Gender
        public const string GenderMale = "M";
        public const string GenderFemale = "F";

        //Export Type - Native, DICOM, Standard
        public const string ExportTypeNative = "N";
        public const string ExportTypeDicom = "D";
        public const string ExportTypeStandard = "S";

        //Export Purpose - Archive, Share, Report a Problem
        public const string ExportPurposeArchive = "A";
        public const string ExportPurposeShare = "S";
        public const string ExportPurposeReport = "R";

        //Export File Option - Leave Unchanged, Mark as Archived, Remove when Complete
        public const string ExportOptionUnchanged = "U";
        public const string ExportOptionArchived = "A";
        public const string ExportOptionRemove = "R";

        //Disk Type - CD/DVD, External Drive
        public const string ExportDiskCd = "C";
        public const string ExportDiskExternal = "E";

        //View Mode - Live View, Stand By
        public const string ViewModeLiveView = "LiveView";
        public const string ViewModeStandBy = "StandBy";
    }
}
