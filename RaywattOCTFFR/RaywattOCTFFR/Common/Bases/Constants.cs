using System.Windows.Media;

namespace RaywattOCTFFR.Common.Bases
{
    public class Constants
    {
        //MainWindow for Cursor
        public static readonly System.Windows.Window mainWindow = System.Windows.Application.Current.MainWindow;

        //Current Page
        private static string _currentPage = "";
        public static string CurrentPage
        {
            get => _currentPage;
            set => _currentPage = value;
        }

        //[Page List]
        //Outset
        public const string OutsetLoginPage = "Views/OutsetLoginPage.xaml";
        public const string OutsetLoadingPage = "Views/OutsetLoadingPage.xaml";
        //Patient
        public const string PatientListPage = "Views/PatientListPage.xaml";
        public const string PatientDetailPage = "Views/PatientDetailPage.xaml";
        public const string PatientEditPage = "Views/PatientEditPage.xaml";
        //Review
        public const string ReviewPage = "Views/ReviewPage.xaml";
        public const string ReviewFfrSettingPage = "Views/ReviewFfrSettingPage.xaml";
        public const string ReviewFfrPage = "Views/ReviewFfrPage.xaml";
        public const string ReviewPresetPage = "Views/ReviewPresetPage.xaml";
        public const string ReviewLumenEditPage = "Views/ReviewLumenEditPage.xaml";
        public const string ReviewCalibrationPage = "Views/ReviewCalibrationPage.xaml";
        //File
        public const string FileImportPage = "Views/File/FileImportPage.xaml";
        public const string FileImportStep1Page = "Views/File/FileImportStep1Page.xaml";
        public const string FileImportStep2Page = "Views/File/FileImportStep2Page.xaml";
        public const string FileImportStep3Page = "Views/File/FileImportStep3Page.xaml";
        public const string FileImportStep4Page = "Views/File/FileImportStep4Page.xaml";
        public const string FileImportStep5Page = "Views/File/FileImportStep5Page.xaml";
        //Setting
        public const string SettingDatabasePage = "Views/Setting/SettingDatabasePage.xaml";
        public const string SettingAboutPage = "Views/Setting/SettingAboutPage.xaml";
        public const string SettingLogPage = "Views/Setting/SettingLogPage.xaml";
        public const string SettingTermsConditionsPage = "Views/Setting/SettingTermsConditionsPage.xaml";
        public const string SettingPasswordChangePage = "Views/Setting/SettingPasswordChangePage.xaml";

        //Admin
        public const string UserListPage = "Views/Admin/UserListPage.xaml";
        public const string UserNewPage = "Views/Admin/UserNewPage.xaml";
        public const string UserEditPage = "Views/Admin/UserEditPage.xaml";

        //Password
        public const string InitialPasswordSetupPage = "Views/Password/InitialPasswordSetupPage.xaml";
        public const string PasswordExpiryCheckPage = "Views/Password/PasswordExpiryCheckPage.xaml";

        //Resolution
        public const double ApplicationWidth = 1920;
        public const double ApplicationHeight = 1080;
        public const double FileExportDialogWidth = 860;
        public const double FileExportDialogHeight = 741;
        public const double FileImportDialogWidth = 860;
        public const double FileImportDialogHeight = 870;
        public const double SettingDialogWidth = 860;
        public const double SettingDialogHeight = 714;
        public const double ConfirmDialogWidth = 360;
        public const double ConfirmDialogHeight = 219;
        public const double AlertDialogWidth = 360;
        public const double AlertDialogHeight = 219;
        public const double FileAlternateIdDialogWidth = 472;
        public const double FileAlternateIdDialogHeight = 472;
        public const double FileFolderBrowseDialogWidth = 472;
        public const double FileFolderBrowseDialogHeight = 659;
        public const double FileFolderActionDialogWidth = 472;
        public const double FileFolderActionDialogHeight = 370;
        public const double FileImportActionDialogWidth = 472;
        public const double FileImportActionDialogHeight = 492;
        public const double FileCopyDialogWidth = 460;
        public const double FileCopyDialogHeight = 219;
        public const double SettingInsideDialogWidth = 584;
        public const double SettingInsideDialogHeight = 500;
        public const double SettingEditPhysicianDialogWidth = 472;
        public const double SettingEditPhysicianDialogHeight = 279;
        public const double PatientCaseEditDialogWidth = 860;
        public const double PatientCaseEditDialogHeight = 734;
        public const double TermsConditionsDialogWidth = 860;
        public const double TermsConditionsDialogHeight = 660;
        public const double PowerOffDialogWidth = 472;
        public const double PowerOffDialogHeight = 269;
        public const double CathRoomDialogWidth = 472;
        public const double CathRoomDialogHeight = 472;
        public const double PhysicianDialogWidth = 472;
        public const double PhysicianDialogHeight = 659;
        public const double LocalHostDialogWidth = 860;
        public const double LocalHostDialogHeight = 614;
        public const double DicomServerDialogWidth = 860;
        public const double DicomServerDialogHeight = 492;
        public const double NewPatientDialogWidth = 360;
        public const double NewPatientDialogHeight = 269;
        public const double DicomPacsDialogWidth = 860;
        public const double DicomPacsDialogHeight = 606;
        public const double EditInstituteDialogWidth = 472;
        public const double EditInstituteDialogHeight = 380;
        public const double PasswordChangeDialogWidth = 472;
        public const double PasswordChangeDialogHeight = 552;

