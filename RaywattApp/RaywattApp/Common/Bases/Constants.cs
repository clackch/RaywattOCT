using RaywattApp.Common.Angio;
using System;
using System.Windows.Media;

namespace RaywattApp.Common.Bases
{
    public class Constants
    {
        //MainWindow for Cursor
        public static System.Windows.Window mainWindow = System.Windows.Application.Current.MainWindow;

        //Current Page
        public static string CurrentPage = "";

        //[Page List]
        //Outset
        public const string OutsetLoadingPage = "Views/OutsetLoadingPage.xaml";
        //Patient
        public const string PatientListPage = "Views/PatientListPage.xaml";
        public const string PatientDetailPage = "Views/PatientDetailPage.xaml";
        public const string PatientNewPage = "Views/PatientNewPage.xaml";
        public const string PatientEditPage = "Views/PatientEditPage.xaml";
        //Recording
        public const string RecordingSetupPage = "Views/RecordingSetupPage.xaml";
        public const string RecordingLiveViewPage = "Views/RecordingLiveViewPage.xaml";
        public const string RecordingCalibrationPage = "Views/RecordingCalibrationPage.xaml";
        public const string RecordingPage = "Views/RecordingPage.xaml";
        public const string RecordingConfirmPage = "Views/RecordingConfirmPage.xaml";
        public const string RecordingCatheterFailPage = "Views/RecordingCatheterFailPage.xaml";
        //Review
        public const string ReviewPage = "Views/ReviewPage.xaml";
        public const string Review3dPage = "Views/Review3dPage.xaml";
        public const string ReviewComparePage = "Views/ReviewComparePage.xaml";
        public const string ReviewFfrPage = "Views/ReviewFfrPage.xaml";
        public const string ReviewPresetPage = "Views/ReviewPresetPage.xaml";
        public const string ReviewAngioCoRegPage = "Views/ReviewAngioCoRegPage.xaml";
        public const string ReviewLumenEditPage = "Views/ReviewLumenEditPage.xaml";
        public const string ReviewCalibrationPage = "Views/ReviewCalibrationPage.xaml";
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
        public const string SettingServicePage = "Views/Setting/SettingServicePage.xaml";
        public const string SettingLogPage = "Views/Setting/SettingLogPage.xaml";
        public const string SettingTermsConditionsPage = "Views/Setting/SettingTermsConditionsPage.xaml";

        //Resolution
        public const double ApplicationWidth = 1280;
        public const double ApplicationHeight = 1024;
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
        public const double SettingInsideDialogHeight = 583;
        public const double SettingEditPhysicianDialogWidth = 472;
        public const double SettingEditPhysicianDialogHeight = 279;
        public const double PatientCaseEditDialogWidth = 860;
        public const double PatientCaseEditDialogHeight = 734;
        public const double TermsConditionsDialogWidth = 860;
        public const double TermsConditionsDialogHeight = 660;
        public const double PowerOffDialogWidth = 472;
        public const double PowerOffDialogHeight = 269;
        public const double CathRoomDialogHeight = 472;
        public const double CathRoomDialogWidth = 472;

        //Max Length
        public const int MaxPatientId = 9;
        public const int MaxPatientLastname = 20;
        public const int MaxPatientFirstname = 20;
        public const int MaxPatientCaseAccessionNumber = 6;
        public const int MaxPatientCaseComment = 200;
        public const int MaxPhysicianName = 40;
        public const int MaxVolumeLabel = 15;

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

        //Export Status
        public const string ExportStatusCopyFile = "Copy file(s)";
        public const string ExportStatusConvertImage = "Convert Image(s)";
        public const string ExportStatusMakeImage = "Make Image(s)";
        public const string ExportStatusSaveVideo = "Save Video";
        public const string ExportStatusSaveMultipleFrames = "Save Multiple Frames";
        public const string ExportStatusSaveMultipleFiles = "Save Multiple Image Files";
        public const string ExportStatusSaveFile = "Save File";
        public const string ExportStatusCompleted = "Completed";

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
        
        //Export DICOM Prefic
        public const string ExportDicomPrefix = "IMG";

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
        public const double ExportAngioSize = 677;
        public const double ExportTextPartSize = 566;
        public const double ExportLongitudeIndicatorWidth = 12;
        public const double CalciumIndicatorExportSizeBig = 989;
        public const double CalciumIndicatorExportSize = 630;
        public const double CalciumThicknessIndicatorExportSizeBig = 1005;
        public const double CalciumThicknessIndicatorExportSize = 644;
        public const double CalciumThicknessIndicatorCenterExportBig = CalciumThicknessIndicatorExportSizeBig / 2;
        public const double CalciumThicknessIndicatorCenterExport = CalciumThicknessIndicatorExportSize / 2;
        public static System.Windows.Point CalciumThicknessIndicatorPointCenterExportBig = new System.Windows.Point(CalciumThicknessIndicatorCenterExportBig, CalciumThicknessIndicatorCenterExportBig);
        public static System.Windows.Point CalciumThicknessIndicatorPointCenterExport = new System.Windows.Point(CalciumThicknessIndicatorCenterExport, CalciumThicknessIndicatorCenterExport);

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
        public const int CompareBackgroundColor = 0x0d0d0d;

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

