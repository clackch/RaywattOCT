using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace RayCoreWrapper
{
    public class RayExportWrapper
    {
        //DICOM
        public enum DicomRWError : int
        {
            Normal = 0,
            OutPlugNull = -1,
            I2dNull = -2,
            FailInsertPixelData_DifferentColsOrRows = -3,
            DatasetNull = -4,
            SequenceNull = -5,
            SaveFileFail = -6,
            FailInsertFirstPixelData = -7,
            MetaInfoNull = -8,
            DicomImageNotExist = -9,
            DcmItemNull = -10,
            PixelDataNotExist = -11,
            LoadFileFail = -12,
            FailImageFinish = -13,
            FailAddProperty = -14,
            FailDeallocMemory = -15
        }

        public enum DICOMDIRRWError : int
        {
            Normal = 0,
            DataDictionaryNotLoaded = -1,
            DirectoryNotExist = -2,
            DICOMDIRNotCreated = -3,
            OtherError = -4,
            CannotAddDICOMDIR = -5,
            NoInputFiles = -6,
            DICOMDIRLoadFail = -7,
            DICOMDIRRecodeLoadFail = -8
        }

        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomStart();
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomImageStart(int numberOfFrames);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomAddImage(int width, int height, IntPtr pixelData);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomImageFinish();
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomStartProperty();
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomAddProperty(int tag, string value, int bufLen = 0);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomStartSequenceProperty(int numberOfItems);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomAddSequenceProperty(int itemnum, int tag, string value);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomSave(string path);
        [DllImport("makedcmDLL.dll")]
        public static extern DICOMDIRRWError DICOMDIRInputFolder(string path);
        [DllImport("makedcmDLL.dll")]
        public static extern DICOMDIRRWError DICOMDIRInputFile(string path);
        [DllImport("makedcmDLL.dll")]
        public static extern DICOMDIRRWError DICOMDIRWrite();

        //CD
        public enum CDBurnError : int
        {
            NotSupportDisc = -1,
            DiscNotEmpty = -2,
            TypeNotMatchRW = -3,
            DiscNotExist = -4,
            InitDeviceFuncNeed = -5,
            OK = 0,
            InvalidPointer = 1,
            OutOfMemory = 2,
            NotKnowFail = 3,
            InvalidArg = 4,
            ImapiRecorderRequired = 5,
            ImapiRecorderCommandTimeout = 6,
            ImapiRecorderInvalidResponseFromDevice = 7,
            ImapiRecorderMediaUpsideDown = 8,
            ImapiRecorderMediaBecomingReady = 9,
            ImapiRecorderMediaNoMedia = 10,
            ImapiRecorderMediaFormatInProgress = 11,
            ImapiRecorderMediaBusy = 12,
            ImapiLossOfStreaming = 13,
            ImapiRecorderMediaIncompatible = 14,
            ImapiRecorderDvdStructureNotPresent = 15,
            ImapiRecorderNoSuchModePage = 16,
            ImapiRecorderInvalidModeParameters = 17,
            ImapiRecorderMediaWriteProtected = 18,
            ImapiRecorderMediaSpeedMismatch = 19,
            HResultFromWin32 = 20,
            ImapiRecorderLocked = 21,
            ImapiDf2DataClientNameIsNotValid = 22,
            ImapiDf2DataWriteInProgress = 23,
            ImapiImageTooBig = 24,
            ImapiNoSupportedFileSystem = 25,
            ImapiInvalidParam = 26,
            ImapiTooManyDirs = 27,
            ImapiFileSystemChangeNotAllowed = 28,
            ImapiIso9660Levels = 29,
            ImapiIncompatiblePreviousSession = 30,
            ImapiDf2DataRecorderNotSupported = 31,
            RegdbClassNotReg = 32,
            ClassNoAggregation = 33,
            NoInterface = 34,
            ImapiEraseMediaIsNotSupported = 35,
            ImapiUnexpectedResponseFromDevice = 36,
            ImapiDf2DataStreamNotSupported = 37,
            ImapiOtherError = 10000
        };

        public enum MediaType : int
        {
            InitDeviceFuncNeed = -5,
            NotSupportDisc = -1,
            TYPE_CDR = 0x1,
            TYPE_CDRW = 0x2,
            TYPE_DVDDASHR = 0x3,
            TYPE_DVDDASHRW = 0x4,
            TYPE_DVDPLUSR = 0x5,
            TYPE_DVDPLUSRW = 0x6,
            TYPE_BDR = 0x7,
            TYPE_BDRE = 0x8,
        };

        public delegate void FormatCallbackFunction(int percent);
        public delegate void WriteCallbackFunction(int percent, int status);

        [DllImport("CDBurndll.dll")]
        public static extern CDBurnError initDevice();
        [DllImport("CDBurndll.dll")]
        public static extern CDBurnError checkDiskOnDrive();
        [DllImport("CDBurndll.dll")]
        public static extern MediaType getDiskType();
        [DllImport("CDBurndll.dll")]
        public static extern long checkFreeBlock();
        [DllImport("CDBurndll.dll")]
        public static extern long checkTotalBlock();
        [DllImport("CDBurndll.dll")]
        public static extern CDBurnError discFormat(string formatAppName, bool fullFormat);
        [DllImport("CDBurndll.dll")]
        public static extern void registerFormatCallback(IntPtr cb);
        [DllImport("CDBurndll.dll")]
        public static extern CDBurnError burningCD(string path, bool eject, string volumeLabel, string writeAppName);
        [DllImport("CDBurndll.dll")]
        public static extern void registerWritingCallback(IntPtr cb);
        [DllImport("CDBurndll.dll")]
        public static extern CDBurnError releaseCD();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string lpFileName, FileAccess dwDesiredAccess, FileShare dwShareMode, uint lpSecurityAttributes, FileMode dwCreationDisposition, int flagsAndAttributes, uint hTemplateFile);
    }
}