        //Max Length
        public const int MaxPatientId = 64;
        public const int MaxLastname = 64;
        public const int MaxFirstname = 64;
        public const int MaxPatientCaseAccessionNumber = 16;
        public const int MaxPatientCaseComment = 200;
        public const int MaxVolumeLabel = 15;
        public const int MaxConfigurationValue = 100;

        //Page
        public const int PageNumberMax = 5;
        public const int PageSizeList = 11;
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

        //Export Status
        public const string ExportStatusCopyFile = "Copy file(s)";
        public const string ExportStatusConvertImage = "Convert Image(s)";
        public const string ExportStatusMakeImage = "Make Image(s)";
        public const string ExportStatusSaveVideo = "Save Video";
        public const string ExportStatusSaveMultipleFrames = "Save Multiple Frames";
        public const string ExportStatusSaveMultipleFiles = "Save Multiple Image Files";
        public const string ExportStatusSaveFile = "Save File";
        public const string ExportStatusCompleted = "Completed";
        public const string ExportStatusTransferDicom = "Transfer DICOM File(s)";

        //Export(Standard) Pullback - AVI, TIFF
        public const string ExportPullbackAVI = "MP4";
        public const string ExportPullbackTIFF = "TIF";

        //Export(Standard) Still Frame - JPEG, Bitmap, TIFF
        public const string ExportStillFrameJPEG = "JPG";
        public const string ExportStillFrameBitmap = "BMP";
        public const string ExportStillFrameTIFF = "TIF";

        //Export Anonymous
        public const string ExportAnonymous = "Anonymous";

        //Export Size (Standard)
        public const double ExportJpegCompression = 0.1f;

        //Export Image
        public const int ExportAnnotationFontSize = 12;

        //Export DICOM Prefix
        public const string ExportDicomPrefix = "IMG";
        public const double DICOMPhysicalDeltaXY = 0.00684931506849; /*0.0289256198347107;*/ // mm

        //Import Type
        public const string ImportTypeDicom = "DICOM";
        public const string ImportTypeTiff = "TIFF";
        public const string ImportTypeRaw = "RAW";

        //File Import/Export
        public const string FileImageExtension = "bin";
        public const string FileExtension = "dbf";
        public const string AnnotationFileExtension = "annot";
        public const string FileNamePrefix = "Export_";

        //Export Layout
        public const double ExportHeight = 1080;
        public const double ExportWidth = 1920;
        public const double ExportCrossSectionBig = 1080;
        public const double ExportCrossSectionSmall = 677;
        public const double ExportCrossSectionImageBig = 965;
        public const double ExportCrossSectionImageSmall = 604;
        public const double ExportLongitudeWidth = 1354;
        public const double ExportLongitudeHeight = 403;
        public const double ExportLongitudeImageWidth = 1200;
        public const double ExportLongitudeImageHeight = 140;
        public const double ExportTextPartSize = 566;
        public const double ExportLongitudeIndicatorWidth = 12;
        public const double CalciumIndicatorExportSizeBig = 989;
        public const double CalciumIndicatorExportSize = 630;
        public const double CalciumThicknessIndicatorExportSizeBig = 1005;
        public const double CalciumThicknessIndicatorExportSize = 644;
        public const double CalciumThicknessIndicatorCenterExportBig = CalciumThicknessIndicatorExportSizeBig / 2;
        public const double CalciumThicknessIndicatorCenterExport = CalciumThicknessIndicatorExportSize / 2;
        public static readonly System.Windows.Point CalciumThicknessIndicatorPointCenterExportBig = new System.Windows.Point(CalciumThicknessIndicatorCenterExportBig, CalciumThicknessIndicatorCenterExportBig);
        public static readonly System.Windows.Point CalciumThicknessIndicatorPointCenterExport = new System.Windows.Point(CalciumThicknessIndicatorCenterExport, CalciumThicknessIndicatorCenterExport);