        //Log Folder
        public const string LogFolderPath = SystemRootPath + "\\Raywatt\\log";
        public const string LogExtension = "*.log";

        //FrameGrabber Folder
        public const string FGFolderPath = SystemRootPath + "\\Raywatt\\FrameGrabber";

        //Encrypt, Decrypt Public Key
        public const string PublicKey = "raywatt07_hwjckklls_298-87-01441";

        //Review - 2D - Cross Section
        public const double OCTImageSize = 1024;
        public const double CrossSectionSize = 620;
        public const double CrossSectionCenter = CrossSectionSize / 2;
        public const double CalciumIndicatorSize = 645;
        public const double CalciumIndicatorAngioSize = 490;
        public const double CalciumThicknessIndicatorSize = 681;
        public const double CalciumThicknessIndicatorAngioSize = 510;
        public const double CalciumThicknessIndicatorCenter = CalciumThicknessIndicatorSize / 2;
        public const double CalciumThicknessIndicatorCenterAngio = CalciumThicknessIndicatorAngioSize / 2;
        public static System.Windows.Point CalciumThicknessIndicatorPointCenter = new System.Windows.Point(CalciumThicknessIndicatorCenter, CalciumThicknessIndicatorCenter);
        public static System.Windows.Point CalciumThicknessIndicatorPointCenterAngio = new System.Windows.Point(CalciumThicknessIndicatorCenterAngio, CalciumThicknessIndicatorCenterAngio);
        public const int CalciumIndicatorColor = 0x57FEEB;

        //Review - 2D - Angio
        public const double AngioSize = 580;
        public const double CrossSectionAngio = 470;
        public const double CrossSectionAngioCenter = CrossSectionAngio / 2;
        public const double CrossSectionAngioScale = CrossSectionAngio / CrossSectionSize;
        public const double CoRegZoomAngioSize = 382;
        public const double CoRegZoomScale = 5;

        //Review - 2D - Longitude
        public const double LongitudeWidth = 1140;
        public const double LongitudeHeight = 105;
        public const double LongitudeImageClipHeight = LongitudeHeight + 10;
        public const double LongitudeScale = LongitudeWidth / 10;
        public const double LongitudeIndicatorWidth = 22;
        public const double LongitudeIndicatorHeight = 116;
        public const double SectionIndicatorWidth = 7.5;
        public const double SectionIndicatorCenterWidth = 1;
        public const double SectionValueWidth = 24;
        public const double SectionValueCenterWidth = 0.25;
        public const double LumenProfileExtraHeight = 6;

        //Review - 2D - Side Menu
        public const string LeftUpMenu = "LeftUpMenu";
        public const string LeftDownMenu = "LeftDownMenu";
        public const string RightMenu = "RightMenu";
        public const double SideBarCollapseSize = 70;
        public const double SideBarExpandSize = 240;

        //Review - 2D - Longitude
        public const string LongitudeProfile = "Profile";
        public const string LongitudeLMode = "LMode";

        //Review - 3D - Cut View
        public const double CutView3dX = 98;
        public const double CutView3dY = 120;
        public const double CutView3dWidth = 736;
        public const double CutView3dHeight = 560;

        //Review - 3D - Fly Through View
        public const double FlyThroughView3dX = 886;
        public const double FlyThroughView3dY = 216;
        public const double FlyThroughView3dWidth = 368;
        public const double FlyThroughView3dHeight = 368;

        //Review - 3D - Cross Section
        public const double CrossSection3dSize = 180;
        public const double CrossSection3dCenter = CrossSection3dSize / 2;

        //Review - 3D - Longitude
        public const double Longitude3dWidth = 868;
        public const double Longitude3dHeight = 150;
        public const double Longitude3dImageClipHeight = Longitude3dHeight + 10;
        public const double Longitude3dScale = Longitude3dWidth / 10;
        public const double LongitudeIndicator3dHeight = 161;

        //Review - 3D - Side Menu
        public const double LeftSideBarExpand3dSize = 329;
        public const double ViewMenu3dY = 70;
        public const double PatientMenu3dY = 409;

