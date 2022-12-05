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
        public const string FileDiskCd = "C";
        public const string FileDiskExternal = "E";
        
        //File Import
        public const string FileExtension = "dbf";
        public const string FileNamePrefix = "Export_";
        
        //View Mode - Live View, Stand By
        public const string ViewModeLiveView = "LiveView";
        public const string ViewModeStandBy = "StandBy";

        //Not Selected
        public const string NotSelected = "Not Selected";

        //Preset
        public const int MaxPatientCasePresetName = 40;
        public const int DefaultCalciumThreshold = 180;
        public const int MaxCalciumThreshold = 360;
        public const int MinCalciumThreshold = 0;
        public const int DefaultExpansionThreshold = 90;
        public const int MaxExpansionThreshold = 100;
        public const int MinExpansionThreshold = 0;
        public const double DefaultAppositionThreshold = 0.3;
        public const double MaxAppositionThreshold = 1.0;
        public const double MinAppositionThreshold = 0.0;

        //Expansion calculation
        public const string PresetTapered = "TAPE";
        public const string PresetOther = "OTHE";

        //Image Background Color
        public const int BackgroundColor = 0xFFFFFF;

        //Update Image Interval (msec)
        public const double UpdateImageInterval = 5;

        //Data File Root Path (OCT raw files)
        public const string DataRootPath = "C:\\DataSave";
    }
}