        //File Icon
        public const string FileIconDrive = "drive";
        public const string FileIconFolder = "folder";
        public const string FileIconFile = "file";

        //Pullback Length
        public const string PullbackLengthLong = "LONG";
        public const string PullbackLengthShort = "SHOR";

        //Not Selected
        public const string NotSelected = "Not Selected";
        public const string NotSelectedCode = "$000";

        //Preset
        public const int MaxPatientCasePresetName = 30;
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
        public const int BackgroundColor = 0x161616;
        public const int CardBackgroundColor = 0x232323;

        //Playback Interval (msec)
        public const double PlaybackInterval = 50;

        //Update Image Interval (msec)
        public const double UpdateImageInterval = 5;

        //Wait For Event Inverval (msec)
        public const double WaitForEventInterval = 20;

        //System File Root Path
        public const string SystemRootPath = "C:";

        //Data File Root Path (OCT raw files) - ※ Patient Folder (DataRootPath + Patient ID)
        public const string DataRootPath = SystemRootPath + "\\Raywatt\\DataSave";

        public const string ConfigPath = SystemRootPath + "\\Raywatt\\System\\config";

        //Log Folder
        public const string LogFolderPath = SystemRootPath + "\\Raywatt\\log\\octffr";
        public const string LogExtension = "*.log";


        //ML Model Folder
        public const string MlModelFolderPath = SystemRootPath + "\\Raywatt\\system\\3rdparty\\model";

        //Storage Limit (GB)
        public const int StorageLimit = 10;

        //Encrypt, Decrypt Public Key
        public const string PublicKey = "raywatt07_hwjckklls_298-87-01441";

        //Review - 2D - Cross Section
        public const double OCTImageSize = 1024;
        public const double CrossSectionSize = 620;
        public const double CrossSectionCenter = CrossSectionSize / 2;
        public const double CrossSectionRadius = CrossSectionCenter;
        public static readonly System.Windows.Point CrossSectionPointCenter = new System.Windows.Point(CrossSectionRadius, CrossSectionRadius);
        public static readonly System.Windows.Rect CrossSectionRect = new System.Windows.Rect(0, 0, CrossSectionSize, CrossSectionSize);
        public const double CalciumIndicatorSize = 645;
        public const double CalciumThicknessIndicatorSize = 681;
        public const double CalciumThicknessIndicatorCenter = CalciumThicknessIndicatorSize / 2;
        public static readonly System.Windows.Point CalciumThicknessIndicatorPointCenter = new System.Windows.Point(CalciumThicknessIndicatorCenter, CalciumThicknessIndicatorCenter);
        public const int CalciumIndicatorColor = 0x57FEEB;

        //Review - 2D - Longitude
        public const double LongitudeWidth = 1140;
        public const double LongitudeHeight = 105;
        public const double LongitudeImageClipHeight = LongitudeHeight + 10;
        public const double LongitudeScale = LongitudeWidth / 10;
        public const double LongitudeIndicatorWidth = 22;
        public const double LongitudeIndicatorHeight = 116;
        public const double SectionIndicatorWidth = 7.5;
        public const double SectionIndicatorMoveWidth = 28;
        public const double SectionIndicatorCenterWidth = 1;
        public const double SectionIndicatorMoveCenterWidth = SectionIndicatorMoveWidth / 2;
        public const double SectionValueWidth = 24;
        public const double SectionValueCenterWidth = 0.25;
        public const double LumenProfileExtraHeight = 6;
        public const double PreLesionLengthInitValue = 15;
        public const double PostLesionLengthInitValue = 4;

        //Review - 2D - Side Menu
        public const string LeftUpMenu = "LeftUpMenu";
        public const string LeftDownMenu = "LeftDownMenu";
        public const string RightMenu = "RightMenu";
        public const double SideBarCollapseSize = 70;
        public const double SideBarExpandSize = 240;

        //Review - 2D - Longitude
        public const string LongitudeProfile = "Profile";
        public const string LongitudeLMode = "LMode";

