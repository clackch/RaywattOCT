namespace RaywattApp.Common.Bases
{
    public class Constants
    {
        //Current Page
        public static string CurrentPage = "";

        //[Page List]
        //Patient
        public const string PatientListPage = "Views/PatientListPage.xaml";
        public const string PatientDetailPage = "Views/PatientDetailPage.xaml";
        public const string PatientNewPage = "Views/PatientNewPage.xaml";
        public const string PatientEditPage = "Views/PatientEditPage.xaml";
        //Recording
        public const string RecordingSetupPage = "Views/RecordingSetupPage.xaml";
        public const string LiveViewPage = "Views/LiveViewPage.xaml";
        public const string CalibrationPage = "Views/CalibrationPage.xaml";
        public const string RecordingPage = "Views/RecordingPage.xaml";
        //Review
        public const string ReviewPage = "Views/ReviewPage.xaml";
        public const string Review3dPage = "Views/Review3dPage.xaml";
        public const string ReviewComparePage = "Views/ReviewComparePage.xaml";
        public const string ReviewFfrPage = "Views/ReviewFfrPage.xaml";
        public const string ReviewPresetPage = "Views/ReviewPresetPage.xaml";
        public const string ReviewAngioCoRegPage = "Views/ReviewAngioCoRegPage.xaml";
        //File
        public const string FileExportStep1Page = "Views/File/FileExportStep1Page.xaml";
        public const string FileExportStep2DicomPage = "Views/File/FileExportStep2DicomPage.xaml";
        public const string FileExportStep2NativePage = "Views/File/FileExportStep2NativePage.xaml";
        public const string FileExportStep2StandardPage = "Views/File/FileExportStep2StandardPage.xaml";
        public const string FileImportPage = "Views/File/FileImportPage.xaml";
        //Setting
        public const string SettingAcquisitionPage = "Views/Setting/SettingAcquisitionPage.xaml";
        public const string SettingLocalizationPage = "Views/Setting/SettingLocalizationPage.xaml";
        public const string SettingDatabasePage = "Views/Setting/SettingDatabasePage.xaml";
        public const string SettingPhysicianPage = "Views/Setting/SettingPhysicianPage.xaml";

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

        //File Type
        public const string FileTypeExport = "E";
        public const string FileTypeImport = "I";

        //Folder Action
        public const string FolderActionCreate = "C";
        public const string FolderActionRename = "R";

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

        //Background Color
        public const int BackgroundColor = 0x0D0D0D;
        public const int CardBackgroundColor = 0x161616;

        //Update Image Interval (msec)
        public const double UpdateImageInterval = 5;

        //Data File Root Path (OCT raw files) - ※ Patient Folder (DataRootPath + Patient ID)
        public const string DataRootPath = "C:\\DataSave";

        //Encrypt, Decrypt Public Key
        public const string PublicKey = "raywatt07_hwjckklls_298-87-01441";

        //Review - 2D - Side Menu
        public const string LeftUpMenu = "LeftUpMenu";
        public const string LeftDownMenu = "LeftDownMenu";
        public const string RightMenu = "RightMenu";
        public const double SideBarCollapseSize = 50;
        public const double LeftSideBarExpandSize = 250;
        public const double RightSideBarExpandAngioSize = 350;
        public const double RightSideBarExpandDefaultSize = 250;

        //Review - 2D - Longitude
        public const string LongitudeProfile = "Profile";
        public const string LongitudeLMode = "LMode";

        //Measurement
        public const string MeasureAddArea = "AA";
        public const string MeasureAddLeng = "AL";
        public const string MeasureAddText = "AT";
        public const string MeasureDeleAll = "DA";
        public const string MeasureDelArea = "A_";
        public const string MeasureDelLeng = "L_";
        public const string MeasureDelLMod = "M_";
    }
}
