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

        //Export Material - Pullback, Current Frame, Bookmarked Frames
        public const string ExportMaterialPullback = "P";
        public const string ExportMaterialCurrent = "C";
        public const string ExportMaterialBookmarked = "B";

        //Disk Type - CD/DVD, External Drive        
        public const string FileDiskCd = "C";
        public const string FileDiskExternal = "E";

        //Export(Raw) File Option - Leave Unchanged, Remove when Complete
        public const string ExportOptionUnchanged = "U";
        public const string ExportOptionRemove = "R";

        //Export(DICOM) Image Type - Multi-frame True Color Secondary Capture, Secondary Capture, Ultrasound Multi-frame, Intravascular OCT - For Presentation
        public const string ExportImageTypeMultiframe = "M";
        public const string ExportImageTypeSecondary = "S";
        public const string ExportImageTypeUltrasound = "U";
        public const string ExportImageTypeIntravascular = "I";

        //Export(DICOM) Format - RGB, Palette
        public const string ExportFormatRGB = "R";
        public const string ExportFormatPalette = "P";

        //Export(DICOM) Measurements - Show All, Hide Lumen Contour, Hide All
        public const string ExportMeasurementShowAll = "S";
        public const string ExportMeasurementHideLumen = "L";
        public const string ExportMeasurementHideAll = "H";

        //Export(DICOM) Modality - OCT, Other(OT), Ultrasound(US)
        public const string ExportModalityOCT = "O";
        public const string ExportModalityOther = "T";
        public const string ExportModalityUltrasound = "U";

        //Export(Standard) Pullback - AVI, TIFF
        public const string ExportPullbackAVI = "A";
        public const string ExportPullbackTIFF = "T";

        //Export(Standard) Compressor - None, MS-MPEG4 V2, Microsoft Video 1
        public const string ExportCompressorNone = "N";
        public const string ExportCompressorMPEG4 = "M";
        public const string ExportCompressorVideo = "V";

        //Export(Standard) Still Frame - JPEG, Bitmap, TIFF
        public const string ExportStillFrameJPEG = "J";
        public const string ExportStillFrameBitmap = "B";
        public const string ExportStillFrameTIFF = "T";

        //Export(DICOM, Standard) Frame Resolution
        public const int MaxFrameWidth = 1024;
        public const int MinFrameWidth = 704;
        public const double FrameWidthHeight = 1.5;
        public const int FrameTickFrequency = 20;

        //File Import/Export
        public const string FileImageExtension = "bin";
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

        //Data File Root Path (OCT raw files) - ※ Patient Folder (DataRootPath + Patient ID)
        public const string DataRootPath = "C:\\DataSave";

        //Encrypt, Decrypt Public Key
        public const string PublicKey = "raywatt07_hwjckklls_298-87-01441";
    }
}