        //Review - AI FFR
        public const double LongitudeFfrWidth = 1080;
        public const double LongitudeFfrHeight = 90;
        public const double LongitudeFfrImageClipHeight = LongitudeFfrHeight + 10;
        public const string FfrStep1 = "FfrStep1";
        public const string FfrStep2 = "FfrStep2";
        public const string FfrStep3 = "FfrStep3";
        public const string FfrStep4 = "FfrStep4";
        public const string FfrStep5 = "FfrStep5";
        public const double CrossSectionFfrSize = 344;

        //Measurement
        public const string MeasureDrawAll = "DrawAll";  //Draw All
        public const string MeasureDeleteAll = "DeleteAll";  //Delete All
        public const string MeasureAddArea = "AddArea";  //Add Area
        public const string MeasureAddLength = "AddLength";  //Add Length
        public const string MeasureAddAngle = "AddAngle";  //Add Length
        public const string MeasureAddText = "AddText";  //Add Text
        public const string MeasureErasePoint = "ErasePoint";  //Erase Point
        public const string MeasureDeleteArea = "GridDeleteArea";  //(Grid) Delete Area
        public const string MeasureDisableLength = "DisableLength";  //Disable Length
        public const string MeasureDisableAngle = "DisableAngle";  //Disable Length
        public const string MeasureDisableText = "DisableText";  //Disable Text
        public const string MeasureDisableErase = "DisableErase";  //Disable Erase
        public const string MeasureZoomIn = "ZoomIn";  //ZoomIn
        public const string MeasureZoomOut = "ZoomOut";  //ZoomOut
        public const string MeasureReDraw = "MeasureReDraw";    //MeasureReDraw

        //Draw Annotation
        public static readonly Brush[] AnnotationBrushes = {
            new SolidColorBrush(Color.FromRgb(0x57, 0xFE, 0xEB)),
            new SolidColorBrush(Color.FromRgb(0xFF, 0xD8, 0x00)),
            new SolidColorBrush(Color.FromRgb(0xFF, 0x7D, 0x77))
        };
        public static readonly DashStyle AnnotationDashLarge = new DashStyle(new double[] { 7, 7 }, 0);
        public static readonly DashStyle AnnotationDashSmall = new DashStyle(new double[] { 2, 5 }, 0);
        public const double AnnotationTextPointSize = 6;
        public const double AnnotationRectWidth = 6;
        public const double AnnotationRectHeight = 6;
        public const double AnnotationStrokeThickness = 1;
        public const double AnnotationStrokeThicknessBold = 3;
        public const double AnnotationScale = 1.2;

        //Catheter Status
        public const string CatheterStatusConnected = "Connected";//Catheter 연결되고, Micro Limit Switch가 On 상태
        public const string CatheterStatusLoading = "Loading";
        public const string CatheterStatusLoaded = "Loaded";
        public const string CatheterStatusEnable = "Enable";
        public const string CatheterStatusFailed = "Failed";
        public const string CatheterStatusUnloading = "Unloading";
        public const string CatheterStatusDisconnected = "Disconnected";

        //Measurement Type
        public const int MeasureCmdDefault = 0;
        public const int MeasureCmdArea = 1;
        public const int MeasureCmdLength = 2;
        public const int MeasureCmdText = 3;
        public const int MeasureCmdErase = 4;
        public const int MeasureCmdAngle = 5;

        //Lumen Contour Command
        public const string LumenContourZoomIn = "ZoomIn";
        public const string LumenContourZoomOut = "ZoomOut";
        public const string LumenContourRestore = "Restore";
        public const string LumenContourReset = "Reset";
        public const string LumenContourAutoDetect = "AutoDetect";
        public const string LumenContourDraw = "Draw";
        public const string LumenContourClear = "Clear";
        public const string LumenContourCurrentInit = "Init";

        //Scale
        public const string ScaleLength = "Length";
        public const string ScaleArea = "Area";

        //Field of View
        public const double DefaultFoV = 10.0f;
        public const double MaximumFoV = 7.0;
        public const double MinimumFoV = 5.0;

        //Image Resolution
        public const double ImageResolution = DefaultFoV / 1024;

        //Manual Calibration
        public const int ZOffsetLimit = 100;

        //Mini-Map
        public const double MiniMapBorderSize = 140;
        public const double MiniMapCanvasSize = 138;
        public const double MiniMapRadius = MiniMapCanvasSize / 2;
        public static readonly System.Windows.Point MiniMapPointCenter = new System.Windows.Point(MiniMapRadius, MiniMapRadius);

        //Zoom
        public const double ZoomScaleDefault = CrossSectionSize / OCTImageSize;
        public const double ZoomScaleMax = ZoomScaleDefault * 2;
    }
}