        //Review - Compare - Cross Section
        public const double CrossSectionCompareSize = 278;
        public const double CrossSectionCompareCenter = CrossSectionCompareSize / 2;

        //Review - Compare - Longitude
        public const double LongitudeCompareWidth = 780;
        public const double LongitudeCompareHeight = 170;
        public const double LongitudeCompareScale = (LongitudeCompareWidth - 1) / 10;
        public const double LongitudeIndicatorCompareHeight = 177;
        public const double LumenProfileExtraCompareHeight = 6;

        //Review - Compare - Side Menu
        public const double SelectPreCaseExpandSize = 460;

        //Measurement
        public const string MeasureDrawAll = "DrawAll";  //Draw All
        public const string MeasureDeleteAll = "DeleteAll";  //Delete All
        public const string MeasureAddArea = "AddArea";  //Add Area
        public const string MeasureAddLength = "AddLength";  //Add Length
        public const string MeasureAddText = "AddText";  //Add Text
        public const string MeasureErasePoint = "ErasePoint";  //Erase Point
        public const string MeasureDeleteArea = "GridDeleteArea";  //(Grid) Delete Area
        public const string MeasureDisableLength = "DisableLength";  //Disable Length
        public const string MeasureDisableText = "DisableText";  //Disable Text
        public const string MeasureDisableErase = "DisableErase";  //Disable Erase
        public const string MeasureZoomIn = "ZoomIn";  //ZoomIn
        public const string MeasureZoomOut = "ZoomOut";  //ZoomOut

        //Draw Annotation
        public static Brush[] AnnotationBrushes = {
            new SolidColorBrush(Color.FromRgb(0xEE, 0x6C, 0x4F)),
            new SolidColorBrush(Color.FromRgb(0xF7, 0xC9, 0x61)),
            new SolidColorBrush(Color.FromRgb(0x47, 0x4F, 0xEB)),
            new SolidColorBrush(Color.FromRgb(0xE9, 0x8C, 0x33)),
            new SolidColorBrush(Color.FromRgb(0x56, 0xBD, 0x7C)),
            new SolidColorBrush(Color.FromRgb(0x21, 0x24, 0x83)),
            new SolidColorBrush(Color.FromRgb(0xF3, 0xAB, 0x97)),
            new SolidColorBrush(Color.FromRgb(0x3D, 0x85, 0x51)),
            new SolidColorBrush(Color.FromRgb(0xA9, 0xA9, 0xF9))
        };
        public static DashStyle AnnotationDashLarge = new DashStyle(new double[] { 7, 7 }, 0);
        public static DashStyle AnnotationDashSmall = new DashStyle(new double[] { 2, 5 }, 0);
        public const double AnnotationTextPointSize = 6;
        public const double AnnotationRectWidth = 6;
        public const double AnnotationRectHeight = 6;
        public const double AnnotationStrokeThickness = 1;
        public const double AnnotationStrokeThicknessBold = 3;
        public const double AnnotationScale = 1.2;

        //Catheter Status
        public const string CatheterStatusConnected = "Connected";
        public const string CatheterStatusLoading = "Loading";
        public const string CatheterStatusLoaded = "Loaded";
        public const string CatheterStatusFailed = "Failed";
        public const string CatheterStatusUnloading = "Unloading";
        public const string CatheterStatusDisconnected = "Disconnected";

        //Measurement Type
        public const int MeasureCmdDefault = 0;
        public const int MeasureCmdArea = 1;
        public const int MeasureCmdLength = 2;
        public const int MeasureCmdText = 3;
        public const int MeasureCmdErase = 4;

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
        public const double MillimeterPerPixel  = 0.0082;

        //Mini-Map
        public const double MiniMapBorderSize = 140;
        public const double MiniMapCanvasSize = 138;

        //Zoom
        public const double ZoomScaleDefault = CrossSectionSize / OCTImageSize;
        public const double ZoomScaleMax = ZoomScaleDefault * 2;

        //Recording
        public const int TransientTime = 1000;
        public const int StartTime = 15;
        public const double AngioWidth = 420;
        public const double AngioHeight = 420;

        //AngioManager
        public const string ServerIP = "127.0.0.1";
        public const int ServerPort = 8888;
        public const byte SOF = 0x3A;
        public const byte EOF = 0xA3;
        public const int ImageHeaderSize = 7;
        public const int ImageTailSize = 2;
        public const int CommandPacketSize = 5;
        public const int DeviceInfoPacketSize = 10;

        //AngioCoRegistration
        public const double ellipsePathWidth = 2;
        public const double ellipsePathHeight = 2;
        public const double ellipseTrackWidth = 6;
        public const double ellipseTrackHeight = 6;
    }
}